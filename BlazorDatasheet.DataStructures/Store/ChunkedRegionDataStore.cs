using BlazorDatasheet.DataStructures.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace BlazorDatasheet.DataStructures.Store
{
    public class ChunkedRegionDataStore<T> : RegionDataStore<T> where T : IEquatable<T>
    {
        private const int CHUNK_SIZE = 5000; // 5000행씩 처리
        private readonly Dictionary<int, RegionDataStore<T>> _chunks = new();

        public ChunkedRegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
            : base(minArea, expandWhenInsertAfter)
        {
        }

        public new RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
        {
            if (axis == Axis.Col)
            {
                // 열 삽입은 모든 청크에 영향
                return base.InsertRowColAt(index, count, axis);
            }

            // 행 삽입은 해당 청크만 처리
            var chunkIndex = index / CHUNK_SIZE;
            var localIndex = index % CHUNK_SIZE;

            if (!_chunks.ContainsKey(chunkIndex))
            {
                _chunks[chunkIndex] = new RegionDataStore<T>(MinArea, ExpandWhenInsertAfter);

                // 해당 청크의 데이터를 메인 store에서 이동
                MoveDataToChunk(chunkIndex);
            }

            // 청크 내에서만 처리
            return _chunks[chunkIndex].InsertRowColAt(localIndex, count, axis);
        }

        public new RegionRestoreData<T> RemoveRowColAt(int index, int count, Axis axis)
        {
            if (axis == Axis.Col)
                return base.RemoveRowColAt(index, count, axis);

            var startChunk = index / CHUNK_SIZE;
            var endChunk = (index + count - 1) / CHUNK_SIZE;
            var restore = new RegionRestoreData<T>();

            for (int i = startChunk; i <= endChunk; i++)
            {
                if (!_chunks.ContainsKey(i))
                    continue;

                var chunkStart = i * CHUNK_SIZE;
                var localStart = Math.Max(index, chunkStart) - chunkStart;
                var localEnd = Math.Min(index + count - 1, chunkStart + CHUNK_SIZE - 1) - chunkStart;
                var part = _chunks[i].RemoveRowColAt(localStart, localEnd - localStart + 1, axis);
                restore.Merge(part);
            }

            restore.Merge(base.RemoveRowColAt(index, count, axis));
            return restore;
        }

        private void MoveDataToChunk(int chunkIndex)
        {
            var startRow = chunkIndex * CHUNK_SIZE;
            var endRow = (chunkIndex + 1) * CHUNK_SIZE - 1;
            var chunkRegion = new RowRegion(startRow, endRow);

            var dataInChunk = GetDataRegions(chunkRegion).ToList();

            foreach (var data in dataInChunk)
            {
                Tree.Delete(data);

                // 로컬 좌표로 변환
                var localData = new DataRegion<T>(data.Data, data.Region.Clone());
                localData.Region.Shift(-startRow, 0);
                localData.UpdateEnvelope();

                _chunks[chunkIndex].Add(localData.Region, localData.Data);
            }
        }

        public new RegionRestoreData<T> Add(IRegion region, T data)
        {
            var startChunk = region.Top / CHUNK_SIZE;
            var endChunk = region.Bottom / CHUNK_SIZE;

            // 단일 청크에만 걸쳐 있는 경우 청크에 저장
            if (startChunk == endChunk)
            {
                if (!_chunks.ContainsKey(startChunk))
                    _chunks[startChunk] = new RegionDataStore<T>(MinArea, ExpandWhenInsertAfter);

                var localRegion = region.Clone();
                localRegion.Shift(-startChunk * CHUNK_SIZE, 0);
                return _chunks[startChunk].Add(localRegion, data);
            }

            // 두 개 이상의 청크에 걸치면 기본 저장소 사용
            return base.Add(region, data);
        }

        public new IEnumerable<T> GetData(int row, int col)
        {
            var chunkIndex = row / CHUNK_SIZE;
            var localRow = row % CHUNK_SIZE;

            if (_chunks.ContainsKey(chunkIndex))
            {
                return _chunks[chunkIndex].GetData(localRow, col);
            }

            return base.GetData(row, col);
        }

        public new IEnumerable<T> GetData(IRegion region)
        {
            return GetDataRegions(region).Select(r => r.Data);
        }

        public new bool Contains(int row, int col)
        {
            var chunkIndex = row / CHUNK_SIZE;
            var localRow = row % CHUNK_SIZE;

            if (_chunks.ContainsKey(chunkIndex) &&
                _chunks[chunkIndex].Contains(localRow, col))
                return true;

            return base.Contains(row, col);
        }

        public new IEnumerable<DataRegion<T>> GetDataRegions(int row, int col)
        {
            var chunkIndex = row / CHUNK_SIZE;
            var localRow = row % CHUNK_SIZE;

            IEnumerable<DataRegion<T>> chunkData = Enumerable.Empty<DataRegion<T>>();
            if (_chunks.ContainsKey(chunkIndex))
            {
                chunkData = _chunks[chunkIndex]
                    .GetDataRegions(localRow, col)
                    .Select(dr =>
                    {
                        var reg = dr.Region.Clone();
                        reg.Shift(chunkIndex * CHUNK_SIZE, 0);
                        return new DataRegion<T>(dr.Data, reg);
                    });
            }

            return chunkData.Concat(base.GetDataRegions(row, col));
        }

        public new IEnumerable<DataRegion<T>> GetDataRegions(IRegion region)
        {
            var startChunk = region.Top / CHUNK_SIZE;
            var endChunk = region.Bottom / CHUNK_SIZE;

            var results = new List<DataRegion<T>>();

            for (int i = startChunk; i <= endChunk; i++)
            {
                if (!_chunks.ContainsKey(i))
                    continue;

                var chunkStart = i * CHUNK_SIZE;
                var chunkEnd = (i + 1) * CHUNK_SIZE - 1;
                var chunkRegion = new RowRegion(chunkStart, chunkEnd);
                var intersection = region.GetIntersection(chunkRegion);
                if (intersection == null)
                    continue;

                intersection.Shift(-chunkStart, 0);
                var localData = _chunks[i]
                    .GetDataRegions(intersection)
                    .Select(dr =>
                    {
                        var reg = dr.Region.Clone();
                        reg.Shift(chunkStart, 0);
                        return new DataRegion<T>(dr.Data, reg);
                    });

                results.AddRange(localData);
            }

            results.AddRange(base.GetDataRegions(region));
            return results;
        }
    }
}

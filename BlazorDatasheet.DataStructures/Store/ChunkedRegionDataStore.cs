using BlazorDatasheet.DataStructures.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace BlazorDatasheet.DataStructures.Store
{
    public class ChunkedRegionDataStore<T> : RegionDataStore<T> where T : IEquatable<T>
    {
        private const int CHUNK_SIZE = 5000; // 5000행씩 처리
        private readonly Dictionary<int, RegionDataStore<T>> _chunks = new();
        private readonly OffsetManager _rowOffsets = new();
        private readonly OffsetManager _colOffsets = new();

        public ChunkedRegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
            : base(minArea, expandWhenInsertAfter)
        {
        }

        private int PhysicalRow(int logicalRow) => _rowOffsets.ToPhysical(logicalRow);
        private int PhysicalCol(int logicalCol) => _colOffsets.ToPhysical(logicalCol);
        private bool RowInserted(int row) => _rowOffsets.IsInserted(row);
        private bool ColInserted(int col) => _colOffsets.IsInserted(col);

        public new RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
        {
            if (axis == Axis.Col)
            {
                var phys = PhysicalCol(index);
                var ret = base.InsertRowColAt(phys, count, axis);
                _colOffsets.Insert(index, count);
                ret.Shifts = [new(axis, index, count, null)];
                return ret;
            }

            var physIndex = PhysicalRow(index);
            var chunkIndex = physIndex / CHUNK_SIZE;
            var localIndex = physIndex % CHUNK_SIZE;

            if (!_chunks.ContainsKey(chunkIndex))
            {
                _chunks[chunkIndex] = new RegionDataStore<T>(MinArea, ExpandWhenInsertAfter);
                MoveDataToChunk(chunkIndex);
            }

            var restore = new RegionRestoreData<T>();
            restore.Merge(_chunks[chunkIndex].InsertRowColAt(localIndex, count, axis));
            restore.Merge(base.InsertRowColAt(physIndex, count, axis));

            _rowOffsets.Insert(index, count);
            restore.Shifts = [new(axis, index, count, null)];
            return restore;
        }

        public new RegionRestoreData<T> RemoveRowColAt(int index, int count, Axis axis)
        {
            if (axis == Axis.Col)
            {
                var phys = PhysicalCol(index);
                var ret = base.RemoveRowColAt(phys, count, axis);
                _colOffsets.Remove(index, count);
                ret.Shifts = [new(axis, index, -count, null)];
                return ret;
            }

            var physStart = PhysicalRow(index);
            var physEnd = PhysicalRow(index + count - 1);

            var startChunk = physStart / CHUNK_SIZE;
            var endChunk = physEnd / CHUNK_SIZE;
            var restore = new RegionRestoreData<T>();

            for (int i = startChunk; i <= endChunk; i++)
            {
                if (!_chunks.ContainsKey(i))
                    continue;

                var chunkStart = i * CHUNK_SIZE;
                var localStart = Math.Max(physStart, chunkStart) - chunkStart;
                var localEnd = Math.Min(physEnd, chunkStart + CHUNK_SIZE - 1) - chunkStart;
                var part = _chunks[i].RemoveRowColAt(localStart, localEnd - localStart + 1, axis);
                restore.Merge(part);
            }

            restore.Merge(base.RemoveRowColAt(physStart, count, axis));

            _rowOffsets.Remove(index, count);
            restore.Shifts = [new(axis, index, -count, null)];
            return restore;
        }

        private void MoveDataToChunk(int chunkIndex)
        {
            var startRow = chunkIndex * CHUNK_SIZE;
            var endRow = (chunkIndex + 1) * CHUNK_SIZE - 1;
            var chunkRegion = new RowRegion(startRow, endRow);

            var dataInChunk = base.GetDataRegions(chunkRegion).ToList();

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
            var phys = new Region(
                PhysicalRow(region.Top), PhysicalRow(region.Bottom),
                PhysicalCol(region.Left), PhysicalCol(region.Right));

            var startChunk = phys.Top / CHUNK_SIZE;
            var endChunk = phys.Bottom / CHUNK_SIZE;

            // 단일 청크에만 걸쳐 있는 경우 청크에 저장
            if (startChunk == endChunk)
            {
                if (!_chunks.ContainsKey(startChunk))
                    _chunks[startChunk] = new RegionDataStore<T>(MinArea, ExpandWhenInsertAfter);

                var localRegion = phys.Clone();
                localRegion.Shift(-startChunk * CHUNK_SIZE, 0);
                return _chunks[startChunk].Add(localRegion, data);
            }

            // 두 개 이상의 청크에 걸치면 기본 저장소 사용
            return base.Add(phys, data);
        }

        public new IEnumerable<T> GetData(int row, int col)
        {
            if (RowInserted(row) || ColInserted(col))
                return Enumerable.Empty<T>();

            var physRow = PhysicalRow(row);
            var physCol = PhysicalCol(col);

            var chunkIndex = physRow / CHUNK_SIZE;
            var localRow = physRow % CHUNK_SIZE;

            if (_chunks.ContainsKey(chunkIndex))
                return _chunks[chunkIndex].GetData(localRow, physCol);

            return base.GetData(physRow, physCol);
        }

        public new IEnumerable<T> GetData(IRegion region)
        {
            return GetDataRegions(region).Select(r => r.Data);
        }

        public new bool Contains(int row, int col)
        {
            if (RowInserted(row) || ColInserted(col))
                return false;

            var physRow = PhysicalRow(row);
            var physCol = PhysicalCol(col);
            var chunkIndex = physRow / CHUNK_SIZE;
            var localRow = physRow % CHUNK_SIZE;

            if (_chunks.ContainsKey(chunkIndex) &&
                _chunks[chunkIndex].Contains(localRow, physCol))
                return true;

            return base.Contains(physRow, physCol);
        }

        public new IEnumerable<DataRegion<T>> GetDataRegions(int row, int col)
        {
            if (RowInserted(row) || ColInserted(col))
                return Enumerable.Empty<DataRegion<T>>();

            var physRow = PhysicalRow(row);
            var physCol = PhysicalCol(col);
            var chunkIndex = physRow / CHUNK_SIZE;
            var localRow = physRow % CHUNK_SIZE;

            IEnumerable<DataRegion<T>> chunkData = Enumerable.Empty<DataRegion<T>>();
            if (_chunks.ContainsKey(chunkIndex))
            {
                chunkData = _chunks[chunkIndex]
                    .GetDataRegions(localRow, physCol)
                    .Select(dr =>
                    {
                        var reg = dr.Region.Clone();
                        reg.Shift(chunkIndex * CHUNK_SIZE, 0);
                        return new DataRegion<T>(dr.Data, reg);
                    });
            }

            return chunkData.Concat(base.GetDataRegions(physRow, physCol));
        }

        public new IEnumerable<DataRegion<T>> GetDataRegions(IRegion region)
        {
            var phys = new Region(
                PhysicalRow(region.Top), PhysicalRow(region.Bottom),
                PhysicalCol(region.Left), PhysicalCol(region.Right));

            var startChunk = phys.Top / CHUNK_SIZE;
            var endChunk = phys.Bottom / CHUNK_SIZE;

            var results = new List<DataRegion<T>>();

            for (int i = startChunk; i <= endChunk; i++)
            {
                if (!_chunks.ContainsKey(i))
                    continue;

                var chunkStart = i * CHUNK_SIZE;
                var chunkEnd = (i + 1) * CHUNK_SIZE - 1;
                var chunkRegion = new RowRegion(chunkStart, chunkEnd);
                var intersection = phys.GetIntersection(chunkRegion);
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

            results.AddRange(base.GetDataRegions(phys));
            return results;
        }
    }
}

using BlazorDatasheet.DataStructures.Geometry;

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
    }
}

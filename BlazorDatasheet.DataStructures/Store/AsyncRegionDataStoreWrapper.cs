using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store
{
    public class AsyncRegionDataStoreWrapper<T> where T : IEquatable<T>
    {
        private readonly RegionDataStore<T> _originalStore;
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public AsyncRegionDataStoreWrapper(RegionDataStore<T> originalStore)
        {
            _originalStore = originalStore;
        }

        // 기존 메서드를 비동기로 래핑
        public async Task<RegionRestoreData<T>> InsertRowColAtAsync(
            int index,
            int count,
            Axis axis,
            IProgress<int> progress = null,
            CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                return await Task.Run(() =>
                {
                    progress?.Report(0);

                    // 기존 RegionDataStore 메서드 그대로 호출
                    var result = _originalStore.InsertRowColAt(index, count, axis);

                    progress?.Report(100);
                    return result;

                }, cancellationToken);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // 다른 메서드들도 동일하게 래핑
        public async Task<IEnumerable<T>> GetDataAsync(int row, int col)
        {
            return await Task.Run(() => _originalStore.GetData(row, col));
        }
    }
}

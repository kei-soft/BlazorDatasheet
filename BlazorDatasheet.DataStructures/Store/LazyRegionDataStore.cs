using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store
{
    public class LazyRegionDataStore<T> : RegionDataStore<T> where T : IEquatable<T>
    {
        private readonly Queue<PendingOperation> _pendingOperations = new();
        private readonly Timer _flushTimer;
        private readonly object _operationLock = new object();

        public LazyRegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
            : base(minArea, expandWhenInsertAfter)
        {
            // 100ms마다 대기 중인 작업들을 한번에 처리
            _flushTimer = new Timer(FlushPendingOperations, null, 100, 100);
        }

        public new RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
        {
            lock (_operationLock)
            {
                // 즉시 처리하지 않고 대기열에 추가
                _pendingOperations.Enqueue(new PendingOperation
                {
                    Type = OperationType.InsertRowCol,
                    Index = index,
                    Count = count,
                    Axis = axis,
                    Timestamp = DateTime.Now
                });

                // 긴급한 경우만 즉시 처리
                if (_pendingOperations.Count > 10)
                {
                    FlushPendingOperations(null);
                }
            }

            // 임시 반환값 (실제로는 비동기 처리됨)
            return new RegionRestoreData<T>();
        }

        private void FlushPendingOperations(object state)
        {
            List<PendingOperation> operations;

            lock (_operationLock)
            {
                if (_pendingOperations.Count == 0) return;

                operations = new List<PendingOperation>();
                while (_pendingOperations.Count > 0)
                {
                    operations.Add(_pendingOperations.Dequeue());
                }
            }

            // 작업들을 그룹화해서 한번에 처리
            var groupedOps = operations.GroupBy(op => new { op.Type, op.Axis });

            foreach (var group in groupedOps)
            {
                ProcessOperationGroup(group.ToList());
            }
        }

        private void ProcessOperationGroup(List<PendingOperation> operations)
        {
            // 여러 삽입 작업을 하나로 합쳐서 처리
            var totalCount = operations.Sum(op => op.Count);
            var firstIndex = operations.Min(op => op.Index);

            // 기존 base 메서드 호출
            base.InsertRowColAt(firstIndex, totalCount, operations.First().Axis);
        }

        private class PendingOperation
        {
            public OperationType Type { get; set; }
            public int Index { get; set; }
            public int Count { get; set; }
            public Axis Axis { get; set; }
            public DateTime Timestamp { get; set; }
        }

        private enum OperationType
        {
            InsertRowCol,
            RemoveRowCol,
            Clear
        }
    }
}

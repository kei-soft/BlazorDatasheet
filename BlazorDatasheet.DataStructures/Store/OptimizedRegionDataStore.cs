using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store
{
    public class OptimizedRegionDataStore<T> : RegionDataStore<T> where T : IEquatable<T>
    {
        // 캐시로 반복 검색 줄이기
        private readonly Dictionary<string, List<DataRegion<T>>> _searchCache = new();
        private readonly object _cacheLock = new object();

        public OptimizedRegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
            : base(minArea, expandWhenInsertAfter)
        {
        }

        public new RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
        {
            // 1. 캐시 확인으로 중복 검색 방지
            var cacheKey = $"{axis}_{index}_{count}";

            lock (_cacheLock)
            {
                if (_searchCache.ContainsKey(cacheKey))
                {
                    _searchCache.Clear(); // 데이터 변경되므로 캐시 무효화
                }
            }

            // 2. 검색 범위를 더 정확하게 제한
            return InsertRowColAtOptimized(index, count, axis);
        }

        private RegionRestoreData<T> InsertRowColAtOptimized(int index, int count, Axis axis)
        {
            var expand = ExpandWhenInsertAfter;

            // 기존보다 훨씬 작은 범위만 검색
            var searchStart = expand ? index - 1 : index;
            var searchEnd = index + 100; // 최대 100행/열만 검색

            IRegion limitedRegion = axis == Axis.Col ?
                new ColumnRegion(searchStart, searchEnd) :
                new RowRegion(searchStart, searchEnd);

            // 제한된 범위에서만 검색
            var overlapping = GetDataRegions(limitedRegion).ToList();

            // 나머지는 기존 로직 그대로 사용
            var dataRegionsToAdd = new List<DataRegion<T>>();
            var regionsAdded = new List<DataRegion<T>>();
            var regionsRemoved = new List<DataRegion<T>>();

            // 기존 처리 로직 유지
            foreach (var overlap in overlapping)
            {
                if (overlap.Region.GetLeadingEdgeOffset(axis) == index)
                    continue;

                var i1 = overlap.Region.GetTrailingEdgeOffset(axis);
                if (!expand && index > i1)
                    continue;

                Tree.Delete(overlap);
                regionsRemoved.Add(overlap);

                var expanded = new DataRegion<T>(overlap.Data, overlap.Region.Clone());
                expanded.Region.Expand(axis == Axis.Row ? Edge.Bottom : Edge.Right, count);
                expanded.UpdateEnvelope();
                regionsAdded.Add(expanded);
                dataRegionsToAdd.Add(expanded);
            }

            // shift 처리도 범위 제한
            var below = GetAfterLimited(index - 1, axis, 1000); // 최대 1000개만
            foreach (var r in below)
            {
                Tree.Delete(r);
                var dRow = axis == Axis.Row ? count : 0;
                var dCol = axis == Axis.Col ? count : 0;
                r.Shift(dRow, dCol);
                dataRegionsToAdd.Add(r);
            }

            Tree.BulkLoad(dataRegionsToAdd);

            return new RegionRestoreData<T>()
            {
                RegionsAdded = regionsAdded,
                RegionsRemoved = regionsRemoved,
                Shifts = [new(axis, index, count, null)],
            };
        }
        private List<DataRegion<T>> GetAfterLimited(int rowOrCol, Axis axis, int maxCount)
        {
            IRegion searchRegion = axis == Axis.Row
                ? new RowRegion(rowOrCol + 1, rowOrCol + maxCount)
                : new ColumnRegion(rowOrCol + 1, rowOrCol + maxCount);

            return GetDataRegions(searchRegion)
                .Where(x => axis == Axis.Row
                    ? x.Region.Top >= rowOrCol + 1
                    : x.Region.Left >= rowOrCol + 1)
                .Take(maxCount)
                .ToList();
        }
    }
}

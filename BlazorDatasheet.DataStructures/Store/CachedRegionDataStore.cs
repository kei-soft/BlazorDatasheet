using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store
{
    public class CachedRegionDataStore<T> : RegionDataStore<T> where T : IEquatable<T>
    {
        private readonly LRUCache<string, RegionRestoreData<T>> _operationCache;
        private readonly Dictionary<string, List<DataRegion<T>>> _regionCache;

        public CachedRegionDataStore(int minArea = 0, bool expandWhenInsertAfter = true)
            : base(minArea, expandWhenInsertAfter)
        {
            _operationCache = new LRUCache<string, RegionRestoreData<T>>(100);
            _regionCache = new Dictionary<string, List<DataRegion<T>>>();
        }

        public new RegionRestoreData<T> InsertRowColAt(int index, int count, Axis axis)
        {
            var cacheKey = $"insert_{axis}_{index}_{count}";

            // 캐시 확인
            if (_operationCache.TryGet(cacheKey, out var cachedResult))
            {
                return cachedResult;
            }

            // 실제 처리
            var result = base.InsertRowColAt(index, count, axis);

            // 결과 캐싱
            _operationCache.Put(cacheKey, result);

            return result;
        }

        public new IEnumerable<DataRegion<T>> GetDataRegions(IRegion region)
        {
            var regionKey = $"{region.Top}_{region.Left}_{region.Bottom}_{region.Right}";

            if (_regionCache.TryGetValue(regionKey, out var cached))
            {
                return cached;
            }

            var result = base.GetDataRegions(region).ToList();
            _regionCache[regionKey] = result;

            return result;
        }
    }

    // LRU 캐시 구현
    public class LRUCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> _cache;
        private readonly LinkedList<KeyValuePair<TKey, TValue>> _list;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
            _list = new LinkedList<KeyValuePair<TKey, TValue>>();
        }

        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _list.Remove(node);
                _list.AddFirst(node);
                value = node.Value.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void Put(TKey key, TValue value)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                _list.Remove(existingNode);
                _cache.Remove(key);
            }
            else if (_cache.Count >= _capacity)
            {
                var lastNode = _list.Last;
                _list.RemoveLast();
                _cache.Remove(lastNode.Value.Key);
            }

            var newNode = new LinkedListNode<KeyValuePair<TKey, TValue>>(
                new KeyValuePair<TKey, TValue>(key, value));
            _list.AddFirst(newNode);
            _cache[key] = newNode;
        }
    }

}

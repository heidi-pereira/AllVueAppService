using System.Collections;

namespace BrandVue.SourceData.Utils
{
    internal class FixedSharedKeysDictionary<TKey, TIntermediate, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IReadOnlyDictionary<TKey, TIntermediate> _sharedKeyLookup;
        private readonly Func<TIntermediate, TValue> _getValue;

        public FixedSharedKeysDictionary(IReadOnlyDictionary<TKey, TIntermediate> sharedKeyLookup, Func<TIntermediate, TValue> getValue)
        {
            _sharedKeyLookup = sharedKeyLookup;
            _getValue = getValue;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _sharedKeyLookup.Select(kvp =>
            new KeyValuePair<TKey, TValue>(kvp.Key, _getValue(kvp.Value))).GetEnumerator();


        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => _sharedKeyLookup.Count;
        public bool ContainsKey(TKey key) => _sharedKeyLookup.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_sharedKeyLookup.TryGetValue(key, out var index))
            {
                value = default;
                return false;
            }

            value = _getValue(index);
            return true;
        }

        public TValue this[TKey key] => _getValue(_sharedKeyLookup[key]);

        public IEnumerable<TKey> Keys => _sharedKeyLookup.Keys;
        public IEnumerable<TValue> Values => _sharedKeyLookup.Values.Select(i => _getValue(i));
    }
}
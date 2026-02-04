using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Respondents
{
    /// <summary>
    /// Here purely as an optimization for long term and transient memory usage. The alloc one is more important (because we frequently access profile fields), so if we avoid enumerator allocation a different way we could probably do without this.
    /// ALLOC: Saves allocating an array/enumerator when someone wants to iterate over "all"
    /// MEM: Avoids storing a ~80 byte dictionary for the sake of one entry
    /// </summary>
    class ZeroEntityResponseForField : IResponsesForField
    {
        private int _value;

        public void Add(EntityIds entityIds, int value)
        {
            if (entityIds != default)
            {
                throw new ArgumentOutOfRangeException(nameof(entityIds), entityIds, "Entity ids should be empty");
            }

            _value = value;
        }

        public Memory<T> WriteWhere<T>(Func<KeyValuePair<EntityIds, int>, bool> predicate,
            Func<KeyValuePair<EntityIds, int>, T> select, IManagedMemoryPool<T> buffer)
        {
            if (predicate(default))
            {
                var mem = buffer.Rent(1);
                mem.Span[0] = select(new(default, _value));
                return mem;
            }
            return Memory<T>.Empty;
        }
        public bool TryGetValue(EntityIds entityValues, out int o)
        {
            if (entityValues == default)
            {
                o = _value;
                return true;
            }

            o = default;
            return false;
        }
    }
}
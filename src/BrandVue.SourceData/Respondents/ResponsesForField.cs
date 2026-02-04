using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Respondents
{
    internal class ResponsesForField : IResponsesForField
    {
        private readonly Dictionary<EntityIds, int> _fieldValues;

        public ResponsesForField()
        {
            _fieldValues = new Dictionary<EntityIds, int>();
        }

        public void Add(EntityIds entityIds, int value)
        {
            lock (_fieldValues)
            {
                _fieldValues[entityIds] = value;
            }
        }

        public bool TryGetValue(EntityIds entityValues, out int o)
        {
            lock (_fieldValues)
            {
                bool gotValue = _fieldValues.TryGetValue(entityValues, out int s);
                o = s;
                return gotValue;
            }
        }

        public Memory<T> WriteWhere<T>(Func<KeyValuePair<EntityIds, int>, bool> predicate,
            Func<KeyValuePair<EntityIds, int>, T> select, IManagedMemoryPool<T> buffer)
        {
            lock(_fieldValues)
            {
                int i = 0;
                int maxMemoryNeeded = _fieldValues.Count;
                var memory = buffer.Rent(maxMemoryNeeded);
                var span = memory.Span;
                foreach (var answer in _fieldValues)
                {
                    if (predicate(new(answer.Key, answer.Value)))
                    {
                        span[i++] = select(new(answer.Key, answer.Value));
                    }
                }
                return memory.Take(i);
            }
        }
    }
}
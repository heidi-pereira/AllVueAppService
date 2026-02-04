using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Respondents
{
    class ScaledResponsesForField : IResponsesForField
    {
        private readonly double _scaleFactor;
        private readonly IResponsesForField _responsesForField;

        public ScaledResponsesForField(double fieldScaleFactor, IResponsesForField responsesForField)
        {
            _scaleFactor = 1 / fieldScaleFactor;
            _responsesForField = responsesForField;
        }

        public void Add(EntityIds entityIds, int value) => _responsesForField.Add(entityIds, value);

        public Memory<T> WriteWhere<T>(Func<KeyValuePair<EntityIds, int>, bool> predicate,
            Func<KeyValuePair<EntityIds, int>, T> select, IManagedMemoryPool<T> buffer)
        {
            //SHOULD DO: Fix ALLOC of lambda here
            return _responsesForField.WriteWhere(predicate, kvp => select(new(kvp.Key, (int)Math.Round(_scaleFactor * kvp.Value))), buffer);
        }

        public bool TryGetValue(EntityIds entityValues, out int o)
        {
            if (_responsesForField.TryGetValue(entityValues, out int value))
            {
                o = (int)Math.Round(_scaleFactor * value);
                return true;
            }
            else
            {
                o = default;
                return false;
            }
        }
    }
}

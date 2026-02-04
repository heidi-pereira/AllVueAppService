using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Respondents
{
    internal interface IResponsesForField
    {
        void Add(EntityIds entityIds, int value);
        bool TryGetValue(EntityIds entityValues, out int o);

        Memory<T> WriteWhere<T>(Func<KeyValuePair<EntityIds, int>, bool> predicate,
            Func<KeyValuePair<EntityIds, int>, T> select, IManagedMemoryPool<T> buffer);
    }
}
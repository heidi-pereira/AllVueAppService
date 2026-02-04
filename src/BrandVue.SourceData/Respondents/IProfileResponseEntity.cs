using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Respondents
{
    public interface IProfileResponseEntity : IIdentifiable
    {
        /// <summary>
        /// If there's no response, returns null
        /// </summary>
        int? GetIntegerFieldValue(ResponseFieldDescriptor field, EntityValueCombination entityValues);
        /// <summary>
        /// ALLOC/PERF: Use IManagedMemoryPool to avoid per-respondent allocation
        /// </summary>
        Memory<T> GetIntegerFieldValues<T>(ResponseFieldDescriptor field,
            Func<KeyValuePair<EntityIds, int>, bool> predicate,
            Func<KeyValuePair<EntityIds, int>, T> select, IManagedMemoryPool<T> memoryPool);
        DateTimeOffset Timestamp { get; }
        int SurveyId { get; }
    }
}

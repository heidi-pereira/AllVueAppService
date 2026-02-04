namespace BrandVue.SourceData.Dates;

/// <summary>
/// All DateTimeOffsets inputs/outputs must/will be UTC with zero timestamp (see ToDateInstance).
/// These are used in high performance locations, so make assumptions about the inputs - pay attention to the argument names.
/// </summary>
internal interface IDateBatcher
{
    DateTimeOffset GetBatchEndContaining(DateTimeOffset startDate);
    DateTimeOffset GetBatchStartContaining(DateTimeOffset startDate);
    int GetBatchIndex(DateTimeOffset lastDayOfBatchZero, DateTimeOffset dayWithinBatchToFind);
}
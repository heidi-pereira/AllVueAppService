namespace BrandVue.SourceData.Dates;

internal static class DateBatcherExtensions
{
    public static int GetNumberOfBatchEndsWithin(this IDateBatcher dateBatcher, DateTimeOffset lastDayOfFirstBatch, DateTimeOffset endDate) =>
        dateBatcher.GetBatchIndex(lastDayOfFirstBatch, endDate) + 1;

    public static DateTimeOffset GetBatchEndStrictlyAfter(this IDateBatcher dateBatcher, DateTimeOffset anyDate) =>
        dateBatcher.GetBatchEndContaining(dateBatcher.GetBatchEndContaining(anyDate).AddDays(1));

    public static DateTimeOffset GetBatchEndOnOrBefore(this IDateBatcher dateBatcher, DateTimeOffset periodEndDate) =>
        dateBatcher.GetBatchEndContaining(periodEndDate) == periodEndDate ? periodEndDate : dateBatcher.GetBatchStartContaining(periodEndDate).AddDays(-1);
}
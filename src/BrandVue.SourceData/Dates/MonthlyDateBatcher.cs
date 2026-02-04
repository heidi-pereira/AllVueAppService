namespace BrandVue.SourceData.Dates;

internal class MonthlyDateBatcher : IDateBatcher
{
    public DateTimeOffset GetBatchEndContaining(DateTimeOffset startDate) => startDate.GetLastDayOfMonthOnOrAfter();
    public DateTimeOffset GetBatchStartContaining(DateTimeOffset startDate) => startDate.GetFirstDayOfMonth();
    public int GetBatchIndex(DateTimeOffset lastDayOfBatchZero, DateTimeOffset dayWithinBatchToFind) =>
        (dayWithinBatchToFind.Year - lastDayOfBatchZero.Year) * 12 + dayWithinBatchToFind.Month - lastDayOfBatchZero.Month;
}
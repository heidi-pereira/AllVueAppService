namespace BrandVue.Services.Llm.Discovery;
public static class DateRangeHelper
{
    public static (DateTime start, DateTime end) CreateDateRangeFromOptions(this DateTime endDate,
        DateRangeOptions navOptionRange)
    {
        return navOptionRange switch
        {
            DateRangeOptions.LastQuarter => (
                endDate.AddMonths(-3).StartOfQuarter(),
                endDate.AddMonths(-3).EndOfQuarter()),

            DateRangeOptions.ThisQuarter => (
                endDate.StartOfQuarter(),
                endDate.EndOfQuarter()),

            DateRangeOptions.ThisYear => (
                endDate.StartOfYear(),
                endDate.EndOfYear()),

            DateRangeOptions.LastYear => (
                endDate.AddMonths(-13).StartOfMonth(),
                endDate.AddMonths(-1).EndOfMonth()),

            DateRangeOptions.Last2Years => (
                endDate.AddMonths(-25).StartOfMonth(),
                endDate.AddMonths(-1).EndOfMonth()),

            DateRangeOptions.Last5Years => (
                endDate.AddMonths(-61).StartOfMonth(),
                endDate.AddMonths(-1).EndOfMonth()),

            DateRangeOptions.LastMonth => (
                endDate.AddMonths(-1).StartOfMonth(),
                endDate.AddMonths(-1).EndOfMonth()),

            DateRangeOptions.ThisMonth => (
                endDate.StartOfMonth(),
                endDate.EndOfMonth()),

            DateRangeOptions.Last6Months => (
                endDate.AddMonths(-6).StartOfMonth(),
                endDate.AddMonths(-1).EndOfMonth()),
            _ => throw new ArgumentOutOfRangeException(nameof(navOptionRange), navOptionRange, null)
        };
    }

    private static DateTime StartOfMonth(this DateTime date) =>
        new DateTime(date.Year, date.Month, 1);

    private static DateTime EndOfMonth(this DateTime date) =>
        date.StartOfMonth().AddMonths(1).AddDays(-1);

    private static DateTime StartOfQuarter(this DateTime date) =>
        new DateTime(date.Year, ((date.Month - 1) / 3) * 3 + 1, 1);

    private static DateTime EndOfQuarter(this DateTime date) =>
        date.StartOfQuarter().AddMonths(3).AddDays(-1);

    private static DateTime StartOfYear(this DateTime date) =>
        new DateTime(date.Year, 1, 1);

    private static DateTime EndOfYear(this DateTime date) =>
        new DateTime(date.Year, 12, 31);
}
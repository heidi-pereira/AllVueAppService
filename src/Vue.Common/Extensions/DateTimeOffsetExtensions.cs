namespace Vue.Common.Extensions
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset GetLastDayOfMonthOnOrPreceding(this DateTimeOffset offset)
        {
            var numberOfDaysInMonth = DateTime.DaysInMonth(offset.Year, offset.Month);
            return numberOfDaysInMonth == offset.Day
                ? offset
                : offset.AddDays(-offset.Day);
        }
    }
}

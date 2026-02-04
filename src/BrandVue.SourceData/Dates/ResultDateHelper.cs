using BrandVue.EntityFramework.MetaData.Averages;

namespace BrandVue.SourceData.Dates
{
    /// <summary>
    /// This class helps with date calculations depending on requested period,
    /// </summary>
    public static class ResultDateHelper
    {
        public static DateTimeOffset GetFirstDayOfPeriodForMonth(DateTimeOffset givenDate, MakeUpTo makeUpTo,
            int numberOfPeriods) =>
            makeUpTo switch
            {
                MakeUpTo.MonthEnd => givenDate.GetFirstDayOfMonth().AddMonths(1 - numberOfPeriods),
                MakeUpTo.QuarterEnd => givenDate.GetFirstDayOfQuarter(),
                MakeUpTo.HalfYearEnd => givenDate.GetFirstDayOfHalfYear(),
                MakeUpTo.CalendarYearEnd => givenDate.GetFirstDayOfYear(),
                _ => throw new ArgumentOutOfRangeException(nameof(makeUpTo), makeUpTo, null)
            };

        public static DateTimeOffset GetFirstDayOfPeriodForDay(DateTimeOffset givenDate, int numberOfPeriods) =>
            givenDate.AddDays(1 - numberOfPeriods);

        public static DateTimeOffset GetFirstAvailableDataDate(MakeUpTo makeUpTo, DateTimeOffset startDate) =>
            makeUpTo switch
            {
                MakeUpTo.MonthEnd => startDate.GetFirstDayOfMonth() == startDate
                    ? startDate
                    : startDate.AddMonths(1).GetFirstDayOfMonth(),
                MakeUpTo.QuarterEnd => startDate.GetFirstDayOfQuarter() == startDate
                    ? startDate.ToDateInstance()
                    : startDate.GetFirstDayOfQuarter().AddMonths(3).GetFirstDayOfQuarter().ToDateInstance(),
                MakeUpTo.HalfYearEnd => startDate.GetFirstDayOfHalfYear() == startDate
                    ? startDate.ToDateInstance()
                    : startDate.GetFirstDayOfHalfYear().AddMonths(6).GetFirstDayOfHalfYear().ToDateInstance(),
                MakeUpTo.CalendarYearEnd => startDate.GetFirstDayOfYear() == startDate
                    ? startDate.ToDateInstance()
                    : startDate.GetFirstDayOfYear().AddMonths(12).GetFirstDayOfYear().ToDateInstance(),
                _ => throw new ArgumentOutOfRangeException(nameof(makeUpTo), makeUpTo, null)
            };

        public static DateTimeOffset LastDayOfPeriodOnOrPreceding(DateTimeOffset periodEndDate, MakeUpTo makeUpTo) =>
            makeUpTo switch
            {
                MakeUpTo.Day => periodEndDate,
                MakeUpTo.WeekEnd => periodEndDate,
                MakeUpTo.MonthEnd => periodEndDate.GetLastDayOfMonthOnOrPreceding(),
                MakeUpTo.QuarterEnd => periodEndDate.GetLastDayOfQuarterOnOrPreceding(),
                MakeUpTo.HalfYearEnd => periodEndDate.GetLastDayOfHalfYearOnOrPreceding(),
                MakeUpTo.CalendarYearEnd => periodEndDate.Month == 12 && periodEndDate.Day == 31
                    ? periodEndDate
                    : periodEndDate.AddYears(-1).GetLastDayOfYear(),
                _ => throw new InvalidOperationException(
                    $"Average does not support MakeUpTo: {makeUpTo}")
            };
    }
}
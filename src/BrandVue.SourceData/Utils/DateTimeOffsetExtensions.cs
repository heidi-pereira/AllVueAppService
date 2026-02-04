using System.Data.SqlTypes;
using System.Globalization;

namespace BrandVue.SourceData
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset ToDateInstance(this DateTimeOffset offset)
        {
            return new DateTimeOffset(offset.Date, TimeSpan.Zero);
        }

        public static DateTimeOffset? ToDateInstance(this DateTimeOffset? offset)
        {
            return offset?.ToDateInstance();
        }

        public static DateTimeOffset GetFirstDayOfYear(this DateTimeOffset offset)
        {
            var firstDay = new DateTimeOffset(
                offset.Year,
                1,
                1,
                1,
                0,
                0,
                TimeSpan.Zero).ToDateInstance();
            return firstDay;
        }

        public static DateTimeOffset GetFirstDayOfMonth(this DateTimeOffset offset)
        {
            if (offset.Day == 1)
            {
                return offset;
            }

            var firstDay = new DateTimeOffset(
                offset.Year,
                offset.Month,
                1,
                1,
                0,
                0,
                TimeSpan.Zero).ToDateInstance();
            return firstDay;
        }

        /// <summary>
        /// N.B. I don't provide an overload that takes a nullable value since there is no
        /// sensible return value for a null DateTimeOffset from this method.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static DateTimeOffset GetLastDayOfMonthOnOrPreceding(this DateTimeOffset offset)
        {
            var numberOfDaysInMonth = DateTime.DaysInMonth(offset.Year, offset.Month);
            return numberOfDaysInMonth == offset.Day
                ? offset
                : offset.AddDays(-offset.Day);
        }

        /// <summary>
        /// N.B. I don't provide an overload that takes a nullable value since there is no
        /// sensible return value for a null DateTimeOffset from this method.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static DateTimeOffset GetLastDayOfMonthOnOrAfter(this DateTimeOffset offset)
        {
            var numberOfDaysInMonth = DateTime.DaysInMonth(offset.Year, offset.Month);
            return numberOfDaysInMonth == offset.Day
                ? offset
                : offset.AddDays(numberOfDaysInMonth - offset.Day).ToDateInstance();
        }

        public static bool IsLastDayOfMonth(this DateTimeOffset offset)
        {
            var numberOfDaysInMonth = DateTime.DaysInMonth(offset.Year, offset.Month);
            return offset.Day == numberOfDaysInMonth;
        }

        public static bool IsLastDayOfQuarter(this DateTimeOffset offset)
        {
            var lastDay = offset.GetLastDayOfQuarterOnOrAfter();
            return lastDay == offset;
        }

        public static bool IsLastDayOfHalfYear(this DateTimeOffset offset)
        {
            var lastDay = offset.GetLastDayOfHalfYearOnOrAfter();
            return lastDay == offset;
        }

        public static DateTimeOffset GetLastDayOfFirstMonthOfQuarter(this DateTimeOffset offset)
        {
            return offset.GetFirstDayOfQuarter().GetLastDayOfMonthOnOrAfter();
        }

        public static DateTimeOffset GetLastDayOfQuarterOnOrAfter(this DateTimeOffset offset)
        {
            return offset.GetLastDayOfQuarter();
        }

        public static DateTimeOffset GetLastDayOfFirstMonthOfHalfYear(this DateTimeOffset offset)
        {
            return offset.GetFirstDayOfHalfYear().GetLastDayOfHalfYearOnOrAfter();
        }

        public static DateTimeOffset GetLastDayOfTheFirstMonthOfYear(this DateTimeOffset offset)
        {
            return offset.GetFirstDayOfYear().GetLastDayOfMonthOnOrAfter();
        }

        public static DateTimeOffset GetLastDayOfNextMonth(this DateTimeOffset offset)
        {
            return offset
                .GetLastDayOfMonthOnOrAfter()
                .AddDays(1)
                .GetLastDayOfMonthOnOrAfter();
        }

        public static int GetQuarterNumber(this DateTimeOffset offset)
        {
            return (offset.Month - 1) / 3 + 1;
        }

        public static DateTimeOffset GetFirstDayOfQuarter(this DateTimeOffset offset)
        {
            var date = new DateTimeOffset(
                offset.Year,
                (offset.GetQuarterNumber() - 1) * 3 + 1,
                1,
                1,
                0,
                0,
                TimeSpan.Zero).ToDateInstance();
            return date;
        }

        public static int GetNumberOfDaysInMonth(this DateTimeOffset offset)
        {
            return DateTime.DaysInMonth(offset.Year, offset.Month);
        }

        public static DateTimeOffset GetLastDayOfQuarter(this DateTimeOffset offset)
        {
            return offset.GetFirstDayOfQuarter().AddMonths(3).AddDays(-1).ToDateInstance();
        }

        public static DateTimeOffset GetLastDayOfQuarterOnOrPreceding(this DateTimeOffset offset)
        {
            var lastDay = offset.GetLastDayOfQuarter();
            return lastDay == offset
                ? offset
                : offset.AddMonths(-3).GetLastDayOfQuarter();
        }

        public static int GetHalfYearNumber(this DateTimeOffset offset)
        {
            return (offset.Month - 1) / 6 + 1;
        }

        public static DateTimeOffset GetFirstDayOfHalfYear(this DateTimeOffset offset)
        {
            var date = new DateTimeOffset(
                offset.Year,
                (offset.GetHalfYearNumber() - 1) * 6 + 1,
                1,
                1,
                0,
                0,
                TimeSpan.Zero).ToDateInstance();
            return date;
        }

        public static DateTimeOffset GetLastDayOfHalfYear(this DateTimeOffset offset)
        {
            return offset.GetFirstDayOfHalfYear().AddMonths(6).AddDays(-1).ToDateInstance();
        }

        public static DateTimeOffset GetLastDayOfHalfYearOnOrPreceding(this DateTimeOffset offset)
        {
            var lastDay = offset.GetLastDayOfHalfYear();
            return lastDay == offset
                ? offset
                : offset.AddMonths(-6).GetLastDayOfHalfYear();
        }

        public static DateTimeOffset GetLastDayOfHalfYearOnOrAfter(this DateTimeOffset offset)
        {
            return offset.GetLastDayOfHalfYear();
        }

        public static DateTimeOffset GetLastDayOfPeriod(this DateTimeOffset offset, int numberOfMonths)
        {
            return offset.AddMonths(numberOfMonths).AddDays(-1);
        }

        public static DateTimeOffset GetLastDayOfYear(this DateTimeOffset offset)
        {
            return new DateTimeOffset(
                offset.Year,
                12,
                31,
                offset.Hour,
                offset.Minute,
                offset.Second,
                offset.Millisecond,
                offset.Offset).ToDateInstance();
        }

        public static DateTimeOffset GetLastDayOfYearOrAfter(this DateTimeOffset offset)
        {
            return offset.IsFirstDayOfTheYear()
                ? new DateTimeOffset(
                offset.Year,
                12,
                31,
                offset.Hour,
                offset.Minute,
                offset.Second,
                offset.Millisecond,
                offset.Offset).ToDateInstance()
                : new DateTimeOffset(
                offset.Year + 1,
                12,
                31,
                offset.Hour,
                offset.Minute,
                offset.Second,
                offset.Millisecond,
                offset.Offset).ToDateInstance();

        }

        public static bool IsFirstDayOfTheYear(this DateTimeOffset offset)
        {
            return offset.Day == 1 && offset.Month == 1;
        }

        public static DateTimeOffset ParseDate(string yyyyMMdd)
        {
            return ParseDate(yyyyMMdd, "yyyy/MM/dd");
        }

        public static DateTimeOffset ParseDate(string dateInput, string format)
        {
            return DateTimeOffset.ParseExact(
                dateInput,
                format,
                CultureInfo.InvariantCulture).ToDateInstance();
        }

        public static DateTimeOffset GetLastDayOfQuarterOrAfter(this DateTimeOffset startDate)
        {
            return startDate.GetFirstDayOfQuarter() == startDate
                ? startDate.GetLastDayOfQuarter()
                : startDate.AddMonths(3).GetLastDayOfQuarter();
        }

        public static DateTimeOffset GetLastDateOfHalfYearOrAfter(this DateTimeOffset startDate)
        {
            return startDate.GetFirstDayOfHalfYear() == startDate
                ? startDate.GetLastDayOfHalfYearOnOrAfter()
                : startDate.AddMonths(6).GetLastDayOfHalfYearOnOrAfter();
        }

        public static DateTimeOffset Min(this DateTimeOffset? possiblyNullDate, DateTimeOffset nonNullDate)
        {
            return possiblyNullDate.HasValue && possiblyNullDate < nonNullDate ? possiblyNullDate.Value : nonNullDate;
        }

        public static DateTimeOffset Max(this DateTimeOffset? possiblyNullDate, DateTimeOffset nonNullDate)
        {
            return possiblyNullDate.HasValue && possiblyNullDate > nonNullDate ? possiblyNullDate.Value : nonNullDate;
        }

        public static DateTimeOffset EndOfDay(this DateTimeOffset dateTimeOffset)
        {
            var startOfDay = new DateTimeOffset(dateTimeOffset.Year, dateTimeOffset.Month, dateTimeOffset.Day, 0, 0, 0, 0, TimeSpan.Zero);
            return startOfDay.AddDays(1).Subtract(TimeSpan.FromTicks(1));
        }

        public static DateTimeOffset EndOfPreviousDay(this DateTimeOffset lastPossibleDataPoint)
        {
            return lastPossibleDataPoint.AddTicks(1).ToDateInstance().AddTicks(-1);
        }

        public static IEnumerable<DateTimeOffset> SpanByDayTo(this DateTimeOffset startDate, DateTimeOffset endDate) =>
            Enumerable
                .Range(0, endDate.Subtract(startDate).Days + 1)
                .Select(oneDay => startDate.AddDays(oneDay));

        public static DateTimeOffset NormalizeSqlDateTime(this DateTimeOffset date)
        {
            var min = SqlDateTime.MinValue.Value.ToUtcDateOffset();
            var max = SqlDateTime.MaxValue.Value.ToUtcDateOffset();
            if (date < min)
            {
                return min;
            }
            if (date > max)
            {
                return max;
            }
            return date;
        }
    }
}

using BrandVue.EntityFramework.MetaData.Averages;
using System.Globalization;

namespace BrandVue.SourceData.Dates
{
    public static class ResultDateFormatter
    {
        public static string FormatDate(DateTimeOffset date, MakeUpTo makeUpTo)
        {
            switch (makeUpTo)
            {
                case MakeUpTo.WeekEnd:
                    return FormatWeekEnd(date);
                case MakeUpTo.MonthEnd:
                    return FormatMonthEnd(date);
                case MakeUpTo.QuarterEnd:
                    return FormatQuarterEnd(date);
                case MakeUpTo.HalfYearEnd:
                    return FormatHalfYearEnd(date);
                case MakeUpTo.CalendarYearEnd:
                    return FormatYearEnd(date);
                default:
                    return date.ToString("dd MMM", CultureInfo.InvariantCulture);
            }
        }

        private static string FormatWeekEnd(DateTimeOffset date)
        {
            // Calculate days to add to get to Sunday (end of week)
            int daysToSunday = (7 - (int)date.DayOfWeek) % 7;
            
            DateTimeOffset weekEnd = date.AddDays(daysToSunday).Date;
            return $"w/e {weekEnd.ToString("dd MMM", CultureInfo.InvariantCulture)}";
        }

        private static string FormatMonthEnd(DateTimeOffset date)
        {
            DateTimeOffset monthEnd = new DateTimeOffset(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month), 0, 0, 0, date.Offset);
            return monthEnd.ToString("MMM yy", CultureInfo.InvariantCulture);
        }

        private static string FormatQuarterEnd(DateTimeOffset date)
        {
            int startMonth = ((date.Month - 1) / 3) * 3 + 1;
            int endMonth = startMonth + 2;
            DateTimeOffset quarterEnd = new DateTimeOffset(date.Year, endMonth, DateTime.DaysInMonth(date.Year, endMonth), 0, 0, 0, date.Offset);
            return $"Q{((date.Month - 1) / 3) + 1} {quarterEnd.Year}";
        }

        private static string FormatHalfYearEnd(DateTimeOffset date)
        {
            int halfYearEndMonth = date.Month <= 6 ? 6 : 12;
            DateTimeOffset halfYearEnd = new DateTimeOffset(date.Year, halfYearEndMonth, DateTime.DaysInMonth(date.Year, halfYearEndMonth), 0, 0, 0, date.Offset);
            return $"{(halfYearEndMonth == 6 ? "1st" : "2nd")} half of {halfYearEnd.Year}";
        }

        private static string FormatYearEnd(DateTimeOffset date)
        {
            DateTimeOffset yearEnd = new DateTimeOffset(date.Year, 12, 31, 0, 0, 0, date.Offset);
            return yearEnd.ToString("yyyy");
        }
    }
}

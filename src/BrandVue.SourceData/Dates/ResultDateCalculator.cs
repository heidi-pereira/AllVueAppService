using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;

namespace BrandVue.SourceData.Dates
{
    /// <summary>
    /// This class calculates the date of first and last results in a range.
    ///
    /// Example: if we selected a year range from 01/01/2018 to 31/12/2018 and want to
    /// see averages quarterly then we will get 4 results by 3 months. And the date of
    /// first result will be 31/03/2018 (we need the end of the first quarter because
    /// it is the date when we actually can get the average result).
    ///
    /// Example: if we selected a year range from 01/01/2018 to 31/12/2018 and want to
    /// see averages yearly then we will get 1 results by 12 months. And the date of
    /// first result will be 31/12/2018 (we need the end of the year because
    /// it is the date when we actually can get the average result).
    /// </summary>
    public static class ResultDateCalculator
    {
        /// <summary>
        /// This will get a viable start date for the calculation taking into account both the
        /// requested period and, if defined, the measures start date, along with the requested
        /// average type.
        /// </summary>
        /// <param name="average"></param>
        /// <param name="measureSpecificStartDateTime"></param>
        /// <param name="beginningOfDataset"></param>
        /// <param name="requestedStartDate"></param>
        /// <returns>First result date</returns>
        public static DateTimeOffset GetFirst(
            AverageDescriptor average,
            DateTimeOffset? measureSpecificStartDateTime,
            DateTimeOffset beginningOfDataset,
            DateTimeOffset requestedStartDate)
        {
            var measureStartDate = beginningOfDataset;
            if (measureSpecificStartDateTime.HasValue && measureSpecificStartDateTime.Value > measureStartDate)
            {
                measureStartDate = measureSpecificStartDateTime.Value;
            }
            //we need this date in order to check if we have ALL data to calculate this point
            var firstCalculationDataStart = GetFirstDayOfPeriodForAverage(average, requestedStartDate);

            //check if we actually have data and change firstCalculationDataStart if not
            if (measureStartDate > firstCalculationDataStart)
            {
                firstCalculationDataStart = average.TotalisationPeriodUnit == TotalisationPeriodUnit.Month
                    ? ResultDateHelper.GetFirstAvailableDataDate(average.MakeUpTo, measureStartDate)
                    : measureStartDate;
            }

            return average.TotalisationPeriodUnit == TotalisationPeriodUnit.Month ? firstCalculationDataStart.GetLastDayOfMonthOnOrAfter() : firstCalculationDataStart;
        }

        public static DateTimeOffset GetFirstDayOfPeriodForAverage(AverageDescriptor average, DateTimeOffset requestedStartDate)
        {
            if (average.TotalisationPeriodUnit == TotalisationPeriodUnit.Month)
            {
                return ResultDateHelper.GetFirstDayOfPeriodForMonth(requestedStartDate, average.MakeUpTo, average.NumberOfPeriodsInAverage);
            }
            return average.MakeUpTo switch
            {
                MakeUpTo.Day => ResultDateHelper.GetFirstDayOfPeriodForDay(requestedStartDate, average.NumberOfPeriodsInAverage),
                MakeUpTo.WeekEnd => ResultDateHelper.GetFirstDayOfPeriodForDay(requestedStartDate, average.NumberOfPeriodsInAverage),
                MakeUpTo.MonthEnd => ResultDateHelper.GetFirstDayOfPeriodForMonth(requestedStartDate, average.MakeUpTo, average.NumberOfPeriodsInAverage),
                _ => throw new ArgumentOutOfRangeException(nameof(average.MakeUpTo), average.MakeUpTo, null)
            };
        }

        public static DateTimeOffset GetLast(DateTimeOffset periodEndDate, AverageDescriptor average)
        {
            if (average.TotalisationPeriodUnit == TotalisationPeriodUnit.Month)
            {
                return ResultDateHelper.LastDayOfPeriodOnOrPreceding(periodEndDate, average.MakeUpTo);
            }
            return average.MakeUpTo switch
            {
                MakeUpTo.Day => periodEndDate,
                MakeUpTo.WeekEnd => periodEndDate,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

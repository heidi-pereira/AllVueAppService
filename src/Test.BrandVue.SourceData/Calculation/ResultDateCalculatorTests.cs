using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CalculationPipeline;
using NUnit.Framework;
using System;
using BrandVue.SourceData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Dates;

namespace Test.BrandVue.SourceData.Calculation
{
    [TestFixture]
    internal class ResultDateCalculatorTests
    {
        [Test]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-08-01", "2018-08-31")]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-08-01", "2018-08-31")]
        [TestCase(3, MakeUpTo.MonthEnd, "2018-08-31", "2018-06-30")]
        [TestCase(3, MakeUpTo.QuarterEnd, "2017-11-20", "2017-10-31")]
        [TestCase(3, MakeUpTo.QuarterEnd, "2017-01-20", "2017-01-31")]
        [TestCase(6, MakeUpTo.HalfYearEnd, "2018-01-01", "2018-01-31")]
        [TestCase(6, MakeUpTo.HalfYearEnd, "2017-11-01", "2017-07-31")]
        [TestCase(12, MakeUpTo.CalendarYearEnd, "2017-06-01", "2017-01-31")]
        [TestCase(12, MakeUpTo.CalendarYearEnd, "2017-10-01", "2017-01-31")]
        public void Check_That_ActualValue_SeriesType_Start_Date_Is_Correct_For_Monthly_Average(
            int numberMonthsInAverage, MakeUpTo makeUpTo, DateTime inputStartDate, DateTime expectedStartOfDataPoints)
        {
            var requestedStartDate = new DateTimeOffset(DateTime.SpecifyKind(inputStartDate, DateTimeKind.Utc));
            var expectedStartDateTimeOffset =
                new DateTimeOffset(DateTime.SpecifyKind(expectedStartOfDataPoints, DateTimeKind.Utc));
            var beginningOfDataset = DateTimeOffset.Parse("2017-01-01");
            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = numberMonthsInAverage,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                MakeUpTo = makeUpTo
            };

            var actualStartDate = ResultDateCalculator.GetFirst(averageDescriptor, null, beginningOfDataset,
                requestedStartDate);

            Assert.That(actualStartDate, Is.EqualTo(expectedStartDateTimeOffset));
        }

        [Test]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-08-01", "2018-08-01")]
        public void ResultDate_Returns_Correct_Date_If_TotalisationPeriod_Is_Day(
            int numberMonthsInAverage, MakeUpTo makeUpTo, DateTime inputStartDate, DateTime expectedStartOfDataPoints)
        {
            var requestedStartDate = inputStartDate.ToUtcDateOffset();
            var expectedStartDateTimeOffset =
                new DateTimeOffset(DateTime.SpecifyKind(expectedStartOfDataPoints, DateTimeKind.Utc));
            var beginningOfDataset = DateTimeOffset.Parse("2017-01-01");
            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = numberMonthsInAverage,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
                MakeUpTo = makeUpTo
            };

            var actualStartDate =
                ResultDateCalculator.GetFirst(averageDescriptor, null, beginningOfDataset,  requestedStartDate);

            Assert.That(actualStartDate, Is.EqualTo(expectedStartDateTimeOffset));
        }

        [Test]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-01-01", "2018-12-31", "2018-01-31", "2018-12-31")]
        [TestCase(3, MakeUpTo.QuarterEnd, "2018-01-01", "2018-12-31", "2018-01-31", "2018-12-31")]
        [TestCase(6, MakeUpTo.HalfYearEnd, "2018-01-01", "2018-12-31", "2018-01-31", "2018-12-31")]
        [TestCase(12, MakeUpTo.CalendarYearEnd, "2018-01-01", "2018-12-31", "2018-01-31", "2018-12-31")]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-01-01", "2019-01-31", "2018-01-31", "2019-01-31")]
        [TestCase(3, MakeUpTo.QuarterEnd, "2018-01-01", "2019-01-31", "2018-01-31", "2018-12-31")]
        [TestCase(6, MakeUpTo.HalfYearEnd, "2018-01-01", "2019-01-31", "2018-01-31", "2018-12-31")]
        [TestCase(12, MakeUpTo.CalendarYearEnd, "2018-01-01", "2019-01-31", "2018-01-31", "2018-12-31")]
        public void Check_That_ActualValue_SeriesType_Start_And_End_Date_Is_Correct(
            int numberMonthsInAverage, MakeUpTo makeUpTo, DateTime inputStartDate, DateTime inputEndDate,
            DateTime expectedStartDate, DateTime expectedEndDate)
        {
            var requestedStartDate = new DateTimeOffset(DateTime.SpecifyKind(inputStartDate, DateTimeKind.Utc));
            var inputEndDateTimeOffset = new DateTimeOffset(DateTime.SpecifyKind(inputEndDate, DateTimeKind.Utc));
            var expectedStartDateTimeOffset =
                new DateTimeOffset(DateTime.SpecifyKind(expectedStartDate, DateTimeKind.Utc));
            var expectedEndDateTimeOffset = new DateTimeOffset(DateTime.SpecifyKind(expectedEndDate, DateTimeKind.Utc));
            var beginningOfDataset = DateTimeOffset.Parse("2017-01-01");

            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = numberMonthsInAverage,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                MakeUpTo = makeUpTo
            };

            var actualStartDate =
                ResultDateCalculator.GetFirst(averageDescriptor, null, beginningOfDataset, requestedStartDate);

            var actualEndDate = ResultDateCalculator.GetLast(inputEndDateTimeOffset, averageDescriptor);

            Assert.That(actualStartDate, Is.EqualTo(expectedStartDateTimeOffset), "start");
            Assert.That(actualEndDate, Is.EqualTo(expectedEndDateTimeOffset), "end");
        }

        [Test]
        [TestCase(1, MakeUpTo.MonthEnd, null, "2018-01-31")]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-01-31", "2018-02-28")]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-01-01", "2018-01-31")]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-02-28", "2018-03-31")]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-02-01", "2018-02-28")]
        [TestCase(3, MakeUpTo.MonthEnd, "2018-02-01", "2018-02-28")]
        [TestCase(3, MakeUpTo.QuarterEnd, "2018-02-01", "2018-04-30")]
        [TestCase(6, MakeUpTo.HalfYearEnd, "2018-02-01", "2018-07-31")]
        [TestCase(12, MakeUpTo.CalendarYearEnd, "2017-02-01", "2018-01-31")]
        public void
            GetFirst_Returns_Correct_Date_If_BeginningOfDataset_Early_Than_MeasureDate_And_Later_Than_RequestedDate(
                int numberMonthsInAverage,
                MakeUpTo makeUpTo,
                DateTime? measureStartDateTime,
                DateTime expectedStartOfDataPoints)
        {
            var start = measureStartDateTime.HasValue
                ? DateTimeOffset.Parse("2017-01-01")
                : DateTimeOffset.Parse("2018-01-01");
            var expectedStartDateTimeOffset = expectedStartOfDataPoints.ToUtcDateOffset();
            var measureDateOffset = measureStartDateTime?.ToUtcDateOffset();
            var beginningOfDataset = DateTimeOffset.Parse("2017-01-01");

            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = numberMonthsInAverage,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                MakeUpTo = makeUpTo
            };

            var actualStartDate = ResultDateCalculator.GetFirst(averageDescriptor, measureDateOffset,
                beginningOfDataset,
                start);

            Assert.That(actualStartDate, Is.EqualTo(expectedStartDateTimeOffset));
        }

        [Test]
        [TestCase(1, MakeUpTo.MonthEnd, "2018-01-31", "2018-02-28")]
        public void GetFirst_Gets_Latest_Date_If_BeginningOfDataset_Later_Than_MeasureDate_And_RequestedDate(
            int numberMonthsInAverage,
            MakeUpTo makeUpTo,
            DateTime dataStartDate,
            DateTime expectedStartDate
            )
        {
            var start = DateTimeOffset.Parse("2017-01-01");
            var beginningOfDataset = dataStartDate.ToUtcDateOffset();
            var measureDateOffset = DateTimeOffset.Parse("2017-01-01");

            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = numberMonthsInAverage,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                MakeUpTo = makeUpTo
            };

            var actualStartDate = ResultDateCalculator.GetFirst(averageDescriptor, measureDateOffset, beginningOfDataset,start);
            Assert.That(actualStartDate, Is.EqualTo(expectedStartDate.ToUtcDateOffset()));
        }

        [Test]
        [TestCase(26, "2018-01-31", "2018-01-31")]
        [TestCase(26 * 7, "2017-02-01", "2017-02-01")]
        public void
            GetFirst_Returns_Correct_Date_If_BeginningOfDataset_Earlier_Than_MeasureDate_And_RequestedDate_For_Days(
                int numberMonthsInAverage,
                DateTime givenMeasureDateOffset,
                DateTime expectedStartOfDataPoints)
        {
            var startDate = DateTimeOffset.Parse("2017-01-01");
            var expectedStartDateTimeOffset = expectedStartOfDataPoints.ToUtcDateOffset();
            var measureDateOffset = givenMeasureDateOffset.ToUtcDateOffset();
            var beginningOfDataset = DateTimeOffset.Parse("2017-01-01");

            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = numberMonthsInAverage,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Day,
            };

            var actualStartDate = ResultDateCalculator.GetFirst(averageDescriptor, measureDateOffset,
                beginningOfDataset,
                startDate);

            Assert.That(actualStartDate, Is.EqualTo(expectedStartDateTimeOffset));
        }

        [Test]
        public void Check_That_Invalid_MakeUpTo_Causes_Exception()
        {
            var start = new DateTimeOffset(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
            var inputEndDateTimeOffset =
                new DateTimeOffset(DateTime.SpecifyKind(start.AddMonths(12).DateTime, DateTimeKind.Utc));

            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = 1,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                MakeUpTo = MakeUpTo.Day
            };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                ResultDateCalculator.GetFirst(averageDescriptor, null, start, inputEndDateTimeOffset);
            });
        }

        [Test]
        public void Check_that_end_date_is_correct_for_monthly_average()
        {
            var end = new DateTimeOffset(new DateTime(2018, 08, 31, 0, 0, 0, DateTimeKind.Utc));
            var averageDescriptor = new AverageDescriptor()
            {
                NumberOfPeriodsInAverage = 1,
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                MakeUpTo = MakeUpTo.MonthEnd
            };

            var endDate = ResultDateCalculator.GetLast(end, averageDescriptor);

            Assert.That(endDate, Is.EqualTo(end));
        }
    }
}
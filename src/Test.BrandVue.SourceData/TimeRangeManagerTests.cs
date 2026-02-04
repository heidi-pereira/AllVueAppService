using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.CalculationPipeline;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class TimeRangeManagerTests
    {
        private class TimeRange
        {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }

            public TimeRange(string start, string end)
            {
                Start = DateTime.Parse(start);
                End = DateTime.Parse(end);
            }

            public override string ToString()
            {
                return $"{Start - End}";
            }
        }

        private static readonly List<TimeRange> InitialList = new List<TimeRange>()
        {
            new TimeRange("01/08/2017", "01/10/2017"),
            new TimeRange("01/01/2018", "31/12/2018")
        };

        [TestCase("2019-01-01", "2019-02-10", ExpectedResult = false)]
        [TestCase("2018-05-01", "2018-12-01", ExpectedResult = true)]
        [TestCase("01/09/2017", "01/12/2017", ExpectedResult = false)]
        [TestCase("01/02/2017", "01/09/2017", ExpectedResult = false)]
        [TestCase("01/09/2017", "30/09/2019", ExpectedResult = false)]
        [TestCase("01/08/2017", "01/10/2017", ExpectedResult = true)]
        public bool IsRangeEntirelyIncluded(string start, string end)
        {
            return GetTestTimeRangesManager().IsRangeEntirelyIncluded(DateTime.Parse(start), DateTime.Parse(end));
        }

        [TestCase("01/07/2016", "01/10/2016", "10/07/2016", "01/10/2016", 3)]
        [TestCase("01/07/2017", "01/10/2017", "10/07/2017", "01/10/2017", 2)]
        [TestCase("01/01/2018", "31/01/2019", "01/09/2018", "20/01/2019", 2)]
        [TestCase("01/08/2017", "01/02/2018", "01/08/2017", "31/12/2018", 1)]
        [TestCase("01/07/2017", "01/11/2017", "01/07/2017", "01/11/2017", 2)]
        [TestCase("01/07/2017", "01/01/2019", "01/07/2017", "01/01/2019", 1)]
        public void AddRangeAndTest(string addingTimeStart, string addingTimeEnd, string start, string end, int numberOfInternalRanges)
        {
            var manager = GetTestTimeRangesManager();

            manager.AddRange(DateTime.Parse(addingTimeStart), DateTime.Parse(addingTimeEnd));

            foreach (var timeRange in InitialList)
            {
                Assert.That(manager.IsRangeEntirelyIncluded(timeRange.Start, timeRange.End), Is.True, $"Initial time range: {timeRange} is no mere included in the RangeManager");
            }

            Assert.That(manager.TimeRanges.Count, Is.EqualTo(numberOfInternalRanges), "Number of internal ranges does not match");

            Assert.That(manager.IsRangeEntirelyIncluded(DateTime.Parse(start), DateTime.Parse(end)), Is.True, $"Date range {start} - {end} is not entirely included in RangeManager");
        }

        private static TimeRangesManager GetTestTimeRangesManager()
        {
            var manager = new TimeRangesManager();
            foreach (var timeRange in InitialList)
            {
                manager.AddRange(timeRange.Start, timeRange.End);
            }

            return manager;
        }
    }
}

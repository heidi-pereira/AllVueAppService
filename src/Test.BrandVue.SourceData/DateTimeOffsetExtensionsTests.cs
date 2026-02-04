using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class DateTimeOffsetExtensionsTests
    {
        [TestCase("2018-12-01", ExpectedResult = "2018-12-31")]
        [TestCase("2018-12-31", ExpectedResult = "2018-12-31")]
        public string GetLastDayOfMonthOnOrAfter(string input)
        {
            return new DateTimeOffset(DateTime.Parse(input)).GetLastDayOfMonthOnOrAfter().ToString("yyyy-MM-dd");
        }


        [TestCase("2018-12-01", ExpectedResult = "2018-11-30")]
        [TestCase("2018-12-31", ExpectedResult = "2018-12-31")]
        public string GetLastDayOfMonthOnOrPreceding(string input)
        {
            return new DateTimeOffset(DateTime.Parse(input)).GetLastDayOfMonthOnOrPreceding().ToString("yyyy-MM-dd");
        }
    }
}

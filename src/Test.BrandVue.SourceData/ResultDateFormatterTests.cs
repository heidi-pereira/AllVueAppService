using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Dates;
using NUnit.Framework;
using System;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class ResultDateFormatterTests
    {
        [Test]
        [TestCase("2025-05-19", "w/e 25 May")] // Monday to Sunday week
        [TestCase("2025-05-20", "w/e 25 May")] // Tuesday to Sunday week
        [TestCase("2025-05-21", "w/e 25 May")] // Wednesday to Sunday week
        [TestCase("2025-05-22", "w/e 25 May")] // Thursday to Sunday week
        [TestCase("2025-05-23", "w/e 25 May")] // Friday to Sunday week
        [TestCase("2025-05-24", "w/e 25 May")] // Saturday to Sunday week
        [TestCase("2025-05-25", "w/e 25 May")] // Sunday to Sunday week
        public void FormatWeekEnd_ShouldReturnCorrectSundayDate(string inputDate, string expectedOutput)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate + "T00:00:00.000Z");

            // Act
            var result = ResultDateFormatter.FormatDate(date, MakeUpTo.WeekEnd);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOutput));
        }

        [Test]
        [TestCase("2025-05-01", "May 25")]
        [TestCase("2025-05-15", "May 25")]
        [TestCase("2025-05-31", "May 25")]
        public void FormatMonthEnd_ShouldReturnCorrectMonthEndDate(string inputDate, string expectedOutput)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate + "T00:00:00.000Z");

            // Act
            var result = ResultDateFormatter.FormatDate(date, MakeUpTo.MonthEnd);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOutput));
        }

        [Test]
        [TestCase("2025-01-15", "Q1 2025")]
        [TestCase("2025-04-15", "Q2 2025")]
        [TestCase("2025-07-15", "Q3 2025")]
        [TestCase("2025-10-15", "Q4 2025")]
        public void FormatQuarterEnd_ShouldReturnCorrectQuarterEndDate(string inputDate, string expectedOutput)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate + "T00:00:00.000Z");

            // Act
            var result = ResultDateFormatter.FormatDate(date, MakeUpTo.QuarterEnd);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOutput));
        }

        [Test]
        [TestCase("2025-01-15", "1st half of 2025")]
        [TestCase("2025-07-15", "2nd half of 2025")]
        public void FormatHalfYearEnd_ShouldReturnCorrectHalfYearEndDate(string inputDate, string expectedOutput)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate + "T00:00:00.000Z");

            // Act
            var result = ResultDateFormatter.FormatDate(date, MakeUpTo.HalfYearEnd);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOutput));
        }

        [Test]
        [TestCase("2025-05-15", "2025")]
        public void FormatYearEnd_ShouldReturnCorrectYearEndDate(string inputDate, string expectedOutput)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate + "T00:00:00.000Z");

            // Act
            var result = ResultDateFormatter.FormatDate(date, MakeUpTo.CalendarYearEnd);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOutput));
        }

        [Test]
        [TestCase("2025-01-06", "w/e 12 Jan")] // Monday -> Sunday
        [TestCase("2025-01-12", "w/e 12 Jan")] // Sunday -> Sunday
        [TestCase("2025-12-29", "w/e 04 Jan")] // Monday -> Sunday (crosses year boundary)
        [TestCase("2025-12-28", "w/e 28 Dec")] // Sunday -> Sunday (end of year)
        public void FormatWeekEnd_ShouldHandleEdgeCases(string inputDate, string expectedOutput)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate + "T00:00:00.000Z");

            // Act
            var result = ResultDateFormatter.FormatDate(date, MakeUpTo.WeekEnd);

            // Assert
            Assert.That(result, Is.EqualTo(expectedOutput));
        }
    }
}
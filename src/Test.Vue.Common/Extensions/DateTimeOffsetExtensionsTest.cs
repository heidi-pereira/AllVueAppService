using System;
using Vue.Common.Extensions;

namespace Test.Vue.Common.Extensions
{
    [TestFixture]
    public class DateTimeOffsetExtensionsTest
    {
        [TestCase("2023-01-31T00:00:00Z", "2023-01-31T00:00:00Z")]
        [TestCase("2023-01-15T00:00:00Z", "2022-12-31T00:00:00Z")]
        [TestCase("2024-02-29T00:00:00Z", "2024-02-29T00:00:00Z")]//leap year
        [TestCase("2023-02-28T00:00:00Z", "2023-02-28T00:00:00Z")]
        public void GetLastDayOfMonthOnOrPreceding_ShouldReturnExpectedDate(string inputDate, string expectedDate)
        {
            // Arrange
            var date = DateTimeOffset.Parse(inputDate);
            var expected = DateTimeOffset.Parse(expectedDate);

            // Act
            var result = date.GetLastDayOfMonthOnOrPreceding();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
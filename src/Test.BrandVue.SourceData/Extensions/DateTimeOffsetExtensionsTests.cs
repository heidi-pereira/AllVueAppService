using System;
using BrandVue.SourceData;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Extensions
{
    public class DateTimeOffsetExtensionsTests
    {
        [Test]
        public void EndOfDayShouldBeOneTickBeforeTomorrow()
        {
            var today = new DateTimeOffset(new DateTime(2019, 11, 25), TimeSpan.Zero);
            var tomorrow = today.AddDays(1);
            
            Assert.That(today.EndOfDay().Ticks, Is.EqualTo(tomorrow.Ticks - 1));
        }
    }
}
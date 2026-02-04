using BrandVue.SourceData.Utils;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Extensions
{
    [TestFixture]
    public class StringExtensionsTests
    {
        [TestCase("Some%!:Metric&With*a load of     odd^characters", "some-metric-with-a-load-of-odd-characters", TestName = "A metric with a one or more unwanted characters")]
        [TestCase("A Perfectly Acceptable Metric Name", "a-perfectly-acceptable-metric-name", TestName = "A metric with a name we would expect")]
        [TestCase("Metric Ending Unwanted Characters!% &    *(", "metric-ending-unwanted-characters", TestName = "A metric with unwanted characters at the end of the name")]
        [TestCase("&^ ([    ^Metric Starting Unwanted Characters", "metric-starting-unwanted-characters", TestName = "A metric with unwanted characters at the start of the name")]
        [TestCase("-----Metric---Containing-Lots--Of-Hyphens---", "metric-containing-lots-of-hyphens", TestName = "A metric with lots of hyphens")]
        [TestCase("A CAPS METRIC", "a-caps-metric", TestName = "A metric in caps")]
        public void TestUrlSanitizeExtension(string input, string output)
        {
            Assert.That(output, Is.EqualTo(input.SanitizeUrlSegment()));
        }
    }
}

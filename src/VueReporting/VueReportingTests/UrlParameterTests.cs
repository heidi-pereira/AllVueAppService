using System;
using System.Collections.Specialized;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using VueReporting;
using VueReporting.Models;
using VueReporting.Services;

namespace VueReportingTests
{
    [TestFixture]
    class UrlParameterTests : TestBase
    {
        [Test]
        [TestCase("http://localhost?test=1&Moving+average=2&test2=3&Range=this+month&start=123", "http://localhost?test=1&test2=3")]
        public void TestUrlParametersExcluded(string url, string expected)
        {
            var manipulatedParameters = new NameValueCollection();

            var brandSet = new EntitySet
            {
                Name = "Test",
                MainInstanceId = null,
                InstanceIds = new long[] {},
                Organisation = "",
            };

            var appSettings = Mock.Of<IAppSettings>(a => 
                a.ExcludedFilters == new[] { "start", "moving average", "Range"} 
                && a.ProductFilter == new Uri(url).Query);

            var parameterManipulator = new ReportParameterManipulator(brandSet, ServiceProvider.GetRequiredService<IBrandVueService>(), appSettings);

            parameterManipulator.ApplyProductFilters(manipulatedParameters, new Uri(url).Query);
            parameterManipulator.RemoveFilteredParameters(manipulatedParameters);

            var expectParameters = HttpUtility.ParseQueryString(new Uri(expected).Query);

            Assert.That(manipulatedParameters, Is.EquivalentTo(expectParameters));
        }
    }
}

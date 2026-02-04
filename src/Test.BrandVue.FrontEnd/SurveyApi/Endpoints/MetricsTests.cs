using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class MetricsTests
    {
        [TestCase("/api/surveysets/UK/metrics")]
        public async Task GivenValidSurveysetThenResponseContainsMetricResults(string url) => 
            await PublicSurveyApi.GetAsyncAssertLengthOk(url, ExpectedOutputs.Metrics());

        [TestCase("/api/surveysets/UK/profile/metrics")]
        public async Task GivenValidSurveysetThenResponseContainsProfileMetricResults(string url) =>
            await PublicSurveyApi.GetAsyncAssertLengthOk(url, ExpectedOutputs.Metrics().Where(m => m.Type == "profile"));

        [TestCase("/api/surveysets/UK/classes/brand/metrics")]
        public async Task GivenValidSurveysetThenResponseContainsBrandMetricResults(string url) =>
            await PublicSurveyApi.GetAsyncAssertLengthOk(url, ExpectedOutputs.Metrics().Where(m => m.Type == "brand"));

        [TestCase("/api/surveysets/UK/classes/product/metrics")]
        public async Task GivenValidSurveysetThenResponseContainsProductProfileMetricResults(string url) =>
            await PublicSurveyApi.GetAsyncAssertOk(url, Enumerable.Empty<MetricDescriptor>());

        [TestCase("/api/surveysets/UK/classes/product/classes/brand/metrics")]
        public async Task GivenValidSurveysetThenResponseContainsProductBrandMetricResults(string url) =>
            await PublicSurveyApi.GetAsyncAssertLengthOk(url, ExpectedOutputs.Metrics().Where(m => m.Type == "brand|product"));

        [TestCase("/api/surveysets/InvalidSurveyset/metrics")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url) => 
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
    }
}
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.SurveyApi.Services;
using Vue.AuthMiddleware;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class MetricResultsTests
    {
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1", 1)] //No dates passed
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&startDate=2019-01-01", 7)] //In range startDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&endDate=2019-6-30", 1)] //In range endDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&startDate=2019-01-01&endDate=2019-7-31", 7)] //In range request
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/14Days?instanceId=1", 1)] //No dates passed
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/14Days?instanceId=1&startDate=2019-07-01", 31)] //In range startDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/14Days?instanceId=1&endDate=2019-07-31", 1)] //In range endDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/14Days?instanceId=1&startDate=2019-06-15&endDate=2019-07-31", 46)] //In range request
        public async Task GivenValidSurveysetThenResponseContainsMetricResultsForClass(string url, int expectedLineCount)
        {
            // act
            var content = await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);

            // assert
            string headerString = $"{PublicApiConstants.MetricResultsFieldNames.EndDate},{PublicApiConstants.MetricResultsFieldNames.Value},{PublicApiConstants.MetricResultsFieldNames.SampleSize}";
            Assert.That(content, Contains.Substring(headerString));
            int actualNewlineCount = content.NewLineCount();
            Assert.That(actualNewlineCount, Is.EqualTo(expectedLineCount + 1),
                $"Expected a row count of {expectedLineCount + 1} plus the header. but was {actualNewlineCount}");
        }

        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&startDate=2010-01-01", "startDate: 2010-01-01 is out of responses date range.")] //Out of range startDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&endDate=2020-12-31", "endDate: 2020-12-31 is out of responses date range.")] //Out of range endDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/14Days?instanceId=1&startDate=2010-01-01", "startDate: 2010-01-01 is out of responses date range.")] //Out of range startDate
        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/14Days?instanceId=1&endDate=2020-12-31", "endDate: 2020-12-31 is out of responses date range.")] //Out of range endDate
        public async Task OutOfRangeRequestDates(string url, string errorMessage)
        {
            // act
            var content = await PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.BadRequest);

            Assert.That(content.Message, Does.Contain(errorMessage));
        }

        [TestCase("/api/surveysets/uk/profile/metrics/net-buzz/Monthly", "ClassInstances key combination: [] is invalid for 'Net Buzz' metric. Keys should be [brand]")] //The metric does not exist in the requested class.
        public async Task MetricDoesNotBelongToTheRequestedClass(string url, string errorMessage)
        {
            // act
            var content = await PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.BadRequest);

            Assert.That(content.Message, Does.Contain(errorMessage));
        }

        [TestCase("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly")] //No instanceId provided for an entity metric
        public async Task InstanceIdNotProvidedForEntityClass(string url)
        {
            // act
            var result = await PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.BadRequest);
            Assert.That(result.Message, Does.Contain("ClassInstanceId must be specified"));
        }

        [TestCase("/api/surveysets/uk/metrics/net-buzz/Monthly")]
        [TestCase("/api/surveysets/uk/metrics/age/Monthly")]
        public async Task RequestWithNoBody(string url)
        {
            var result = await PublicSurveyApi
                .PostAsyncAssert<ErrorApiResponse>(url, string.Empty, HttpStatusCode.BadRequest);

            Assert.That(result.Message, Is.EqualTo("A non-empty request body is required."));
        }

        [Test]
        public async Task GivenValidAwaitedRequestsApiCorrectlyQueuesThem()
        {
            var server = PublicSurveyApi.WithAccessToResources(new[] { Constants.ResourceNames.MetricResults });
            for (int i = 0; i < 4; i++)
            {
                await server.GetAsyncAssert("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&startDate=2019-01-01&endDate=2019-7-31", HttpStatusCode.OK);
            }
        }

        [Test]
        public async Task GivenValidNonAwaitedRequestsApiCorrectlySendsTooManyRequests()
        {
            var server = PublicSurveyApi.WithAccessToResources(new[] { Constants.ResourceNames.MetricResults }).OverrideAppSettings(new()
            {
                { "API.RateLimits:PublicApi:Enabled", true.ToString() }
            });
            var client = server.TestServerHttpClient();
            var requests = Enumerable.Range(1, 4).Select(_ => client.GetAsync("/api/surveysets/uk/classes/brand/metrics/net-buzz/Monthly?instanceId=1&startDate=2019-01-01&endDate=2019-7-31"));
            var responses = await Task.WhenAll(requests);
            Assert.That(responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToArray(), Has.Length.EqualTo(3));
            Assert.That(responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToArray(), Has.Length.EqualTo(1));
        }
    }
}

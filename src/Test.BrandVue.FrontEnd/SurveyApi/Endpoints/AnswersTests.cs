using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using Microsoft.AspNetCore.ResponseCompression;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using Test.BrandVue.FrontEnd.SurveyApi.Services;
using Vue.AuthMiddleware;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class AnswersTests
    {
        [TestCase("/api/surveysets/uk/classes/brand/answers/2018-12-31", "Brand_Id")]
        [TestCase("/Api/Surveysets/UK/Classes/Brand/answers/2018-12-31", "Brand_Id")] // Check for case insensitivity even though we don't guarantee it everywhere
        [TestCase("/api/surveysets/UK/profile/answers/2018-12-31", PublicApiConstants.EntityResponseFieldNames.WeightingCellId)]
        [TestCase("/api/surveysets/UK/profile/answers/2017-5-1", PublicApiConstants.EntityResponseFieldNames.WeightingCellId)] //We won't document it without leading zeros, but should make it work for the sake of sanity
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answers/2017-5-1", "Product_Id" + "," + "Brand_Id")]
        public async Task GivenValidSurveysetThenResponseContainsSurveyResponsesForClass(string url, string expectedHeaderField)
        {
            // act
            var content = await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);

            // assert
            Assert.That(content, Contains.Substring($"{expectedHeaderField}"));
            Assert.That(content.NewLineCount(), Is.GreaterThan(1),
                $"Expected some rows of response data but only got the header row");
        }

        [TestCase("/api/surveysets/InvalidSurveyset/classes/Brand/answers/2019-02-28")]
        [TestCase("/api/surveysets/UK/classes/InvalidClass/answers/2019-02-28")]
        [TestCase("/api/surveysets/InvalidSurveyset/profile/answers/2019-02-28")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }

        [TestCase("/api/surveysets/uk/classes/product/answers/2018-12-20")]
        public async Task GivenValidSurveysetThenResponseContainsNoSurveyResponsesForProductClass(string url)
        {
            // act
            var content = await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);
            var contentLineCount = content.NewLineCount();

            // assert
            Assert.That(content, Contains.Substring($"Product_Id"));
            Assert.That(contentLineCount, Is.EqualTo(1),
                $"Expected only the header line in the response but got {contentLineCount - 1} additional lines of text");
        }

        [TestCase("/api/surveysets/UK/classes/Brand/answers/9999-02-28")]
        [TestCase("/api/surveysets/UK/profile/answers/9999-02-28")]
        public async Task GivenValidSurveysetButOutOfRangeDateThenResponseIsNotFound(string url)
        {
            // arrange
            var responseFieldManager = Substitute.For<IResponseFieldManager>();
            var quotaCells = Substitute.For<IGroupedQuotaCells>();
            var profileResponseAccessor = MockRepositoryData.SubstituteProfileResponseAccessor(responseFieldManager, quotaCells);

            // act
            var responseContent = await PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.NotFound);

            // assert
            Assert.That(responseContent.Message,
                Is.EqualTo("date: " + DateModelBinder.RequestedDateOutOfDateRange(
                    url.Split('/').Last(),
                    profileResponseAccessor.StartDate,
                    profileResponseAccessor.EndDate)).IgnoreCase);
        }


        [TestCase("/api/surveysets/uk/classes/product/classes/product/answers/2018-12-20")]
        public async Task GivenValidSurveysetThenResponseContainsNoSurveyResponsesForProductNestedClass(string url)
        {
            // act
            var content = await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.BadRequest);

            // assert
            Assert.That(content, Contains.Substring("Parent class and child class must be different"));
        }

        [TestCase("/api/surveysets/UK/classes/Brand/answers/2019-01-01")]
        public async Task GivenValidSurveysetButDateWithNoResponseThenGetOnlyCsvHeaderLine(string url)
        {
            var responseContent = await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);
            var numberOfLineBreaks = responseContent.NewLineCount();

            Assert.That(numberOfLineBreaks, Is.EqualTo(1), 
                $"Expected only the header line in the response but got {numberOfLineBreaks - 1} additional lines of text");
        }

        [Test]
        public async Task GivenValidAwaitedRequestsApiCorrectlyQueuesThem()
        {
            var server = PublicSurveyApi.WithAccessToResources(new[] { Constants.ResourceNames.RawSurveyData });
            for (int i = 0; i < 4; i++)
            {
                await server.GetAsyncAssert("/api/surveysets/uk/classes/brand/answers/2018-12-31", HttpStatusCode.OK);
            }
        }

        [Test]
        public async Task GivenValidNonAwaitedRequestsApiCorrectlySendsTooManyRequests()
        {
            var server = PublicSurveyApi
                .WithAccessToResources(new[] { Constants.ResourceNames.RawSurveyData })
                .OverrideAppSettings(new()
                {
                    { "API.RateLimits:PublicApi:Enabled", true.ToString() }
                });
            var client = server.TestServerHttpClient();
            var requests = Enumerable.Range(1, 4).Select(_ => client.GetAsync("/api/surveysets/uk/classes/brand/answers/2018-12-31"));
            var responses = await Task.WhenAll(requests);
            Assert.That(responses.Where(r => r.StatusCode == HttpStatusCode.OK).ToArray(), Has.Length.EqualTo(3), "Not correctly rate limited");
            Assert.That(responses.Where(r => r.StatusCode == HttpStatusCode.TooManyRequests).ToArray(), Has.Length.EqualTo(1));
        }
    }
}
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using Test.BrandVue.FrontEnd.SurveyApi.Services;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class DeprecatedAnswersetsTests
    {
        [TestCase("/api/surveysets/uk/classes/brand/answersets/2018-12-31", "Brand_Id")]
        [TestCase("/Api/Surveysets/UK/Classes/Brand/answersets/2018-12-31", "Brand_Id")] // Check for case insensitivity even though we don't guarantee it everywhere
        [TestCase("/api/surveysets/UK/profile/answersets/2018-12-31", PublicApiConstants.EntityResponseFieldNames.DemographicCellId)]
        [TestCase("/api/surveysets/UK/profile/answersets/2017-5-1", PublicApiConstants.EntityResponseFieldNames.DemographicCellId)] //We won't document it without leading zeros, but should make it work for the sake of sanity
        [TestCase("/api/surveysets/UK/classes/product/classes/brand/answersets/2017-5-1", "Product_Id" + "," + "Brand_Id")]
        public async Task GivenValidSurveysetThenResponseContainsSurveyResponsesForClass(string url, string expectedHeaderField)
        {
            // act
            var content = await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);

            // assert
            Assert.That(content, Contains.Substring($"{expectedHeaderField}"));
            Assert.That(content.NewLineCount(), Is.GreaterThan(1),
                $"Expected some rows of response data but only got the header row");
        }

        [TestCase("/api/surveysets/InvalidSurveyset/classes/Brand/answersets/2019-02-28")]
        [TestCase("/api/surveysets/UK/classes/InvalidClass/answersets/2019-02-28")]
        [TestCase("/api/surveysets/InvalidSurveyset/profile/answersets/2019-02-28")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }

        [TestCase("/api/surveysets/uk/classes/product/answersets/2018-12-20")]
        public async Task GivenValidSurveysetThenResponseContainsNoSurveyResponsesForProductClass(string url)
        {
            // act
            var content = await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);
            var contentLineCount = content.NewLineCount();

            // assert
            Assert.That(content, Contains.Substring($"Product_Id"));
            Assert.That(contentLineCount, Is.EqualTo(1),
                $"Expected only the header line in the response but got {contentLineCount - 1} additional lines of text");
        }

        [TestCase("/api/surveysets/UK/classes/Brand/answersets/9999-02-28")]
        [TestCase("/api/surveysets/UK/profile/answersets/9999-02-28")]
        public async Task GivenValidSurveysetButOutOfRangeDateThenResponseIsNotFound(string url)
        {
            // arrange
            var responseFieldManager = Substitute.For<IResponseFieldManager>();
            var quotaCells = Substitute.For<IGroupedQuotaCells>();
            var profileResponseAccessor = MockRepositoryData.SubstituteProfileResponseAccessor(responseFieldManager, quotaCells);

            // act
            var responseContent = await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url, HttpStatusCode.NotFound);

            // assert
            Assert.That(responseContent.Message,
                Is.EqualTo("date: " + DateModelBinder.RequestedDateOutOfDateRange(
                    url.Split('/').Last(),
                    profileResponseAccessor.StartDate,
                    profileResponseAccessor.EndDate)).IgnoreCase);
        }

        [TestCase("/api/surveysets/UK/classes/Brand/answersets/2019-01-01")]
        public async Task GivenValidSurveysetButDateWithNoResponseThenGetOnlyCsvHeaderLine(string url)
        {
            var responseContent = await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.OK);
            var numberOfLineBreaks = responseContent.NewLineCount();

            Assert.That(numberOfLineBreaks, Is.EqualTo(1), 
                $"Expected only the header line in the response but got {numberOfLineBreaks - 1} additional lines of text");
        }
    }
}
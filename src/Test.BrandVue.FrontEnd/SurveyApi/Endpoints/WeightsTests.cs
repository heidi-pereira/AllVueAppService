using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class WeightsTests
    {
        [TestCase("/api/surveysets/UK/averages/14Days/weights/2018-07-07/")]
        [TestCase("/api/surveysets/UK/averages/Monthly/weights/2018-07-07/")]
        [TestCase("/api/surveysets/UK/averages/28Days/weights/2018-07-07/")]
        public async Task GivenWeRequestWeightingsEndpointWithCorrectParametersGetWeightingResults(string url)
        {
            await BrandVueTestServer.PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.CellWeightings());
        }

        [TestCase("/api/surveysets/UK/averages/{average}/weights/2018-07-07/", "Quarterly")]
        [TestCase("/api/surveysets/UK/averages/{average}/weights/2018-07-07/", "HalfYearly")]
        public async Task GivenWeRequestWeightingsEndpointWithUnsupportedAveragesThenBadRequest(string url, string average)
        { 
            var errorApiResponse = await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url.Replace("{average}", average), HttpStatusCode.NotFound);
            Assert.That(errorApiResponse.Message, Is.EqualTo($"Month-based averages such as {average} are weighted per-month. Please request the 'Monthly' average for each constituent month."));
        }

        [TestCase("/api/surveysets/UK/averages/BLAH/weights/2018-07-07/")]
        [TestCase("/api/surveysets/UK/averages/14Days/weights/3000-01-01/")]
        [TestCase("/api/surveysets/UK/averages/14Days/weights/2010-01-01/")]
        [TestCase("/api/surveysets/UK/averages/Annual/weights/2018-07-07/")]
        public async Task GivenWeRequestWeightingsEndpointWithWrongParametersWeGetNotFoundResponse(string url)
        {
            await BrandVueTestServer.PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}
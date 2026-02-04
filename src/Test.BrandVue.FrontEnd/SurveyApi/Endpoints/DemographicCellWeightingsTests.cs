using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class DemographicCellWeightingsTests
    {
        [TestCase("/api/surveysets/UK/averages/14Days/weightings/2018-07-07/")]
        [TestCase("/api/surveysets/UK/averages/Monthly/weightings/2018-07-07/")]
        [TestCase("/api/surveysets/UK/averages/28Days/weightings/2018-07-07/")]
        public async Task GivenWeRequestWeightingsEndpointWithCorrectParametersGetWeightingResults(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.DemographicCellWeightings());
        }

        [TestCase("/api/surveysets/UK/averages/{average}/weightings/2018-07-07/", "Quarterly")]
        [TestCase("/api/surveysets/UK/averages/{average}/weightings/2018-07-07/", "HalfYearly")]
        public async Task GivenWeRequestWeightingsEndpointWithUnsupportedAveragesThenBadRequest(string url, string average)
        { 
            var errorApiResponse = await PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url.Replace("{average}", average), HttpStatusCode.NotFound);
            Assert.That(errorApiResponse.Message, Is.EqualTo($"Month-based averages such as {average} are weighted per-month. Please request the 'Monthly' average for each constituent month."));
        }

        [TestCase("/api/surveysets/UK/averages/BLAH/weightings/2018-07-07/")]
        [TestCase("/api/surveysets/UK/averages/14Days/weightings/3000-01-01/")]
        [TestCase("/api/surveysets/UK/averages/14Days/weightings/2010-01-01/")]
        [TestCase("/api/surveysets/UK/averages/Annual/weightings/2018-07-07/")]
        public async Task GivenWeRequestWeightingsEndpointWithWrongParametersWeGetNotFoundResponse(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}
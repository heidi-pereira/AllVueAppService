using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class DemographicCellsTests
    {
        [TestCase("/api/surveysets/UK/demographicCells")]
        public async Task GivenValidSurveysetThenResponseContainsApiAverageResults(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.DemographicCellDescriptors());
        }

        [TestCase("/api/surveysets/InvalidSurveyset/demographicCells")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}
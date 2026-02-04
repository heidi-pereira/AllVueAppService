using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class AveragesTests
    {
        [TestCase("/api/surveysets/UK/averages")]
        public async Task GivenValidSurveysetThenResponseContainsApiAverageResults(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.Averages());
        }

        [TestCase("/api/surveysets/InvalidSurveyset/averages")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}
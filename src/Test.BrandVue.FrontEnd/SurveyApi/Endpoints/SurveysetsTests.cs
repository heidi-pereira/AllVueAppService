using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class SurveysetsTests
    {
        [TestCase("/api/surveysets")]
        public async Task GivenNothingThenResponseContainsSurveysets(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.SurveysetDescriptors());
        }

        [TestCase("/api/surveysets/UK")]
        public async Task GivenValidSurveysetThenResponseContainsSurveysetInfo(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.SurveysetInfo());
        }

        [TestCase("/api/surveysets/InvalidSurveyset/")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}
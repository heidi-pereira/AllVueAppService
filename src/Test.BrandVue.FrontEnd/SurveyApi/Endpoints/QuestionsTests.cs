using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class QuestionsTests
    {
        [TestCase("/api/surveysets/UK/questions")]
        public async Task GivenValidSurveysetAndClassThenResponseContainsAllExpectedQuestionAnswerTypes(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.Questions());
        }

        [TestCase("/api/surveysets/InvalidSurveyset/questions")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}

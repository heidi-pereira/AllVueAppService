using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon.Extensions;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class ProfileQuestionsTests
    {
        [TestCase("/api/surveysets/UK/profile/questions")]
        public async Task GivenValidSurveysetThenResponseContainsAllExpectedQuestionAnswerTypes(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.Questions(TestEntityTypeRepository.Profile));
        }

        [TestCase("/api/surveysets/InvalidSurveyset/profile/questions")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}

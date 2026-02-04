using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class ClassesTests
    {
        [TestCase("/api/surveysets/UK/classes")]
        public async Task GivenValidSurveysetThenResponseContainsClassResults(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.ClassDescriptors());
        }

        [TestCase("/api/surveysets/UK/classes/Brand/instances")]
        public async Task GivenValidSurveysetAndBrandThenResponseContainsBrandInstanceResults(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.BrandInstanceDescriptors());
        }

        [TestCase("/api/surveysets/UK/classes/Product/instances")]
        public async Task GivenValidSurveysetAndProductThenResponseContainsProductInstanceResults(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.ProductInstanceDescriptors());
        }

        [TestCase("/api/surveysets/InvalidSurveyset/classes")]
        [TestCase("/api/surveysets/UK/classes/InvalidClass/instances")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }
    }
}
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon.Extensions;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Endpoints
{
    [TestFixture]
    public class DeprecatedClassQuestionsTests
    {
        [TestCase("/api/surveysets/UK/classes/Brand/questions")]
        public async Task GivenValidSurveysetAndBrandThenResponseContainsAllExpectedBrandQuestions(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.Questions(TestEntityTypeRepository.Brand));
        }

        [TestCase("/api/surveysets/UK/classes/Product/questions")]
        public async Task GivenValidSurveysetAndProductThenResponseContainsAllExpectedBrandQuestions(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.Questions(TestEntityTypeRepository.Product));
        }

        [TestCase("/api/surveysets/UK/classes/product/classes/brand/questions")]
        public async Task GivenValidSurveysetAndNestedClassThenResponseContainsAllExpectedQuestionAnswerTypes(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, ExpectedOutputs.Questions(TestEntityTypeRepository.Product, TestEntityTypeRepository.Brand));
        }

        [TestCase("/api/surveysets/InvalidSurveyset/classes/brand/questions")]
        [TestCase("/api/surveysets/UK/classes/InvalidBrand/questions")]
        public async Task GivenInvalidSurveysetThenResponseIsNotFound(string url)
        {
            await PublicSurveyApi.GetAsyncAssert(url, HttpStatusCode.NotFound);
        }

        [TestCase("/api/surveysets/US/classes/Product/questions")]
        public async Task GivenValidSurveysetWithNoProductQuestionsThenEmptyList(string url)
        {
            await PublicSurveyApi.GetAsyncAssertOk(url, new List<ClassDescriptor>());
        }
    }
}

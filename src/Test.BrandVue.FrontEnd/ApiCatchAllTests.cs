using System.Net;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd
{
    [TestFixture]
    public class ApiCatchAllTests
    {
        [TestCase("/api/surveysets/uk/doesnotexist", HttpStatusCode.NotFound, "Request API resource not found")]
        [TestCase("/api/surveysets/uk/classes/brand/not-here", HttpStatusCode.NotFound, "Request API resource not found")]
        public async Task RequestResourceNotFoundForUKSubset(string url, HttpStatusCode expectedCode, string expectedMessage)
        {
            // act
            var response = await PublicSurveyApi.GetAsyncAssert<ErrorApiResponse>(url, expectedCode);
            Assert.That(response.Message, Is.EqualTo(expectedMessage));
        }

        [TestCase("/api/metta", HttpStatusCode.NotFound, "Request API resource not found")]
        [TestCase("/api/meta/not-here", HttpStatusCode.NotFound, "Request API resource not found")]
        public async Task InternalApiControllerNotFound(string url, HttpStatusCode expectedCode, string expectedMessage)
        {
            // act
            var response = await InternalBrandVueApi.GetAsyncAssert<ErrorApiResponse>(url, expectedCode);
            Assert.That(response.Message, Is.EqualTo(expectedMessage), "Characterization: We don't care much about the internal api message, just characterizing that it's the same");
        }
    }
}

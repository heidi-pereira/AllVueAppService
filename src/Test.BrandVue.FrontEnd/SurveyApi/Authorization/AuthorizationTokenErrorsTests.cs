using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BrandVue.PublicApi.Models;
using NUnit.Framework;
using Vue.AuthMiddleware.Api;
using static Test.BrandVue.FrontEnd.BrandVueTestServer;

namespace Test.BrandVue.FrontEnd.SurveyApi.Authorization
{
    [TestFixture]
    public class AuthorizationTokenErrorsTests
    {
        [Test]
        public async Task NoAuthorizationHeaderPresent()
        {
            var errorApiResponse = await PublicSurveyApi.WithOnly(_ => { })
                .GetAsyncAssert<ErrorApiResponse>("/api/surveysets", HttpStatusCode.Unauthorized);
            Assert.That(errorApiResponse.Message, Is.EqualTo(UnauthorizedApiKeyResponses.NoAuthorizationHeaderPresent));
        }

        [Test]
        public async Task NoBearerPrefixBeforeToken()
        {
            var errorApiResponse = await PublicSurveyApi.WithOnly(AuthHeader(ApiKeyConstants.DebugApiKey, null))
                .GetAsyncAssert<ErrorApiResponse>("/api/surveysets", HttpStatusCode.Unauthorized);
            Assert.That(errorApiResponse.Message, Is.EqualTo(UnauthorizedApiKeyResponses.NoBearerPrefixBeforeToken));
        }

        [Test]
        public async Task NoReplacementOfTokenFromDocumentation()
        {
            var errorApiResponse = await PublicSurveyApi.WithOnly(AuthHeader("Bearer", ApiKeyConstants.ExampleApiKeyForDocsAndSampleScripts))
                .GetAsyncAssert<ErrorApiResponse>("/api/surveysets", HttpStatusCode.Unauthorized);
            Assert.That(errorApiResponse.Message, Is.EqualTo(UnauthorizedApiKeyResponses.NoReplacementOfTokenFromDocumentation));
        }

        [Test]
        public async Task IncorrectApiKeyLength()
        {
            const string token = "a_token_of_insufficient_length";
            var errorApiResponse = await PublicSurveyApi.WithOnly(AuthHeader("Bearer", token))
                .GetAsyncAssert<ErrorApiResponse>("/api/surveysets", HttpStatusCode.Unauthorized);
            Assert.That(errorApiResponse.Message, Is.EqualTo(UnauthorizedApiKeyResponses.IncorrectApiKeyLength(token.Length)));
        }

        [Test]
        public async Task IncorrectApiKeyOfCorrectLength()
        {
            string token = new string(Enumerable.Repeat('a', 64).ToArray());
            var errorApiResponse = await PublicSurveyApi.WithOnly(AuthHeader("Bearer", token))
                .GetAsyncAssert<ErrorApiResponse>("/api/surveysets", HttpStatusCode.Unauthorized);
            Assert.That(errorApiResponse.Message, Is.EqualTo(UnauthorizedApiKeyResponses.NotAuthenticated));
        }

        private static Action<HttpClient> AuthHeader(string scheme, string parameter)
        {
            var authHeader = new AuthenticationHeaderValue(scheme, parameter);
            return httpClient => httpClient.DefaultRequestHeaders.Authorization = authHeader;
        }
    }
}

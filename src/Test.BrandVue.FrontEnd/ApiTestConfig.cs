using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BrandVue.PublicApi;
using BrandVue.PublicApi.Models;
using BrandVue.Services;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Vue.AuthMiddleware.Api;

namespace Test.BrandVue.FrontEnd
{
    public class ApiTestConfig
    {
        private readonly bool _isPublicSurveyApi;
        public Action<TestServer, HttpClient> SetDefaultHttpClientOptions { get; }

        public string Root { get; }

        public ApiTestConfig(string root, Action<TestServer, HttpClient> defaultHttpClientOptions = null)
        {
            Root = root;
            _isPublicSurveyApi = Root == PublicApiConstants.ApiRoot;
            SetDefaultHttpClientOptions = defaultHttpClientOptions ?? AddAuth;
        }

        public ApiTestConfig With(Action<HttpClient> configureClient)
        {
            return new ApiTestConfig(Root, (_, httpClient) => configureClient(httpClient));
        }

        private void AddAuth(TestServer server, HttpClient client)
        {
            if (_isPublicSurveyApi) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKeyConstants.DebugApiKey);
        }

        public async Task<TContent> ReadAsync<TContent>(HttpResponseMessage actualResponse) where TContent : class
        {
            string stringContent = await actualResponse.Content.ReadAsStringAsync();
            if (typeof(TContent) == typeof(string))
            {
                return (TContent) (object) stringContent;
            }
            if (_isPublicSurveyApi && actualResponse.StatusCode == HttpStatusCode.OK)
            {
                return Deserialize<ApiResponse<TContent>>(stringContent).Value;
            }
            return Deserialize<TContent>(stringContent);
        }

        private static TContent Deserialize<TContent>(string stringContent) where TContent : class =>
            string.IsNullOrEmpty(stringContent) ? null : JsonConvert.DeserializeObject<TContent>(stringContent, BrandVueJsonConvert.Settings);
    }
}
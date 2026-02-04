using IdentityModel.Client;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Vue.Common.BrandVueApi.Models;

namespace Vue.Common.BrandVueApi
{
    public class BrandVueApiClient : IBrandVueApiClient
    {
        private readonly string _apiKey;
        private readonly string _apiBaseUrl;
        private readonly IHttpClientFactory _httpClientFactory;


        public BrandVueApiClient(string apiKey, string apiBaseUrl, IHttpClientFactory httpClientFactory)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _apiBaseUrl = apiBaseUrl ?? throw new ArgumentNullException(nameof(apiBaseUrl));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        private string GetApiServerUrlWithShortCode(string shortCode)
        {
            if (string.IsNullOrEmpty(shortCode))
                return _apiBaseUrl;

            try
            {
                var uri = new UriBuilder(_apiBaseUrl);
                if (uri.Host == "localhost")
                    return _apiBaseUrl;

                uri.Host = $"{shortCode}.{uri.Host}";
                return uri.ToString();
            }
            catch (Exception)
            {
                return _apiBaseUrl;
            }
        }

        public async Task<QuestionsAvailable> GetProjectQuestionsAvailableAsync(string companyShortCode, string productShortCode, string subProductId, CancellationToken cancellationToken)
        {
            var results = new List<List<QuestionWithSurveySets>>();
            var allSubsets = await GetSubsets(companyShortCode, productShortCode, subProductId, cancellationToken);
            var lookupForSubsets = new Dictionary<string, List<SurveySet>>();
            foreach (var subset in allSubsets)
            {
                var url = ConstructEndpointUrl(companyShortCode, productShortCode, subProductId, "questions?includeText=true", subset.SurveySetId);
                var questionsForThisSubset = await MakeRequestAsync<List<QuestionWithSurveySets>>(url, cancellationToken);
                results.Add(questionsForThisSubset);
                foreach (var question in questionsForThisSubset)
                {
                    if (!lookupForSubsets.ContainsKey(question.QuestionId))
                    {
                        lookupForSubsets[question.QuestionId] = new List<SurveySet>();
                    }
                    lookupForSubsets[question.QuestionId].Add(subset);
                }
            }

            var union = results.SelectMany(r => r)
                .GroupBy(q => q.QuestionId)
                .Select(g => QuestionWithSurveySets(g, lookupForSubsets))
                .ToList();
            
            return new QuestionsAvailable(allSubsets, union);
        }

        private static QuestionWithSurveySets QuestionWithSurveySets(IGrouping<string, QuestionWithSurveySets> g, Dictionary<string, List<SurveySet>> lookupForSubsets)
        {
            var question = g.First();
            question.SurveySets = lookupForSubsets[question.QuestionId];
            return question;
        }

        internal virtual async Task<List<SurveySet>> GetSubsets(
            string companyShortCode, 
            string productShortCode, 
            string subProductId, 
            CancellationToken cancellationToken
        )
        {
            var apiUrl = GetApiServerUrlWithShortCode(companyShortCode);
            var request = $"{apiUrl}{productShortCode}/{subProductId}/api/surveysets";
            return await MakeRequestAsync<List<SurveySet>>(request, cancellationToken);
        }

        private string ConstructEndpointUrl(string companyShortCode, string productShortCode, string subProductId, string endpoint,
            string subset)
        {
            var apiUrl = GetApiServerUrlWithShortCode(companyShortCode);
            return $"{apiUrl}{productShortCode}/{subProductId}/api/surveysets/{subset}/{endpoint}";
        }

        protected virtual async Task<T> MakeRequestAsync<T>(string apiUrl, CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("BrandVueApi");
            client.SetBearerToken(_apiKey);
            using (var request = new HttpRequestMessage(HttpMethod.Get, apiUrl))
            {
                request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
                var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Request to {apiUrl} failed with status code {response.StatusCode}");
                }
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
                return JsonConvert.DeserializeObject<ApiResponse<T>>(responseText).Value;
            }
        }
    }
}

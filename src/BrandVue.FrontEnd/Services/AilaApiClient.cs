using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace BrandVue.Services
{
    public class AilaApiClient : IAilaApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private const string C_BaseUrl = "https://aila.savanta.com/pubapi/v1/";

        public AilaApiClient(IHttpClientFactory httpClientFactory, string ailaApiKey)
        {
            _httpClient = httpClientFactory.CreateClient(nameof(AilaApiClient));
            _apiKey = ailaApiKey;
        }

        /// <inheritdoc/>
        public async Task<string> CreateChatCompletionAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken)
        {
            if (userPrompt is null)
            {
                throw new ArgumentNullException(nameof(userPrompt));
            }

            if(userPrompt.Length == 0   )
            {
                throw new ArgumentException("User prompt cannot be empty", nameof(userPrompt));
            }

            var payload = CreateChatCompletionRequest(userPrompt, systemPrompt);

            var requestJson = JsonSerializer.Serialize(payload);
            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            content.Headers.Add("X-API-Key", _apiKey);
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(C_BaseUrl + "chatcompletions", content, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                throw new AilaApiException("Failed to call Aila API.", ex);
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new AilaApiException($"Failed to call Aila API. The service returned error {response.StatusCode}.");
            }

            try
            {
                var ailaContent = JsonSerializer.Deserialize<AilaCompletionResponse[]>(responseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return ailaContent.Single().Message.Content;
            }
            catch (JsonException)
            {
                throw new AilaApiException("Failed to call Aila API. Could not parse: " + responseJson);
            }
        }

        private object CreateChatCompletionRequest(string userPrompt, string systemPrompt)
        {
            if (systemPrompt is null)
            {
                return new
                {
                    messages = new object[]
                    {
                        new
                        {
                            role = "user",
                            content = userPrompt
                        }
                    }
                };
            }

            return new
            {
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = systemPrompt
                    },
                    new
                    {
                        role = "user",
                        content = userPrompt
                    }
                }
            };
        }

        public class AilaCompletionResponse
        {
            public AilaCompletionMessage Message { get; set; }
        }

        public class AilaCompletionMessage
        {
            public string Content { get; set; }
        }

        public class AilaApiException : Exception
        {
            public AilaApiException(string message) : base(message) { }
            public AilaApiException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}

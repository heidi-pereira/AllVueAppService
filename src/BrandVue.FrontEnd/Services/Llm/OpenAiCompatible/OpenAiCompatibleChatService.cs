
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using BrandVue.Services.Llm.Interfaces;
using Microsoft.Extensions.Options;

namespace BrandVue.Services.Llm.OpenAiCompatible;

/// <summary>
/// Allows the consumption of any OpenAI-compatible API such as Portkey.
/// </summary>
public class OpenAiCompatibleChatService : IChatCompletionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly string _summaryPromptId;
    private readonly bool _portkeyNoTracking;

    public OpenAiCompatibleChatService(IHttpClientFactory httpClientFactory, IOptions<OpenAiCompatibleChatServiceSettings> clientSettings)
    {
        _httpClientFactory = httpClientFactory;

        _apiKey = clientSettings.Value.ApiKey ?? throw new ArgumentNullException(nameof(clientSettings.Value.ApiKey));
        _model = clientSettings.Value.Model ?? throw new ArgumentNullException(nameof(clientSettings.Value.Model));
        _baseUrl = clientSettings.Value.BaseUrl ?? throw new ArgumentNullException(nameof(clientSettings.Value.BaseUrl));
        _summaryPromptId = clientSettings.Value.SummaryPromptId ?? throw new ArgumentNullException(nameof(clientSettings.Value.SummaryPromptId));
        _portkeyNoTracking = clientSettings.Value.PortkeyNoTracking;
    }

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(_baseUrl);
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        if (_portkeyNoTracking)
        {
            client.DefaultRequestHeaders.Add("x-portkey-debug", "false");
        }
        return client;
    }

    public async Task<ChatCompletionMessage> GetChatCompletionAsync(IEnumerable<ChatCompletionMessage> messages, CancellationToken cancellationToken)
    {
        using var client = CreateHttpClient();

        var payload = new
        {
            model = _model,
            messages = messages.Select(m => new
            {
                role = m.Role.ToString().ToLower(),
                content = m.Content
            }).ToArray(),
            stream=false
        };

        var response = await client.PostAsync("chat/completions", new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Error: {response.StatusCode}, Content: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize<OpenAiCompatibleResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

        if (openAiResponse == null || openAiResponse.Choices == null || openAiResponse.Choices.Count == 0)
        {
            throw new Exception($"Invalid response from Gemini API: {openAiResponse}");
        }

        return new ChatCompletionMessage
        {
            Role = ChatRole.Assistant,
            Content = openAiResponse.Choices[0].Message.Content
        };
    }

    public async Task<ChatCompletionMessage> GetSurveyResponseSummary(string context, string locale, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(locale);

        using var client = CreateHttpClient();

        var payload = new
        {
            stream = false,
            variables = new
            {
                locale = locale,
                text = context
            }
        };

        var response = await client.PostAsync(
            $"prompts/{_summaryPromptId}/completions",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Error: {response.StatusCode}, Content: {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var openAiResponse = JsonSerializer.Deserialize<OpenAiCompatibleResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });

        if (openAiResponse == null || openAiResponse.Choices == null || openAiResponse.Choices.Count == 0)
        {
            throw new Exception($"Invalid response from Portkey API: {openAiResponse}");
        }

        return new ChatCompletionMessage
        {
            Role = ChatRole.Assistant,
            Content = openAiResponse.Choices[0].Message.Content
        };
    }

    public class OpenAiCompatibleResponse
    {
        public List<OpenAiChoice> Choices { get; set; }
    }

    public class OpenAiChoice
    {
        public OpenAiMessage Message { get; set; }
    }

    public class OpenAiMessage
    {
        public string Content { get; set; }
    }
}
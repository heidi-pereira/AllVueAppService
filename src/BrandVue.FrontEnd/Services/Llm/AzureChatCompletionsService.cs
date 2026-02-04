using Azure.AI.OpenAI;
using OpenAI;
using System.ClientModel.Primitives;
using System.ClientModel;
using OpenAI.Chat;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Options;

namespace BrandVue.Services.Llm;

public class AzureChatCompletionService : IAzureChatCompletionService
{
    private readonly AzureAiClientSettings _clientSettings;
    private readonly OpenAIClient _client;

    public AzureChatCompletionService(IOptions<AzureAiClientSettings> clientSettings, IHttpClientFactory httpClientFactory)
    {
        _clientSettings = clientSettings.Value;
        var options = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2024_06_01)
        {
            Transport = new HttpClientPipelineTransport(httpClientFactory.CreateClient()),
            RetryPolicy = new ClientRetryPolicy(_clientSettings.MaxRetries),
            NetworkTimeout = TimeSpan.FromSeconds(_clientSettings.DefaultTimeout),
            Endpoint = new Uri(_clientSettings.Endpoint),
        };
        _client = new AzureOpenAIClient(new Uri(_clientSettings.Endpoint), new ApiKeyCredential(_clientSettings.Key), options);
    }

    public async Task<ChatCompletion> GetChatCompletionAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options, CancellationToken cancellationToken)
    {
        var chatClient = _client.GetChatClient(_clientSettings.Deployment);
        ClientResult<ChatCompletion> result;

        result = await chatClient.CompleteChatAsync(messages, options, cancellationToken);

        return result.Value;
    }

}

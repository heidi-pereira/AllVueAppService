using BrandVue.Settings;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net.Http;
using System.Threading;

namespace BrandVue.Services.Llm;

/// <summary>
/// Represents a client for the AI Document Ingestor API.
/// </summary>
/// <remarks>
/// Process documents for AI ingestion, but also includes a PowerPoint annotation service. See NextGen team for details. 
/// </remarks>
public interface IAiDocumentIngestorApiClient
{
    /// <summary>
    /// Takes a PowerPoint file and returns a stream of the mutated PowerPoint file.
    /// </summary>
    /// <remarks>
    /// The API will change the value of one heading and one body element per slide
    /// if a those slides have suitable-looking text elements to replace. If not,
    /// the service will inject a new elements as needed.
    /// </remarks>
    Task<Stream> AnnotatePowerPointAsync(Stream file, bool injectExecutiveSummary, string pageRange, CancellationToken cancellationToken);
}

public class AiDocumentIngestorApiClient : IAiDocumentIngestorApiClient
{
    private readonly Uri _baseUri;
    private readonly string _apiKey;
    private readonly IHttpClientFactory _httpClientFactory;

    public AiDocumentIngestorApiClient(IOptions<AiDocumentIngestorApiClientSettings> settings, IHttpClientFactory httpClientFactory)
    {
        _apiKey = settings?.Value.ApiKey ?? throw new ArgumentException("API Key cannot be null");
        _baseUri = new Uri(settings.Value.BaseUrl);
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task<Stream> AnnotatePowerPointAsync(Stream file, bool injectExecutiveSummary, string pageRange, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        // Bearer token
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        // 15 minute timeout
        client.Timeout = TimeSpan.FromMinutes(15);

        var formData = new MultipartFormDataContent
        {
            { new StreamContent(file), "file", "file.pptx" },
            { new StringContent("gpt-4o"), "modelId" },
            { new StringContent("false"), "useVision" },
            { new StringContent(injectExecutiveSummary.ToString().ToLowerInvariant()), "injectExecutiveSummary" },
            { new StringContent(pageRange), "pageRange" },
        };

        var path = new Uri(_baseUri, "v1/annotate");

        var response = await client.PostAsync(path, formData, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }
}

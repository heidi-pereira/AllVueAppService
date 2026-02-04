using NUnit.Framework;
using NSubstitute;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using BrandVue.Services;
using Microsoft.AspNetCore.Http;
using System;

namespace Test.BrandVue.FrontEnd.Services;

[TestFixture]
public class AilaApiClientTests
{
    private TestServer _server;
    private HttpClient _httpClient;
    private int _port;
    private const string ApiKey = "test-api-key";
    private const string TestResponse = "This is a test response";
    private const string ChatCompletionPath = "/pubapi/v1/chatcompletions";

    [SetUp]
    public void Setup()
    {
        _port = new Random().Next(49152, 65535); // Random port in the ephemeral port range.
        _server = new TestServer(new WebHostBuilder()
            .UseUrls($"http://localhost:{_port}")
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    if (context.Request.Path == ChatCompletionPath && context.Request.Method == "POST")
                    {
                        var apiKey = context.Request.Headers["X-API-Key"].ToString();
                        if (apiKey != ApiKey)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                            return;
                        }

                        var response = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    content = TestResponse
                                }
                            }
                        };

                        await context.Response.WriteAsJsonAsync(response);
                    }
                    else
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                });
            }));

        _httpClient = _server.CreateClient();
    }

    [TearDown]
    public void TearDown()
    {
        _server.Dispose();
        _httpClient.Dispose();
    }

    [Test]
    public async Task CreateChatCompletionAsync_ValidRequest_ReturnsExpectedResponse()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        var client = new AilaApiClient(httpClientFactory, ApiKey);

        // Act
        var result = await client.CreateChatCompletionAsync("Test prompt", null, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(TestResponse));
    }

    [Test]
    public void CreateChatCompletionAsync_NullUserPrompt_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var client = new AilaApiClient(httpClientFactory, ApiKey);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.CreateChatCompletionAsync(null, null, CancellationToken.None));
    }

    [Test]
    public void CreateChatCompletionAsync_EmptyUserPrompt_ThrowsArgumentException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var client = new AilaApiClient(httpClientFactory, ApiKey);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() =>
            client.CreateChatCompletionAsync("", null, CancellationToken.None));
    }

    [Test]
    public async Task CreateChatCompletionAsync_UnauthorizedRequest_ThrowsAilaApiException()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        httpClientFactory.CreateClient(Arg.Any<string>()).Returns(_httpClient);

        var client = new AilaApiClient(httpClientFactory, "wrong-api-key");

        // Act & Assert
        var ex = Assert.ThrowsAsync<AilaApiClient.AilaApiException>(() =>
            client.CreateChatCompletionAsync("Test prompt", null, CancellationToken.None));
        Assert.That(ex.Message, Does.Contain("Unauthorized"));
    }
}
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

internal class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpResponseMessage _response;

    public FakeHttpMessageHandler(HttpResponseMessage response)
    {
        _response = response;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // You can inspect 'request' and return different responses if needed
        return Task.FromResult(_response);
    }
}
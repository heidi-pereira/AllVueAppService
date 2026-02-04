using Microsoft.AspNetCore.Http;

namespace Vue.Common.Extensions;

public static class HttpRequestExtensions
{
    public static Uri OriginalUrl(this HttpRequest httpRequest) =>
    httpRequest.Headers.TryGetValue("X-MS-ORIGINAL-URL", out var originalUrl) ? new Uri(originalUrl.First()) : new Uri(httpRequest.Scheme + "://" + httpRequest.Host + httpRequest.Path);
}

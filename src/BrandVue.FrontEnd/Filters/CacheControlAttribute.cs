using BrandVue.EntityFramework.Exceptions;
using BrandVue.Services;
using BrandVue.SourceData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace BrandVue.Filters
{
    public class CacheControlAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// It is standard practice to vary on this header
        /// </summary>
        private readonly string[] _baseVaryHeaders = { "Accept-Encoding" };

        /// <summary>
        /// Gets or sets whether the response should never be cached. If this is true duration is ignored
        /// </summary>
        public bool NoStore { get; set; } = false;

        /// <summary>
        /// These tell the client that cache entry keys should be formed from the values of these headers when making requests.
        /// It is essential that these exact strings are sent as request headers from the client for this to work.
        /// </summary>
        public string[] VaryOn { get; set; } = Array.Empty<string>();

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var typedHeaders = context.HttpContext.Response.GetTypedHeaders();
            if (NoStore)
            {
                typedHeaders.CacheControl = new CacheControlHeaderValue { NoStore = true, MaxAge = TimeSpan.Zero, SharedMaxAge = TimeSpan.Zero };
            }
            else
            {
                var webAppSettings = context.HttpContext.RequestServices.GetRequiredService<InitialWebAppConfig>();
                if (HttpMethods.IsGet(context.HttpContext.Request.Method) && webAppSettings.BrowserCachingEnabled)
                {
                    typedHeaders.CacheControl = new CacheControlHeaderValue { Private = true };
                    typedHeaders.Expires = DateTimeOffset.UtcNow.EndOfDay();
                    context.HttpContext.Response.Headers[HeaderNames.Vary] = _baseVaryHeaders.Concat(VaryOn).ToArray();
                    //Clear the pragma header to support HTTP/1.0.
                    context.HttpContext.Response.Headers[HeaderNames.Pragma] = StringValues.Empty;
                }
            }

            base.OnActionExecuting(context);
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                // Don't cache exception so can refresh browser to try again
                context.HttpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue { NoStore = true, MaxAge = TimeSpan.Zero, SharedMaxAge = TimeSpan.Zero };

                if (context.Exception.GetBaseException() is TooBusyException)
                {
                    // Convert a TooBusyException to a 503 Service Unavailable
                    context.Exception = null;
                    context.HttpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                }
            }

            base.OnActionExecuted(context);
        }
    }
}
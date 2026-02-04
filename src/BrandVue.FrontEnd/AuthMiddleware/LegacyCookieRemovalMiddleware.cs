using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Vue.AuthMiddleware;

namespace BrandVue.AuthMiddleware
{
    public class LegacyCookieRemovalMiddleware
    {
        private readonly RequestDelegate _next;

        public LegacyCookieRemovalMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        [DebuggerStepThrough]
        public async Task Invoke(HttpContext httpContext)
        {
            var rootPath = "/";
            var appBasePath = httpContext.Request.PathBase.Value ?? rootPath;

            // only clear the legacy cookie from path "/" if we know what the app base path is and it is definitely not "/"
            if (!string.IsNullOrWhiteSpace(appBasePath) && !string.Equals(appBasePath,rootPath, StringComparison.OrdinalIgnoreCase))
            {
                var cookieOptions = new CookieOptions
                {
                    // identifies the old cookie from the path it used to use
                    Path = rootPath
                };

                // remove main legacy cookie:
                httpContext.Response.Cookies.Delete(Constants.CookieName, cookieOptions);
                // remove legacy chunk 1:
                httpContext.Response.Cookies.Delete($"{Constants.CookieName}C1", cookieOptions);
                // remove legacy chunk 2:
                httpContext.Response.Cookies.Delete($"{Constants.CookieName}C2", cookieOptions);
            }

            await _next.Invoke(httpContext);
        }
    }
}
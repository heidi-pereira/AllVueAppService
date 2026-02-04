using BrandVue.SourceData.Import;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BrandVue.Filters
{
    /// <summary>
    /// Allow client to opt out of caching when the API version has changed. Also see VueApiInfo.ts.
    /// </summary>
    internal class IncludeBrowserCacheKeyAttribute : ResultFilterAttribute
    {
        private const string ServerVueApiVersion = "ServerVueApiVersion"; //Must match VueApiInfo.serverVersionHeaderName in the typescript

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            string serverVersion = context.HttpContext.RequestServices.GetRequiredService<ISubProductBrowserCacheKeyTracker>().GetCurrent();
            //Include the server version in any MVC action
            context.HttpContext.Response.Headers.Add(ServerVueApiVersion, serverVersion);
            base.OnResultExecuting(context);
        }
    }
}
using BrandVue.SourceData.Import;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace BrandVue.Filters
{
    /// <summary>
    /// Add this to an action to invalidate any browser caches for subsequent data requests
    /// </summary>
    internal class InvalidateBrowserCacheAttribute : ResultFilterAttribute
    {
        public InvalidateBrowserCacheAttribute()
        {
            //Execute this filter early so subsequent filters get the updated value
            Order = int.MinValue;
        }

        public override void OnResultExecuting(ResultExecutingContext context)
        {
            context.HttpContext.RequestServices.GetRequiredService<ISubProductBrowserCacheKeyTracker>().Update();
            base.OnResultExecuting(context);
        }
    }
}
using BrandVue.EntityFramework;
using BrandVue.Middleware;
using BrandVue.SourceData;
using Microsoft.AspNetCore.Http;

namespace BrandVue
{
    public static class FreshchatHelper
    {
        public static bool FreshchatEnabled(AppSettings appSettings, HttpContext httpContext, bool export)
        {
            if (export)
            {
                return false;
            }

            var productName = httpContext.GetOrCreateRequestScope().ProductName;

            var freshchatEnvironments = appSettings.GetSetting("freshchatEnvironments").Split(',');
            var freshchatProducts = appSettings.GetSetting("freshchatProducts").Split(',');

            return freshchatProducts.Any(p=>p.Equals(productName, StringComparison.InvariantCultureIgnoreCase)) 
                   && appSettings.IsDeployedEnvironmentOneOfThese(freshchatEnvironments);
        }
    }
}
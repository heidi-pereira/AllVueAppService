using BrandVue.Middleware;
using Microsoft.AspNetCore.Http;

namespace Vue.AuthMiddleware.OAuth.OpenIdConnect
{
    public static class Notifications
    {
        public static string GetRedirectUri(HttpContext context)
        {

            return GetAbsoluteAppBaseUrl(context) + Constants.OpenIdConnectSignInPath;
        }

        private static string GetAbsoluteAppBaseUrl(HttpContext context)
        {
            string requestPathBase = context.Request.PathBase;
            return context.Request.Scheme +
                   Uri.SchemeDelimiter +
                   context.Request.Host +
                   requestPathBase;
        }
    }
}
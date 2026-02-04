using System.Net;
using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Http;
using Vue.AuthMiddleware;

namespace BrandVue.Middleware
{
    public static class ContextExtensions
    {
        public static RequestScope GetOrCreateRequestScope(this HttpContext context)
        {
            var requestScope = context.Items.TryGetValue("RequestScope", out var cached)
                ? cached : context.Items["RequestScope"] = CreateRequestScope(context);
            return (RequestScope) requestScope;
        }

        public static bool IsRequestedResourceApi(this HttpContext context)
        {
            var requestedResource = context.GetOrCreateRequestScope().Resource;
            return requestedResource == RequestResource.InternalApi || requestedResource == RequestResource.PublicApi;
        }

        private static RequestScope CreateRequestScope(HttpContext context)
        {
            var isLocal = IsLocal(context);
            var currentRequest = context.Request;

            return context.GetService<RequestScopeRetriever>().GetRequestScope(currentRequest.Host, currentRequest.PathBase, currentRequest.Path, isLocal,
                false, OrganisationForReportingOnBehalfOf(context));
        }

        public static bool IsTestEnv(this HttpContext context)
        {
            return new[]
            {
                ".test-vue-te.ch",
                ".test.all-vue.com"
            }.Any(hostSuffix => context.Request.Host.Value.EndsWith(hostSuffix, StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool IsLocalWithAuthBypass(this HttpContext context)
        {
            return context.GetService<AppSettings>().AllowLocalToBypassConfiguredAuthServer && IsLocal(context);
        }

        public static bool HasAuthHeader(this HttpContext context)
        {
            return context.Request.Headers.ContainsKey("Authorization");
        }

        private static string OrganisationForReportingOnBehalfOf(HttpContext context)
        {
            if (isBvReportingCallThroughHeader(context) || isBvReportingCallThroughQuery(context))
            {
                if (context.Request.Headers.TryGetValue("X-BVOrg", out var organisationByHeader) &&
                    !string.IsNullOrEmpty(organisationByHeader))
                {
                    return organisationByHeader;
                }
                if (context.Request.Query.TryGetValue("BVOrg", out var organisationByQuery) &&
                    !string.IsNullOrEmpty(organisationByQuery)) 
                { 
                    return organisationByQuery; 
                }
            }
            return null;
        }

        private static bool IsLocal(HttpContext context)
        {
            bool isLoopback = context.Connection.LocalIpAddress is not null &&
                              IPAddress.IsLoopback(context.Connection.LocalIpAddress);

            return isLoopback
                   || isBvReportingCallThroughHeader(context)
                   || isBvReportingCallThroughQuery(context)
                   || Equals(context.Connection.RemoteIpAddress, context.Connection.LocalIpAddress);
        }

        private static bool isBvReportingCallThroughHeader(HttpContext context)
        {
            return context.Request.Headers.TryGetValue("X-BVReporting", out var headerAccessToken) &&
                            headerAccessToken == context.GetService<AppSettings>().ReportingApiAccessToken;
        }

        private static bool isBvReportingCallThroughQuery(HttpContext context)
        {
            return context.Request.Query.TryGetValue("BVReporting", out var queryAccessToken) &&
                queryAccessToken == context.GetService<AppSettings>().ReportingApiAccessToken;
        }

        public static string GetBasePathIncludingSubProduct(this HttpContext httpContext)
        {
            var appBasePath = (httpContext.Request.PathBase.Value ?? "").TrimEnd('/');
            if (RouteConfig.UseSubProductPathPrefix)
            {
                appBasePath = appBasePath + "/" + httpContext.GetOrCreateRequestScope().SubProduct;
            }

            return appBasePath;
        }

        public static string GetBaseUrl(this HttpContext httpContext) => $"{httpContext.Request.Scheme}{Uri.SchemeDelimiter}{httpContext.Request.Host}".TrimEnd('/');

        public static string GetUrlIncludingSubProduct(this HttpContext httpContext)
        {
            return $"{httpContext.Request.Scheme}{Uri.SchemeDelimiter}{httpContext.Request.Host}{GetBasePathIncludingSubProduct(httpContext)}";
        }
    }
}
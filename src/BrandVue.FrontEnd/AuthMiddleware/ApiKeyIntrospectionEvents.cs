using BrandVue.Middleware;
using Microsoft.AspNetCore.Http;
using Vue.AuthMiddleware.Api;

namespace Vue.AuthMiddleware
{
    internal static class ApiKeyIntrospectionEvents
    {
        public static bool IsValidLocalDebugApiKey(HttpContext context)
        {
            if (!context.IsLocalWithAuthBypass()) return false;

            string authHeaderValue = context.Request.Headers["Authorization"];
            var caseSensitiveForApiKey = StringComparison.Ordinal;
            return authHeaderValue != null && !ApiKeyValidator.NoBearerPrefixBeforeToken(authHeaderValue)
                                           && ApiKeyValidator.ExtractApiKeyFromAuthorizationHeader(authHeaderValue).Equals(ApiKeyConstants.DebugApiKey, caseSensitiveForApiKey);
        }
    }
}
using Vue.AuthMiddleware.Api;
using Microsoft.AspNetCore.Http;

namespace Vue.AuthMiddleware
{
    public static class ApiKeyValidator
    {
        public static bool IsValidAuthorizationHeader(HttpContext context, out string validationFailureMessage)
        {
            validationFailureMessage = "";
            if (NoAuthorizationHeaderPresent(context))
            {
                validationFailureMessage = UnauthorizedApiKeyResponses.NoAuthorizationHeaderPresent;
                return false;
            }
            string authorizationHeader = context.Request.Headers["Authorization"];
            if (NoBearerPrefixBeforeToken(authorizationHeader))
            {
                validationFailureMessage = UnauthorizedApiKeyResponses.NoBearerPrefixBeforeToken;
                return false;
            }
            string authorizationHeaderApiKey = ExtractApiKeyFromAuthorizationHeader(authorizationHeader);
            if (NoReplacementOfTokenFromDocumentation(authorizationHeaderApiKey))
            {
                validationFailureMessage = UnauthorizedApiKeyResponses.NoReplacementOfTokenFromDocumentation;
                return false;
            }

            if (IncorrectApiKeyLength(authorizationHeaderApiKey))
            {
                validationFailureMessage = UnauthorizedApiKeyResponses.IncorrectApiKeyLength(authorizationHeaderApiKey.Length);
                return false;
            }

            return true;
        }

        private static bool NoAuthorizationHeaderPresent(HttpContext context) =>
            !context.Request.Headers.ContainsKey("Authorization");

        public static bool NoBearerPrefixBeforeToken(string token) =>
            !token.StartsWith(ApiKeyConstants.BearerTokenStringPrefix, StringComparison.OrdinalIgnoreCase);

        private static bool NoReplacementOfTokenFromDocumentation(string token) =>
            token.Contains(ApiKeyConstants.ExampleApiKeyForDocsAndSampleScripts);

        private static bool IncorrectApiKeyLength(string token) =>
            token.Length != ApiKeyConstants.ValidApiKeyStringLengthIdentityServer3 &&
             token.Length != ApiKeyConstants.ValidApiKeyStringLengthIdentityServer4 &&
            !token.Equals(ApiKeyConstants.DebugApiKey);

        public static string ExtractApiKeyFromAuthorizationHeader(string authorizationString) =>
            authorizationString.Substring(ApiKeyConstants.BearerTokenStringPrefix.Length);
    }
}

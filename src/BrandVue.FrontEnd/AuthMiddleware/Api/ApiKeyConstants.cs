using System.Security.Claims;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware.Api
{
    public static class ApiKeyConstants
    {
        public const string ExampleApiKeyForDocsAndSampleScripts = "YourApiKey";
        public const string BearerTokenStringPrefix = "Bearer ";

        public const string CorrectAuthorizationHeaderUsageDescription = "The authorization header should be of the form 'Bearer YourApiKey', where 'YourApiKey' should be replaced with the key given to you by your Savanta Account Manager.";

        public const short ValidApiKeyStringLengthIdentityServer3 = 64;
        public const short ValidApiKeyStringLengthIdentityServer4 = 43;
        public const string DebugApiKey = "ThisIsTheDebugApiKey";
        public const string DefaultClaimValue = "ApiAccess";

        public static Claim[] GetPublicApiClaims(string product, string[] resources)
        {
            string productKeysEmptyStringValue =  $"\"{product}\": [\"{string.Join("\", \"", resources)}\"]";
            return new[] { new Claim(OptionalClaims.BrandVueApi, $"{{{productKeysEmptyStringValue}}}") };
        }
    }
}
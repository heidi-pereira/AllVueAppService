namespace Vue.AuthMiddleware.Api
{
    public static class UnauthorizedApiKeyResponses
    {
        public static string NoAuthorizationHeaderPresent { get; } =
            $"The request has no authorization header. Please supply an authorization header. {ApiKeyConstants.CorrectAuthorizationHeaderUsageDescription}";

        public static string NoBearerPrefixBeforeToken { get; } =
            $"'{ApiKeyConstants.BearerTokenStringPrefix}' prefix missing from authorization header. {ApiKeyConstants.CorrectAuthorizationHeaderUsageDescription}";

        public static string NoReplacementOfTokenFromDocumentation { get; } =
            $"The authorization header contained the literal string '{ApiKeyConstants.ExampleApiKeyForDocsAndSampleScripts}' rather than your api key. {ApiKeyConstants.CorrectAuthorizationHeaderUsageDescription}";

        public static string IncorrectApiKeyLength(int length) =>
            $"The authorization header api key was {length} characters long. Valid api keys should be {ApiKeyConstants.ValidApiKeyStringLengthIdentityServer3} or {ApiKeyConstants.ValidApiKeyStringLengthIdentityServer4} characters long. {ApiKeyConstants.CorrectAuthorizationHeaderUsageDescription}";

        public const string NotAuthenticated = "Unrecognized API key. Pass the API key given by your account manager in the Authorization header in the form 'Bearer YourToken' and check the domain is correct";
    }
}

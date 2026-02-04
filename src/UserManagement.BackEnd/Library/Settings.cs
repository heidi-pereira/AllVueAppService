using BrandVue.EntityFramework;

namespace UserManagement.BackEnd.Library
{
    public class Settings
    {
        public required string AnswersConnectionString { get; set; }
        public required string MetadataConnectionString { get; set; }
        public string? OverrideLocalOrg { get; set; }
        public required string AuthAuthority { get; set; }
        public required string AuthClientSecret { get; set; }
        public required string AuthClientId { get; set; }
        public required string ApplicationBasePath { get; set; }
        public required string MixPanelToken { get; set; }
        public string RunningEnvironment { get; set; }
        public string RunningEnvironmentDescription { get; set; }
        public string BrandVueApiKey { get; set; }
        public string BrandVueApiBaseUrl { get; set; }
    }
    public static class AppSettingsExtensions
    {
        public static bool IsAuthServerConfigured(this Settings appSettings) =>
            !string.IsNullOrWhiteSpace(appSettings.AuthServerUrl());

        private static string AuthServerUrl(this Settings appSettings) => appSettings.AuthAuthority?.TrimEnd('/');

        public static string GetAuthServerUrl(this Settings appSettings)
        {
            if (!appSettings.IsAuthServerConfigured()) return "https://test.all-vue.com/auth";
            return appSettings.AuthServerUrl();
        }

        public static string GetAuthServerUrlWithShortCode(this Settings appSettings, string shortCode)
        {
            var baseUrl = GetAuthServerUrl(appSettings);

            try
            {
                var uri = new UriBuilder(baseUrl);
                uri.Host = $"{shortCode}.{uri.Host}";
                return uri.ToString();
            }
            catch (Exception)
            {
                return baseUrl;
            }
        }
    }
}

using BrandVue.EntityFramework;

namespace Vue.AuthMiddleware
{
    public static class AppSettingsExtensions
    {
        public static string GetAuthServerUrl(this AppSettings appSettings)
        {
            if (!appSettings.IsAuthServerConfigured()) return "https://test.all-vue.com/auth";
            return appSettings.AuthServerUrl();
        }

        public static string GetAuthServerUrlWithShortCode(this AppSettings appSettings, string shortCode)
        {
            var baseUrl = GetAuthServerUrl(appSettings);
            if (string.Equals(appSettings.AppDeploymentEnvironment, AppSettings.DevEnvironmentName, StringComparison.OrdinalIgnoreCase))
            {
                return baseUrl;
            }

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

        public static bool IsAuthServerConfigured(this AppSettings appSettings) =>
            !string.IsNullOrWhiteSpace(appSettings.AuthServerUrl());

        private static string AuthServerUrl(this AppSettings appSettings) => appSettings.GetSetting("authServerUrl")?.TrimEnd('/');
    }
}
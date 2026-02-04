using System.Configuration;
using BrandVue.EntityFramework;
using BrandVue.RouteConstraints;

namespace BrandVue
{
    public class RouteConfig
    {
        private const string SubProductRouteWithConstraint = "{subproduct:" + ValidSubproductConstraint.Key + "}/";

        static RouteConfig()
        {
            // Do not follow this pattern of creating app settings, you can easily resolve it using constructor injection, or from the context with GetService.
            // This is a special case because we need to use this for the value of attributes, which are created statically before most other code runs
            var appSettings = new AppSettings();
            string settingText = appSettings.GetSetting("useSubProductPathPrefix");
            bool.TryParse(settingText, out bool useSubProductPathPrefix);
            // We never want the brandvue api to be compiled with this prefix even if someone messes with their local app settings rather than using user secrets
            useSubProductPathPrefix = useSubProductPathPrefix && !AppSettings.IsCompileTime;
            UseSubProductPathPrefix = useSubProductPathPrefix;
            PathPrefix = useSubProductPathPrefix ? SubProductRouteWithConstraint : "";
        }

        public static bool UseSubProductPathPrefix { get; }
        public static string PathPrefix { get; }
    }
}
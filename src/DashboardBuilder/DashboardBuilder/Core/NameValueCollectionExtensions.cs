using System.Collections.Specialized;

namespace DashboardBuilder.Core
{
    internal static class NameValueCollectionExtensions
    {
        public static string GetTrimmedString(this NameValueCollection nameValueCollection, string settingName)
        {
            return nameValueCollection[settingName]?.Trim();
        }

        public static bool GetBool(this NameValueCollection nameValueCollection, string settingName)
        {
            return bool.Parse(nameValueCollection[settingName]);
        }
    }
}
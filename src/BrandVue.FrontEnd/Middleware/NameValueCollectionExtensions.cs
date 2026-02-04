using System.Collections.Specialized;

namespace BrandVue.Middleware
{
    public static class NameValueCollectionExtensions
    {
        public static bool GetBool(this NameValueCollection nameValueCollection, string key) =>
            bool.TryParse(nameValueCollection[key], out bool isTrue) && isTrue;
    }
}
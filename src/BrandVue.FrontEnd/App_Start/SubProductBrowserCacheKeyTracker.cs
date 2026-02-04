using System.Reflection;
using BrandVue.SourceData.Import;

namespace BrandVue
{
    public class SubProductBrowserCacheKeyTracker : ISubProductBrowserCacheKeyTracker
    {
        private readonly string _serverVersion;
        private string _metaVersion;

        public SubProductBrowserCacheKeyTracker()
        {
            _serverVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";
            Update();
        }

        public string GetCurrent() => $"{_serverVersion}-{_metaVersion}";

        public void Update() => _metaVersion = DateTime.Now.ToString("yyyyMMddhhmmss");
    }
}
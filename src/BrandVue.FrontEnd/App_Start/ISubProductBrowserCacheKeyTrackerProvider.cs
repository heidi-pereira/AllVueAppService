using BrandVue.SourceData.Import;

namespace BrandVue
{
    public interface ISubProductBrowserCacheKeyTrackerProvider
    {
        ISubProductBrowserCacheKeyTracker SubProductBrowserCacheKeyTracker { get; }
    }
}

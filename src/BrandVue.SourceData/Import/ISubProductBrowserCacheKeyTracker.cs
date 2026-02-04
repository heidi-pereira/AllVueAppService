namespace BrandVue.SourceData.Import
{
    public interface ISubProductBrowserCacheKeyTracker
    {
        string GetCurrent();
        void Update();
    }
}
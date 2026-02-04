using Microsoft.Extensions.Caching.Memory;

namespace BrandVue.Services.Exporter
{
    public interface IExportFileCache
    {
        IMemoryCache GetCache();
        MemoryCacheEntryOptions GetEntryOptions();
    }

    public class ExportFileCache : IExportFileCache
    {
        private readonly IMemoryCache _tableExportCache;

        public ExportFileCache()
        {
            _tableExportCache = new MemoryCache(new MemoryCacheOptions());
        }

        public IMemoryCache GetCache() => _tableExportCache;
        public MemoryCacheEntryOptions GetEntryOptions() => new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(2))
            .SetSlidingExpiration(TimeSpan.FromMinutes(10));
    }
}

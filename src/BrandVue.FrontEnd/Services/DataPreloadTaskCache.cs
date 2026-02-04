using Microsoft.Extensions.Caching.Memory;

namespace BrandVue.Services
{
    public interface IDataPreloadTaskCache
    {
        IMemoryCache GetCache();
        MemoryCacheEntryOptions GetEntryOptions();
    }

    public class DataPreloadTaskCache : IDataPreloadTaskCache
    {
        private readonly IMemoryCache _taskCache;

        public DataPreloadTaskCache()
        {
            _taskCache = new MemoryCache(new MemoryCacheOptions());
        }

        public IMemoryCache GetCache() => _taskCache;

        public MemoryCacheEntryOptions GetEntryOptions() => new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(2))
            .SetSlidingExpiration(TimeSpan.FromMinutes(10));
    }
}

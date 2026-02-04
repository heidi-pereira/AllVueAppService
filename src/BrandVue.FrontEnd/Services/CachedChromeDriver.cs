using System.Runtime.Caching;
using OpenQA.Selenium.Chrome;

namespace BrandVue.Services
{
    public class CachedChromeDriver : IDisposable
    {
        private const int ChromeCacheSeconds = 10;
        private const int MaxCacheSize = 10;
        private static readonly ObjectCache Cache = new MemoryCache("ChromeDriver");
        private static readonly object LockObject= new object();
        private readonly string _chromeDriverDirectory;
        private readonly ChromeOptions _chromeOptions;

        public CachedChromeDriver(string chromeDriverDirectory, ChromeOptions chromeOptions)
        {
            _chromeDriverDirectory = chromeDriverDirectory;
            _chromeOptions = chromeOptions;
            Driver = GetDriverFromCache();
        }

        public ChromeDriver Driver { get; }

        private ChromeDriver GetDriverFromCache()
        {
            lock (LockObject)
            {
                var cachedItem = Cache.FirstOrDefault();

                ChromeDriver driver;
                if (cachedItem.Key != null)
                {
                    driver = (ChromeDriver) cachedItem.Value;
                    Cache.Remove(cachedItem.Key);
                }
                else
                {
                    // if you get an exception of the form:
                    // System.InvalidOperationException: 'session not created: This version of ChromeDriver only supports Chrome version 74...
                    // you need to update your Selenium.WebDriver.ChromeDriver NUGet package to match your version of Chrome/update to latest
                    // but this can only be done for debugging as test and live servers are pinned to a specific version
                    // do not commit the NuGet package update
                    driver = new ChromeDriver(_chromeDriverDirectory, _chromeOptions);
                }

                return driver;
            }
        }

        private static void RemovedCallback(CacheEntryRemovedArguments arg)
        {
            if (arg.RemovedReason != CacheEntryRemovedReason.Removed)
            {
                if (arg.CacheItem.Value is IDisposable item)
                {
                    item.Dispose();
                }
            }
        }

        public void Dispose()
        {
            lock (LockObject)
            {
                if (Cache.GetCount() < MaxCacheSize)
                {
                    var policy = new CacheItemPolicy
                    {
                        RemovedCallback = RemovedCallback,
                        SlidingExpiration = TimeSpan.FromSeconds(ChromeCacheSeconds)
                    };
                    Cache.Add(Guid.NewGuid().ToString(), Driver, policy);
                }
                else
                {
                    Driver.Dispose();
                }
            }
        }
    }
}
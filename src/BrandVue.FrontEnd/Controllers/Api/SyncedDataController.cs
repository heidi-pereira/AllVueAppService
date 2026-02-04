using BrandVue.Filters;
using BrandVue.Models;
using BrandVue.SourceData.LazyLoading;
using Microsoft.AspNetCore.Mvc;
using Vue.AuthMiddleware;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/synceddata")]
    [CacheControl(NoStore = true)]

    public class SyncedDataController : ApiController
    {
        private IDataLimiter _dataLimiter;
        public SyncedDataController (ILazyDataLoader loader)
        {
            _dataLimiter = loader.DataLimiter;
        }

        [Route("statistics")]
        [CacheControl(NoStore = true)]
        public DataLimiterStats DataLimiterStats()
        {
            return _dataLimiter.Stats;
        }

    }
}

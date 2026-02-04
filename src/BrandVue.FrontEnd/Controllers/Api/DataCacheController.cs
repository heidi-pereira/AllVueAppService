using System.Runtime;
using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.Filters;
using BrandVue.Services;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using Vue.AuthMiddleware;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/meta")]
    [CacheControl(NoStore = true)]
    public class DataCacheController : ApiController
    {
        private readonly IProductContext _productContext;
        private readonly IInvalidatableLoaderCache _invalidatableLoaderCache;
        private readonly IDataPreloader _dataPreloader;
        private readonly ISubsetRepository _subsetRepository;

        public DataCacheController(IProductContext productContext, 
            IInvalidatableLoaderCache invalidatableLoaderCache,
            IDataPreloader dataPreloader,
            ISubsetRepository subsetRepository
            )
        {
            _productContext = productContext;
            _invalidatableLoaderCache = invalidatableLoaderCache;
            _dataPreloader = dataPreloader;
            _subsetRepository = subsetRepository;
        }

        [HttpPost]
        [Route("forceReloadOfSurvey")]
        [RoleAuthorisation(Roles.Administrator)]
        [InvalidateBrowserCache]
        public IActionResult ForceReloadOfSurvey()
        {
            AssertAllVue();
            _dataPreloader.CancelTask();
            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
            _invalidatableLoaderCache.InvalidateQuestions(_subsetRepository.GetSurveyIdsForEnabledSubsets());

            return Ok();
        }
        
        [HttpPost]
        [Route("gc")]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [InvalidateBrowserCache]
        public IActionResult GarbageCollection(bool compact = false, bool collect = false, bool optimised = false)
        {
            var message = "Garbage collection request";
            
            if (compact)
            {
                // Compact large object heap on next collection
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                message += " with LOH compaction on next collection";
            }

            if (collect)
            {
                // Allow some control over the mode in case forced it too aggressive
                var mode = optimised ? GCCollectionMode.Optimized : GCCollectionMode.Forced;
                
                // Force a garbage collection
                GC.Collect(GC.MaxGeneration, mode, true, true);
                
                // Message to indicate a collection was forced or optimised
                message += optimised ? " with optimised collection" : " with forced collection";
            }
            else
            {
                message += " with no collection";
            }

            return Ok(message);
        }
        
        [HttpPost]
        [Route("forceLoadReportData")]
        [RoleAuthorisation(Roles.Administrator)]
        [InvalidateBrowserCache]
        public Task<DataPreloadTaskStatus> ForceLoadReportData(CancellationToken cancellationToken)
        {
            AssertAllVue();

            var preloadTaskStatus = _dataPreloader.PreloadReportDataIntoMemory(cancellationToken);

            return Task.FromResult(preloadTaskStatus);
        }

        [Route("checkDataPreloadStatus")]
        [CacheControl(NoStore = true)]
        [RoleAuthorisation(Roles.Administrator)]
        public Task<DataPreloadTaskStatus> CheckDataPreloadStatus()
        {
            var preloadTaskStatus = _dataPreloader.CheckTaskStatus();
            return Task.FromResult(preloadTaskStatus);
        }

        [Route("clearDataPreloadStatus")]
        [CacheControl(NoStore = true)]
        [RoleAuthorisation(Roles.Administrator)]
        public void ClearDataPreloadStatus()
        {
            _dataPreloader.ClearTaskStatus();
        }

        private void AssertAllVue()
        {
            if (!_productContext.GenerateFromSurveyIds)
                throw new NotImplementedException("This functionality is only supported for non-map-file Vues");
        }
    }
}
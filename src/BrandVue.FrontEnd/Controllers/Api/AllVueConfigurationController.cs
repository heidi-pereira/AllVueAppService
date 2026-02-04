using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Filters;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc;
using Vue.AuthMiddleware;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/allVueConfiguration")]
    [CacheControl(NoStore = true)]
    public class AllVueConfigurationController : ApiController
    {
        private readonly IAllVueConfigurationRepository _productConfigurationRepository;
        private readonly IProductContext _productContext;
        private readonly IInvalidatableLoaderCache _invalidatableLoaderCache;
        private readonly ISubsetRepository _subsetRepository;

        public AllVueConfigurationController(
            IAllVueConfigurationRepository productConfigurationRepository,
            IProductContext productContext,
            IInvalidatableLoaderCache invalidatableLoaderCache,
            ISubsetRepository subsetRepository)
        {
            _productConfigurationRepository = productConfigurationRepository;
            _productContext = productContext;
            _invalidatableLoaderCache = invalidatableLoaderCache;
            _subsetRepository = subsetRepository;
        }

        [HttpGet]
        [RoleAuthorisation(Roles.Administrator)]
        [Route("configuration")]
        public AllVueConfigurationDetails GetProductConfiguration()
        {
            return _productConfigurationRepository.GetConfigurationDetails();
        }

        [HttpPut]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        [Route("configuration")]
        public bool UpdateConfiguration([FromBody] AllVueConfigurationDetails productConfiguration)
        {
            _productConfigurationRepository.UpdateConfiguration(productConfiguration);
            _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
            _invalidatableLoaderCache.InvalidateQuestions(_subsetRepository.GetSurveyIdsForEnabledSubsets());
            return true;
        }
    }
}
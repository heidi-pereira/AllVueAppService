using System.Threading;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.Filters;
using BrandVue.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Vue.Common.Constants.Constants;

namespace BrandVue.Controllers.Api
{
    public record FeatureModel(int Id, string DocumentationUrl, bool IsActive, FeatureCode FeatureCode, string Name, bool IsInEnum, bool IsInDatabase);
    
    [SubProductRoutePrefix("api/features/[action]")]
    [CacheControl(NoStore = true)]
    public class FeaturesController : ApiController
    {
        private readonly IFeaturesService _featuresService;
        public FeaturesController(IFeaturesService featuresService)
        {
            _featuresService = featuresService;
        }

        [HttpGet]
        public async Task<IEnumerable<FeatureModel>> GetFeatures(CancellationToken token)
        {
            return await _featuresService.GetFeaturesAsync(token);
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<bool> Update(FeatureModel feature, CancellationToken token)
        {
            return await _featuresService.UpdateFeature(feature, token);
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<int> ActivateFeature(FeatureModel feature, CancellationToken token)
        {
            return await _featuresService.ActivateFeature(feature, token);
        }

        [HttpPost]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<bool> DeactivateFeature(int featureId, CancellationToken token)
        {
            return await _featuresService.DeactivateFeature(featureId, token);
        }

        [HttpDelete]
        [RoleAuthorisation(Roles.SystemAdministrator)]
        public async Task<bool> Delete(int featureId, CancellationToken token)
        {
            return await _featuresService.DeleteFeature(featureId, token);
        }
    }
}
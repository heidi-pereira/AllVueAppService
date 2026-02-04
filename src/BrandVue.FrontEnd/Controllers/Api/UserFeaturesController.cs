using BrandVue.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using System.Threading;
using BrandVue.EntityFramework.MetaData.Interfaces;
using BrandVue.Filters;
using Vue.Common.Constants.Constants;
using Vue.Common.FeatureFlags;

namespace BrandVue.Controllers.Api;

[SubProductRoutePrefix("api/userfeatures")]
public class UserFeaturesController : ApiController
{
    private readonly IFeatureToggleService _featureToggleService;
    private readonly IFeatureToggleCacheService _featureToggleCacheService;
    private readonly IUserFeaturesRepository _userFeaturesRepository;

    public record UserFeatureModel(int FeatureId, string UserId);

    public UserFeaturesController(IFeatureToggleService featureToggleService, IFeatureToggleCacheService featureToggleCacheService, IUserFeaturesRepository userFeaturesRepository)
    {
        _featureToggleService = featureToggleService;
        _featureToggleCacheService = featureToggleCacheService;
        _userFeaturesRepository = userFeaturesRepository;
    }

    [HttpGet]
    [Route("availables")]
    public async Task<IEnumerable<Feature>> Get(CancellationToken token)
    {
        return await _featureToggleService.GetEnabledFeaturesForCurrentUserAsync(token);
    }

    [HttpGet]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task<IEnumerable<UserFeatureModel>> GetUserFeaturesByFeature(int featureId, CancellationToken token)
    {
        var result = await _userFeaturesRepository.GetUserFeaturesByFeature(featureId, token);
        return result.Select(x => new UserFeatureModel(x.FeatureId, x.UserId));
    }

    [HttpPost]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task<UserFeatureModel> SetUserFeature(string userId, int featureId, CancellationToken token)
    {
        var result = await _featureToggleService.SaveUserFeaturesAsync(userId, featureId, token);
        return new UserFeatureModel(result.FeatureId, result.UserId);
    }

    [HttpPost("delete")]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task<bool> DeleteUserFeature(string userId, int featureId, CancellationToken token)
    {
        return await _userFeaturesRepository.DeleteUserFeatureAsync(userId, featureId, token);
    }

    [HttpGet]
    [Route("clearCache")]
    [RoleAuthorisation(Roles.SystemAdministrator)]
    public async Task ClearCache(CancellationToken token)
    {
        await _featureToggleCacheService.InvalidateCacheAsync(token);
    }

}

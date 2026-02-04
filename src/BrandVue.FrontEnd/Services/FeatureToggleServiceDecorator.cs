using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.Services.Interfaces;
using System.Threading;
using Vue.Common.FeatureFlags;
using ZiggyCreatures.Caching.Fusion;

namespace BrandVue.Services;

public class FeatureToggleServiceDecorator : IFeatureToggleService, IFeatureToggleCacheService
{
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(8);
    private readonly IUserContext _userContext;
    private readonly IFusionCache _fusionCache;
    private readonly IFeatureToggleService _featureToggleService;
    private const string CacheTag = "user_features";
    private const string BaseCacheKey = "user_features_";

    public static string CacheName => "FeatureToggleCache";


    public FeatureToggleServiceDecorator(
        IUserContext userInformationProvider,
        IFusionCacheProvider cacheProvider,
        IFeatureToggleService featureToggleService)
    {

        if(cacheProvider is null)
            throw new ArgumentNullException(nameof(cacheProvider));
        _fusionCache = cacheProvider.GetCache(CacheName);
        _userContext = userInformationProvider
            ?? throw new ArgumentNullException(nameof(userInformationProvider));
        _featureToggleService = featureToggleService
            ?? throw new ArgumentNullException(nameof(featureToggleService));
    }

    public async Task<IEnumerable<Feature>> GetEnabledFeaturesForCurrentUserAsync(CancellationToken token)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
        {
            throw new ArgumentNullException(nameof(_userContext.UserId));
        }

        string cacheKey = $"{BaseCacheKey}{_userContext.UserId}";

        var userFeatures = await _fusionCache.GetOrSetAsync<IEnumerable<Feature>>(
            cacheKey,
            (ctx, _) =>
            {
                ctx.Tags = [CacheTag];
                return _featureToggleService.GetEnabledFeaturesForCurrentUserAsync(token);
            },
            options => options.SetDuration(_cacheDuration),
            token: token);

        return userFeatures;
    }
    public async Task<UserFeature> SaveUserFeaturesAsync(string userId, int featureId, CancellationToken token)
    {
        var updatedUserFeature = await _featureToggleService.SaveUserFeaturesAsync(userId, featureId, token);
        await _fusionCache.RemoveAsync($"{BaseCacheKey}{userId}", token: token);
        return updatedUserFeature;
    }
    public async Task<OrganisationFeature> SaveOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken token)
    {
        var updatedOrganisationFeature = await _featureToggleService.SaveOrganisationFeaturesAsync(organisationId, featureId, token);
        await _fusionCache.RemoveAsync($"{BaseCacheKey}{organisationId}", token: token);
        return updatedOrganisationFeature;
    }
    public async Task<bool> DeleteOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken token)
    {
        var isDeleted =  await _featureToggleService.DeleteOrganisationFeaturesAsync(organisationId, featureId, token);
        if(isDeleted)
        {
            await _fusionCache.RemoveAsync($"{BaseCacheKey}{organisationId}", token: token);
        }
        return isDeleted;
    }
    public async Task InvalidateCacheAsync(CancellationToken token)
    {
        await _fusionCache.RemoveByTagAsync(CacheTag, token: token);
    }

    public async Task<bool> IsFeatureEnabledAsync(FeatureCode featureCode, CancellationToken token)
    {
        var enabledFeatures = await GetEnabledFeaturesForCurrentUserAsync(token);

        return enabledFeatures.Any(f => f.FeatureCode == featureCode);
    }
}

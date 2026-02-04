using BrandVue.EntityFramework.MetaData.FeatureToggle;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Vue.Common.FeatureFlags;

public static class FeatureToggleServiceExtensions
{
    public static async Task<bool> IsFeatureEnabledForUserAsync(this IFeatureToggleService fts, ILogger logger, FeatureCode featureCode, CancellationToken token)
    {
        var userFeatures = await fts.GetEnabledFeaturesForCurrentUserAsync(token);
        logger.LogWarning(JsonConvert.SerializeObject(userFeatures));
        return userFeatures.Any(a => a.FeatureCode == featureCode);
    }
}


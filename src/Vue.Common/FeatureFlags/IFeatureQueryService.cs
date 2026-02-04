using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace Vue.Common.FeatureFlags;

/// <summary>
/// Service for querying feature states and retrieving feature information.
/// This interface focuses on read operations for feature toggles.
/// </summary>
public interface IFeatureQueryService
{
    /// <summary>
    /// Checks if a feature is enabled by its name.
    /// </summary>
    /// <param name="featureName">The name of the feature to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature is enabled, false otherwise</returns>
    Task<bool> IsFeatureEnabledAsync(FeatureCode featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all enabled features for the current user.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of enabled features</returns>
    Task<IEnumerable<Feature>> GetEnabledFeaturesForCurrentUserAsync(CancellationToken cancellationToken = default);
}

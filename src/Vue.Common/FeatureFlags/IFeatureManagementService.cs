using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace Vue.Common.FeatureFlags;

/// <summary>
/// Service for managing feature toggle assignments.
/// This interface focuses on write operations for feature toggles.
/// </summary>
public interface IFeatureManagementService
{
    /// <summary>
    /// Saves or updates feature assignment for a specific user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="featureId">The feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved user feature assignment</returns>
    Task<UserFeature> SaveUserFeaturesAsync(string userId, int featureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates feature assignment for a specific organisation.
    /// </summary>
    /// <param name="organisationId">The organisation ID</param>
    /// <param name="featureId">The feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The saved organisation feature assignment</returns>
    Task<OrganisationFeature> SaveOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes feature assignment for a specific organisation.
    /// </summary>
    /// <param name="organisationId">The organisation ID</param>
    /// <param name="featureId">The feature ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the feature assignment was successfully deleted, false otherwise</returns>
    Task<bool> DeleteOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken cancellationToken = default);
}

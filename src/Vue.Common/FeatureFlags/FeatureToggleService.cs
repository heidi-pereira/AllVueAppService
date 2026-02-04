using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace Vue.Common.FeatureFlags;

/// <summary>
/// Composite service that provides both feature querying and management capabilities.
/// This service maintains backward compatibility by delegating to specialized services.
/// </summary>
public class FeatureToggleService : IFeatureToggleService
{
    private readonly IFeatureQueryService _featureQueryService;
    private readonly IFeatureManagementService _featureManagementService;

    public FeatureToggleService(
        IFeatureQueryService featureQueryService,
        IFeatureManagementService featureManagementService)
    {
        _featureQueryService = featureQueryService
            ?? throw new ArgumentNullException(nameof(featureQueryService));
        _featureManagementService = featureManagementService
            ?? throw new ArgumentNullException(nameof(featureManagementService));
    }

    // IFeatureQueryService implementation - delegate to query service
    public async Task<bool> IsFeatureEnabledAsync(FeatureCode featureCode, CancellationToken cancellationToken = default)
    {       
        return await _featureQueryService.IsFeatureEnabledAsync(featureCode, cancellationToken);
    }

    public async Task<IEnumerable<Feature>> GetEnabledFeaturesForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return await _featureQueryService.GetEnabledFeaturesForCurrentUserAsync(cancellationToken);
    }

    // IFeatureManagementService implementation - delegate to management service
    public async Task<UserFeature> SaveUserFeaturesAsync(string userId, int featureId, CancellationToken cancellationToken = default)
    {
        return await _featureManagementService.SaveUserFeaturesAsync(userId, featureId, cancellationToken);
    }

    public async Task<OrganisationFeature> SaveOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken cancellationToken = default)
    {
        return await _featureManagementService.SaveOrganisationFeaturesAsync(organisationId, featureId, cancellationToken);
    }

    public async Task<bool> DeleteOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken cancellationToken = default)
    {
        return await _featureManagementService.DeleteOrganisationFeaturesAsync(organisationId, featureId, cancellationToken);
    }
}

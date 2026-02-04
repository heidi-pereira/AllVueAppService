using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Vue.Common.Auth;

namespace Vue.Common.FeatureFlags;

/// <summary>
/// Service implementation for managing feature toggle assignments.
/// Handles write operations for feature toggles.
/// </summary>
public class FeatureManagementService : IFeatureManagementService
{
    private readonly IUserFeaturesRepository _userFeaturesRepository;
    private readonly IOrganisationFeaturesRepository _organisationFeaturesRepository;
    private readonly IUserContext _userContext;

    public FeatureManagementService(
        IUserFeaturesRepository userFeaturesRepository,
        IOrganisationFeaturesRepository organisationFeaturesRepository,
        IUserContext userContext)
    {
        _userFeaturesRepository = userFeaturesRepository
            ?? throw new ArgumentNullException(nameof(userFeaturesRepository));
        _organisationFeaturesRepository = organisationFeaturesRepository
            ?? throw new ArgumentNullException(nameof(organisationFeaturesRepository));
        _userContext = userContext
            ?? throw new ArgumentNullException(nameof(userContext));
    }

    public async Task<UserFeature> SaveUserFeaturesAsync(string userId, int featureId, CancellationToken cancellationToken = default)
    {
        return await _userFeaturesRepository.SaveUserFeatureAsync(userId, featureId, _userContext.UserId, cancellationToken);
    }

    public async Task<OrganisationFeature> SaveOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken cancellationToken = default)
    {
        return await _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(organisationId, featureId, _userContext.UserId, cancellationToken);
    }

    public async Task<bool> DeleteOrganisationFeaturesAsync(string organisationId, int featureId, CancellationToken cancellationToken = default)
    {
        return await _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(organisationId, featureId, cancellationToken);
    }
}

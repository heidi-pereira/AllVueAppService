using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Microsoft.Extensions.Logging;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

namespace Vue.Common.FeatureFlags;

/// <summary>
/// Service implementation for querying feature states and retrieving feature information.
/// Handles read operations for feature toggles.
/// </summary>
public class FeatureQueryService : IFeatureQueryService
{
    private readonly IUserFeaturesRepository _userFeaturesRepository;
    private readonly IOrganisationFeaturesRepository _organisationFeaturesRepository;
    private readonly IAuthApiClient _authApiClient;
    private readonly IUserContext _userContext;
    private readonly ILogger<FeatureQueryService> _logger;

    public FeatureQueryService(
        IUserFeaturesRepository userFeaturesRepository,
        IOrganisationFeaturesRepository organisationFeaturesRepository,
        IUserContext userContext,
        IAuthApiClient authApiClient,
        ILogger<FeatureQueryService> logger)
    {
        _userFeaturesRepository = userFeaturesRepository
            ?? throw new ArgumentNullException(nameof(userFeaturesRepository));
        _organisationFeaturesRepository = organisationFeaturesRepository
            ?? throw new ArgumentNullException(nameof(organisationFeaturesRepository));
        _userContext = userContext
            ?? throw new ArgumentNullException(nameof(userContext));
        _authApiClient = authApiClient
            ?? throw new ArgumentNullException(nameof(authApiClient));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> IsFeatureEnabledAsync(FeatureCode featureCode, CancellationToken cancellationToken = default)
    {
        if (featureCode == FeatureCode.unknown)
            throw new ArgumentException("Invalid feature code provided", nameof(featureCode));

        var enabledFeatures = await GetEnabledFeaturesForCurrentUserAsync(cancellationToken);

        return enabledFeatures.Any(f => f.FeatureCode == featureCode);
    }

    public async Task<IEnumerable<Feature>> GetEnabledFeaturesForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_userContext.UserId))
        {
            throw new ArgumentNullException(nameof(_userContext.UserId));
        }

        var userFeatures = await _userFeaturesRepository.GetEnabledFeaturesForUserAsync(_userContext.UserId, cancellationToken);

        if (string.IsNullOrEmpty(_userContext.AuthCompany))
        {
            return userFeatures;
        }

        try
        {
            var organisation = await _authApiClient.GetCompanyByShortcode(_userContext.AuthCompany, cancellationToken);
            _logger.LogInformation("Retrieved organisation: {Organisation}", organisation.DisplayName);
            if (organisation is null)
            {
                return userFeatures;
            }

            var orgFeatures = await _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(organisation.Id, cancellationToken);
            _logger.LogInformation("Retrieved organisation features: {OrgFeatures}", string.Join(", ", orgFeatures.Select(f => f.Name)));

            return userFeatures.Union(orgFeatures);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organisation features");
            return userFeatures;
        }
    }
}

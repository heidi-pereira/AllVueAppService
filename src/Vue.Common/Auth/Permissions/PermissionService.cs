using BrandVue.EntityFramework.MetaData.FeatureToggle;
using Microsoft.Extensions.Logging;
using System.ComponentModel.Design;
using Vue.Common.FeatureFlags;

namespace Vue.Common.Auth.Permissions;

/// <summary>
/// Service for managing user feature permissions.
/// This service checks if permission features are enabled and retrieves
/// user-specific feature permissions from the UserManagement API.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IUserPermissionHttpClient _userPermissionHttpClient;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IUserPermissionHttpClient userPermissionHttpClient,
        ILoggerFactory loggerFactory)
    {
        _userPermissionHttpClient = userPermissionHttpClient ?? throw new ArgumentNullException(nameof(userPermissionHttpClient));
        _logger = loggerFactory != null ? loggerFactory.CreateLogger<PermissionService>() : throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async Task<IReadOnlyCollection<IPermissionFeatureOptionWithCode>> GetAllUserFeaturePermissionsAsync(string userId, string userRole)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("PermissionService: UserId is null or empty. Returning empty permissions list.");
            return new List<IPermissionFeatureOptionWithCode>();
        }

        var response = await _userPermissionHttpClient.GetUserFeaturePermissionsAsync(userId,userRole);
        return response?.Select(CreatePermissionFeatureOptions).ToList() ?? userRole.DefaultPermissions();
    }

    private static PermissionFeatureOptionWithCode CreatePermissionFeatureOptions(PermissionFeatureOptionDto x)
    {
        PermissionFeaturesOptions featureOption;
        if (Enum.IsDefined(typeof(PermissionFeaturesOptions), x.Id))
        {
            featureOption = (PermissionFeaturesOptions)x.Id;
        }
        else
        {
            throw new InvalidCastException($"Invalid Id '{x.Id}' for PermissionFeaturesOptions enum.");
        }
        return new PermissionFeatureOptionWithCode(
            x.Id,
            x.Name,
            featureOption
        );
    }

    public async Task<IReadOnlyCollection<SummaryProjectAccess>> GetSummaryPermissionProjectAccessAsync(string[] companiesAuthId,
        CancellationToken token)
    {
        return await _userPermissionHttpClient.GetSummaryProjectAccessAsync(companiesAuthId, token);
    }

    public async Task<DataPermissionDto?> GetUserDataPermissionForCompanyAndProjectAsync(string companyId, string shortCode, string subProductId, string userId)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(shortCode) || string.IsNullOrWhiteSpace(subProductId) || string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("PermissionService: CompanyId, shortCode, subProductId or UserId is null or empty. Returning empty permissions list.");
            return null;
        }
        var response = await _userPermissionHttpClient.GetUserDataPermissionForCompanyAndProjectAsync(companyId, shortCode, subProductId, userId);
        return response;
    }

    public async Task<int?> GetUserDataGroupRuleIdForCompanyAndProjectAsync(string companyId, string shortCode, string subProductId, string userId)
    {
        if (string.IsNullOrWhiteSpace(companyId) || string.IsNullOrWhiteSpace(shortCode) || string.IsNullOrWhiteSpace(subProductId) || string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("PermissionService: CompanyId, shortCode, subProductId or UserId is null or empty. Returning null.");
            return null;
        }
        var response = await _userPermissionHttpClient.GetUserDataGroupRuleIdForCompanyAndProjectAsync(companyId, shortCode, subProductId, userId);
        return response;
    }
}

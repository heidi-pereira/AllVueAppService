namespace Vue.Common.Auth.Permissions;

/// <summary>
/// HTTP client interface for retrieving user permission data from the UserManagement API.
/// </summary>
public interface IUserPermissionHttpClient
{
    Task<IReadOnlyCollection<PermissionFeatureOptionDto>?> GetUserFeaturePermissionsAsync(string userId, string defaultRole);

    Task<DataPermissionDto?> GetUserDataPermissionForCompanyAndProjectAsync(string companyId, string shortCode, string subProductId, string userId);
    Task<int?> GetUserDataGroupRuleIdForCompanyAndProjectAsync(string companyId, string shortCode, string subProductId, string userId);
    Task<IReadOnlyCollection<SummaryProjectAccess>> GetSummaryProjectAccessAsync(string[] companiesAuthId, CancellationToken token);
}

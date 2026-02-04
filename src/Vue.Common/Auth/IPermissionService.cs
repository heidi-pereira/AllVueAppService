using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth;

public interface IPermissionService
{
    public Task<IReadOnlyCollection<IPermissionFeatureOptionWithCode>> GetAllUserFeaturePermissionsAsync(string userId, string userRole);
    public Task<DataPermissionDto?> GetUserDataPermissionForCompanyAndProjectAsync(string companyId, string productShortCode, string subProductId, string userId);
    public Task<IReadOnlyCollection<SummaryProjectAccess>> GetSummaryPermissionProjectAccessAsync(string[] companiesAuthId, CancellationToken token);
    public Task<int?> GetUserDataGroupRuleIdForCompanyAndProjectAsync(string companyId, string productShortCode, string subProductId, string userId);
}
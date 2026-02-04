using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions;

namespace UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;

public interface IUserDataPermissionRepository
{
    Task<IList<UserDataPermission>> GetByUserIdAsync(string userId, CancellationToken token);
    Task<UserDataPermission> GetByUserIdByCompanyAndProjectAsync(string userId, string company, ProjectOrProduct projectId, CancellationToken token);
    Task<IEnumerable<UserDataPermission>> GetByCompanyAndAllVueProjectAsync(string company, ProjectOrProduct projectId, CancellationToken token);
    Task<IEnumerable<UserDataPermission>> GetByCompaniesAsync(string[] companies, CancellationToken token);
    Task AddAsync(UserDataPermission permission, CancellationToken token);
    Task UpdateAsync(UserDataPermission permission, CancellationToken token);
    Task DeleteAsync(int id, CancellationToken token);
    Task DeleteAllPermissionsForUserAsync(string userId, CancellationToken token);
    
    Task<UserDataPermission?> GetByIdAsync(int userDataPermissionsId, CancellationToken token);
    Task<IList<UserDataPermission>> GetByUserIdsAsync(string[] userIds, CancellationToken token);

    Task<IEnumerable<UserDataPermission>> GetByCompaniesAndAllVueProjectsAsync(string[] companies,
        ProjectOrProduct projectId, CancellationToken token);
    Task<IEnumerable<UserDataPermission>> GetByRuleIdAsync(int ruleId, CancellationToken token);

    Task<IList<UserDataPermission>> GetByUserIdsAndProjectAsync(string[] userIds, ProjectOrProduct projectId, CancellationToken token);
    Task<IList<UserDataPermission>> GetByRuleId(int ruleId, CancellationToken token);
}
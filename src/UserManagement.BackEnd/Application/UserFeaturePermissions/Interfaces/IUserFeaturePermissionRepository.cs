using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

public interface IUserFeaturePermissionRepository
{
    Task<UserFeaturePermission?> GetByUserIdAsync(string userId);
    Task<IEnumerable<UserFeaturePermission>> GetAllAsync();
    Task<bool> HasRoleAssignments(int roleId, CancellationToken cancellationToken = default);
    Task<UserFeaturePermission> AddAsync(UserFeaturePermission permission);
    Task UpdateAsync(UserFeaturePermission permission);
    Task<UserFeaturePermission> UpsertAsync(UserFeaturePermission permission);
    Task DeleteAsync(int id);
    Task DeleteAllPermissionsForUserAsync(string userId, CancellationToken cancellationToken);
    Task DeleteByUserIdAsync(string userId);
}
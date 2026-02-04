using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions;

public interface IPermissionFeatureRepository
{
    Task<IEnumerable<PermissionFeature>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PermissionFeature?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
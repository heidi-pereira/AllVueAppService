
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces
{
    public interface IPermissionOptionRepository
    {
        Task<IEnumerable<PermissionOption>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<PermissionOption>> GetAllByIdsAsync(IEnumerable<int> optionIds, CancellationToken cancellationToken = default);
    }
}
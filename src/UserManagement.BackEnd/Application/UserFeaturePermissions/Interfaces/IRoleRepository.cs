
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

public interface IRoleRepository
{
    Task<Role> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetByOrganisationIdAsync(string organisation);
    Task<Role> AddAsync(Role role);
    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetAllAsync();
    Task DeleteAsync(int roleId, CancellationToken cancellationToken = default);
}
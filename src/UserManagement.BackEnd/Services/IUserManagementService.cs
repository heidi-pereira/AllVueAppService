using UserManagement.BackEnd.Models;

namespace UserManagement.BackEnd.Services
{
    public interface IUserManagementService
    {
        Task<IEnumerable<User>> GetUsersForProjectByCompanyAsync(string companyId, CancellationToken token);
        Task<IEnumerable<User>> GetUsersWithRolesAsync(bool includeSavantaUsers, string? companyId, CancellationToken cancellationToken);
    }
}

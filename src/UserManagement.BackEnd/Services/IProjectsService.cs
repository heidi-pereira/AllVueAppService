using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UserManagement.BackEnd.Models;

namespace UserManagement.BackEnd.Services
{
    public interface IProjectsService
    {
        Task<IEnumerable<Project>> GetProjectsByCompanyId(string? companyId, CancellationToken token);
        Task<Project?> GetProjectById(string companyAuthId, ProjectIdentifier projectId, CancellationToken token);

        Task<List<CompanyWithProductsAndProjects>> GetCompanyAndChildCompanyWithProjectIdentifier(string companyShortCode,
            CancellationToken token);

        Task<List<Project>> GetProjectsByCompanyShortCode(bool includeSavanta, string companyShortCode, CancellationToken token);

        Task SetProjectSharedStatus(string company, ProjectIdentifier projectId, bool isShared, CancellationToken token);
        Task<VariablesAvailable> GetProjectVariablesAvailable(string companyId, ProjectIdentifier projectId, CancellationToken token);
        Task<int> GetProjectResponseCountFromFilter(string companyId, ProjectIdentifier projectId, List<AllVueFilter> filters, CancellationToken token);
        ProjectIdentifier AuthProjectToProjectIdentifier(string projectName);
        ProjectIdentifier AuthShortCodeAndProjectToProjectIdentifier(string productShortCode, string projectName);
        Task<IList<string>> GetLegacySharedUsers(ProjectIdentifier projectId, string shortCode, string currentUserId, CancellationToken token);
        Task MigrateLegacySharedUsers(string company, ProjectIdentifier projectId, string shortCode, string currentUserId, CancellationToken token);
    }
}

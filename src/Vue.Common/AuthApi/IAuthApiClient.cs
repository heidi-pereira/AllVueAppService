using AuthServer.GeneratedAuthApi;
using System.Drawing;
using Vue.Common.AuthApi.Models;

namespace Vue.Common.AuthApi
{
    public interface IAuthApiClientCustomerPortal
    {
        Task<CompanyModel> GetCompanyById(string id, CancellationToken token);
        Task<CompanyModel> GetCompanyByShortcode(string shortcode, CancellationToken token);
        Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId, bool includeSavantaUsers, CancellationToken cancellationToken);

        Task<IEnumerable<UserProject>> GetProjectsForUser(string userId, CancellationToken token);
        Task<bool> CanUserAccessProject(string shortCode, string userId, string projectId, CancellationToken token);
        Task<IEnumerable<string>> GetSharedProjects(IEnumerable<string> projectIds, CancellationToken token);
        Task<string> GetFaviconUrl(string portalGroup, CancellationToken token);
    }

    public interface IAuthApiClient
    {
        Task<IEnumerable<CompanyModel>> GetAllCompanies(CancellationToken cancellationToken);

        Task<CompanyModel> GetCompanyById(string id, CancellationToken token);
        Task<IEnumerable<CompanyModel>> GetCompanies(IEnumerable<string> companyIds,
            CancellationToken cancellationToken);
        Task<CompanyModel> GetCompanyByShortcode(string shortCode, CancellationToken cancellationToken);
        Task<string> GetReportTemplatePathAsync(bool useCustomReport, string shortCode,
            CancellationToken cancellationToken);
        Task<ThemeDetails> GetThemeDetails(string shortCode, CancellationToken cancellationToken);
        Task<Image> GetLogoImage(ThemeDetails themeDetails, CancellationToken cancellationToken);
        Task<string> GetFaviconUrl(string shortCode, CancellationToken cancellationToken);
        Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId,bool includeSavantaUsers, CancellationToken cancellationToken);

        Task AddUserProjects(string shortCode, IEnumerable<UserProject> projects, CancellationToken cancellationToken);
        Task RemoveUserFromProject(string shortCode, int userProjectId, string userId,
            CancellationToken cancellationToken);
        Task<bool> CanUserAccessProject(string shortCode, string userId, string projectId, CancellationToken cancellationToken);
        Task SetProjectShared(string projectId, bool isShared, string currentUserId,
            CancellationToken cancellationToken);
        Task<bool> IsProjectShared(string projectId, CancellationToken cancellationToken);


        //Customer Portal Specific Methods
        Task<IEnumerable<UserProject>> GetProjectsForUser(string userId, CancellationToken token);
        Task<IEnumerable<string>> GetSharedProjects(IEnumerable<string> projectIds, CancellationToken token);
    }
}

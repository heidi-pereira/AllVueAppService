using AuthServer.GeneratedAuthApi;
using Microsoft.Extensions.Caching.Memory;
using System.Drawing;

namespace Vue.Common.AuthApi
{
    public class CachedAuthApiClient : IAuthApiClient, IUserServiceByAuth
    {
        private readonly IAuthApiClient _apiClient;
        private readonly IUserServiceByAuth _userServiceByAuth;
        private IMemoryCache _userProjectAccessCache;
        private readonly IMemoryCache _companyAccessCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 500 });

        private const string AllCompaniesCacheKey = "AllCompanies";

        public CachedAuthApiClient(bool isDevAuthServer, string authClientId, string authClientSecret, string authServerUrl, IHttpClientFactory httpClientFactory)
        {
            var apiClient = new AuthApiClient(isDevAuthServer, authClientId, authClientSecret, authServerUrl, httpClientFactory);

            _apiClient = apiClient;
            _userServiceByAuth = apiClient;
            _userProjectAccessCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 500 });
        }

        private record CachedProjectIdsForUser(string[] AccessibleProjectIds, string[] UnaccessibleProjectIds);

        private void SetCompaniesCache(IEnumerable<CompanyModel> companies)
        {
            var options = new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };
            _companyAccessCache.Set(AllCompaniesCacheKey, companies, options);
            foreach (var company in companies)
            {
                _companyAccessCache.Set(company.Id, company, options);
            }
        }

        private bool TryGetCompaniesCache(out IEnumerable<CompanyModel> companies)
        {
            return _companyAccessCache.TryGetValue(AllCompaniesCacheKey, out companies);
        }

        public async Task<IEnumerable<CompanyModel>> GetAllCompanies(CancellationToken cancellationToken)
        {
            if (TryGetCompaniesCache(out var cachedCompanies))
            {
                return cachedCompanies;
            }
            var allCompanies = await _apiClient.GetAllCompanies(cancellationToken);
            SetCompaniesCache(allCompanies);
            return allCompanies;
        }

        public async Task<CompanyModel> GetCompanyById(string companyId, CancellationToken cancellationToken) 
        {
            if (_companyAccessCache.TryGetValue(companyId, out var company) && company is CompanyModel myCompany)
            {
                return myCompany;
            }
            return await _apiClient.GetCompanyById(companyId, cancellationToken);
        }

        public async Task<IEnumerable<CompanyModel>> GetCompanies(IEnumerable<string> companyIds, CancellationToken cancellationToken) =>
            await _apiClient.GetCompanies(companyIds, cancellationToken);

        public async Task<CompanyModel> GetCompanyByShortcode(string shortCode, CancellationToken cancellationToken) =>
            await _apiClient.GetCompanyByShortcode(shortCode, cancellationToken);

        public async Task<string> GetReportTemplatePathAsync(bool useCustomReport, string shortCode,
            CancellationToken cancellationToken) =>
            await _apiClient.GetReportTemplatePathAsync(useCustomReport, shortCode, cancellationToken);

        public async Task<ThemeDetails> GetThemeDetails(string shortCode, CancellationToken cancellationToken) => await _apiClient.GetThemeDetails(shortCode, cancellationToken);

        public async Task<Image> GetLogoImage(ThemeDetails themeDetails, CancellationToken cancellationToken) => await _apiClient.GetLogoImage(themeDetails, cancellationToken);
        public Task<string> GetFaviconUrl(string shortCode, CancellationToken cancellationToken) => _apiClient.GetFaviconUrl(shortCode, cancellationToken);

        public async Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId, bool includeSavantaUsers, CancellationToken cancellationToken) =>
            await _apiClient.GetAllUserDetailsForCompanyScopeAsync(shortCode, currentUserId, requestCompanyId,includeSavantaUsers, cancellationToken);

        public async Task AddUserProjects(string shortCode, IEnumerable<UserProject> projects,
            CancellationToken cancellationToken)
        {
            await _apiClient.AddUserProjects(shortCode, projects, cancellationToken);
            var userIds = projects.Select(p => p.ApplicationUserId).Distinct();
            foreach (var userId in userIds)
            {
                _userProjectAccessCache.Remove(userId);
            }
        }

        public async Task RemoveUserFromProject(string shortCode, int userProjectId, string userId,
            CancellationToken cancellationToken)
        {
            await _apiClient.RemoveUserFromProject(shortCode, userProjectId, userId, cancellationToken);
            _userProjectAccessCache.Remove(userId);
        }

        public async Task<bool> CanUserAccessProject(string shortCode, string userId, string projectId,
            CancellationToken cancellationToken)
        {
            if (!_userProjectAccessCache.TryGetValue<CachedProjectIdsForUser>(userId, out var cachedProjectsForUser))
                cachedProjectsForUser = new(Array.Empty<string>(), Array.Empty<string>());

            if (cachedProjectsForUser.AccessibleProjectIds.Contains(projectId))
                return true;

            if (cachedProjectsForUser.UnaccessibleProjectIds.Contains(projectId))
                return false;

            var canAccessProject = await _apiClient.CanUserAccessProject(shortCode, userId, projectId, cancellationToken);

            var accessibleProjectIds = cachedProjectsForUser.AccessibleProjectIds;
            var unaccessibleProjectIds = cachedProjectsForUser.UnaccessibleProjectIds;
            if (canAccessProject)
                accessibleProjectIds = accessibleProjectIds.Append(projectId).ToArray();
            else
            {
                unaccessibleProjectIds = unaccessibleProjectIds.Append(projectId).ToArray();
            }

            var updatedCachedProjects = new CachedProjectIdsForUser(accessibleProjectIds, unaccessibleProjectIds);
            var options = new MemoryCacheEntryOptions
            {
                Size = 1,
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            };
            _userProjectAccessCache.Set(userId, updatedCachedProjects, options);

            return canAccessProject;
        }

        public async Task SetProjectShared(string projectId, bool isShared, string currentUserId,
            CancellationToken cancellationToken)
        {
            await _apiClient.SetProjectShared(projectId, isShared, currentUserId, cancellationToken);
            if (!isShared)
            {
                //project access may be cached, clear entire cache
                var oldCache = Interlocked.Exchange(ref _userProjectAccessCache, new MemoryCache(new MemoryCacheOptions { SizeLimit = 500 }));
                oldCache.Dispose();
            }
        }

        public async Task<bool> IsProjectShared(string projectId, CancellationToken cancellationToken) => await _apiClient.IsProjectShared(projectId, cancellationToken);
        
        public async Task<IEnumerable<UserProject>> GetProjectsForUser(string userId, CancellationToken token) => await _apiClient.GetProjectsForUser(userId, token);

        public async Task<IEnumerable<string>> GetSharedProjects(IEnumerable<string> projectIds, CancellationToken token) => await _apiClient.GetSharedProjects(projectIds, token);

        public Task DeleteUser(string shortCode, string requesterEmail, string userId,
            CancellationToken cancellationToken) =>
            _userServiceByAuth.DeleteUser(shortCode, requesterEmail, userId, cancellationToken);

        public Task ResendEmail(string shortCode, string userEmail, CancellationToken cancellationToken) 
            => _userServiceByAuth.ResendEmail(shortCode, userEmail, cancellationToken);

        public Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyAndChildrenScopeAsync(
            string shortCode, 
            string currentUserId, 
            string requestCompanyId,
            bool includeSavantaUsers,
            bool includeProductDetails,
            CancellationToken cancellationToken) 
            => _userServiceByAuth.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                shortCode,
                currentUserId,
                requestCompanyId,
                includeSavantaUsers,
                includeProductDetails,
                cancellationToken);

        public Task<UserProjectsModel> GetUserAsync(string shortCode, string userId, string proxyUserId, CancellationToken cancellationToken)
            => _userServiceByAuth.GetUserAsync(shortCode, userId, proxyUserId, cancellationToken);

        public Task<UserProjectsModel> UpdateUserAsync(string shortCode, string currentUser, UserUpdateDetails user, CancellationToken cancellationToken) 
            => _userServiceByAuth.UpdateUserAsync(shortCode, currentUser, user, cancellationToken);

        public Task<UserProjectsModel> CreateUserAsync(string shortCode, string currentUser, UserAddDetails user, CancellationToken cancellationToken)
            => _userServiceByAuth.CreateUserAsync(shortCode, currentUser, user, cancellationToken);
    }
}

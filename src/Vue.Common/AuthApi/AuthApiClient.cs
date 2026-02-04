using AuthServer.GeneratedAuthApi;
using Svg;
using System.Drawing;
using System.Drawing.Imaging;

namespace Vue.Common.AuthApi
{
    public class AuthApiClient : IAuthApiClient, IAuthApiClientCustomerPortal, IUserServiceByAuth
    {
        private readonly string _authClientId;
        private readonly string _authClientSecret;
        private readonly string _authServerUrl;
        private readonly bool _isDevAuthServer;
        private readonly IHttpClientFactory _httpClientFactory;

        private string AuthServerUrl => _authServerUrl;

        public AuthApiClient(bool isDevAuthServer, string authClientId, string authClientSecret, string authServerUrl, IHttpClientFactory httpClientFactory)
        {
            _isDevAuthServer = isDevAuthServer;
            _authClientId = authClientId;
            _authClientSecret = authClientSecret;
            _httpClientFactory = httpClientFactory;
            _authServerUrl = authServerUrl;
        }

        private AuthAPITokenHandler GetTokenHandler(HttpClient client)
        {
            return new AuthAPITokenHandler(client, AuthServerUrl, _authClientId, _authClientSecret);
        }

        private string GetAuthServerUrlWithShortCode(string baseUrl, string shortCode)
        {
            if (_isDevAuthServer)
                return baseUrl;

            if (string.IsNullOrEmpty(shortCode))
                return baseUrl;

            try
            {
                var uri = new UriBuilder(baseUrl);
                uri.Host = $"{shortCode}.{uri.Host}";
                return uri.ToString();
            }
            catch (Exception)
            {
                return baseUrl;
            }
        }

        public async Task<IEnumerable<CompanyModel>> GetAllCompanies(CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var companiesClient = new CompaniesClient(AuthServerUrl, httpClient);
            return await authClient.DoWithToken(() => companiesClient.GetAllAsync(cancellationToken));
        }

        public async Task<CompanyModel> GetCompanyByShortcode(string shortCode, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var companiesClient = new CompaniesClient(AuthServerUrl, httpClient);
            return await authClient.DoWithToken(() => companiesClient.GetByShortcodeAsync(shortCode, cancellationToken));
        }

        public async Task<CompanyModel> GetCompanyById(string id, CancellationToken token)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var companiesClient = new CompaniesClient(AuthServerUrl, httpClient);
            return await authClient.DoWithToken(() => companiesClient.GetAsync(id, token));
        }

        public async Task<IEnumerable<CompanyModel>> GetCompanies(IEnumerable<string> companyIds, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var companiesClient = new CompaniesClient(AuthServerUrl, httpClient);
            return await authClient.DoWithToken(() => companiesClient.GetManyAsync(companyIds, cancellationToken));
        }

        public async Task<string> GetReportTemplatePathAsync(bool useCustomReport, string shortCode,
            CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var authClient = GetTokenHandler(httpClient);
            var themeClient = new ThemeClient(authServerWithShortCode, httpClient);
            return await authClient.DoWithToken(() => themeClient.GetReportTemplatePathAsync(!useCustomReport, cancellationToken));
        }


        public async Task<ThemeDetails> GetThemeDetails(string shortCode, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl,shortCode);
            var authClient = GetTokenHandler(httpClient);
            var themeClient = new ThemeClient(authServerWithShortCode, httpClient);
            return await authClient.DoWithToken(() => themeClient.GetThemeLogoAndColoursAsync(cancellationToken));
        }

        public async Task<Image> GetLogoImage(ThemeDetails themeDetails, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            return await GetLogoImage(themeDetails, httpClient, cancellationToken);
        }

        public static async Task<Image> GetLogoImage(ThemeDetails themeDetails, HttpClient httpClient, CancellationToken cancellationToken)
        {
            var imageStream = await httpClient.GetStreamAsync(themeDetails.LogoUrl, cancellationToken);
            if (themeDetails.LogoUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                var svgDocument = SvgDocument.Open<SvgDocument>(imageStream);
                //ugly hack to get around it losing transparency
                using var bitmap = svgDocument.Draw();
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return Image.FromStream(stream);
            }
            else
            {
                return Image.FromStream(imageStream);
            }
        }
        
        public async Task<string> GetFaviconUrl(string shortCode, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            string authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var themeClient = new ThemeClient(authServerWithShortCode, httpClient);
            return await themeClient.GetFaviconAsync(cancellationToken);
        }

        public async Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId, bool includeSavantaUsers, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var authClient = GetTokenHandler(httpClient);

            var userProjectsClient = new UserProjectsClient(authServerWithShortCode, httpClient);
            return await authClient.DoWithToken(() =>

                includeSavantaUsers?
                userProjectsClient.GetAllUserDetailsExceptRoleForCompanyScopeIncludingSavantaAsync(currentUserId, requestCompanyId, cancellationToken)
                    :
                userProjectsClient.GetAllUserDetailsForCompanyScopeAsync(currentUserId, requestCompanyId, cancellationToken));
        }

        public async Task AddUserProjects(string shortCode, IEnumerable<UserProject> projects,
            CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var authClient = GetTokenHandler(httpClient);

            var userProjectsClient = new UserProjectsClient(authServerWithShortCode, httpClient);
            await authClient.DoWithToken(() => userProjectsClient.AddUserProjectsAsync(projects, cancellationToken));
        }

        public async Task RemoveUserFromProject(string shortCode, int userProjectId, string userId,
            CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var authClient = GetTokenHandler(httpClient);

            var userProjectsClient = new UserProjectsClient(authServerWithShortCode, httpClient);
            await authClient.DoWithToken(() => userProjectsClient.RemoveUserProjectsAsync(new []{userProjectId}, cancellationToken));
        }

        public async Task<bool> CanUserAccessProject(string shortCode, string userId, string projectId,
            CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var authClient = GetTokenHandler(httpClient);

            var userProjectsClient = new UserProjectsClient(authServerWithShortCode, httpClient);
            return await authClient.DoWithToken(() => userProjectsClient.CanUserAccessProjectsAsync(userId, new[] { projectId }, cancellationToken));
        }

        public async Task SetProjectShared(string projectId, bool isShared, string currentUserId,
            CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var userProjectsClient = new UserProjectsClient(AuthServerUrl, httpClient);

            await authClient.DoWithToken(() => userProjectsClient.SetProjectSharedAsync(projectId, isShared, currentUserId, cancellationToken));
        }

        public async Task<bool> IsProjectShared(string projectId, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var userProjectsClient = new UserProjectsClient(AuthServerUrl, httpClient);

            return await authClient.DoWithToken(() => userProjectsClient.IsProjectSharedAsync(projectId, cancellationToken));
        }

        //Customer Portal Specific Methods
        public async Task<IEnumerable<UserProject>> GetProjectsForUser(string userId, CancellationToken token)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var userProjectsClient = new UserProjectsClient(AuthServerUrl, httpClient);
            
            return await authClient.DoWithToken(() => userProjectsClient.GetProjectsForUserAsync(userId, token));
        }

        public async Task<IEnumerable<string>> GetSharedProjects(IEnumerable<string> projectIds, CancellationToken token)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            var userProjectsClient = new UserProjectsClient(AuthServerUrl, httpClient);
            return await authClient.DoWithToken(() => userProjectsClient.GetSharedProjectsAsync(projectIds, token));
        }

        public async Task DeleteUser(string shortCode, string requesterEmail, string userId,
            CancellationToken cancellationToken)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient();
                var authClient = GetTokenHandler(httpClient);
                string authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
                var userClient = new UserClient(authServerWithShortCode, httpClient);
                await authClient.DoWithToken(() =>
                    userClient.DeleteUserAsync(requesterEmail, userId, cancellationToken));
            }
            catch (ApiException e)
            {
                if (e.StatusCode == (int) System.Net.HttpStatusCode.NoContent)
                {
                    return;
                }
                throw;
            }

        }

        public async Task ResendEmail(string shortCode, string userEmail, CancellationToken cancellationToken)
        {
            try
            {

                using var httpClient = _httpClientFactory.CreateClient();
                var authClient = GetTokenHandler(httpClient);
                string authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
                var userClient = new UserClient(authServerWithShortCode, httpClient);
                await authClient.DoWithToken(() => userClient.ForgotPasswordAsync(userEmail, cancellationToken));
            }
            catch (ApiException e)
            {
                if (e.StatusCode == (int)System.Net.HttpStatusCode.NoContent)
                {
                    return;
                }
                throw;
            }
        }

        public async Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyAndChildrenScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId, bool includeSavantaUsers, bool includeProductDetails,
            CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var authClient = GetTokenHandler(httpClient);

            var userProjectsClient = new UserProjectsClient(authServerWithShortCode, httpClient);
            return await authClient.DoWithToken(() =>
                userProjectsClient.GetAllUserDetailsForCompanyScopeAndChildCompaniesAsync(
                    currentUserId,
                    requestCompanyId, 
                    includeSavantaUsers,
                    includeProductDetails,
                    cancellationToken));
        }
        public async Task<UserProjectsModel> GetUserAsync(string shortCode, string userId, string proxyUserId, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            string authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var userClient = new UserClient(authServerWithShortCode, httpClient);
            return await authClient.DoWithToken(() => userClient.GetUserAsync(userId, proxyUserId, cancellationToken));
        }

        public async Task<UserProjectsModel> UpdateUserAsync(string shortCode, string currentUser, UserUpdateDetails user, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            string authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var userClient = new UserClient(authServerWithShortCode, httpClient);

            return await authClient.DoWithToken(() => userClient.UpdateUserAsync(currentUser, user, cancellationToken));
        }

        public async Task<UserProjectsModel> CreateUserAsync(string shortCode, string currentUser, UserAddDetails user, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            var authClient = GetTokenHandler(httpClient);
            string authServerWithShortCode = GetAuthServerUrlWithShortCode(AuthServerUrl, shortCode);
            var userClient = new UserClient(authServerWithShortCode, httpClient);

            return await authClient.DoWithToken(() => userClient.AddUserAsync(currentUser, user, cancellationToken));
        }
    }
}

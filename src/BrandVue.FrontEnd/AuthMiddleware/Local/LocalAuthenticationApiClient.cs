using System.Drawing;
using System.Net.Http;
using System.Threading;
using AuthServer.GeneratedAuthApi;
using BrandVue;
using BrandVue.EntityFramework;
using BrandVue.Services;
using BrandVue.SourceData.AnswersMetadata;
using Microsoft.Extensions.Logging;
using Vue.Common.AuthApi;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware.Local
{
    public class LocalAuthenticationApiClient : IAuthApiClient
    {
        private const string SavantaLiveCompanyId = "d570a72d-3ce4-4705-96fe-39a9b7ca132c";
        private const string MorarLiveCompanyId = "4057047C-325C-4384-90A8-C506481AB431";
        private const string WGSNLiveCompanyId = "c34ffd50-2d9e-4b7e-bbfb-d17c9f411232";
        private const string BrandVueLiveCompanyId = "5aab7fae-2720-464b-b2e9-4c3c533d9ff7";

        private readonly IRequestScopeAccessor _requestScopeAccessor;
        private readonly ProductContextProvider _productContextProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        private static HashSet<string> SharedProjectIds = new HashSet<string>();
        private static List<string> UserIdsAddedToProject = new List<string>();

        public LocalAuthenticationApiClient(AppSettings appSettings, IRequestScopeAccessor requestScopeAccessor, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            /*
             We can't access the product context directly here, it's too early to have a RequestScope
             See similar code in the PerRequestSubProductLoaderDecorator.cs constructor
            */
            _productContextProvider = new ProductContextProvider(appSettings, new AnswerDbContextFactory(appSettings.ConnectionString), loggerFactory.CreateLogger<ProductContextProvider>());
            _requestScopeAccessor = requestScopeAccessor;
            _httpClientFactory = httpClientFactory;
        }

        public Task<IEnumerable<CompanyModel>> GetAllCompanies(CancellationToken cancellationToken) => GetCompanies(
            new[] { "1", "2", "3", 
                SavantaLiveCompanyId, 
                MorarLiveCompanyId, 
                WGSNLiveCompanyId, 
                BrandVueLiveCompanyId, }, CancellationToken.None);

        private static readonly Dictionary<string, string> _mapOfLiveCompanyIdsToOrganization = new Dictionary<string, string>
        {
            { BrandVueLiveCompanyId, "BrandVue" },
            { MorarLiveCompanyId, "Morar" },
            { SavantaLiveCompanyId, "Savanta" },
            { WGSNLiveCompanyId, "WGSN" },
        };

        string CompanyIdToName(string companyId, int index, Func<int,string> defaultNaming)
        {
            if (_mapOfLiveCompanyIdsToOrganization.ContainsKey(companyId))
            {
                return _mapOfLiveCompanyIdsToOrganization[companyId];
            }
            return defaultNaming(index);
        }

        string CompanyIdToShortCode(string companyId, int index)
        {
            return CompanyIdToName(companyId, index, i => $"testorganisation{i}").ToLowerInvariant();
        }

        string CompanyIdToDisplayName(string companyId, int index)
        {
            return CompanyIdToName(companyId, index, i => $"Test organisation {i}");
        }

        public async Task<CompanyModel> GetCompanyByShortcode(string companyIds,
            CancellationToken cancellationToken)
        {
            var companies = await GetAllCompanies(cancellationToken);
            return companies.SingleOrDefault(x => x.ShortCode == companyIds);

        }

        public async Task<CompanyModel> GetCompanyById(string companyId, CancellationToken cancellationToken)
        {
            var many = await GetCompanies(new[] { companyId }, cancellationToken);
            return many.Single();
        }
        public Task<IEnumerable<CompanyModel>> GetCompanies(IEnumerable<string> companyIds, CancellationToken cancellationToken)
        {

            var localCompanies = companyIds.Select((companyId, index) => new CompanyModel
            {
                Id = companyId,
                ShortCode = CompanyIdToShortCode(companyId, index),
                DisplayName = CompanyIdToDisplayName(companyId, index),
                ParentCompanyDisplayName = companyId != SavantaLiveCompanyId ? CompanyIdToShortCode(SavantaLiveCompanyId, 0) : "",
                SecurityGroup = null,
                SecurityGroupName = null,
                Url = null,
                Products = Array.Empty<ProductModel>()
            });
            return Task.FromResult(localCompanies);
        }

        public Task<string> GetReportTemplatePathAsync(bool useCustomReport, string shortCode,
            CancellationToken cancellationToken)
        {
            //azure CDN endpoint taken from Octopus variables for Auth Server
            return Task.FromResult("https://svtsurveyassetstest.blob.core.windows.net/allvue-test/reportTemplates/savantaReportTemplate.potx");
        }

        public Task<ThemeDetails> GetThemeDetails(string shortCode, CancellationToken cancellationToken)
        {
            //default Savanta theme details taken from Auth Server
            return Task.FromResult(new ThemeDetails
            {
                CompanyDisplayName = "Savanta",
                LogoUrl = "https://savanta.test.all-vue.com/auth/logos/savantalogo.svg",
                HeaderBackgroundColour = "#191919",
                HeaderTextColour = "#ffffff",
                HeaderBorderColour = "#f4adb3",
                ShowHeaderBorder = true
            });
        }

        public async Task<Image> GetLogoImage(ThemeDetails themeDetails, CancellationToken cancellationToken)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            return await AuthApiClient.GetLogoImage(themeDetails, httpClient, cancellationToken);
        }

        public Task<string> GetFaviconUrl(string shortCode, CancellationToken cancellationToken)
        {
            return Task.FromResult("https://svtsurveyassetstest.blob.core.windows.net/brandvue-beta/brandvue/assets/favicon.ico");
        }

        public Task<IEnumerable<UserProjectsModel>> GetAllUserDetailsForCompanyScopeAsync(string shortCode,
            string currentUserId, string requestCompanyId, bool includeSavantaUsers, CancellationToken cancellationToken)
        {
            var productContext = _productContextProvider.ProvideProductContext(_requestScopeAccessor.RequestScope);
            var localOrgId = shortCode;

            var localUserProjects = new List<UserProjectsModel>
            {
                new UserProjectsModel
                {
                    ApplicationUserId = "1",
                    FirstName = "Aaron",
                    LastName = "Bertrand",
                    RoleName = Roles.Administrator,
                    Email = "aaron.bertrand@testorganisation.com",
                    OrganisationId = localOrgId,
                    OrganisationName = "Test organisation",
                    Projects = GetProjectsForUser("1", productContext)
                },
                new UserProjectsModel
                {
                    ApplicationUserId = "2",
                    FirstName = "Bernie",
                    LastName = "Bobbleton",
                    RoleName = Roles.Administrator,
                    Email = "bernie.bobbleton@testorganisation.com",
                    OrganisationId = localOrgId,
                    OrganisationName = "Test organisation",
                    Projects = GetProjectsForUser("2", productContext)
                },
                new UserProjectsModel
                {
                    ApplicationUserId = "3",
                    FirstName = "Gary",
                    LastName = "Client",
                    RoleName = Roles.User,
                    Email = "gary.client@testorganisation.com",
                    OrganisationId = localOrgId,
                    OrganisationName = "Test organisation",
                    Projects = GetProjectsForUser("3", productContext)
                },
                new UserProjectsModel
                {
                    ApplicationUserId = "4",
                    FirstName = "Jamie",
                    LastName = "Bones",
                    RoleName = Roles.User,
                    Email = "jamie.j.bones@testorganisation.com",
                    OrganisationId = localOrgId,
                    OrganisationName = "Test organisation",
                    Projects = GetProjectsForUser("4", productContext)
                },
                new UserProjectsModel
                {
                    ApplicationUserId = "5",
                    FirstName = "Les",
                    LastName = "Dennis",
                    RoleName = Roles.TrialUser,
                    Email = "les.dennis@testorganisation.com",
                    OrganisationId = localOrgId,
                    OrganisationName = "Test organisation",
                    Projects = GetProjectsForUser("5", productContext)
                },
                new UserProjectsModel
                {
                    ApplicationUserId = "LocalUserId",
                    FirstName = "Local User",
                    LastName = "Savanta",
                    RoleName = Roles.SystemAdministrator,
                    Email = "tech@savanta.com",
                    OrganisationId = localOrgId,
                    OrganisationName = "Test organisation",
                    Projects = GetProjectsForUser("LocalUserId", productContext)
                },
            };
            return Task.FromResult<IEnumerable<UserProjectsModel>>(localUserProjects);
        }

        private List<UserProject> GetProjectsForUser(string applicationUserId, IProductContext productContext)
        {
            var index = UserIdsAddedToProject.IndexOf(applicationUserId);
            if (index >= 0)
            {
                return new List<UserProject>
                {
                    new UserProject
                    {
                        Id = index,
                        ApplicationUserId = applicationUserId,
                        ProjectId = productContext.SubProductId,
                    }
                };
            }
            return new();
        }

        public Task AddUserProjects(string shortCode, IEnumerable<UserProject> projects,
            CancellationToken cancellationToken)
        {
            UserIdsAddedToProject = UserIdsAddedToProject
                .Concat(projects.Select(p => p.ApplicationUserId))
                .Distinct().ToList();
            return Task.CompletedTask;
        }

        public Task RemoveUserFromProject(string shortCode, int userProjectId, string userId,
            CancellationToken cancellationToken)
        {
            if (userProjectId >= 0 && userProjectId < UserIdsAddedToProject.Count)
            {
                UserIdsAddedToProject.RemoveAt(userProjectId);
            }
            return Task.CompletedTask;
        }

        public Task<bool> CanUserAccessProject(string shortCode, string userId, string projectId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task SetProjectShared(string projectId, bool isShared, string currentUserId,
            CancellationToken cancellationToken)
        {
            if (isShared)
            {
                SharedProjectIds.Add(projectId);
            }
            else
            {
                SharedProjectIds.Remove(projectId);
            }
            return Task.CompletedTask;
        }

        public Task<bool> IsProjectShared(string projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(SharedProjectIds.Contains(projectId));
        }

        public Task<IEnumerable<UserProject>> GetProjectsForUser(string userId, CancellationToken token)
        {
            var productContext = _productContextProvider.ProvideProductContext(_requestScopeAccessor.RequestScope);
            IEnumerable<UserProject> projects = GetProjectsForUser(userId, productContext);
            return Task.FromResult(projects);
        }

        public Task<IEnumerable<string>> GetSharedProjects(IEnumerable<string> projectIds, CancellationToken token)
        {
            IEnumerable<string> sharedProjects = new List<string>();
            return Task.FromResult(sharedProjects);
        }
    }
}

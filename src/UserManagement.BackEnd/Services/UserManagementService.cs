using AuthServer.GeneratedAuthApi;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Models;
using UserManagement.BackEnd.WebApi.Models;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.AuthApi.Models;
using UserProject = UserManagement.BackEnd.Models.UserProject;

namespace UserManagement.BackEnd.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IUserContext _userContext;
        private readonly IAuthApiClient _authApiClient;
        private readonly IUserServiceByAuth _userService;
        private readonly IUserFeaturePermissionRepository _userFeaturePermissionRepository;
        private readonly IUserDataPermissionRepository _userDataPermissionsRepository;
        private readonly IProjectsService _projectsService;
        private readonly IProductsService _productsService;
        private readonly IAllVueRuleRepository _allVueRuleRepository;
        private readonly IExtendedAuthApiClient _extendedAuthApiClient;

        public UserManagementService(
            IUserContext userContext,
            IAuthApiClient authApiClient,
            IUserServiceByAuth userService,
            IUserFeaturePermissionRepository userFeaturePermissionRepository,
            IUserDataPermissionRepository userDataPermissionRepository,
            IProjectsService projectsService,
            IProductsService productsService,
            IAllVueRuleRepository allVueRuleRepository,
            IExtendedAuthApiClient extendedAuthApiClient)
        {
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _authApiClient = authApiClient ?? throw new ArgumentNullException(nameof(authApiClient));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _userFeaturePermissionRepository = userFeaturePermissionRepository ?? throw new ArgumentNullException(nameof(userFeaturePermissionRepository));
            _userDataPermissionsRepository = userDataPermissionRepository;
            _projectsService = projectsService ?? throw new ArgumentNullException(nameof(projectsService));
            _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
            _allVueRuleRepository = allVueRuleRepository;
            _extendedAuthApiClient = extendedAuthApiClient;
        }

        private async Task<CompanyModel> GetCompanyAsync(string? companyId, CancellationToken cancellationToken)
        {   
            if (string.IsNullOrEmpty(companyId))
            {
                return await _authApiClient.GetCompanyByShortcode(_userContext.AuthCompany, cancellationToken);
            }
            else
            {
                var companies = await _authApiClient.GetCompanies([companyId], cancellationToken);
                return companies.FirstOrDefault()!;
            }
        }

        private User GetUserFromUserProjectModel(UserProjectsModel user, Dictionary<string, RoleDetails> userRoleLookup, Dictionary<string, List<DataGroup>> dataGroupLookup, Dictionary<string, List<DataGroup>> companyDataGroupLookup)
        {
            var validRole = userRoleLookup.TryGetValue(user.ApplicationUserId, out var roleFromPermissions);
            var hasDataGroups = dataGroupLookup.TryGetValue(user.ApplicationUserId, out var dataGroupList);
            var projects = user.Projects.Select(up => new UserProject(
                _projectsService.AuthProjectToProjectIdentifier(up.ProjectId), user.OrganisationId, 0, "")).ToList();
            if (hasDataGroups)
            {
                projects.AddRange(dataGroupList.Select(dataGroup => dataGroup.ToUserProject()));
            }

            if (companyDataGroupLookup.TryGetValue(user.OrganisationId, out var sharedDataGroupList))
            {
                projects.AddRange(sharedDataGroupList.Select(dataGroup => dataGroup.ToUserProject()));
            }

            return new User
            {
                Id = user.ApplicationUserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                LastLogin = user.LastLogin,
                Verified = user.Verified,
                OwnerCompanyDisplayName = user.OrganisationName,
                OwnerCompanyId = user.OrganisationId,
                Role = validRole
                    ? roleFromPermissions.RoleName
                    : user.RoleName, 
                RoleId = roleFromPermissions?.Id,
                IsExternalLogin = user.IsOrganisationExternalLogin,
                Projects = projects,
                Products = _productsService.ToProducts(user.Products),
                SurveyVueEditingAvailable = user.Products?.Any( x=> x.ShortCode == ProductsService.AuthProductIdFor_SurveyVueEditor)??false,
                SurveyVueFeedbackAvailable = user.Products?.Any(x => x.ShortCode == ProductsService.AuthProductIdFor_SurveyVueFeedback) ?? false,

            };
        }

        private async Task<Dictionary<string, List<DataGroup>>> GetSharedDataGroupDictionary(string[] companyIds, List<CompanyNode> companyAndChildren, CancellationToken cancellationToken)
        {
            var allVueRules = await _allVueRuleRepository.GetByCompaniesAsync(companyIds, cancellationToken);
            var sharedDataGroups = allVueRules.Where(rule => rule.AllUserAccessForSubProduct);

            if (!sharedDataGroups.Any())
            {
                return new Dictionary<string, List<DataGroup>>();
            }

            var companyDataGroupLookup = sharedDataGroups
                .GroupBy(dg => dg.Organisation)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(rule => rule.ToDataGroup()).ToList()
                );

            var parentChildDataGroupLookup = new Dictionary<string, List<DataGroup>>();

            List<DataGroup> GetDescendantDataGroups(CompanyNode parent)
            {
                var dataGroups = companyDataGroupLookup.TryGetValue(parent.Id, out var dg) ? dg : new List<DataGroup>();

                dataGroups.AddRange(parent.Children.SelectMany(child => GetDescendantDataGroups(child)));
                return dataGroups;
            }

            foreach (var parent in companyAndChildren)
            {
                parentChildDataGroupLookup.Add(parent.Id, GetDescendantDataGroups(parent));
            }

            return parentChildDataGroupLookup;
        }

        record RoleDetails(int Id, string RoleName);
        private async Task<IEnumerable<User>> GetUsersFromUserProjectModelsAsync(IEnumerable<UserProjectsModel> users, CancellationToken cancellationToken)
        {
            var companyAndChildren = (await _extendedAuthApiClient
                .GetCompanyAndChildrenList(_userContext.AuthCompany, cancellationToken)).ToList();
            var companyIds = companyAndChildren.Select(company => company.Id).ToArray();

            var userFeaturePermissions = await _userFeaturePermissionRepository.GetAllAsync();

            var userRoleLookup = userFeaturePermissions
                .ToDictionary(
                    permission => permission.UserId,
                    permission => new RoleDetails(permission.UserRole.Id, RoleName: permission.UserRole.RoleName)
                );

            var dataGroups = await _userDataPermissionsRepository.GetByCompaniesAsync(companyIds, cancellationToken);
            var userDataGroupLookup = dataGroups
                .GroupBy(dg => dg.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(dataPermission => dataPermission.ToDataGroup()).ToList()
                );

            var companyDataGroupLookup = await GetSharedDataGroupDictionary(companyIds, companyAndChildren, cancellationToken);

            return users.Select(user => GetUserFromUserProjectModel(user, userRoleLookup, userDataGroupLookup, companyDataGroupLookup)).ToList();
        }

        public async Task<IEnumerable<User>> GetUsersWithRolesAsync(bool includeSavantaUsers, string? companyId, CancellationToken cancellationToken)
        {
            CompanyModel company = await GetCompanyAsync(companyId, cancellationToken);
            if (company == null)
            {
                throw new ArgumentException("Company not found", nameof(companyId));
            }

            var users = await _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                company.ShortCode,
                _userContext.UserId,
                company.Id,
                includeSavantaUsers,
                true,
                cancellationToken);

            return await GetUsersFromUserProjectModelsAsync(users, cancellationToken);
        }

        public async Task<IEnumerable<User>> GetUsersForProjectByCompanyAsync(string companyId, CancellationToken token)
        {
            var includeSavantaUsers = false;

            CompanyModel company = await GetCompanyAsync(companyId, token);
            if (company == null)
            {
                throw new ArgumentException("Company not found", nameof(companyId));
            }

            var currentUserId = _userContext.UserId;
            var users = await _authApiClient.GetAllUserDetailsForCompanyScopeAsync(company.ShortCode, currentUserId, companyId,
                includeSavantaUsers, token);
                
            return await GetUsersFromUserProjectModelsAsync(users, token);
        }
    }
}

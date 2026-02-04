using AuthServer.GeneratedAuthApi;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Models;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

namespace UserManagement.BackEnd.Services
{
    public class UserOrchestratorService : IUserOrchestratorService
    {
        private readonly IUserServiceByAuth _userServiceByAuth;
        private readonly IUserDataPermissionRepository _userDataPermissionRepository;
        private readonly IUserFeaturePermissionRepository _userFeaturePermissionRepository;
        private readonly IUserContext _userContext;
        private readonly IProductsService _productsService;
        private readonly IRoleRepository _roleRepository;

        public UserOrchestratorService(IUserContext userContext, 
            IUserServiceByAuth userServiceByAuth, 
            IUserDataPermissionRepository userDataPermissionRepository, 
            IUserFeaturePermissionRepository userFeaturePermissionRepository,
            IRoleRepository roleRepository,
            IProductsService productsService)
        {
            _userServiceByAuth = userServiceByAuth;
            _userDataPermissionRepository = userDataPermissionRepository;
            _userFeaturePermissionRepository = userFeaturePermissionRepository;
            _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
            _productsService = productsService ?? throw new ArgumentNullException(nameof(productsService));
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        }

        private string UserManagementRoleToAuthServerRoleName(string name, int? roleId)
        {
            if (roleId.HasValue)
            {
                return "User";
            }
            return name switch
            {
                "Administrator" => "Administrator",
                "SystemAdministrator" => "SystemAdministrator",
                "User" => "User",
                "ReportViewer" => "ReportViewer",
                _ => "User"
            };
        }
        public async Task AddUser(UserToAdd user, string userCompanyShortCode, string updatedByUserId, CancellationToken token)
        {
            var products = _productsService.GetAuthProductIds(user.Products, user.SurveyVueEditingAvailable,
                user.SurveyVueFeedbackAvailable);

            var userCreated = await _userServiceByAuth.CreateUserAsync(userCompanyShortCode, updatedByUserId, new UserAddDetails()
            {
                OrganizationId = user.OwnerCompanyId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = UserManagementRoleToAuthServerRoleName(user.Role, user.RoleId),
                Products = products,
                Email = user.Email,
            }, token);

            if (user.RoleId.HasValue)
            {
                var rolesAvailable = await _roleRepository.GetByOrganisationIdAsync(user.OwnerCompanyId);
                var permission = new Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
                    userCreated.ApplicationUserId,
                    rolesAvailable.Single(x => x.Id == user.RoleId),
                    updatedByUserId
                );
                await _userFeaturePermissionRepository.UpsertAsync(permission);
            }
        }

        public async Task UpdateUser(User user, string userCompanyShortCode, string updatedByUserId, CancellationToken token)
        {
            var products = _productsService.GetAuthProductIds(user.Products, user.SurveyVueEditingAvailable,
                user.SurveyVueFeedbackAvailable);

            await _userServiceByAuth.UpdateUserAsync(userCompanyShortCode, updatedByUserId, new UserUpdateDetails()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RoleName = UserManagementRoleToAuthServerRoleName(user.Role, user.RoleId),
                Products = products,
            }, token);

            if (user.RoleId.HasValue)
            {
                var rolesAvailable = await _roleRepository.GetByOrganisationIdAsync(user.OwnerCompanyId);
                var permission = new Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
                    user.Id,
                    rolesAvailable.Single(x => x.Id == user.RoleId),
                    updatedByUserId
                );

                await _userFeaturePermissionRepository.UpsertAsync(permission);
            }
            else
            {
                await _userFeaturePermissionRepository.DeleteByUserIdAsync(user.Id);
            }
        }

        public async Task DeleteUser(string userCompanyShortCode, string requesterEmail, string userIdOfUserToDelete, CancellationToken token)
        {
            if (_userContext.UserId == userIdOfUserToDelete)
            {
                throw new UnauthorizedAccessException("You can not delete yourself.");
            }

            await _userServiceByAuth.DeleteUser(userCompanyShortCode, requesterEmail, userIdOfUserToDelete, token);
            await _userDataPermissionRepository.DeleteAllPermissionsForUserAsync(userIdOfUserToDelete, token);
            await _userFeaturePermissionRepository.DeleteAllPermissionsForUserAsync(userIdOfUserToDelete, token);
        }

        public async Task ForgotPassword(string userCompanyShortCode, string userEmail, CancellationToken token)
        {
            await _userServiceByAuth.ResendEmail(userCompanyShortCode, userEmail, token);
        }

        public async Task<User> GetUserAsync(string userCompanyShortCode, string userId, CancellationToken token)
        {
            var userFeaturePermissions = await _userFeaturePermissionRepository.GetByUserIdAsync(userId);
            var user = await _userServiceByAuth.GetUserAsync(userCompanyShortCode, userId, _userContext.UserId, token);
            var userRoleLookup = userFeaturePermissions != null ? userFeaturePermissions.UserRole.RoleName : user.RoleName;
            return new User
            {
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Id = user.ApplicationUserId,
                OwnerCompanyId = user.OrganisationId,
                OwnerCompanyDisplayName = user.OrganisationName,
                IsExternalLogin = user.IsOrganisationExternalLogin,
                LastLogin = user.LastLogin,
                Verified = user.Verified,
                Role = userRoleLookup,
                RoleId = userFeaturePermissions?.UserRole.Id,
                Products = _productsService.ToProducts(user.Products),
                Projects = new List<Models.UserProject>(),
                SurveyVueEditingAvailable = user.Products?.Any(x => x.ShortCode == ProductsService.AuthProductIdFor_SurveyVueEditor) ?? false,
                SurveyVueFeedbackAvailable = user.Products?.Any(x => x.ShortCode == ProductsService.AuthProductIdFor_SurveyVueFeedback) ?? false,
            };
        }

    }
}
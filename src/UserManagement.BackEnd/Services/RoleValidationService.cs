using AuthServer.GeneratedAuthApi;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

namespace UserManagement.BackEnd.Services
{
    public class RoleValidationService : IRoleValidationService
    {
        private readonly IPermissionOptionRepository _permissionOptionRepository;
        private readonly IUserContext _userContext;
        private readonly IAuthApiClient _authApiClient;
        private readonly IRoleRepository _roleRepository;

        public RoleValidationService(
            IPermissionOptionRepository permissionOptionRepository,
            IUserContext userContext,
            IAuthApiClient authApiClient,
            IRoleRepository roleRepository)
        {
            _permissionOptionRepository = permissionOptionRepository;
            _userContext = userContext;
            _authApiClient = authApiClient;
            _roleRepository = roleRepository;
        }

        public async Task ValidateRole(string roleName, IEnumerable<int> permissionOptionIds, int? existingRoleId = null,
            CancellationToken cancellationToken = default)
        {
            var organisationShortCode = GetOrganisationShortCodeFromUserContext();
            var organisation = await GetOrganisationByShortcode(organisationShortCode, cancellationToken);
            await ValidateRoleName(roleName, organisation.Id, existingRoleId, cancellationToken);
            await ValidatePermissionOptionIdsAsync(permissionOptionIds, cancellationToken);
        }

        internal async Task ValidateRoleName(string roleName, string organisationId, int? existingRoleId = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(roleName) || roleName.Length > Role.MaxRoleNameLength)
                throw new ValidationException($"Role name must be between 1 and {Role.MaxRoleNameLength} characters.");

            var roles = await _roleRepository.GetByOrganisationIdAsync(organisationId);
            if (roles.Any(r => string.Equals(r.RoleName, roleName, StringComparison.OrdinalIgnoreCase) && r.Id != existingRoleId))
                throw new ValidationException($"Role name '{roleName}' already exists in this organisation.");
        }

        internal async Task ValidatePermissionOptionIdsAsync(
            IEnumerable<int> permissionOptionIds, CancellationToken cancellationToken = default)
        {
            var allValidOptions = await _permissionOptionRepository.GetAllAsync(cancellationToken);
            var allValidOptionIds = allValidOptions.Select(o => o.Id).ToHashSet();

            if (permissionOptionIds.Any(p => !allValidOptionIds.Contains(p)))
                throw new ValidationException("Invalid permissions specified.");
        }

        private string GetOrganisationShortCodeFromUserContext(CancellationToken cancellationToken = default)
        {
            var organisationShortCode = _userContext.UserOrganisation;
            if (string.IsNullOrEmpty(organisationShortCode))
                throw new ValidationException("User is not associated with any organisation");

            return organisationShortCode;
        }

        private async Task<CompanyModel> GetOrganisationByShortcode(string organisationShortCode, CancellationToken cancellationToken = default)
        {
            var organisation = await _authApiClient.GetCompanyByShortcode(organisationShortCode, cancellationToken)
                ?? throw new ValidationException($"Organisation not found for short code '{organisationShortCode}'");

            return organisation;
        }
    }
}
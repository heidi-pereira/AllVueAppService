using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;
using Vue.Common.AuthApi;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.CreateRole
{
    public record CreateRoleCommand(
        string RoleName,
        List<int> PermissionOptionIds
    ) : IRequest<RoleDto>;

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, RoleDto>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionOptionRepository _permissionOptionRepository;
        private readonly IUserContext _userContext;
        private readonly IAuthApiClient _authApiClient;
        private readonly IRoleValidationService _roleValidationService; // Added

        public CreateRoleCommandHandler(
            IRoleRepository roleRepository,
            IPermissionOptionRepository permissionOptionRepository,
            IUserContext userContext,
            IAuthApiClient authApiClient,
            IRoleValidationService roleValidationService)
        {
            _roleRepository = roleRepository;
            _permissionOptionRepository = permissionOptionRepository;
            _userContext = userContext;
            _authApiClient = authApiClient;
            _roleValidationService = roleValidationService;
        }

        public async Task<RoleDto> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            await _roleValidationService.ValidateRole(request.RoleName, request.PermissionOptionIds, null, cancellationToken);

            var allValidOptions = await _permissionOptionRepository.GetAllAsync(cancellationToken);
            var organisationShortCode = _userContext.UserOrganisation;
            var organisation = await _authApiClient.GetCompanyByShortcode(organisationShortCode, cancellationToken);
            var role = new Role(request.RoleName, organisation.Id, _userContext.UserId);

            var permissionOptions = allValidOptions.Where(o => request.PermissionOptionIds.Contains(o.Id)).ToList();

            foreach (var option in permissionOptions)
            {
                role.AssignPermission(option);
            }

            var newRole = await _roleRepository.AddAsync(role);

            return new RoleDto(
                newRole.Id,
                newRole.RoleName,
                newRole.OrganisationId,
                newRole.Options.Select(p => new PermissionFeatureOptionDto(p.Id, p.Name)).ToList()
            );
        }
    }
}
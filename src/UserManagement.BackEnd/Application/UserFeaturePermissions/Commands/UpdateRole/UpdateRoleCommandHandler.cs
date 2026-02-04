using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.UpdateRole
{
    public record UpdateRoleCommand(
        int RoleId,
        string RoleName,
        List<int> PermissionOptionIds,
        string UpdatedByUserId
    ) : IRequest<RoleDto>;

    public class UpdateRoleCommandHandler : IRequestHandler<UpdateRoleCommand, RoleDto>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionOptionRepository _permissionOptionRepository;
        private readonly IRoleValidationService _roleValidationService;

        public UpdateRoleCommandHandler(
            IRoleRepository roleRepository,
            IPermissionOptionRepository permissionOptionRepository,
            IRoleValidationService roleValidationService)
        {
            _roleRepository = roleRepository;
            _permissionOptionRepository = permissionOptionRepository;
            _roleValidationService = roleValidationService;
        }

        public async Task<RoleDto> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            await _roleValidationService.ValidateRole(request.RoleName, request.PermissionOptionIds, request.RoleId, cancellationToken);

            var existingRole = await _roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
            if (existingRole == null)
                throw new KeyNotFoundException($"Role with ID {request.RoleId} not found.");

            var allValidOptions = await _permissionOptionRepository.GetAllAsync(cancellationToken);
            var newPermissionOptions = allValidOptions
                .Where(o => request.PermissionOptionIds.Contains(o.Id))
                .ToList();

            existingRole.Update(request.RoleName, request.UpdatedByUserId, newPermissionOptions);

            await _roleRepository.UpdateAsync(existingRole, cancellationToken);

            return new RoleDto(
                existingRole.Id,
                existingRole.RoleName,
                existingRole.OrganisationId,
                existingRole.Options.Select(p => new PermissionFeatureOptionDto(p.Id, p.Name)).ToList()
            );
        }
    }
}
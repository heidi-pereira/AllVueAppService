using Azure.Core;
using MediatR;
using System.ComponentModel.DataAnnotations;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteRole
{
    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserFeaturePermissionRepository _userFeaturePermissionRepository;

        public DeleteRoleCommandHandler(
            IRoleRepository roleRepository,
            IUserFeaturePermissionRepository userFeaturePermissionRepository)
        {
            _roleRepository = roleRepository;
            _userFeaturePermissionRepository = userFeaturePermissionRepository;
        }

        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            await ValidateRoleForDeletion(request.RoleId, cancellationToken);
            await _roleRepository.DeleteAsync(request.RoleId, cancellationToken);

            return true;
        }

        private async Task ValidateRoleForDeletion(int requestRoleId, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(requestRoleId, cancellationToken);
            if (role == null)
                throw new ValidationException($"Role with ID {requestRoleId} does not exist.");

            var isAssignedToUsers = await _userFeaturePermissionRepository.HasRoleAssignments(role.Id, cancellationToken);
            if (isAssignedToUsers)
                throw new ValidationException("Cannot delete role: it is assigned to one or more users.");
        }
    }
}

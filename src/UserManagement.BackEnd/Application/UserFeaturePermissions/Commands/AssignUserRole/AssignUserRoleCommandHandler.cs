using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole
{
    public class AssignUserRoleCommandHandler : IRequestHandler<AssignUserRoleCommand, UserFeaturePermissionDto>
    {
        private readonly IUserFeaturePermissionRepository _userFeaturePermissionRepository;
        private readonly IRoleRepository _roleRepository;

        public AssignUserRoleCommandHandler(
            IUserFeaturePermissionRepository userFeaturePermissionRepository,
            IRoleRepository roleRepository)
        {
            _userFeaturePermissionRepository = userFeaturePermissionRepository;
            _roleRepository = roleRepository;
        }

        public async Task<UserFeaturePermissionDto> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.UserRoleId, cancellationToken);
            if (role == null)
            {
                throw new ArgumentException($"Role with ID {request.UserRoleId} not found");
            }

            var permission = new UserFeaturePermission(
                request.UserId,
                role,
                request.UpdatedByUserId
            );
            
            var savedPermission = await _userFeaturePermissionRepository.UpsertAsync(permission);

            return new UserFeaturePermissionDto(
                savedPermission.Id,
                savedPermission.UserId,
                savedPermission.UserRoleId,
                savedPermission.UpdatedByUserId,
                savedPermission.UpdatedDate
            );
        }
    }
}

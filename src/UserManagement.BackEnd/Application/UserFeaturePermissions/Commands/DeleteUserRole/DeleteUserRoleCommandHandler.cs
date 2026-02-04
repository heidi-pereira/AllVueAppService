using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteUserRole
{
    public class DeleteUserRoleCommandHandler : IRequestHandler<DeleteUserRoleCommand, bool>
    {
        private readonly IUserFeaturePermissionRepository _userFeaturePermissionRepository;

        public DeleteUserRoleCommandHandler(IUserFeaturePermissionRepository userFeaturePermissionRepository)
        {
            _userFeaturePermissionRepository = userFeaturePermissionRepository;
        }

        public async Task<bool> Handle(DeleteUserRoleCommand request, CancellationToken cancellationToken)
        {
            await _userFeaturePermissionRepository.DeleteByUserIdAsync(request.UserId);
            return true;
        }
    }
}

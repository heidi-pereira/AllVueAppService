using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserFeaturePermissions
{
    public class GetAllUserFeaturePermissionsQueryHandler : IRequestHandler<GetAllUserFeaturePermissionsQuery, IEnumerable<UserFeaturePermissionDto>>
    {
        private readonly IUserFeaturePermissionRepository _repository;

        public GetAllUserFeaturePermissionsQueryHandler(IUserFeaturePermissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<UserFeaturePermissionDto>> Handle(GetAllUserFeaturePermissionsQuery request, CancellationToken cancellationToken)
        {
            var permissions = await _repository.GetAllAsync();
            
            return permissions.Select(permission => new UserFeaturePermissionDto(
                permission.Id,
                permission.UserId,
                permission.UserRoleId,
                permission.UpdatedByUserId,
                permission.UpdatedDate
            ));
        }
    }
}

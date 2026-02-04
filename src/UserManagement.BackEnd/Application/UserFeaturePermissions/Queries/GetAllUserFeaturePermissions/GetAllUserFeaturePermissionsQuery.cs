using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserFeaturePermissions
{
    public class GetAllUserFeaturePermissionsQuery : IRequest<IEnumerable<UserFeaturePermissionDto>>
    {
    }
}

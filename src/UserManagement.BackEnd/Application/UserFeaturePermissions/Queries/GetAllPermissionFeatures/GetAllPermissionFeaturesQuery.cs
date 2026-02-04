using MediatR;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllFeaturePermissions
{
    public class GetAllPermissionFeaturesQuery : IRequest<IEnumerable<PermissionFeatureDto>>
    {
    }
}
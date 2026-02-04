using MediatR;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserPermissionFeatures
{
    public class GetAllUserPermissionOptionsQuery : IRequest<IEnumerable<IPermissionFeatureOption>>
    {
        public string UserId { get; }
        public string DefaultRole { get; }

        public GetAllUserPermissionOptionsQuery(string userId, string defaultRole)
        {
            UserId = userId;
            DefaultRole = defaultRole;
        }
    }
}
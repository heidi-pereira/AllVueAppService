using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserPermissionFeatures
{
    public class GetAllUserPermissionOptionsQueryHandler(IUserFeaturePermissionRepository repository) : IRequestHandler<GetAllUserPermissionOptionsQuery, IEnumerable<IPermissionFeatureOption>>
    {
        private readonly IUserFeaturePermissionRepository _repository = repository;

        public async Task<IEnumerable<IPermissionFeatureOption>> Handle(GetAllUserPermissionOptionsQuery request, CancellationToken cancellationToken)
        {
            var userFeaturePermission = await _repository.GetByUserIdAsync(request.UserId);
            if (userFeaturePermission == null)
            {
                var permissions = request.DefaultRole.DefaultPermissions();
                return permissions.Select(o => new PermissionFeatureOptionDto(o.Id, o.Name));
            }
            if (userFeaturePermission.UserRole == null || userFeaturePermission.UserRole.Options == null || !userFeaturePermission.UserRole.Options.Any())
            {
                return [];
            }
            return userFeaturePermission.UserRole.Options.Select(o => new PermissionFeatureOptionDto(o.Id, o.Name));
        }
    }
}
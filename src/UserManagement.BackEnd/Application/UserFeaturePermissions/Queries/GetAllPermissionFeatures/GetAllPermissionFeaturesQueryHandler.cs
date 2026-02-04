using MediatR;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllFeaturePermissions;
using Vue.Common.Auth.Permissions;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllPermissionFeatures
{
    public class GetAllPermissionFeaturesQueryHandler(IPermissionFeatureRepository repository) : IRequestHandler<GetAllPermissionFeaturesQuery, IEnumerable<PermissionFeatureDto>>
    {
        private readonly IPermissionFeatureRepository _repository = repository;

        public async Task<IEnumerable<PermissionFeatureDto>> Handle(GetAllPermissionFeaturesQuery request, CancellationToken cancellationToken)
        {
            var features = await _repository.GetAllAsync(cancellationToken);
            if (features == null || !features.Any())
            {
                return [];
            }

            return features.Select(f => new PermissionFeatureDto(
                f.Id,
                f.Name,
                f.SystemKey.ToString(),
                [.. f.Options.Select(o => new PermissionFeatureOptionDto(
                    o.Id,
                    o.Name
                ))]
            ));
        }
    }
}
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using InfrastructureFeature = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionFeature;

namespace UserManagement.BackEnd.Domain.UserFeaturePermissions
{
    public static class PermissionFeatureMapper
    {
        public static PermissionFeature MapFromInfrastructure(InfrastructureFeature infraFeature)
        {
            var feature = new PermissionFeature(
                infraFeature.Id,
                infraFeature.Name,
                infraFeature.SystemKey
            );
            foreach (var infraOption in infraFeature.Options)
            {
                var option = new PermissionOption(
                    infraOption.Id,
                    infraOption.Name,
                    feature
                );
                feature.AddOption(option);
            }
            return feature;
        }

        public static InfrastructureFeature MapToInfrastructure(PermissionFeature domainFeature)
        {
            return new InfrastructureFeature
            {
                Id = domainFeature.Id,
                Name = domainFeature.Name,
                SystemKey = domainFeature.SystemKey,
                Options = domainFeature.Options.Select(PermissionOptionMapper.MapToInfrastructure).ToList()
            };
        }
    }
}
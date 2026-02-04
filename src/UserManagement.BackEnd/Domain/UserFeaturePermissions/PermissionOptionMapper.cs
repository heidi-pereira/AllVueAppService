using InfrastructureOption = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionOption;

namespace UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

public static class PermissionOptionMapper
{
    public static PermissionOption MapFromInfrastructure(InfrastructureOption infraOption)
    {
        if (infraOption == null)
            throw new ArgumentNullException(nameof(infraOption));
            
        if (infraOption.Feature == null)
            throw new InvalidOperationException($"Feature is null for PermissionOption {infraOption.Id}. Ensure the Feature property is included in the query.");
            
        return new PermissionOption(
            infraOption.Id,
            infraOption.Name,
            new PermissionFeature(
                infraOption.Feature.Name,
                infraOption.Feature.SystemKey
            )
        );
    }

    public static InfrastructureOption MapToInfrastructure(PermissionOption domainOption)
    {
        var infraOption = new InfrastructureOption
        {
            Id = domainOption.Id,
            Name = domainOption.Name,
            Feature = new BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionFeature
            {
                Name = domainOption.Feature.Name,
                SystemKey = domainOption.Feature.SystemKey
            }
        };
        return infraOption;
    }
}

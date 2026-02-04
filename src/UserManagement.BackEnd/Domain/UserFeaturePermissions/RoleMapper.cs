using InfrastructureRole = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.Role;

namespace UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;

public static class RoleMapper
{
    public static Role MapFromInfrastructure(InfrastructureRole infraRole)
    {
        var role = new Role(
            infraRole.Id, // Assuming Id is accessible; if not, adjust accordingly
            infraRole.RoleName,
            infraRole.OrganisationId,
            infraRole.UpdatedByUserId
        );

        // If you need to set Id and UpdatedDate (private setters), use reflection or adjust Role class as needed.
        // Map permissions if available
        if (infraRole.Options != null)
        {
            foreach (var infraOption in infraRole.Options)
            {
                var option = PermissionOptionMapper.MapFromInfrastructure(infraOption);
                role.AssignPermission(option);
            }
        }

        return role;
    }

    public static InfrastructureRole MapToInfrastructure(Role domainRole)
    {
        var infraRole = new InfrastructureRole
        {
            RoleName = domainRole.RoleName,
            OrganisationId = domainRole.OrganisationId,
            UpdatedByUserId = domainRole.UpdatedByUserId,
            UpdatedDate = domainRole.UpdatedDate,
            // Set Id if needed and accessible
            // Id = domainRole.Id,
        };

        if (domainRole.Options != null)
        {
            infraRole.Options = [.. domainRole.Options.Select(PermissionOptionMapper.MapToInfrastructure)];
        }

        return infraRole;
    }
}

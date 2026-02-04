using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using InfrastructureUserPermission = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.UserFeaturePermission;

namespace UserManagement.BackEnd.Domain.UserFeaturePermissions
{
    public class UserFeaturePermissionMapper
    {
        public static UserFeaturePermission MapFromInfrastructure(InfrastructureUserPermission infraOption)
        {
            var domainEntity = new UserFeaturePermission(
                infraOption.Id,
                infraOption.UserId,
                RoleMapper.MapFromInfrastructure(infraOption.UserRole),
                infraOption.UpdatedByUserId
            );
            
            return domainEntity;
        }

        public static InfrastructureUserPermission MapToInfrastructure(UserFeaturePermission domainOption)
        {
            var infraOption = new InfrastructureUserPermission
            {
                Id = domainOption.Id,
                UserId = domainOption.UserId,
                UserRoleId = domainOption.UserRoleId,
                UserRole = RoleMapper.MapToInfrastructure(domainOption.UserRole),
                UpdatedByUserId = domainOption.UpdatedByUserId,
                UpdatedDate = domainOption.UpdatedDate
            };
            return infraOption;
        }
    }
}
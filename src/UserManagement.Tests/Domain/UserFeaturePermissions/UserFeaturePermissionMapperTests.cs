using UserManagement.BackEnd.Domain.UserFeaturePermissions;
using Role = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.Role;
using UserFeaturePermission = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.UserFeaturePermission;

namespace UserManagement.Tests.Domain.UserFeaturePermissions
{
    [TestFixture]
    public class UserFeaturePermissionMapperTests
    {
        [Test]
        public void MapFromInfrastructure_MapsCorrectly()
        {
            // Arrange
            var infraRole = new Role
            {
                Id = 5,
                RoleName = "Manager",
                OrganisationId = "OrgX",
                UpdatedByUserId = "User42",
                UpdatedDate = new DateTime(2024, 3, 3)
            };
            var infraUserPermission = new UserFeaturePermission
            {
                Id = 100,
                UserId = "User123",
                UserRoleId = 5,
                UserRole = infraRole,
                UpdatedByUserId = "User42",
                UpdatedDate = new DateTime(2024, 3, 3)
            };

            // Act
            var domainUserPermission = UserFeaturePermissionMapper.MapFromInfrastructure(infraUserPermission);

            // Assert
            Assert.That(domainUserPermission.UserId, Is.EqualTo(infraUserPermission.UserId));
            Assert.That(domainUserPermission.UserRole.RoleName, Is.EqualTo(infraRole.RoleName));
            Assert.That(domainUserPermission.UserRole.OrganisationId, Is.EqualTo(infraRole.OrganisationId));
            Assert.That(domainUserPermission.UpdatedByUserId, Is.EqualTo(infraUserPermission.UpdatedByUserId));
        }

        [Test]
        public void MapToInfrastructure_MapsCorrectly()
        {
            // Arrange
            var domainRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
                7, "Supervisor", "OrgY", "User99"
            );

            var domainUserPermission = new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
                "User300",
                domainRole,
                "User99"
            );

            // Act
            var infraUserPermission = UserFeaturePermissionMapper.MapToInfrastructure(domainUserPermission);

            // Assert
            Assert.That(infraUserPermission.Id, Is.EqualTo(0));
            Assert.That(infraUserPermission.UserId, Is.EqualTo("User300"));
            Assert.That(infraUserPermission.UserRoleId, Is.EqualTo(7));
            Assert.That(infraUserPermission.UserRole.RoleName, Is.EqualTo("Supervisor"));
            Assert.That(infraUserPermission.UserRole.OrganisationId, Is.EqualTo("OrgY"));
            Assert.That(infraUserPermission.UpdatedByUserId, Is.EqualTo("User99"));
            Assert.That(infraUserPermission.UpdatedDate, Is.LessThan(DateTime.Now));
        }
    }
}

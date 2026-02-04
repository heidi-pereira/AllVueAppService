using Role = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role;
using PermissionOption = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.PermissionOption;
using PermissionFeature = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.PermissionFeature;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetRolesByCompany;
using BrandVue.EntityFramework.MetaData.Authorisation;

namespace UserManagement.BackEnd.Tests.UserFeaturePermissions.Queries
{
    [TestFixture]
    public class GetRolesByCompanyQueryHandlerTests
    {
        private IRoleRepository _roleRepositorySub = null!;
        private GetRolesByCompanyQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _roleRepositorySub = Substitute.For<IRoleRepository>();
            _handler = new GetRolesByCompanyQueryHandler(_roleRepositorySub);
        }

        [Test]
        public async Task Handle_ReturnsRolesForCompanyOnly()
        {
            // Arrange
            var companyId = "company-123";

            var allRoles = new List<Role>
            {
                new Role(1, "Admin", "company-123", "system@example.com"),
                new Role(2, "User", "company-123", "system@example.com"),
                new Role(3, "Manager", "other-company", "system@example.com"), // Should be filtered out
                new Role(4, "External", "another-company", "system@example.com") // Should be filtered out
            };

            _roleRepositorySub.GetAllAsync().Returns(allRoles);

            // Act
            var result = await _handler.Handle(new GetRolesByCompanyQuery(companyId), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(2)); // Should only include roles for company-123
            Assert.That(resultList.Any(r => r.RoleName == "Admin" && r.Organisation == "company-123"), Is.True);
            Assert.That(resultList.Any(r => r.RoleName == "User" && r.Organisation == "company-123"), Is.True);
            Assert.That(resultList.Any(r => r.Organisation == "other-company"), Is.False);
            Assert.That(resultList.Any(r => r.Organisation == "another-company"), Is.False);
        }

        [Test]
        public async Task Handle_ReturnsEmptyWhenNoRolesFoundForCompany()
        {
            // Arrange
            var companyId = "nonexistent-company";
            var allRoles = new List<Role>
            {
                new Role(1, "Admin", "other-company", "system@example.com"),
                new Role(2, "User", "another-company", "system@example.com")
            };
            
            _roleRepositorySub.GetAllAsync().Returns(allRoles);

            // Act
            var result = await _handler.Handle(new GetRolesByCompanyQuery(companyId), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_CallsRoleRepositoryOnly()
        {
            // Arrange
            var companyId = "company-123";
            var allRoles = new List<Role>
            {
                new Role(1, "Admin", "company-123", "system@example.com")
            };
            
            _roleRepositorySub.GetAllAsync().Returns(allRoles);

            // Act
            await _handler.Handle(new GetRolesByCompanyQuery(companyId), CancellationToken.None);

            // Assert
            await _roleRepositorySub.Received(1).GetAllAsync();
        }

        [Test]
        public async Task Handle_MapsRolePermissionsCorrectly()
        {
            // Arrange
            var companyId = "company-123";

            var roleWithPermissions = new Role(1, "Admin", "company-123", "system@example.com");
            var systemKey = SystemKey.AllVue;
            var feature = new PermissionFeature(1, "USER_MANAGEMENT", systemKey);
            var permission1 = new PermissionOption(1, "VIEW_USERS", feature);
            var permission2 = new PermissionOption(2, "EDIT_USERS", feature);
            roleWithPermissions.AssignPermission(permission1);
            roleWithPermissions.AssignPermission(permission2);

            var allRoles = new List<Role> { roleWithPermissions };

            _roleRepositorySub.GetAllAsync().Returns(allRoles);

            // Act
            var result = await _handler.Handle(new GetRolesByCompanyQuery(companyId), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            var resultList = result.ToList();
            Assert.That(resultList.Count, Is.EqualTo(1));
            
            var roleDto = resultList.First();
            Assert.That(roleDto.RoleName, Is.EqualTo("Admin"));
            Assert.That(roleDto.Organisation, Is.EqualTo("company-123"));
            Assert.That(roleDto.Permissions.Count, Is.EqualTo(2));
            Assert.That(roleDto.Permissions.Any(p => p.Name == "VIEW_USERS"), Is.True);
            Assert.That(roleDto.Permissions.Any(p => p.Name == "EDIT_USERS"), Is.True);
        }
    }
}

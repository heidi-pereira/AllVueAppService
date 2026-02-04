using BrandVue.EntityFramework.MetaData.Authorisation;
using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using InfraPermissionFeature = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionFeature;
using InfraPermissionOption = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionOption;
using InfraRole = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.Role;

namespace UserManagement.Tests.Domain.UserFeaturePermissions
{
    [TestFixture]
    public class RoleMapperTests
    {
        [Test]
        public void MapFromInfrastructure_MapsCorrectly()
        {
            // Arrange
            var infraFeature = new InfraPermissionFeature
            {
                Name = "FeatureA",
                SystemKey = SystemKey.AllVue
            };
            var infraOption = new InfraPermissionOption
            {
                Id = 1,
                Name = "OptionA",
                Feature = infraFeature
            };
            var infraRole = new InfraRole
            {
                Id = 10,
                RoleName = "Admin",
                OrganisationId = "Org1",
                UpdatedByUserId = "User123",
                UpdatedDate = new DateTime(2024, 1, 1),
                Options = new List<InfraPermissionOption> { infraOption }
            };

            // Act
            var domainRole = RoleMapper.MapFromInfrastructure(infraRole);

            // Assert
            Assert.That(domainRole.Id, Is.EqualTo(infraRole.Id));
            Assert.That(domainRole.RoleName, Is.EqualTo(infraRole.RoleName));
            Assert.That(domainRole.OrganisationId, Is.EqualTo(infraRole.OrganisationId));
            Assert.That(domainRole.UpdatedByUserId, Is.EqualTo(infraRole.UpdatedByUserId));
            Assert.That(domainRole.Options.Count(), Is.EqualTo(1));
            var domainOption = domainRole.Options.First();
            Assert.That(domainOption.Id, Is.EqualTo(infraOption.Id));
            Assert.That(domainOption.Name, Is.EqualTo(infraOption.Name));
            Assert.That(domainOption.Feature.Name, Is.EqualTo(infraFeature.Name));
            Assert.That(domainOption.Feature.SystemKey, Is.EqualTo(infraFeature.SystemKey));
        }

        [Test]
        public void MapToInfrastructure_MapsCorrectly()
        {
            // Arrange
            var domainFeature = new BackEnd.Domain.UserFeaturePermissions.Entities.PermissionFeature("FeatureB", SystemKey.AllVue);
            var domainOption = new BackEnd.Domain.UserFeaturePermissions.Entities.PermissionOption(2, "OptionB", domainFeature);
            var domainRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(
                20, "User", "Org2", "User456"
            );
            domainRole.AssignPermission(domainOption);

            // Set UpdatedDate if needed
            var updatedDate = new DateTime(2024, 2, 2);
            typeof(BackEnd.Domain.UserFeaturePermissions.Entities.Role)
                .GetProperty("UpdatedDate")?
                .SetValue(domainRole, updatedDate);

            // Act
            var infraRole = RoleMapper.MapToInfrastructure(domainRole);

            // Assert
            Assert.That(infraRole.RoleName, Is.EqualTo(domainRole.RoleName));
            Assert.That(infraRole.OrganisationId, Is.EqualTo(domainRole.OrganisationId));
            Assert.That(infraRole.UpdatedByUserId, Is.EqualTo(domainRole.UpdatedByUserId));
            Assert.That(infraRole.UpdatedDate, Is.EqualTo(updatedDate));
            Assert.That(infraRole.Options.Count, Is.EqualTo(1));
            var infraOption = infraRole.Options.First();
            Assert.That(infraOption.Id, Is.EqualTo(domainOption.Id));
            Assert.That(infraOption.Name, Is.EqualTo(domainOption.Name));
            Assert.That(infraOption.Feature.Name, Is.EqualTo(domainFeature.Name));
            Assert.That(infraOption.Feature.SystemKey, Is.EqualTo(domainFeature.SystemKey));
        }
    }
}

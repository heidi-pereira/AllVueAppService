using UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using BrandVue.EntityFramework.MetaData.Authorisation;
using InfrastructureOption = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionOption;
using InfrastructureFeature = BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions.PermissionFeature;

namespace UserManagement.Tests.Domain.UserFeaturePermissions
{
    [TestFixture]
    public class PermissionOptionMapperTests
    {
        [Test]
        public void MapFromInfrastructure_MapsCorrectly()
        {
            // Arrange
            var infraFeature = new InfrastructureFeature
            {
                Name = "FeatureA",
                SystemKey = SystemKey.AllVue
            };
            var infraOption = new InfrastructureOption
            {
                Id = 1,
                Name = "OptionA",
                Feature = infraFeature
            };

            // Act
            var domainOption = PermissionOptionMapper.MapFromInfrastructure(infraOption);

            // Assert
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

            // Act
            var infraOption = PermissionOptionMapper.MapToInfrastructure(domainOption);

            // Assert
            Assert.That(infraOption.Id, Is.EqualTo(domainOption.Id));
            Assert.That(infraOption.Name, Is.EqualTo(domainOption.Name));
            Assert.That(infraOption.Feature.Name, Is.EqualTo(domainFeature.Name));
            Assert.That(infraOption.Feature.SystemKey, Is.EqualTo(domainFeature.SystemKey));
        }
    }
}

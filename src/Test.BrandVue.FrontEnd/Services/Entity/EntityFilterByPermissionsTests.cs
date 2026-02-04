using System.Collections.Generic;
using System.Linq;
using BrandVue.Services.Entity;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Variable;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using NSubstitute;
using NUnit.Framework;
using Vue.Common.Auth.Permissions;

namespace Test.BrandVue.FrontEnd.Services.Entity
{
    [TestFixture]
    public class EntityFilterByPermissionsTests
    {
        [Test]
        public void Filter_ReturnsAll_WhenDataPermissionsFiltersAreEmpty()
        {
            // Arrange
            var variableConfigRepo = Substitute.For<IVariableConfigurationRepository>();
            var userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();
            userDataPermissionsOrchestrator.GetDataPermission()
                .Returns(new DataPermissionDto("dummy", new int[0], null));

            var sut = new EntityFilterByPermissions(variableConfigRepo, userDataPermissionsOrchestrator);

            var entities = new List<EntityInstance>
            {
                new EntityInstance { Id = 1 },
                new EntityInstance { Id = 2 }
            };

            // Act
            var result = sut.Filter("AnyIdentifier", entities);

            // Assert
            Assert.That(result, Is.EquivalentTo(entities));
        }

        [Test]
        public void Filter_ReturnsOnlyPermittedEntities_WhenEntitySetReferencesFilteredQuestion()
        {
            // Arrange
            var filterEntityTypeName = "Brand";
            var permittedIds = new List<int> { 1, 3 };

            var variableConfigRepo = Substitute.For<IVariableConfigurationRepository>();
            var userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();

            var filter = new DataPermissionFilterDto(42, permittedIds);
            var permission = new DataPermissionDto(
                "dummy",
                new int[0],
                new List<DataPermissionFilterDto> { filter }
            );

            userDataPermissionsOrchestrator.GetDataPermission().Returns(permission);

            variableConfigRepo.Get(42).Returns(new VariableConfiguration
            {
                Definition = new QuestionVariableDefinition
                {
                    QuestionVarCode = "Q1",
                    EntityTypeNames = new[] { (DbLocationUnquotedColumnName: "", EntityTypeName: filterEntityTypeName) }
                }
            });

            var sut = new EntityFilterByPermissions(variableConfigRepo, userDataPermissionsOrchestrator);

            var entities = new List<EntityInstance>
            {
                new EntityInstance { Id = 1 },
                new EntityInstance { Id = 2 },
                new EntityInstance { Id = 3 }
            };

            // Act
            var result = sut.Filter(filterEntityTypeName, entities);

            // Assert
            Assert.That(result.Select(e => e.Id), Is.EquivalentTo(new[] { 1, 3 }));
        }

        [Test]
        public void Filter_ReturnsAllEntities_WhenEntitySetDoesNotReferenceFilteredQuestion()
        {
            // Arrange
            var filterEntityTypeName = "Brand";
            var unrelatedEntityTypeName = "Product";
            var permittedIds = new List<int> { 1 };

            var variableConfigRepo = Substitute.For<IVariableConfigurationRepository>();
            var userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();

            var filter = new DataPermissionFilterDto(42, permittedIds);
            var permission = new DataPermissionDto(
                "dummy",
                new int[0],
                new List<DataPermissionFilterDto> { filter }
            );

            userDataPermissionsOrchestrator.GetDataPermission().Returns(permission);

            variableConfigRepo.Get(42).Returns(new VariableConfiguration
            {
                Definition = new QuestionVariableDefinition
                {
                    QuestionVarCode = "Q1",
                    EntityTypeNames = new[] { (DbLocationUnquotedColumnName: "", EntityTypeName: filterEntityTypeName) } 
                }
            });

            var sut = new EntityFilterByPermissions(variableConfigRepo, userDataPermissionsOrchestrator);

            var entities = new List<EntityInstance>
            {
                new EntityInstance { Id = 1 },
                new EntityInstance { Id = 2 }
            };

            // Act
            var result = sut.Filter(unrelatedEntityTypeName, entities);

            // Assert
            Assert.That(result, Is.EquivalentTo(entities));
        }
    }

}
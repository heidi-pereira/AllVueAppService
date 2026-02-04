using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllPermissionFeatures;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllFeaturePermissions;
using UserManagement.BackEnd.Application.UserFeaturePermissions;
using DomainEntities = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities;
using BrandVue.EntityFramework.MetaData.Authorisation;

namespace UserManagement.Tests.Application.UserFeaturePermissions.Queries
{
    public class GetAllPermissionFeaturesQueryHandlerTests
    {
        private IPermissionFeatureRepository _repository;
        private GetAllPermissionFeaturesQueryHandler _handler;

        [SetUp]
        public void Setup()
        {
            _repository = Substitute.For<IPermissionFeatureRepository>();
            _handler = new GetAllPermissionFeaturesQueryHandler(_repository);
        }

        [Test]
        public async Task Handle_ReturnsMappedPermissionFeatureDtos()
        {
            // Arrange
            var featureId = 1;
            var optionId = 1;
            var domainFeature = new DomainEntities.PermissionFeature(
                    featureId,
                    "Feature1",
                    SystemKey.AllVue
                );
            domainFeature.AddOption(new DomainEntities.PermissionOption(optionId, "Option1", domainFeature));
            var features = new List<DomainEntities.PermissionFeature>() { domainFeature };
            _repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(features);

            var query = new GetAllPermissionFeaturesQuery();

            // Act
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Id, Is.EqualTo(featureId));
            Assert.That(result[0].Name, Is.EqualTo("Feature1"));
            Assert.That(result[0].SystemKey, Is.EqualTo(SystemKey.AllVue.ToString()));
            Assert.That(result[0].Options, Has.Count.EqualTo(1));
            Assert.That(result[0].Options[0].Id, Is.EqualTo(optionId));
            Assert.That(result[0].Options[0].Name, Is.EqualTo("Option1"));
        }

        [Test]
        public async Task Handle_CallsRepositoryGetAllAsync()
        {
            // Arrange
            var query = new GetAllPermissionFeaturesQuery();

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            await _repository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        }
    }
}
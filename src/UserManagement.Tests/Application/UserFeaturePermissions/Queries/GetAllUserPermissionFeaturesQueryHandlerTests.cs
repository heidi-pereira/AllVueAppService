using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using Role = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role;
using PermissionOption = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.PermissionOption;
using PermissionFeature = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.PermissionFeature;
using UserFeaturePermission = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission;
using BrandVue.EntityFramework.MetaData.Authorisation;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserPermissionFeatures.Tests
{
    [TestFixture]
    public class GetAllUserPermissionFeaturesQueryHandlerTests
    {
        private IUserFeaturePermissionRepository _repository = null!;
        private GetAllUserPermissionOptionsQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _repository = Substitute.For<IUserFeaturePermissionRepository>();
            _handler = new GetAllUserPermissionOptionsQueryHandler(_repository);
        }

        [Test]
        public async Task Handle_ReturnsPermissionFeatureOptionDtos()
        {
            // Arrange
            var userId = "user123";
            var permissionFeature = new PermissionFeature(1, "Feature1", SystemKey.AllVue);
            var options = new List<PermissionOption>
            {
                new PermissionOption(1, "Option1", permissionFeature),
                new PermissionOption(2, "Option2", permissionFeature)
            };

            var userRole = new Role("Name", "Organisation", "UpdatedByUserId");
            userRole.AssignPermission(options[0]);
            userRole.AssignPermission(options[1]);

            var userFeaturePermission = new UserFeaturePermission(userId, userRole, "UpdatedByUserId");

            _repository.GetByUserIdAsync(userId).Returns(Task.FromResult(userFeaturePermission));

            var query = new GetAllUserPermissionOptionsQuery(userId, "User");

            // Act
            var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

            // Assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Id, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("Option1"));
            Assert.That(result[1].Id, Is.EqualTo(2));
            Assert.That(result[1].Name, Is.EqualTo("Option2"));
        }
    }
}

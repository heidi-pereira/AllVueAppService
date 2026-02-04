using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllUserFeaturePermissions;
using DomainRole = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role;
using DomainUserFeaturePermission = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission;

namespace UserManagement.BackEnd.Tests.UserFeaturePermissions.Queries
{
    [TestFixture]
    public class GetAllUserFeaturePermissionsQueryHandlerTests
    {
        private IUserFeaturePermissionRepository _repositorySub = null!;
        private GetAllUserFeaturePermissionsQueryHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _repositorySub = Substitute.For<IUserFeaturePermissionRepository>();
            _handler = new GetAllUserFeaturePermissionsQueryHandler(_repositorySub);
        }

        [Test]
        public async Task Handle_ReturnsAllUserFeaturePermissions()
        {
            // Arrange
            var role1 = new DomainRole(1, "Admin", "Org1", "system@example.com");
            var role2 = new DomainRole(2, "User", "Org1", "system@example.com");
            
            var permission1 = new DomainUserFeaturePermission(1, "user1", role1, "admin@example.com");
            
            var permission2 = new DomainUserFeaturePermission(2, "user2", role2, "admin@example.com");
            
            var permissions = new List<DomainUserFeaturePermission> { permission1, permission2 };
            _repositorySub.GetAllAsync().Returns(permissions);

            // Act
            var result = await _handler.Handle(new GetAllUserFeaturePermissionsQuery(), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            
            var resultList = result.ToList();
            Assert.That(resultList[0].Id, Is.EqualTo(1));
            Assert.That(resultList[0].UserId, Is.EqualTo("user1"));
            Assert.That(resultList[0].UserRoleId, Is.EqualTo(1));
            Assert.That(resultList[0].UpdatedByUserId, Is.EqualTo("admin@example.com"));
            
            Assert.That(resultList[1].Id, Is.EqualTo(2));
            Assert.That(resultList[1].UserId, Is.EqualTo("user2"));
            Assert.That(resultList[1].UserRoleId, Is.EqualTo(2));
            Assert.That(resultList[1].UpdatedByUserId, Is.EqualTo("admin@example.com"));
        }

        [Test]
        public async Task Handle_ReturnsEmptyList_WhenNoPermissionsExist()
        {
            // Arrange
            _repositorySub.GetAllAsync().Returns(new List<DomainUserFeaturePermission>());

            // Act
            var result = await _handler.Handle(new GetAllUserFeaturePermissionsQuery(), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task Handle_MapsAllPropertiesCorrectly()
        {
            // Arrange
            var role = new DomainRole(10, "Administrator", "TestOrg", "creator@example.com");
            var permission = new DomainUserFeaturePermission(42, "test-user-id", role, "updater@example.com");
            
            var permissions = new List<DomainUserFeaturePermission> { permission };
            _repositorySub.GetAllAsync().Returns(permissions);

            // Act
            var result = await _handler.Handle(new GetAllUserFeaturePermissionsQuery(), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(1));
            
            var dto = result.First();
            Assert.That(dto.Id, Is.EqualTo(42));
            Assert.That(dto.UserId, Is.EqualTo("test-user-id"));
            Assert.That(dto.UserRoleId, Is.EqualTo(10));
            Assert.That(dto.UpdatedByUserId, Is.EqualTo("updater@example.com"));
            Assert.That(dto.UpdatedDate, Is.EqualTo(permission.UpdatedDate));
        }

        [Test]
        public async Task Handle_CallsRepositoryGetAllAsync()
        {
            // Arrange
            _repositorySub.GetAllAsync().Returns(new List<DomainUserFeaturePermission>());

            // Act
            await _handler.Handle(new GetAllUserFeaturePermissionsQuery(), CancellationToken.None);

            // Assert
            await _repositorySub.Received(1).GetAllAsync();
        }

        [Test]
        public async Task Handle_WithMultiplePermissions_ReturnsCorrectCount()
        {
            // Arrange
            var role = new DomainRole(1, "TestRole", "TestOrg", "system@example.com");
            var permissions = new List<DomainUserFeaturePermission>();
            
            for (int i = 1; i <= 5; i++)
            {
                var permission = new DomainUserFeaturePermission(i, $"user{i}", role, "admin@example.com");
                permissions.Add(permission);
            }
            
            _repositorySub.GetAllAsync().Returns(permissions);

            // Act
            var result = await _handler.Handle(new GetAllUserFeaturePermissionsQuery(), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(5));
            
            var resultList = result.ToList();
            for (int i = 0; i < 5; i++)
            {
                Assert.That(resultList[i].Id, Is.EqualTo(i + 1));
                Assert.That(resultList[i].UserId, Is.EqualTo($"user{i + 1}"));
            }
        }
    }
}

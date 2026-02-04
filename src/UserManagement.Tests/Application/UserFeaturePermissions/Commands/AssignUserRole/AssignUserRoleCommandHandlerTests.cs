using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.AssignUserRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using DomainRole = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role;
using DomainUserFeaturePermission = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission;

namespace UserManagement.Tests.Application.UserFeaturePermissions.Commands.AssignUserRole
{
    [TestFixture]
    public class AssignUserRoleCommandHandlerTests
    {
        private IUserFeaturePermissionRepository _userFeaturePermissionRepository = null!;
        private IRoleRepository _roleRepository = null!;
        private AssignUserRoleCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _userFeaturePermissionRepository = Substitute.For<IUserFeaturePermissionRepository>();
            _roleRepository = Substitute.For<IRoleRepository>();
            _handler = new AssignUserRoleCommandHandler(_userFeaturePermissionRepository, _roleRepository);
        }

        [Test]
        public async Task Handle_WhenRoleExists_ShouldCreateUserFeaturePermission()
        {
            // Arrange
            var userId = "test-user-id";
            var userRoleId = 1;
            var updatedByUserId = "admin-user-id";
            var role = new DomainRole(userRoleId, "Test Role", "Test Org", updatedByUserId);
            
            var command = new AssignUserRoleCommand(userId, userRoleId, updatedByUserId);
            
            _roleRepository.GetByIdAsync(userRoleId, Arg.Any<CancellationToken>()).Returns(role);
            
            // Set up the saved entity with an ID
            var savedPermission = new DomainUserFeaturePermission(1, userId, role, updatedByUserId);
            _userFeaturePermissionRepository.UpsertAsync(Arg.Any<DomainUserFeaturePermission>()).Returns(savedPermission);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1)); // Now we should get the correct ID
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.UserRoleId, Is.EqualTo(userRoleId));
            Assert.That(result.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            Assert.That(result.UpdatedDate, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)));

            await _userFeaturePermissionRepository.Received(1).UpsertAsync(Arg.Any<DomainUserFeaturePermission>());
        }

        [Test]
        public void Handle_WhenRoleDoesNotExist_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = "test-user-id";
            var userRoleId = 999;
            var updatedByUserId = "admin-user-id";
            
            var command = new AssignUserRoleCommand(userId, userRoleId, updatedByUserId);
            
            _roleRepository.GetByIdAsync(userRoleId, Arg.Any<CancellationToken>()).Returns(Task.FromResult<DomainRole>(null!));

            // Act & Assert
            var exception = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(exception?.Message, Does.Contain($"Role with ID {userRoleId} not found"));
        }

        [Test]
        public async Task Handle_ShouldCallUpsertAsync_RegardlessOfExistingPermission()
        {
            // Arrange
            var userId = "test-user-id";
            var userRoleId = 1;
            var updatedByUserId = "admin-user-id";
            var role = new DomainRole(userRoleId, "Test Role", "Test Org", updatedByUserId);
            
            var command = new AssignUserRoleCommand(userId, userRoleId, updatedByUserId);
            
            _roleRepository.GetByIdAsync(userRoleId, Arg.Any<CancellationToken>()).Returns(role);
            
            // Set up the saved entity with an ID (UpsertAsync handles both insert and update)
            var savedPermission = new DomainUserFeaturePermission(5, userId, role, updatedByUserId);
            _userFeaturePermissionRepository.UpsertAsync(Arg.Any<DomainUserFeaturePermission>()).Returns(savedPermission);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(5));
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.UserRoleId, Is.EqualTo(userRoleId));
            Assert.That(result.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            
            // Should call UpsertAsync exactly once
            await _userFeaturePermissionRepository.Received(1).UpsertAsync(Arg.Any<DomainUserFeaturePermission>());
            
            // Should not call the old methods
            await _userFeaturePermissionRepository.DidNotReceive().GetByUserIdAsync(Arg.Any<string>());
            await _userFeaturePermissionRepository.DidNotReceive().UpdateAsync(Arg.Any<DomainUserFeaturePermission>());
            await _userFeaturePermissionRepository.DidNotReceive().AddAsync(Arg.Any<DomainUserFeaturePermission>());
        }
    }
}

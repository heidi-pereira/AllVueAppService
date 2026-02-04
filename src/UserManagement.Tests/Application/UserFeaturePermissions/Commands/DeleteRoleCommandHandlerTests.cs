using System.ComponentModel.DataAnnotations;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

namespace UserManagement.Tests.Application.UserFeaturePermissions.Commands
{
    [TestFixture]
    public class DeleteRoleCommandHandlerTests
    {
        private IRoleRepository _roleRepository = null!;
        private IUserFeaturePermissionRepository _userFeaturePermissionRepository = null!;
        private DeleteRoleCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _roleRepository = Substitute.For<IRoleRepository>();
            _userFeaturePermissionRepository = Substitute.For<IUserFeaturePermissionRepository>();
            _handler = new DeleteRoleCommandHandler(_roleRepository, _userFeaturePermissionRepository);
        }

        [Test]
        public async Task Handle_ShouldDeleteRole_WhenRoleExistsAndNotAssigned()
        {
            // Arrange
            var roleId = 1;
            var role = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(roleId, "TestRole", "Org", "User");
            _roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
            _userFeaturePermissionRepository.GetAllAsync().Returns(new List<BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>());

            var command = new DeleteRoleCommand(roleId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            await _roleRepository.Received(1).DeleteAsync(roleId, Arg.Any<CancellationToken>());
        }

        [Test]
        public void Handle_ShouldThrowValidationException_WhenRoleDoesNotExist()
        {
            // Arrange
            var roleId = 99;
            _roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns((BackEnd.Domain.UserFeaturePermissions.Entities.Role)null);
            var command = new DeleteRoleCommand(roleId);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain($"Role with ID {roleId} does not exist."));
        }

        [Test]
        public void Handle_ShouldThrowValidationException_WhenRoleIsAssignedToUser()
        {
            // Arrange
            var roleId = 2;
            var role = new BackEnd.Domain.UserFeaturePermissions.Entities.Role(roleId, "AssignedRole", "Org", "User");
            _roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
            _userFeaturePermissionRepository.HasRoleAssignments(roleId, Arg.Any<CancellationToken>()).Returns(true);

            var command = new DeleteRoleCommand(roleId);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("Cannot delete role: it is assigned to one or more users."));
        }
    }
}

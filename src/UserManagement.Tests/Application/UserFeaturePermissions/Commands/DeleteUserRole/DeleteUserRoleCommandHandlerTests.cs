using NSubstitute;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.DeleteUserRole;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;

namespace UserManagement.Tests.Application.UserFeaturePermissions.Commands.DeleteUserRole
{
    [TestFixture]
    public class DeleteUserRoleCommandHandlerTests
    {
        private IUserFeaturePermissionRepository _userFeaturePermissionRepository = null!;
        private DeleteUserRoleCommandHandler _handler = null!;

        [SetUp]
        public void SetUp()
        {
            _userFeaturePermissionRepository = Substitute.For<IUserFeaturePermissionRepository>();
            _handler = new DeleteUserRoleCommandHandler(_userFeaturePermissionRepository);
        }

        [Test]
        public async Task Handle_ShouldDeleteUserRoleAndReturnTrue()
        {
            // Arrange
            var userId = "test-user-id";
            var command = new DeleteUserRoleCommand(userId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.True);
            await _userFeaturePermissionRepository.Received(1).DeleteByUserIdAsync(userId);
        }
    }
}

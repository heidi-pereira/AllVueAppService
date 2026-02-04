using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Queries.GetAllRoles;
using Role = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role;

namespace UserManagement.BackEnd.Tests.UserFeaturePermissions.Queries
{
    [TestFixture]
    public class GetAllRolesQueryHandlerTests
    {
        private IRoleRepository _roleRepositorySub;
        private GetAllRolesQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _roleRepositorySub = Substitute.For<IRoleRepository>();
            _handler = new GetAllRolesQueryHandler(_roleRepositorySub);
        }

        [Test]
        public async Task Handle_ReturnsAllRoles()
        {
            // Arrange
            var roles = new List<Role>
            {
               new Role(
                    10,
                    "Admin",
                    "Org1",
                    "SomeStringValue"
                ),
                new Role(
                    2,
                    "User",
                    "Org1",
                    "SomeStringValue"
                )
            };
            _roleRepositorySub.GetAllAsync().Returns(roles);

            // Act
            var result = await _handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.Any(r => r.RoleName == "Admin"), Is.True);
            Assert.That(result.Any(r => r.RoleName == "User"), Is.True);
        }

        [Test]
        public async Task Handle_ReturnsEmptyList_WhenNoRolesExist()
        {
            // Arrange
            _roleRepositorySub.GetAllAsync().Returns(new List<Role>());

            // Act
            var result = await _handler.Handle(new GetAllRolesQuery(), CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }
    }
}

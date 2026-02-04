using BrandVue.EntityFramework.MetaData.Authorisation;
using System.ComponentModel.DataAnnotations;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.AuthApi;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.UpdateRole
{
    [TestFixture]
    public class UpdateRoleCommandHandlerTests
    {
        private IRoleRepository _roleRepository;
        private IPermissionOptionRepository _permissionOptionRepository;
        private IRoleValidationService _roleValidationService;
        private UpdateRoleCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _roleRepository = Substitute.For<IRoleRepository>();
            _permissionOptionRepository = Substitute.For<IPermissionOptionRepository>();
            var userContext = Substitute.For<IUserContext>();
            userContext.UserOrganisation.Returns("TestOrganisation");
            var authApiClient = Substitute.For<IAuthApiClient>();
            authApiClient.GetCompanyByShortcode("TestOrganisation", Arg.Any<CancellationToken>())
                .Returns(new AuthServer.GeneratedAuthApi.CompanyModel { Id = "1", ShortCode = "TestOrganisation", DisplayName = "Test Organisation" });
            _roleValidationService = new RoleValidationService(_permissionOptionRepository, userContext, authApiClient, _roleRepository);
            _handler = new UpdateRoleCommandHandler(_roleRepository, _permissionOptionRepository, _roleValidationService);
        }

        [Test]
        public async Task Handle_ShouldUpdateRole_WhenValidRequest()
        {
            // Arrange
            var roleId = 10;
            var roleName = "Admin";
            var permissionOptionIds = new List<int> { 2 };
            var updatedByUserId = "User123";

            var role = new Domain.UserFeaturePermissions.Entities.Role(roleId, "OldName", "Org1", "OldUser");
            var allOptions = new List<Domain.UserFeaturePermissions.Entities.PermissionOption>
            {
                new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "edit", new Domain.UserFeaturePermissions.Entities.PermissionFeature(2, "Feature2", SystemKey.AllVue)),
                new Domain.UserFeaturePermissions.Entities.PermissionOption(3, "delete", new Domain.UserFeaturePermissions.Entities.PermissionFeature(3, "Feature3", SystemKey.AllVue))
            };

            _roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
            _permissionOptionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allOptions);

            var command = new UpdateRoleCommand(roleId, roleName, permissionOptionIds, updatedByUserId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(roleId));
            Assert.That(result.RoleName, Is.EqualTo(roleName));
            Assert.That(result.Permissions.Count, Is.EqualTo(1));
            Assert.That(result.Permissions.Any(p => p.Name == "edit"), Is.True);
            await _roleRepository.Received(1).UpdateAsync(role);
        }

        [Test]
        public void Handle_ShouldThrow_WhenRoleNotFound()
        {
            // Arrange
            var allOptions = new List<Domain.UserFeaturePermissions.Entities.PermissionOption>
            {
                new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "edit", new Domain.UserFeaturePermissions.Entities.PermissionFeature(2, "Feature2", SystemKey.AllVue)),
                new Domain.UserFeaturePermissions.Entities.PermissionOption(3, "delete", new Domain.UserFeaturePermissions.Entities.PermissionFeature(3, "Feature3", SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allOptions);

            var command = new UpdateRoleCommand(99, "Admin", new List<int> { 2 }, "User123");
            _roleRepository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Domain.UserFeaturePermissions.Entities.Role)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("Role with ID 99 not found"));
        }

        [Test]
        public void Handle_ShouldThrow_WhenInvalidPermission()
        {
            // Arrange
            var roleId = 10;
            var role = new Domain.UserFeaturePermissions.Entities.Role(roleId, "OldName", "Org1", "OldUser");
            var allOptions = new List<Domain.UserFeaturePermissions.Entities.PermissionOption>
            {
                new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "edit", new Domain.UserFeaturePermissions.Entities.PermissionFeature(1, "Feature1", SystemKey.AllVue))
            };

            _roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
            _permissionOptionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allOptions);

            var command = new UpdateRoleCommand(roleId, "Admin", new List<int> { 999 }, "User123");

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("Invalid permissions specified"));
        }

        [Test]
        public void Handle_ShouldThrow_WhenRoleNameTooLong()
        {
            // Arrange
            var roleId = 10;
            var role = new Domain.UserFeaturePermissions.Entities.Role(roleId, "OldName", "Org1", "OldUser");
            var allOptions = new List<Domain.UserFeaturePermissions.Entities.PermissionOption>
            {
                new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "edit", new Domain.UserFeaturePermissions.Entities.PermissionFeature(1, "Feature1", SystemKey.AllVue))
            };

            _roleRepository.GetByIdAsync(roleId, Arg.Any<CancellationToken>()).Returns(role);
            _permissionOptionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allOptions);

            var longName = new string('A', 36);
            var command = new UpdateRoleCommand(roleId, longName, new List<int> { 2 }, "User123");

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("Role name must be between 1 and 35 characters"));
        }
    }
}
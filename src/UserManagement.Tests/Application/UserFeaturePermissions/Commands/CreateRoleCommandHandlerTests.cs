using BrandVue.EntityFramework.MetaData.Authorisation;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using AuthServer.GeneratedAuthApi;

namespace UserManagement.BackEnd.Application.UserFeaturePermissions.Commands.CreateRole
{
    [TestFixture]
    public class CreateRoleCommandHandlerTests
    {
        private const string Organisation = "Org1";
        private const string CurrentUser = "User123";
        private IRoleRepository _roleRepository;
        private IPermissionOptionRepository _permissionOptionRepository;
        private IUserContext _userContext;
        private IAuthApiClient _authApiClient = Substitute.For<IAuthApiClient>();
        private IRoleValidationService _roleValidationService;
        private CreateRoleCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _roleRepository = Substitute.For<IRoleRepository>();
            _permissionOptionRepository = Substitute.For<IPermissionOptionRepository>();
            _userContext = Substitute.For<IUserContext>();
            _userContext.UserOrganisation.Returns(Organisation);
            _userContext.UserId.Returns(CurrentUser);
            _roleValidationService = new RoleValidationService(_permissionOptionRepository, _userContext, _authApiClient, _roleRepository);
            _handler = new CreateRoleCommandHandler(_roleRepository, _permissionOptionRepository, _userContext, _authApiClient, _roleValidationService);
        }

        [Test]
        public async Task HandleAsync_ShouldReturnRoleDto_WhenRoleIsCreated()
        {
            // Arrange
            var command = new CreateRoleCommand(
                "Admin",
                new List<int> { 1, 2 }
            );

            // Mock the company/organisation returned by the auth API
            var company = new CompanyModel { Id = "company-id-123", ShortCode = Organisation };
            _authApiClient.GetCompanyByShortcode(Organisation, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(company));

            // Mock the permission options
            var permissionOptions = new List<Domain.UserFeaturePermissions.Entities.PermissionOption>
            {
                new Domain.UserFeaturePermissions.Entities.PermissionOption(1, "Read", new Domain.UserFeaturePermissions.Entities.PermissionFeature(1, "Feature1", SystemKey.AllVue)),
                new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "Write", new Domain.UserFeaturePermissions.Entities.PermissionFeature(2, "Feature2", SystemKey.AllVue))
            };
            _permissionOptionRepository.GetAllByIdsAsync(Arg.Is<List<int>>(ids => ids.Contains(1) && ids.Contains(2)))
                .Returns(Task.FromResult(permissionOptions.AsEnumerable()));

            // Create the expected role that would be returned by the repository
            var role = new Domain.UserFeaturePermissions.Entities.Role
            (
                10,
                "Admin",
                company.Id,
                CurrentUser
            );
            var allOptions = new List<Domain.UserFeaturePermissions.Entities.PermissionOption>
            {
                new Domain.UserFeaturePermissions.Entities.PermissionOption(1, "Read", new Domain.UserFeaturePermissions.Entities.PermissionFeature(1, "Feature1", SystemKey.AllVue)),
                new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "Write", new Domain.UserFeaturePermissions.Entities.PermissionFeature(2, "Feature2", SystemKey.AllVue)),
                new Domain.UserFeaturePermissions.Entities.PermissionOption(3, "delete", new Domain.UserFeaturePermissions.Entities.PermissionFeature(3, "Feature3", SystemKey.AllVue))
            };

            role.AssignPermission(new Domain.UserFeaturePermissions.Entities.PermissionOption(1, "Read", new Domain.UserFeaturePermissions.Entities.PermissionFeature(1, "Feature1", SystemKey.AllVue)));
            role.AssignPermission(new Domain.UserFeaturePermissions.Entities.PermissionOption(2, "Write", new Domain.UserFeaturePermissions.Entities.PermissionFeature(2, "Feature2", SystemKey.AllVue)));

            _roleRepository.AddAsync(Arg.Any<Domain.UserFeaturePermissions.Entities.Role>())
                .Returns(Task.FromResult(role));
            _permissionOptionRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(allOptions);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(role.Id));
            Assert.That(result.RoleName, Is.EqualTo(role.RoleName));
            Assert.That(result.Organisation, Is.EqualTo(role.OrganisationId));
            Assert.That(result.Permissions.Count, Is.EqualTo(2));
            Assert.That(result.Permissions.Any(p => p.Id == 1 && p.Name == "Read"), Is.True);
            Assert.That(result.Permissions.Any(p => p.Id == 2 && p.Name == "Write"), Is.True);
        }
    }
}

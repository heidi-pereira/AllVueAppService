using AuthServer.GeneratedAuthApi;
using System.ComponentModel.DataAnnotations;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Infrastructure.Repositories.UserFeaturePermissions;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using PermissionFeature = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.PermissionFeature;
using PermissionOption = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.PermissionOption;
using Role = UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.Role;

namespace UserManagement.Tests.Services
{
    [TestFixture]
    public class RoleValidationServiceTests
    {
        private IPermissionOptionRepository _permissionOptionRepository = null!;
        private IUserContext _userContext = null!;
        private IAuthApiClient _authApiClient = null!;
        private IRoleRepository _roleRepository = null!;
        private RoleValidationService _service = null!;
        private CancellationToken _cancellationToken;

        private const string TestOrganisationShortCode = "TEST_ORG";
        private const string TestCompanyId = "company-123";

        [SetUp]
        public void SetUp()
        {
            _permissionOptionRepository = Substitute.For<IPermissionOptionRepository>();
            _userContext = Substitute.For<IUserContext>();
            _authApiClient = Substitute.For<IAuthApiClient>();
            _roleRepository = Substitute.For<IRoleRepository>();
            _cancellationToken = CancellationToken.None;

            _service = new RoleValidationService(
                _permissionOptionRepository,
                _userContext,
                _authApiClient,
                _roleRepository);
        }

        [Test]
        public async Task ValidateRole_WithValidInputs_DoesNotThrow()
        {
            // Arrange
            var validRoleName = "ValidRole";
            var validPermissionIds = new List<int> { 1, 2 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue)),
                new PermissionOption(2, "WRITE", new PermissionFeature("FEATURE2", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };
            var testCompany = new CompanyModel
            {
                Id = TestCompanyId,
                ShortCode = TestOrganisationShortCode,
                DisplayName = "Test Company"
            };
            var existingRoles = new List<Role>();

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);
            _userContext.UserOrganisation.Returns(TestOrganisationShortCode);
            _authApiClient.GetCompanyByShortcode(TestOrganisationShortCode, _cancellationToken).Returns(testCompany);
            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
                await _service.ValidateRole(validRoleName, validPermissionIds, null, _cancellationToken));
        }

        [Test]
        public async Task ValidateRole_WithDuplicateRoleName_ThrowsValidationException()
        {
            // Arrange
            var duplicateRoleName = "DuplicateRole";
            var validPermissionIds = new List<int> { 1 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };
            var testCompany = new CompanyModel
            {
                Id = TestCompanyId,
                ShortCode = TestOrganisationShortCode,
                DisplayName = "Test Company"
            };
            var existingRoles = new List<Role>
            {
                new Role(1, duplicateRoleName, TestOrganisationShortCode, "user1")
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);
            _userContext.UserOrganisation.Returns(TestOrganisationShortCode);
            _authApiClient.GetCompanyByShortcode(TestOrganisationShortCode, _cancellationToken).Returns(testCompany);
            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () =>
                await _service.ValidateRole(duplicateRoleName, validPermissionIds, null, _cancellationToken));
            Assert.That(exception.Message, Is.EqualTo($"Role name '{duplicateRoleName}' already exists in this organisation."));
        }

        [Test]
        public async Task ValidateRole_WithInvalidPermissionIds_ThrowsValidationException()
        {
            // Arrange
            var validRoleName = "ValidRole";
            var invalidPermissionIds = new List<int> { 99 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };
            var testCompany = new CompanyModel
            {
                Id = TestCompanyId,
                ShortCode = TestOrganisationShortCode,
                DisplayName = "Test Company"
            };
            var existingRoles = new List<Role>();

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);
            _userContext.UserOrganisation.Returns(TestOrganisationShortCode);
            _authApiClient.GetCompanyByShortcode(TestOrganisationShortCode, _cancellationToken).Returns(testCompany);
            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () =>
                await _service.ValidateRole(validRoleName, invalidPermissionIds, null, _cancellationToken));
            Assert.That(exception.Message, Is.EqualTo("Invalid permissions specified."));
        }

        [Test]
        public async Task ValidateRole_WithNullUserOrganisation_ThrowsValidationException()
        {
            // Arrange
            var validRoleName = "ValidRole";
            var validPermissionIds = new List<int> { 1 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);
            _userContext.UserOrganisation.Returns((string)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () =>
                await _service.ValidateRole(validRoleName, validPermissionIds, null, _cancellationToken));
            Assert.That(exception.Message, Is.EqualTo("User is not associated with any organisation"));
        }

        [Test]
        public async Task ValidateRole_WithNonExistentOrganisation_ThrowsValidationException()
        {
            // Arrange
            var validRoleName = "ValidRole";
            var validPermissionIds = new List<int> { 1 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);
            _userContext.UserOrganisation.Returns(TestOrganisationShortCode);
            _authApiClient.GetCompanyByShortcode(TestOrganisationShortCode, _cancellationToken).Returns((CompanyModel)null);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () =>
                await _service.ValidateRole(validRoleName, validPermissionIds, null, _cancellationToken));
            Assert.That(exception.Message, Is.EqualTo($"Organisation not found for short code '{TestOrganisationShortCode}'"));
        }

        [Test]
        public void ValidateRoleName_WithValidRoleName_DoesNotThrow()
        {
            // Arrange
            var validRoleName = "ValidRole";
            var existingRoles = new List<Role>();

            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _service.ValidateRoleName(validRoleName, TestCompanyId, null, _cancellationToken));
        }

        [Test]
        public void ValidateRoleName_WithNullRoleName_ThrowsValidationException()
        {
            // Arrange
            string? nullRoleName = null;
            var existingRoles = new List<Role>();

            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () => 
                await _service.ValidateRoleName(nullRoleName!, TestCompanyId, null, _cancellationToken));
            Assert.That(exception!.Message, Is.EqualTo($"Role name must be between 1 and {Role.MaxRoleNameLength} characters."));
        }

        [Test]
        public void ValidateRoleName_WithEmptyRoleName_ThrowsValidationException()
        {
            // Arrange
            var emptyRoleName = string.Empty;
            var existingRoles = new List<Role>();

            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () => 
                await _service.ValidateRoleName(emptyRoleName, TestCompanyId, null, _cancellationToken));
            Assert.That(exception!.Message, Is.EqualTo($"Role name must be between 1 and {Role.MaxRoleNameLength} characters."));
        }

        [Test]
        public void ValidateRoleName_WithWhitespaceOnlyRoleName_ThrowsValidationException()
        {
            // Arrange
            var whitespaceRoleName = "   ";
            var existingRoles = new List<Role>();

            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () => await _service.ValidateRoleName(whitespaceRoleName, TestCompanyId, null, _cancellationToken));
            Assert.That(exception!.Message, Is.EqualTo($"Role name must be between 1 and {Role.MaxRoleNameLength} characters."));
        }

        [Test]
        public void ValidateRoleName_WithRoleNameAtMaxLength_DoesNotThrow()
        {
            // Arrange
            var maxLengthRoleName = new string('A', Role.MaxRoleNameLength);
            var existingRoles = new List<Role>();

            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _service.ValidateRoleName(maxLengthRoleName, TestCompanyId, null, _cancellationToken));
        }

        [Test]
        public void ValidateRoleName_WithRoleNameExceedingMaxLength_ThrowsValidationException()
        {
            // Arrange
            var tooLongRoleName = new string('A', Role.MaxRoleNameLength + 1);
            var existingRoles = new List<Role>();

            _roleRepository.GetByOrganisationIdAsync(TestCompanyId).Returns(existingRoles);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () => await _service.ValidateRoleName(tooLongRoleName, TestCompanyId, null, _cancellationToken));
            Assert.That(exception!.Message, Is.EqualTo($"Role name must be between 1 and {Role.MaxRoleNameLength} characters."));
        }

        [Test]
        public async Task ValidatePermissionOptionIdsAsync_WithValidPermissionOptions_DoesNotThrow()
        {
            // Arrange
            var validPermissionIds = new List<int> { 1, 2, 3 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue)),
                new PermissionOption(2, "WRITE", new PermissionFeature("FEATURE2", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue)),
                new PermissionOption(3, "DELETE", new PermissionFeature("FEATURE3", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => 
                await _service.ValidatePermissionOptionIdsAsync(validPermissionIds, _cancellationToken));
        }

        [Test]
        public async Task ValidatePermissionOptionIdsAsync_WithEmptyPermissionIds_DoesNotThrow()
        {
            // Arrange
            var emptyPermissionIds = new List<int>();
            var allValidOptions = new List<PermissionOption>();

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => 
                await _service.ValidatePermissionOptionIdsAsync(emptyPermissionIds, _cancellationToken));
        }

        [Test]
        public async Task ValidatePermissionOptionIdsAsync_WithInvalidPermissionIds_ThrowsValidationException()
        {
            // Arrange
            var invalidPermissionIds = new List<int> { 1, 99 }; // 99 is invalid
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue)),
                new PermissionOption(2, "WRITE", new PermissionFeature("FEATURE2", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);

            // Act & Assert
            var exception = Assert.ThrowsAsync<ValidationException>(async () => 
                await _service.ValidatePermissionOptionIdsAsync(invalidPermissionIds, _cancellationToken));
            
            Assert.That(exception.Message, Is.EqualTo("Invalid permissions specified."));
        }

        [Test]
        public async Task ValidatePermissionOptionIdsAsync_WithPartiallyValidPermissionIds_ThrowsValidationException()
        {
            var mixedPermissionIds = new List<int> { 1, 2, 99, 100 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue)),
                new PermissionOption(2, "WRITE", new PermissionFeature("FEATURE2", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);

            var exception = Assert.ThrowsAsync<ValidationException>(async () =>
                await _service.ValidatePermissionOptionIdsAsync(mixedPermissionIds, _cancellationToken));
            Assert.That(exception.Message, Is.EqualTo("Invalid permissions specified."));
        }

        [Test]
        public async Task ValidatePermissionOptionIdsAsync_CallsRepositoryWithCorrectParameters()
        {
            // Arrange
            var permissionIds = new List<int> { 1 };
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);

            // Act
            await _service.ValidatePermissionOptionIdsAsync(permissionIds, _cancellationToken);

            // Assert
            await _permissionOptionRepository.Received(1).GetAllAsync(_cancellationToken);
        }

        [Test]
        public async Task ValidatePermissionOptionIdsAsync_WithDuplicatePermissionIds_ValidatesCorrectly()
        {
            // Arrange
            var duplicatePermissionIds = new List<int> { 1, 1, 2, 2 }; // Duplicates should still validate
            var allValidOptions = new List<PermissionOption>
            {
                new PermissionOption(1, "READ", new PermissionFeature("FEATURE1", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue)),
                new PermissionOption(2, "WRITE", new PermissionFeature("FEATURE2", BrandVue.EntityFramework.MetaData.Authorisation.SystemKey.AllVue))
            };

            _permissionOptionRepository.GetAllAsync(_cancellationToken).Returns(allValidOptions);

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => 
                await _service.ValidatePermissionOptionIdsAsync(duplicatePermissionIds, _cancellationToken));
        }

        [Test]
        public void Constructor_WithValidDependencies_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
                new RoleValidationService(_permissionOptionRepository, _userContext, _authApiClient, _roleRepository));
        }
    }
}

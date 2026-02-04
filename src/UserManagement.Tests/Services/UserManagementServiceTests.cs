using AuthServer.GeneratedAuthApi;
using UserManagement.BackEnd.Application.UserDataPermissions.Interfaces;
using UserManagement.BackEnd.Application.UserFeaturePermissions.Interfaces;
using UserManagement.BackEnd.Services;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.AuthApi.Models;
using ProjectIdentifier = UserManagement.BackEnd.Models.ProjectIdentifier;

namespace UserManagement.Tests.Services
{
    [TestFixture]
    public class UserManagementServiceTests
    {
        private IUserContext _userContext = null!;
        private IAuthApiClient _authApiClient = null!;
        private IUserServiceByAuth _userService = null!;
        private IUserFeaturePermissionRepository _userFeaturePermissionRepository = null!;
        private IUserDataPermissionRepository _userDataPermissionRepository = null!;
        private UserManagementService _service = null!;
        private IProjectsService _projectsService = null!;
        private IProductsService _productService = null!;
        private IAllVueRuleRepository _allVueRuleRepository = null!;
        private IExtendedAuthApiClient _extendedAuthApiClient = null!;
        private CancellationToken _cancellationToken;

        private const string TestCompanyId = "company-123";
        private const string TestCompanyShortCode = "TEST_COMPANY";
        private const string TestUserId = "user-456";

        [SetUp]
        public void SetUp()
        {
            _userContext = Substitute.For<IUserContext>();
            _authApiClient = Substitute.For<IAuthApiClient>();
            _userService = Substitute.For<IUserServiceByAuth>();
            _userFeaturePermissionRepository = Substitute.For<IUserFeaturePermissionRepository>();
            _userDataPermissionRepository = Substitute.For<IUserDataPermissionRepository>();
            _projectsService = Substitute.For<IProjectsService>();
            _productService = Substitute.For<IProductsService>();
            _allVueRuleRepository = Substitute.For<IAllVueRuleRepository>();
            _extendedAuthApiClient = Substitute.For<IExtendedAuthApiClient>();
            _cancellationToken = CancellationToken.None;

            _userContext.UserId.Returns(TestUserId);
            _userContext.AuthCompany.Returns(TestCompanyShortCode);

            _projectsService.AuthProjectToProjectIdentifier(Arg.Any<string>()).Returns((CallInfo) =>
            {
                var projectId = CallInfo.Arg<string>();
                if (int.TryParse(projectId, out var id))
                {
                    return new ProjectIdentifier(ProjectType.AllVueSurvey, id);
                }

                return new ProjectIdentifier(ProjectType.AllVueSurveyGroup, 1);
            });

            _service = new UserManagementService(
                _userContext,
                _authApiClient,
                _userService,
                _userFeaturePermissionRepository,
                _userDataPermissionRepository,
                _projectsService,
                _productService,
                _allVueRuleRepository,
                _extendedAuthApiClient);
        }

        [Test]
        public void Constructor_WithNullUserContext_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UserManagementService(null, _authApiClient, _userService, _userFeaturePermissionRepository, _userDataPermissionRepository, _projectsService, _productService, _allVueRuleRepository, _extendedAuthApiClient));
        }

        [Test]
        public void Constructor_WithNullAuthApiClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UserManagementService(_userContext, null, _userService, _userFeaturePermissionRepository, _userDataPermissionRepository, _projectsService, _productService, _allVueRuleRepository, _extendedAuthApiClient));
        }

        [Test]
        public void Constructor_WithNullUserService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UserManagementService(_userContext, _authApiClient, null, _userFeaturePermissionRepository, _userDataPermissionRepository, _projectsService, _productService, _allVueRuleRepository,  _extendedAuthApiClient));
        }

        [Test]
        public void Constructor_WithNullUserFeaturePermissionRepository_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new UserManagementService(_userContext, _authApiClient, _userService, null, _userDataPermissionRepository, _projectsService, _productService, _allVueRuleRepository, _extendedAuthApiClient));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WithNullOrEmptyCompanyId_UsesUserContextAuthCompany()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true,_cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(Task.FromResult<IEnumerable<UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>>(userFeaturePermissions));

            // Act
            var result = await _service.GetUsersWithRolesAsync(false,null, _cancellationToken);

            // Assert
            await _authApiClient.Received(1).GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken);
            await _userService.Received(1).GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true,_cancellationToken);
            
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WithSpecificCompanyId_UsesProvidedCompanyId()
        {
            // Arrange
            var specificCompanyId = "specific-company-456";
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanies(Arg.Any<IEnumerable<string>>(), _cancellationToken)
                .Returns(Task.FromResult<IEnumerable<CompanyModel>>(new[] { expectedCompany }));
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true,_cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(Task.FromResult<IEnumerable<UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>>(userFeaturePermissions));

            // Act
            var result = await _service.GetUsersWithRolesAsync(false, specificCompanyId, _cancellationToken);

            // Assert
            await _authApiClient.Received(1).GetCompanies(
                Arg.Any<IEnumerable<string>>(), _cancellationToken);
            await _authApiClient.DidNotReceive().GetCompanyByShortcode(Arg.Any<string>(), Arg.Any<CancellationToken>());
            
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_MapsUserPropertiesCorrectly()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true, _cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(Task.FromResult<IEnumerable<UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>>(userFeaturePermissions));

            // Act
            var result = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);
            var resultList = result.ToList();

            // Assert
            var firstUser = resultList[0];
            var authUser = authUsers[0];
            
            Assert.That(firstUser.Id, Is.EqualTo(authUser.ApplicationUserId));
            Assert.That(firstUser.FirstName, Is.EqualTo(authUser.FirstName));
            Assert.That(firstUser.LastName, Is.EqualTo(authUser.LastName));
            Assert.That(firstUser.Email, Is.EqualTo(authUser.Email));
            Assert.That(firstUser.LastLogin, Is.EqualTo(authUser.LastLogin));
            Assert.That(firstUser.Verified, Is.EqualTo(authUser.Verified));
            Assert.That(firstUser.OwnerCompanyDisplayName, Is.EqualTo(authUser.OrganisationName));
            Assert.That(firstUser.IsExternalLogin, Is.EqualTo(authUser.IsOrganisationExternalLogin));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WithUserFeaturePermission_UsesFeaturePermissionRole()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true, _cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(userFeaturePermissions);

            // Act
            var result = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);
            var resultList = result.ToList();

            // Assert
            var firstUser = resultList[0];
            Assert.That(firstUser.Role, Is.EqualTo("Administrator")); // From feature permission
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WithoutUserFeaturePermission_UsesOriginalRole()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true, _cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(userFeaturePermissions);

            // Act
            var result = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);
            var resultList = result.ToList();

            // Assert
            var secondUser = resultList[1];
            Assert.That(secondUser.Role, Is.EqualTo("StandardUser")); // Original role since no feature permission
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WithEmptyUserFeaturePermissions_UsesOriginalRoles()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var emptyUserFeaturePermissions = new List<UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true, _cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(Task.FromResult<IEnumerable<UserManagement.BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>>(emptyUserFeaturePermissions));

            // Act
            var result = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);
            var resultList = result.ToList();

            // Assert
            Assert.That(resultList[0].Role, Is.EqualTo("Admin")); // Original role
            Assert.That(resultList[1].Role, Is.EqualTo("StandardUser")); // Original role
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WithNoUsers_ReturnsEmptyList()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var emptyAuthUsers = new List<UserProjectsModel>();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, false, _cancellationToken)
                .Returns(emptyAuthUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(userFeaturePermissions);

            // Act
            var result = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(0));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WhenAuthApiClientThrows_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Auth API error");
            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(Task.FromException<CompanyModel>(expectedException));

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.GetUsersWithRolesAsync(false, null, _cancellationToken));
            
            Assert.That(exception?.Message, Is.EqualTo("Auth API error"));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WhenUserServiceThrows_PropagatesException()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            
            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, true, _cancellationToken)
                .Returns(Task.FromException<IEnumerable<UserProjectsModel>>(new InvalidOperationException("User service error")));

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.GetUsersWithRolesAsync(false, null, _cancellationToken));
            
            Assert.That(exception?.Message, Is.EqualTo("User service error"));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_WhenUserFeaturePermissionRepositoryThrows_PropagatesException()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode, TestUserId, TestCompanyId, false, false, _cancellationToken)
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(Task.FromException<IEnumerable<BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>>(new InvalidOperationException("Repository error")));

            // Act & Assert
            var exception = Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _service.GetUsersWithRolesAsync(false, null, _cancellationToken));
            
            Assert.That(exception?.Message, Is.EqualTo("Repository error"));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_PassesCorrectParametersToUserService()
        {
            // Arrange
            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);
            
            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<string>(), 
                Arg.Any<bool>(), 
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(userFeaturePermissions);

            // Act
            await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);

            // Assert
            await _userService.Received(1).GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                TestCompanyShortCode,  // company.ShortCode
                TestUserId,           // _userContext.UserId
                TestCompanyId,        // company.Id
                false,                // includeSavantaUsers
                true,
                _cancellationToken
            );
        }

        [Test]
        public async Task GetUsersWithRolesAsync_ReturnsUsersWithCorrectProjects_WhenThereAreDataGroupsTheUserIsAssignedTo()
        {
            // Arrange
            var expectedProjectsForUser1 = new List<ProjectIdentifier>()
            {
                new ProjectIdentifier(ProjectType.AllVueSurvey, 1),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 2),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 4)
            };
            var expectedProjectsForUser2 = new List<ProjectIdentifier>()
            {
                new ProjectIdentifier(ProjectType.AllVueSurvey, 3),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 4),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 5)
            };

            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();
            var testCompanies = CreateTestCompanyNodeList();
            var allVueRules = GetTestAllVueRules();
            var userDataPermissions = GetTestUserDataPermissions(allVueRules);

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);

            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(userFeaturePermissions);

            _extendedAuthApiClient.GetCompanyAndChildrenList(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(testCompanies);

            _allVueRuleRepository.GetByCompaniesAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(allVueRules);

            _userDataPermissionRepository.GetByCompaniesAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(userDataPermissions);

            // Act
            var users = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);

            var user1 = users.FirstOrDefault(user => user.Id == "user1");
            var user1ProjectIds = user1.Projects.Select(project => project.ProjectIdentifier).ToList();
            var user2 = users.FirstOrDefault(user => user.Id == "user2");
            var user2ProjectIds = user2.Projects.Select(project => project.ProjectIdentifier).ToList();

            // Assert
            Assert.That(user1ProjectIds, Is.EquivalentTo(expectedProjectsForUser1));
            Assert.That(user2ProjectIds, Is.EquivalentTo(expectedProjectsForUser2));
        }

        [Test]
        public async Task GetUsersWithRolesAsync_ReturnsUsersWithCorrectProjects_WhenThereAreDataGroupsSharedToAllUsers()
        {

            // Arrange
            var expectedProjectsForUser1 = new List<ProjectIdentifier>()
            {
                new ProjectIdentifier(ProjectType.AllVueSurvey, 1),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 2),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 6),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 7)
            };
            var expectedProjectsForUser2 = new List<ProjectIdentifier>()
            {
                new ProjectIdentifier(ProjectType.AllVueSurvey, 3),
                new ProjectIdentifier(ProjectType.AllVueSurvey, 6)
            };

            var expectedCompany = CreateTestCompany();
            var authUsers = CreateTestAuthUsers();
            var userFeaturePermissions = CreateTestUserFeaturePermissions();
            var testCompanies = CreateTestCompanyNodeList();
            var allVueRules = GetTestAllVueRulesWithShared();
            var userDataPermissions = new List<UserDataPermission>();

            _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, _cancellationToken)
                .Returns(expectedCompany);

            _userService.GetAllUserDetailsForCompanyAndChildrenScopeAsync(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    Arg.Any<bool>(),
                    Arg.Any<CancellationToken>())
                .Returns(authUsers);

            _userFeaturePermissionRepository.GetAllAsync()
                .Returns(userFeaturePermissions);

            _extendedAuthApiClient.GetCompanyAndChildrenList(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(testCompanies);

            _allVueRuleRepository.GetByCompaniesAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(allVueRules);

            _userDataPermissionRepository.GetByCompaniesAsync(Arg.Any<string[]>(), Arg.Any<CancellationToken>())
                .Returns(userDataPermissions);

            // Act
            var users = await _service.GetUsersWithRolesAsync(false, null, _cancellationToken);

            var user1 = users.FirstOrDefault(user => user.Id == "user1");
            var user1ProjectIds = user1.Projects.Select(project => project.ProjectIdentifier).ToList();
            var user2 = users.FirstOrDefault(user => user.Id == "user2");
            var user2ProjectIds = user2.Projects.Select(project => project.ProjectIdentifier).ToList();

            // Assert
            Assert.That(user1ProjectIds, Is.EquivalentTo(expectedProjectsForUser1));
            Assert.That(user2ProjectIds, Is.EquivalentTo(expectedProjectsForUser2));
        }

        private CompanyModel CreateTestCompany()
        {
            return new CompanyModel
            {
                Id = TestCompanyId,
                ShortCode = TestCompanyShortCode,
                DisplayName = "Test Company"
            };
        }

        private List<UserProjectsModel> CreateTestAuthUsers()
        {
            var project1 = new UserProject
            {
                Id = 0,
                ProjectId = "1"
            };
            
            var project2 = new UserProject
            {
                Id = 1,
                ProjectId = "2"
            };
            
            var project3 = new UserProject
            {
                Id = 2,
                ProjectId = "3"
            };

            var user1 = new UserProjectsModel
            {
                ApplicationUserId = "user1",
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@test.com",
                LastLogin = DateTimeOffset.Now.AddDays(-1),
                Verified = true,
                OrganisationId = TestCompanyId,
                OrganisationName = "Test Org 1",
                RoleName = "Admin",
                IsOrganisationExternalLogin = false,
                Projects = new List<UserProject> { project1, project2 }
            };

            var user2 = new UserProjectsModel
            {
                ApplicationUserId = "user2",
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@test.com",
                LastLogin = DateTimeOffset.Now.AddDays(-2),
                Verified = false,
                OrganisationId = "child-company",
                OrganisationName = "Test Org 2",
                RoleName = "StandardUser",
                IsOrganisationExternalLogin = true,
                Projects = new List<UserProject> { project3 }
            };

            return new List<UserProjectsModel> { user1, user2 };
        }

        private List<BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission> CreateTestUserFeaturePermissions()
        {
            var adminRole = new BackEnd.Domain.UserFeaturePermissions.Entities.Role("Administrator", "TestOrg", "system@test.com");

            return new List<BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission>
            {
                new BackEnd.Domain.UserFeaturePermissions.Entities.UserFeaturePermission(
                    "user1", 
                    adminRole,
                    "system@test.com")
                // user2 intentionally has no feature permission to test fallback
            };
        }

        private List<CompanyNode> CreateTestCompanyNodeList()
        {
            var companyNodes = new List<CompanyNode>();
            var grandParentCompany = new CompanyNode()
            {
                Id = TestCompanyId,
                ShortCode = "GRANDPARENT",
                HasExternalSSOProvider = false
            };
            var parentCompany = new CompanyNode()
            {
                Id = "parent-company",
                ShortCode = "PARENT",
                HasExternalSSOProvider = false
            };
            var childCompany = new CompanyNode()
            {
                Id = "child-company",
                ShortCode = "CHILD",
                HasExternalSSOProvider = false
            };
            var childCompany2 = new CompanyNode()
            {
                Id = "child-company2",
                ShortCode = "CHILD",
                HasExternalSSOProvider = false
            };
            parentCompany.Children = new List<CompanyNode> { childCompany, childCompany2 };
            grandParentCompany.Children = new List<CompanyNode> { parentCompany };

            return new List<CompanyNode>() { grandParentCompany, parentCompany, childCompany, childCompany2 };
        }

        private List<AllVueRule> GetTestAllVueRules()
        {
            var rules = new List<AllVueRule>()
            {
                new AllVueRule()
                {
                    Id = 4,
                    Organisation = "child-company",
                    ProjectType = ProjectType.AllVueSurvey,
                    ProjectOrProductId = 4,
                    AllUserAccessForSubProduct = false,
                    Filters = new List<AllVueFilter>(),
                },
                new AllVueRule()
                {
                    Id = 5,
                    Organisation = "child-company",
                    ProjectType = ProjectType.AllVueSurvey,
                    ProjectOrProductId = 5,
                    AllUserAccessForSubProduct = false,
                    Filters = new List<AllVueFilter>(),
                }
            };

            return rules;
        }

        private List<AllVueRule> GetTestAllVueRulesWithShared()
        {
            var rules = GetTestAllVueRules();
            rules.AddRange(new List<AllVueRule>()
            {
                new AllVueRule()
                {
                    Id = 6,
                    Organisation = "child-company",
                    ProjectType = ProjectType.AllVueSurvey,
                    ProjectOrProductId = 6,
                    AllUserAccessForSubProduct = true,
                    Filters = new List<AllVueFilter>(),
                },
                new AllVueRule()
                {
                    Id = 7,
                    Organisation = "parent-company",
                    ProjectType = ProjectType.AllVueSurvey,
                    ProjectOrProductId = 7,
                    AllUserAccessForSubProduct = true,
                    Filters = new List<AllVueFilter>(),
                },
            });

            return rules;
        }

        private List<UserDataPermission> GetTestUserDataPermissions(List<AllVueRule> allVueRules)
        {
            var userDataPermissions = new List<UserDataPermission>()
            {
                new UserDataPermission
                {
                    UserId = "user1",
                    RuleId = 4,
                },
                new UserDataPermission
                {
                    UserId = "user2",
                    RuleId = 4
                },
                new UserDataPermission
                {
                    UserId = "user2",
                    RuleId = 5
                }
            };

            foreach (var permission in userDataPermissions)
            {
                permission.Rule = allVueRules.FirstOrDefault(x => x.Id == permission.RuleId);
            }

            return userDataPermissions;
        }
    }
}

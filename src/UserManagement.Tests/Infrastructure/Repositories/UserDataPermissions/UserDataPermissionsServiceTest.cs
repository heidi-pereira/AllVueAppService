using UserManagement.BackEnd.Application.UserDataPermissions.Services;
using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using AllVueRule = BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue.AllVueRule;

namespace UserManagement.Tests.Infrastructure.Repositories.UserDataPermissions
{
    [TestFixture]
    public partial class UserDataPermissionsServiceTest
    {
        private const string UserId1 = "User1";
        private const string UserId2 = "User2";
        private const string UserId3 = "User3";
        private const string UpdatedByUserId = "admin_123";
        private const string DefaultOrg = "savanta";
        private const int DefaultSubProject = 1;
        private const int DefaultSubProject2 = 2;
        private MetaDataContext? _context;
        UserDataPermissionsService _service;
        private List<UserDataPermission>? _mockData;

        private class TestTimeProvider : TimeProvider
        {
            public override DateTimeOffset GetUtcNow()
            {
                return DateTimeOffset.MinValue;
            }
        }

        [SetUp]
        public void SetUp()
        {
            var dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _context = new MetaDataContext(dbContextOptions);
            _service = new UserDataPermissionsService(
                new UserDataPermissionRepository(_context),
                new AllVueRuleRepository(_context), new TestTimeProvider());
            AllVueRuleRepositoryTests.ResetAllVueRuleId();
            var ruleForDefaultOrgAndProject = AllVueRuleRepositoryTests.CreateAllVueRule(DefaultOrg,
                new ProjectOrProduct(ProjectType.AllVueSurveyGroup, DefaultSubProject), "rule");
            var ruleForDefaultOrgAndOtherProject = AllVueRuleRepositoryTests.CreateAllVueRule(DefaultOrg,
                new ProjectOrProduct(ProjectType.AllVueSurveyGroup, DefaultSubProject2), "rule2");
            var defaultRuleFortOrgAndOtherProject = AllVueRuleRepositoryTests.CreateAllVueRule(DefaultOrg,
                new ProjectOrProduct(ProjectType.AllVueSurveyGroup, DefaultSubProject), "defaultRule");
            defaultRuleFortOrgAndOtherProject.AllUserAccessForSubProduct = true;
            var ruleForOthertOrgAndOtherProject = AllVueRuleRepositoryTests.CreateAllVueRule("other",
                new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 10000), "otherRule");


            int id = 1;
            _mockData =
            [
                new()
                {
                    Id = id++, UserId = UserId1, UpdatedByUserId = UpdatedByUserId, Rule = ruleForDefaultOrgAndProject
                },
                new()
                {
                    Id = id++, UserId = UserId2, UpdatedByUserId = UpdatedByUserId, Rule = ruleForDefaultOrgAndProject
                },
                new()
                {
                    Id = id++, UserId = UserId3, UpdatedByUserId = UpdatedByUserId, Rule = ruleForDefaultOrgAndProject
                },
                new()
                {
                    Id = id++, UserId = UserId1, UpdatedByUserId = UpdatedByUserId,
                    Rule = ruleForDefaultOrgAndOtherProject
                },
            ];

            _context.Set<AllVueRule>().AddRange([
                ruleForDefaultOrgAndProject,
                ruleForDefaultOrgAndOtherProject,
                defaultRuleFortOrgAndOtherProject,
                ruleForOthertOrgAndOtherProject
            ]);
            _context.Set<UserDataPermission>().AddRange(_mockData);

            _context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _context!.Database.EnsureDeleted();
            _context.Dispose();
            _context = null;
        }

        [Test]
        public async Task GetByUserIdAsync_ShouldReturnPermission_WhenPermissionExists()
        {
            // Act
            var result = await _service!.GetByUserIdAsync(UserId1, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result.First().UserId, Is.EqualTo(UserId1));
            Assert.That(result.First().AllVueRule.RuleName, Is.EqualTo("rule"));
        }

        [TestCase(UserId1, DefaultOrg, DefaultSubProject, "rule")]
        [TestCase("Some_other_userid", DefaultOrg, DefaultSubProject, "defaultRule")]
        [TestCase("Some_other_userid", DefaultOrg, DefaultSubProject2, null)]
        [TestCase(UserId1, DefaultOrg, DefaultSubProject2, "rule2")]
        [TestCase(UserId2, DefaultOrg, DefaultSubProject2, null)]
        public async Task GetByUserIdByOrgAndProjectAsync(string userId, string company, int project, string ruleName)
        {
            // Act
            var result = await _service!.GetByUserIdByCompanyAndProjectAsync(userId, company,
                new ProjectOrProduct(ProjectType.AllVueSurveyGroup, project), CancellationToken.None);

            // Assert
            if (ruleName == null)
            {
                Assert.That(result, Is.Null);
                return;
            }

            Assert.That(result, Is.Not.Null);
            Assert.That(result.RuleName, Is.EqualTo(ruleName));
        }

        [TestCase(new[] { DefaultOrg }, 3)]
        [TestCase(new[] { DefaultOrg, "someothercompany" }, 3)]
        [TestCase(new[] { DefaultOrg, "other" }, 4)]
        public async Task GetAllVueRulesByCompaniesAsync_ShouldReturnRulesForGivenCompanies(string[] companies,
            int expected)
        {
            // Act
            var result = await _service.GetAllVueRulesByCompaniesAsync(companies, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(expected));
        }

        [Test]
        public async Task DeleteAllVueUserDataPermissionAsync_ShouldRemovePermission_WhenIdExists()
        {
            // Arrange
            var userId = UserId1;
            var token = CancellationToken.None;
            var permissionToDelete = _mockData!.First();
            var idToDelete = permissionToDelete.Id;

            // Ensure the permission exists before deletion
            var beforeDelete = await _context!.Set<UserDataPermission>().FindAsync(idToDelete);
            Assert.That(beforeDelete, Is.Not.Null);

            // Act
            await _service.DeleteAllVueUserDataPermissionAsync(idToDelete, token);

            // Assert
            var afterDelete = await _context.Set<UserDataPermission>().FindAsync(idToDelete);
            Assert.That(afterDelete, Is.Null);
        }

        [Test]
        public async Task DeleteAllVueUserDataPermissionAsync_ShouldNotThrow_WhenIdDoesNotExist()
        {
            // Arrange
            var nonExistentId = 99999999;
            var token = CancellationToken.None;
            var countBefore = _context!.Set<UserDataPermission>().Count();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () =>
            {
                await _service.DeleteAllVueUserDataPermissionAsync(nonExistentId, token);
            });

            // Ensure no records were deleted
            var countAfter = _context.Set<UserDataPermission>().Count();
            Assert.That(countAfter, Is.EqualTo(countBefore));
        }

        [Test]
        public async Task AddAllVueRuleAsync_ShouldAddRuleToDatabase()
        {
            // Arrange
            var token = CancellationToken.None;
            var userId = "test_user";
            var newRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: 0,
                ruleName: "newRule",
                allCompanyUsersCanAccessProject: false,
                company: "newCompany",
                projectType: ProjectType.AllVueSurveyGroup,
                projectId: 1001,
                availableVariableIds: new List<int> { 1, 2 },
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                "",
                DateTime.UtcNow
            );

            // Act
            await _service.AddAllVueRuleAsync(userId, newRule, token);

            // Assert
            var addedRule = _context!.Set<AllVueRule>()
                .FirstOrDefault(r => r.RuleName == "newRule" && r.Organisation == "newCompany");
            Assert.That(addedRule, Is.Not.Null);
            Assert.That(addedRule.RuleName, Is.EqualTo("newRule"));
            Assert.That(addedRule.Organisation, Is.EqualTo("newCompany"));
            Assert.That(addedRule.ProjectType, Is.EqualTo(ProjectType.AllVueSurveyGroup));
            Assert.That(addedRule.ProjectOrProductId, Is.EqualTo(1001));
            Assert.That(addedRule.AvailableVariableIds, Is.EquivalentTo(new List<int> { 1, 2 }));
            Assert.That(addedRule.UpdatedByUserId, Is.EqualTo(userId));
            Assert.That(addedRule.UpdatedDate, Is.EqualTo(DateTimeOffset.MinValue.UtcDateTime));
        }

        [Test]
        public async Task AssignAllVueRuleToUserDataPermissionAsync_ShouldAssignNewRuleAndUpdateAuditFields()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "assigner_user";

            // Get an existing user data permission
            var userDataPermissionEntityID = 1;

            // Create and add a new AllVueRule
            var newRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: 0,
                ruleName: "AssignedRule",
                allCompanyUsersCanAccessProject: false,
                company: "assignedCompany",
                projectType: ProjectType.AllVueSurveyGroup,
                projectId: 1002,
                availableVariableIds: new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );
            var result = _context.Set<AllVueRule>().Add(newRule.FromAllVueRule());
            _context.SaveChanges();

            // Act
            await _service.AssignAllVueRuleToUserDataPermissionAsync(userDataPermissionEntityID, result.Entity.Id,
                updatedByUserId, token);

            // Assert
            var updatedEntity = _context.Set<UserDataPermission>().Find(userDataPermissionEntityID);
            Assert.That(updatedEntity, Is.Not.Null);
            Assert.That(updatedEntity.RuleId, Is.EqualTo(result.Entity.Id));
            Assert.That(updatedEntity.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            Assert.That(updatedEntity.UpdatedDate, Is.EqualTo(DateTimeOffset.MinValue.UtcDateTime));
        }

        [Test]
        public async Task DeleteAllVueRuleAsync_ShouldDeleteRule_WhenRuleExists()
        {
            // Arrange
            var token = CancellationToken.None;
            var rule = _context!.Set<AllVueRule>().First();
            var ruleId = rule.Id;

            // Ensure the rule exists before deletion
            var beforeDelete = await _context.Set<AllVueRule>().FindAsync(ruleId);
            Assert.That(beforeDelete, Is.Not.Null);
            var referenced = await _context.Set<UserDataPermission>().CountAsync(x => x.RuleId == ruleId);
            Assert.That(referenced, Is.Not.EqualTo(0));

            // Act
            await _service.DeleteAllVueRuleAsync(ruleId, token);

            // Assert
            var afterDelete = await _context.Set<AllVueRule>().FindAsync(ruleId);
            Assert.That(afterDelete, Is.Null);
            referenced = await _context.Set<UserDataPermission>().CountAsync(x => x.RuleId == ruleId);
            Assert.That(referenced, Is.EqualTo(0));

        }

        [Test]
        public async Task DeleteAllVueRuleAsync_ShouldNotThrow_WhenRuleDoesNotExist()
        {
            // Arrange
            var token = CancellationToken.None;
            var nonExistentRuleId = 99999999;
            var countBefore = _context!.Set<AllVueRule>().Count();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => { await _service.DeleteAllVueRuleAsync(nonExistentRuleId, token); });

            // Ensure no records were deleted
            var countAfter = _context.Set<AllVueRule>().Count();
            Assert.That(countAfter, Is.EqualTo(countBefore));
        }


        [TestCase(DefaultSubProject, new[] { "rule", "defaultRule" })]
        [TestCase(DefaultSubProject2, new[] { "rule2" })]
        [TestCase(0, new string[0])]
        public async Task GetAllVueRulesByProjectAsync_ShouldReturnExpectedRules_WhenGivenAProjectId(int project,
            string[] expectedRuleNames)
        {
            // Act
            var results =
                await _service!.GetAllVueRulesByProjectAsync(DefaultOrg,
                    new ProjectOrProduct(ProjectType.AllVueSurveyGroup, project), CancellationToken.None);

            // Assert
            Assert.That(results.Count, Is.EqualTo(expectedRuleNames.Length));
            var ruleNames = results.Select(r => r.RuleName).ToList();
            Assert.That(ruleNames.All(ruleName => expectedRuleNames.Contains(ruleName)), Is.True, $"Expected: {string.Join(", ", expectedRuleNames)}; Received: {string.Join(", ", ruleNames)}");
        }

        [TestCase(1, new[] { UserId1, UserId2, UserId3 })]
        [TestCase(2, new [] { UserId1 })]
        [TestCase(3, new string[0])]
        public async Task GetUserIdsAssignedToAllVueRuleAsync_ShouldReturnListOfUserIds_WhenGivenARuleId(int ruleId, string[] expectedUserIds)
        {
            // Act
            var userIds = await _service!.GetUserIdsAssignedToAllVueRuleAsync(ruleId, CancellationToken.None);

            // Assert
            Assert.That(userIds.Count, Is.EqualTo(expectedUserIds.Length));
            Assert.That(userIds.All(userId => expectedUserIds.Contains(userId)), Is.True, $"Expected: {string.Join(", ", expectedUserIds)}; Received: {string.Join(", ", userIds)}");
        }
    }
}

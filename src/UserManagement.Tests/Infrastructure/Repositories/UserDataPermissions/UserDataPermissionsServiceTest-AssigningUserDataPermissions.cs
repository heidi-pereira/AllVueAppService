using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using AllVueRule = BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue.AllVueRule;


namespace UserManagement.Tests.Infrastructure.Repositories.UserDataPermissions
{
    public partial class UserDataPermissionsServiceTest
    {
        [Test]
        public async Task AssignAllVueRuleToUserDataPermissionAsync_CreatesPermissions_ForNewUsers()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "bulk_assigner";
            var newUserIds = new[] { "bulkUser1", "bulkUser2" };
            var newRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: 0,
                ruleName: "BulkRule",
                allCompanyUsersCanAccessProject: false,
                company: "bulkOrg",
                projectType: ProjectType.AllVueSurvey,
                projectId:1,
                availableVariableIds: new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );
            var ruleEntity = _context!.Set<AllVueRule>().Add(newRule.FromAllVueRule());
            _context.SaveChanges();

            // Act
            await _service.AssignAllVueRuleToUserDataPermissionAsync(newUserIds, ruleEntity.Entity.Id, updatedByUserId, token);

            // Assert
            foreach (var userId in newUserIds)
            {
                var permission = _context.Set<UserDataPermission>().FirstOrDefault(p => p.UserId == userId && p.RuleId == ruleEntity.Entity.Id);
                Assert.That(permission, Is.Not.Null);
                Assert.That(permission.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            }
        }

        [Test]
        public async Task AssignAllVueRuleToUserDataPermissionAsync_UpdatesPermissions_ForExistingUsers()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "bulk_assigner";
            var existingUserIds = new[] { UserId1, UserId2 };
            var newRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: 0,
                ruleName: "BulkUpdateRule",
                allCompanyUsersCanAccessProject: false,
                company: "bulkOrg",
                projectType: ProjectType.AllVueSurvey,
                projectId: 1,
                availableVariableIds: new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );
            var ruleEntity = _context!.Set<AllVueRule>().Add(newRule.FromAllVueRule());
            _context.SaveChanges();

            // Act
            await _service.AssignAllVueRuleToUserDataPermissionAsync(existingUserIds, ruleEntity.Entity.Id, updatedByUserId, token);

            // Assert
            foreach (var userId in existingUserIds)
            {
                var permission = _context.Set<UserDataPermission>().FirstOrDefault(p => p.UserId == userId && p.RuleId == ruleEntity.Entity.Id);
                Assert.That(permission, Is.Not.Null);
                Assert.That(permission.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            }
        }

        [Test]
        public async Task AssignAllVueRuleToUserDataPermissionAsync_CreatesAndUpdates_ForMixedUsers()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "bulk_assigner";
            var mixedUserIds = new[] { UserId1, "bulkNewUser" };
            var newRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: 0,
                ruleName: "BulkMixedRule",
                allCompanyUsersCanAccessProject: false,
                company: "bulkOrg",
                projectType: ProjectType.AllVueSurvey,
                projectId: 1,
                availableVariableIds: new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );
            var ruleEntity = _context!.Set<AllVueRule>().Add(newRule.FromAllVueRule());
            _context.SaveChanges();

            // Act
            await _service.AssignAllVueRuleToUserDataPermissionAsync(mixedUserIds, ruleEntity.Entity.Id, updatedByUserId, token);

            // Assert
            // Existing user updated
            var existingPermission = _context.Set<UserDataPermission>().FirstOrDefault(p => p.UserId == UserId1 && p.RuleId == ruleEntity.Entity.Id);
            Assert.That(existingPermission, Is.Not.Null);
            Assert.That(existingPermission.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            // New user created
            var newPermission = _context.Set<UserDataPermission>().FirstOrDefault(p => p.UserId == "bulkNewUser" && p.RuleId == ruleEntity.Entity.Id);
            Assert.That(newPermission, Is.Not.Null);
            Assert.That(newPermission.UpdatedByUserId, Is.EqualTo(updatedByUserId));
        }

        [Test]
        public void AssignAllVueRuleToUserDataPermissionAsync_ThrowsArgumentException_WhenRuleNotFound()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "bulk_assigner";
            var userIds = new[] { "userX" };
            var nonExistentRuleId = 999999;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.AssignAllVueRuleToUserDataPermissionAsync(userIds, nonExistentRuleId, updatedByUserId, token);
            });
            Assert.That(ex!.Message, Does.Contain("AllVueRule with ID"));
        }
    }
}
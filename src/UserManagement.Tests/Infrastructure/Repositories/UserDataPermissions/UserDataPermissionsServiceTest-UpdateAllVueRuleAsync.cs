namespace UserManagement.Tests.Infrastructure.Repositories.UserDataPermissions
{
    [TestFixture]
    public partial class UserDataPermissionsServiceTest
    {
        [Test]
        public async Task UpdateAllVueRuleAsync_ShouldUpdateRule_WhenRuleExists()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "update_user";
            var rule = _context!.Set<AllVueRule>().First();
            var newName = "UpdatedRuleName";
            rule.RuleName = newName;

            var domainRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: rule.Id,
                ruleName: newName,
                allCompanyUsersCanAccessProject: rule.AllUserAccessForSubProduct,
                company: rule.Organisation,
                projectType: rule.ProjectType,
                projectId: rule.ProjectOrProductId,
                availableVariableIds: rule.AvailableVariableIds?.ToList() ?? new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );

            // Act
            await _service.UpdateAllVueRuleAsync(updatedByUserId, domainRule, token);

            // Assert
            var updatedRule = _context.Set<AllVueRule>().Find(rule.Id);
            Assert.That(updatedRule, Is.Not.Null);
            Assert.That(updatedRule.RuleName, Is.EqualTo(newName));
            Assert.That(updatedRule.UpdatedByUserId, Is.EqualTo(updatedByUserId));
            Assert.That(updatedRule.UpdatedDate, Is.EqualTo(DateTimeOffset.MinValue.UtcDateTime));
        }

        [Test]
        public void UpdateAllVueRuleAsync_ShouldThrowArgumentNullException_WhenRuleIsNull()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "update_user";

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _service.UpdateAllVueRuleAsync(updatedByUserId, null, token);
            });
        }

        [Test]
        public async Task UpdateAllVueRuleAsync_ShouldNotThrow_WhenRuleDoesNotExist()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "update_user";
            var nonExistentRuleId = 999999;
            var domainRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: nonExistentRuleId,
                ruleName: "NonExistentRule",
                allCompanyUsersCanAccessProject: false,
                company: "noCompany",
                projectType: ProjectType.AllVueSurveyGroup,
                projectId: 1009,
                availableVariableIds: new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.UpdateAllVueRuleAsync(updatedByUserId, domainRule, token);
            });
        }

        [Test]
        public async Task UpdateAllVueRuleAsync_ShouldUpdateCompany_WhenCompanyIsChanged()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "update_user";
            var rule = _context!.Set<AllVueRule>().First();
            var newCompany = "UpdatedCompany";
            var originalCompany = rule.Organisation;

            var domainRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: rule.Id,
                ruleName: rule.RuleName,
                allCompanyUsersCanAccessProject: rule.AllUserAccessForSubProduct,
                company: newCompany,
                projectType: rule.ProjectType,
                projectId: rule.ProjectOrProductId,
                availableVariableIds: rule.AvailableVariableIds?.ToList() ?? new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );

            // Act
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.UpdateAllVueRuleAsync(updatedByUserId, domainRule, token);
            });

            // Assert
            var updatedRule = _context.Set<AllVueRule>().Find(rule.Id);
            Assert.That(updatedRule, Is.Not.Null);
            Assert.That(updatedRule.Organisation, Is.Not.EqualTo(newCompany));
            Assert.That(updatedRule.Organisation, Is.EqualTo(originalCompany));
        }

        [Test]
        public async Task UpdateAllVueRuleAsync_ShouldUpdateProject_WhenProjectIsChanged()
        {
            // Arrange
            var token = CancellationToken.None;
            var updatedByUserId = "update_user";
            var rule = _context!.Set<AllVueRule>().First();
            var newProject = 1001;
            var originalProject = new ProjectOrProduct(rule.ProjectType, rule.ProjectOrProductId);

            var domainRule = new BackEnd.Domain.UserDataPermissions.Entities.AllVueRule(
                id: rule.Id,
                ruleName: rule.RuleName,
                allCompanyUsersCanAccessProject: rule.AllUserAccessForSubProduct,
                company: rule.Organisation,
                projectType: rule.ProjectType, 
                projectId: newProject,
                availableVariableIds: rule.AvailableVariableIds?.ToList() ?? new List<int>(),
                filters: new List<BackEnd.Domain.UserDataPermissions.Entities.AllVueFilter>(),
                updatedByUserId: updatedByUserId,
                updatedDate: DateTime.UtcNow
            );

            // Act
            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _service.UpdateAllVueRuleAsync(updatedByUserId, domainRule, token);
            });

            // Assert
            var updatedRule = _context.Set<AllVueRule>().Find(rule.Id);
            Assert.That(updatedRule, Is.Not.Null);
            Assert.That(updatedRule.ProjectOrProductId, Is.Not.EqualTo(newProject));
            Assert.That(updatedRule.ProjectOrProductId, Is.EqualTo(originalProject.ProjectId));
        }

    }
}

namespace UserManagement.Tests.Infrastructure.Repositories.UserDataPermissions
{
    [TestFixture]
    public partial class UserDataPermissionsServiceTest
    {
        [Test]
        public async Task GetByCompanyAndProjectAsync_ShouldReturnPermissions_WhenPermissionsExist()
        {
            // Arrange
            var token = CancellationToken.None;
            var company = DefaultOrg;
            var project = DefaultSubProject;

            // Act
            var result = await _service.GetByCompaniesAndProjectAsync([company,"SomeOther"], new ProjectOrProduct(ProjectType.AllVueSurveyGroup, project), token);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.GreaterThan(0));
            Assert.That(result.All(p => p.AllVueRule.Company == company && p.AllVueRule.ProjectId == project),
                Is.True);
        }

        [Test]
        public async Task GetByCompanyAndProjectAsync_ShouldReturnEmpty_WhenNoPermissionsExist()
        {
            // Arrange
            var token = CancellationToken.None;
            var company = "NonExistentCompany";
            var project = 1003;

            // Act
            var result = await _service.GetByCompaniesAndProjectAsync([company], new ProjectOrProduct(ProjectType.AllVueSurveyGroup, project), token);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetByCompanyAndProjectAsync_ShouldReturnOnlyMatchingPermissions()
        {
            // Arrange
            var token = CancellationToken.None;
            var company = DefaultOrg;
            var project = DefaultSubProject2;

            // Act
            var result = await _service.GetByCompaniesAndProjectAsync([company], new ProjectOrProduct(ProjectType.AllVueSurveyGroup, project), token);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.All(p => p.AllVueRule.Company == company && p.AllVueRule.ProjectId == project),
                Is.True);
        }
    }
}

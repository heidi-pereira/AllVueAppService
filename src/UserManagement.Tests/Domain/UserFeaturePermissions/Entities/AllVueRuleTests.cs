using UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UDF = UserManagement.BackEnd.Domain.UserDataPermissions.Entities;
using UserManagement.BackEnd.Models;

namespace UserManagement.Tests.Domain.UserFeaturePermissions.Entities
{
    [TestFixture]
    public class AllVueRuleExtensionsTests
    {
        [Test]
        public void CreateDefaultRole_NoExistingRules_ReturnsBaseName()
        {
            var rules = new List<UDF.AllVueRule>();
            var company = "TestCo";
            var projectId = new ProjectIdentifier(ProjectType.BrandVue, 1);

            var rule = rules.CreateDefaultRole("All", company, projectId, true);

            Assert.That("All", Is.EqualTo(rule.RuleName));
            Assert.That(rule.AllCompanyUsersCanAccessProject, Is.True);
        }

        [Test]
        public void CreateDefaultRole_ExistingBaseName_AddsSuffix()
        {
            var rules = new List<UDF.AllVueRule>
            {
                new UDF.AllVueRule(1, "All", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow)
            };
            var company = "TestCo";
            var projectId = new ProjectIdentifier(ProjectType.BrandVue, 1);

            var rule = rules.CreateDefaultRole("All", company, projectId, true);

            Assert.That("All~1", Is.EqualTo(rule.RuleName));
        }

        [Test]
        public void CreateDefaultRole_MultipleExistingSuffixes_AddsNextAvailableSuffix()
        {
            var rules = new List<UDF.AllVueRule>
            {
                new UDF.AllVueRule(1, "All", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow),
                new UDF.AllVueRule(2, "All~1", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow),
                new UDF.AllVueRule(3, "All~2", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow)
            };
            var company = "TestCo";
            var projectId = new ProjectIdentifier(ProjectType.BrandVue, 1);

            var rule = rules.CreateDefaultRole("All", company, projectId, true);

            Assert.That("All~3", Is.EqualTo(rule.RuleName));
        }

        [Test]
        public void CreateDefaultRole_CaseInsensitiveMatching_AddsSuffix()
        {
            var rules = new List<UDF.AllVueRule>
            {
                new UDF.AllVueRule(1, "all", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow)
            };
            var company = "TestCo";
            var projectId = new ProjectIdentifier(ProjectType.BrandVue, 1);

            var rule = rules.CreateDefaultRole("All", company, projectId, true);

            Assert.That("All~1", Is.EqualTo(rule.RuleName));
        }

        [Test]
        public void CreateDefaultRole_CustomBaseName_UniqueSuffix()
        {
            var rules = new List<UDF.AllVueRule>
            {
                new UDF.AllVueRule(1, "Custom", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow),
                new UDF.AllVueRule(2, "Custom~1", true, "TestCo", ProjectType.BrandVue, 1, new List<int>(),
                    new List<UDF.AllVueFilter>(), "", DateTime.UtcNow)
            };
            var company = "TestCo";
            var projectId = new ProjectIdentifier(ProjectType.BrandVue, 1);

            var rule = rules.CreateDefaultRole("Custom", company, projectId, true);

            Assert.That("Custom~2", Is.EqualTo(rule.RuleName));
        }
    }
}
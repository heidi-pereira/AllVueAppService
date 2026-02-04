using BrandVue.EntityFramework.MetaData.Authorisation;

namespace UserManagement.Tests.Infrastructure.Repositories.UserDataPermissions
{
    [TestFixture]
    public class AllVueRuleRepositoryTests
    {
        private const string RuleOneName = "Rule1";
        private const string RuleTwoName = "Rule2";
        private MetaDataContext? _mockContext;
        private AllVueRuleRepository? _repository;

        static int _AllVueFilterId = 1;
        static int _AllVueRuleId = 1;

        internal static void ResetAllVueRuleId()
        {
            _AllVueRuleId = 1;
        }

        private void AddRule(string organisation, ProjectOrProduct subProduct, string ruleName)
        {
            var mockRule = CreateAllVueRule(organisation, subProduct, ruleName);
            _mockContext?.AllVueRules.AddRange(new []{ mockRule });
        }

        public static AllVueRule CreateAllVueRule(string organisation, ProjectOrProduct subProduct, string ruleName)
        {
            var mockFilter1 = new List<AllVueFilter>
            {
                new AllVueFilter
                {
                    Id = _AllVueFilterId++, AllVueRule = new AllVueRule { },
                    EntityIds = new int[] { 1, 2 }, EntitySetId = 3, VariableConfigurationId = 4
                },
            };

            var mockRule = new AllVueRule
            {
                Id = _AllVueRuleId++, ProjectType = subProduct.ProjectType, ProjectOrProductId = subProduct.ProjectId, RuleName = ruleName,
                AllUserAccessForSubProduct = false,
                AvailableVariableIds = new[] { 1, 2 }, Organisation = organisation, SystemKey = SystemKey.AllVue,
                Filters = mockFilter1, UpdatedByUserId = "bill@example.com"
            };

            foreach (var mockFilter in mockFilter1)
            {
                mockFilter.AllVueRule = mockRule;
                mockFilter.AllVueRuleId = mockRule.Id;
            }

            return mockRule;
        }


        private void AddDefaultRule(string company, ProjectOrProduct subProduct)
        {
            _mockContext.AllVueRules.AddRange(new[] { CreateAllVueDefaultRule(company, subProduct) });
        }

        private static AllVueRule CreateAllVueDefaultRule(string organisation, ProjectOrProduct subProduct)
        {
            var mockRule = new AllVueRule
            {
                Id = _AllVueRuleId++, ProjectType = subProduct.ProjectType, ProjectOrProductId = subProduct.ProjectId, RuleName = "defaultRule",
                AllUserAccessForSubProduct = true,
                AvailableVariableIds = new List<int>(), Organisation = organisation, SystemKey = SystemKey.AllVue,
                Filters = null, UpdatedByUserId = "bill@example.com"
            };
            return mockRule;
        }

        [SetUp]
        public async Task SetUp()
        {
            var options = new DbContextOptionsBuilder<MetaDataContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _mockContext = new MetaDataContext(options);
            _repository = new AllVueRuleRepository(_mockContext);

            AddRule("savanta", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1), RuleOneName);
            AddRule("savanta", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1), RuleTwoName);
            AddRule("savanta", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 2), RuleOneName);
            AddRule("savanta", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 2), RuleTwoName);
            AddRule("childOrganisation", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1), RuleTwoName);
            AddRule(Guid.NewGuid().ToString("N"), new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1001), "SomeRule");
            AddRule(Guid.NewGuid().ToString("N"), new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1002), "SomeRule");
            AddRule(Guid.NewGuid().ToString("N"), new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1002), "SomeRule");
            AddDefaultRule("savanta", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1));

            await _mockContext.SaveChangesAsync();
        }
        [TearDown]
        public void TearDown()
        {
            _mockContext!.Database.EnsureDeleted();
            _mockContext.Dispose();
            _mockContext = null;
        }

        [Test]
        public async Task GetById_ShouldReturnCorrectRule_WhenIdExists()
        {
            // Arrange
            var organisation = "testOrg";
            var subProduct = new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1);
            var ruleName = "TestRule";
            var rule = CreateAllVueRule(organisation, subProduct, ruleName);
            _mockContext!.AllVueRules.Add(rule);
            await _mockContext.SaveChangesAsync();

            // Act
            var result = await _repository!.GetById(rule.Id, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(rule.Id));
            Assert.That(result.RuleName, Is.EqualTo(ruleName));
            Assert.That(result.Organisation, Is.EqualTo(organisation));
            Assert.That(result.ProjectType, Is.EqualTo(subProduct.ProjectType));
            Assert.That(result.ProjectOrProductId, Is.EqualTo(subProduct.ProjectId));
        }

        [Test]
        public async Task GetById_ShouldReturnNull_WhenIdDoesNotExist()
        {
            // Arrange
            var nonExistentId = -12345;

            // Act
            var result = await _repository!.GetById(nonExistentId, CancellationToken.None);

            // Assert
            Assert.That(result, Is.Null);
        }

        [TestCase( new string[] {"savanta" }, 5)]
        [TestCase( new string[] { "childOrganisation" }, 1)]
        [TestCase( new string[] { "savanta", "childOrganisation" }, 6)]
        [TestCase( new string[] { "savanta", "childOrganisation","somethingelse" }, 6)]
        [TestCase( new string[] { "somethingelse" }, 0)]
        public async Task GetAllAsync_ShouldReturnAllAllVueRulesForOrganisation(string [] companies, int expected)
        {
            // Act
            var result = await _repository!.GetByCompaniesAsync(companies, CancellationToken.None);
            // Assert
            Assert.That(result.Count(), Is.EqualTo(expected));
        }

        [TestCase("savanta", 1, true)]
        [TestCase("savanta", -1, false)]
        [TestCase("nonExistingOrganisation", 1, false)]
        public async Task GetAllAsync_ShouldReturnDefaultAllVueRulesForOrganisationAndSubProduct(string organisation, int productId, bool expectedHasDefaultRule)
        {
            // Act
            var result = await _repository!.GetDefaultByCompanyAndAllVueProjectAsync(organisation, new ProjectOrProduct(ProjectType.AllVueSurveyGroup, productId), CancellationToken.None);
            // Assert
            if (expectedHasDefaultRule)
            {
                Assert.That(result.AllUserAccessForSubProduct, Is.True);
            }
            else
            {
                Assert.That(result, Is.Null);
            }
        }

        [Test,Ignore("I have failed to make the SQL Unique constraint work in the test")]
        public void AddingDuplicateDefaultShouldThrownAnException()
        {
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _repository!.AddAsync(CreateAllVueDefaultRule("savanta", new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1)), CancellationToken.None);
            });
        }

        [Test]
        public async Task DeleteAllVueRulesForOrganisationAndSubProduct()
        {
            // Arrange
            var vueRule = _mockContext.AllVueRules.First();

            // Act
            await _repository!.DeleteAsync(vueRule.Id, CancellationToken.None);

            // Assert
            var deletedPermission = await _mockContext.Set<AllVueRule>().FindAsync(vueRule.Id);
            Assert.That(deletedPermission, Is.Null);
        }

        [Test]
        public async Task UpdateAsync_ShouldUpdatePermission()
        {
            // Arrange
            var vueRule = _mockContext.Set<AllVueRule>().First();
            vueRule.Organisation = "SomethingWeird";

            // Act
            await _repository!.UpdateAsync(vueRule,CancellationToken.None);

            // Assert
            var updatedPermission = await _mockContext.Set<AllVueRule>().FindAsync(vueRule.Id);
            Assert.That(updatedPermission, Is.Not.Null);
            Assert.That(updatedPermission!.Organisation, Is.EqualTo("SomethingWeird"));
        }
    }
}

namespace UserManagement.Tests.Infrastructure.Repositories.UserDataPermissions;

[TestFixture]
public class UserDataPermissionRepositoryTests
{
    private const string UserId1 = "User1";
    private const string UserId2 = "User2";
    private const string UserId3 = "User3";
    private const string UpdatedByUserId = "admin_123";
    private const string DefaultOrg = "savanta";
    private const int DefaultSubProduct = 1;
    private const int SecondSubProduct = 2;

    private static readonly ProjectOrProduct _DefaultProject = new(ProjectType.AllVueSurveyGroup, DefaultSubProduct);
    private static readonly ProjectOrProduct _SecondProject = new(ProjectType.AllVueSurvey, SecondSubProduct);
    private MetaDataContext? _context;
    private UserDataPermissionRepository? _repository;
    private List<UserDataPermission>? _mockData;
    private UserDataPermission? _newPermission;
    private AllVueRule _rule = AllVueRuleRepositoryTests.CreateAllVueRule(DefaultOrg, _DefaultProject, "rule");

    [SetUp]
    public void SetUp()
    {
        var dbContextOptions = new DbContextOptionsBuilder<MetaDataContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new MetaDataContext(dbContextOptions);
        _repository = new UserDataPermissionRepository(_context);

        var secondAllVueRule = AllVueRuleRepositoryTests.CreateAllVueRule(DefaultOrg, _SecondProject, "rule 2");

        _mockData =
        [
            new() { Id = 1, UserId = UserId1, UpdatedByUserId = UpdatedByUserId, Rule = _rule},
            new() { Id = 2, UserId = UserId2, UpdatedByUserId = UpdatedByUserId, Rule = _rule},
            new() { Id = 3, UserId = UserId3, UpdatedByUserId = UpdatedByUserId, Rule = _rule},
            new() { Id = 4, UserId = UserId1, UpdatedByUserId = UpdatedByUserId, Rule = secondAllVueRule},
            new() { Id = 5, UserId = UserId2, UpdatedByUserId = UpdatedByUserId, Rule = secondAllVueRule}
        ];

        _context.AllVueRules.AddRange([_rule]);
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

    [TestCase("savanta", 1, 3)]
    [TestCase("savanta", -1, 0)]
    [TestCase("nonExistingCompany", 1, 0)]
    public async Task GetAllAsync_ShouldReturnAllAllVueRulesForOrganisationAndSubProduct(string organisation, int subProduct, int expected)
    {
        // Act
        var result = await _repository!.GetByCompaniesAndAllVueProjectsAsync([organisation], new ProjectOrProduct(ProjectType.AllVueSurveyGroup, subProduct), CancellationToken.None);
        // Assert
        Assert.That(result.Count(), Is.EqualTo(expected));
    }

    [Test]
    public async Task GetByUserIdAsync_ShouldReturnPermission_WhenPermissionExists()
    {
        // Act
        var result = await _repository!.GetByUserIdAsync(UserId3,CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.First().UserId, Is.EqualTo(UserId3));
        Assert.That(result.First().Rule.RuleName, Is.EqualTo("rule"));
    }

    [Test]
    public async Task AddAllVueRulesForOrganisationAndSubProduct()
    {
        // Act
        var projectOrProduct = new ProjectOrProduct(ProjectType.AllVueSurveyGroup, 1);
        var rule = AllVueRuleRepositoryTests.CreateAllVueRule("organisation", projectOrProduct, Guid.NewGuid().ToString("N"));
        _context.AllVueRules.AddRange([rule]);
        List<UserDataPermission> mockData =
        [
            new() { Id = 100, UserId = UserId1, UpdatedByUserId = UpdatedByUserId, Rule = rule},
            new() { Id = 102, UserId = UserId2, UpdatedByUserId = UpdatedByUserId, Rule =rule},
            new() { Id = 103, UserId = UserId3, UpdatedByUserId = UpdatedByUserId, Rule = rule}
        ];

        _context.AllVueRules.AddRange(rule);
        _context.Set<UserDataPermission>().AddRange(mockData);

        _context.SaveChanges();

        var rulesAfterAdd = await _repository!.GetByCompaniesAndAllVueProjectsAsync(["organisation"], new ProjectOrProduct(projectOrProduct.ProjectType, projectOrProduct.ProjectId), CancellationToken.None);
        // Assert
        Assert.That(rulesAfterAdd.Count(), Is.EqualTo(3));
        Assert.That(rulesAfterAdd.All(ruleAdded=> ruleAdded.Rule.RuleName == rule.RuleName), Is.EqualTo(true));
    }

    [Test]
    public async Task GetByUserIdAsync_WhenPermissionDoesNotExist()
    {
        // Act & Assert
        var result = await _repository!.GetByUserIdAsync("NonExistentUser", CancellationToken.None);
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllPermissions()
    {
        // Act
        var result = await _repository!.GetByCompanyAndAllVueProjectAsync(DefaultOrg, new ProjectOrProduct(_DefaultProject.ProjectType, _DefaultProject.ProjectId), CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(3));
    }

    [TestCase(new String[] {DefaultOrg}, 5)]
    [TestCase(new String[] {"somethingelse"}, 0)]
    [TestCase(new String[] {DefaultOrg, "somethingelse"}, 5)]
    public async Task GetAllAsync_ShouldReturnAllPermissionsForCompanies(string[] companies, int expected)
    {
        // Act
        var result = await _repository!.GetByCompaniesAsync(companies, CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(expected));
    }

    [Test]
    public async Task AddAsync_ShouldAddPermission()
    {
        int newId = _mockData?.Count + 1 ?? 1;
        // Arrange
        _newPermission = new UserDataPermission { Id = newId, UserId = UserId3, UpdatedByUserId = UpdatedByUserId, Rule = _rule};
        // Act
        await _repository!.AddAsync(_newPermission, CancellationToken.None);

        // Assert
        var addedPermission = await _context.Set<UserDataPermission>().FindAsync(_newPermission.Id);
        Assert.That(addedPermission, Is.Not.Null);
        Assert.That(addedPermission!.UserId, Is.EqualTo(UserId3));
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdatePermission()
    {
        // Arrange
        var existingPermission = _mockData!.First();
        existingPermission.UserId = "UpdatedUser";

        // Act
        await _repository!.UpdateAsync(existingPermission, CancellationToken.None);

        // Assert
        var updatedPermission = await _context.Set<UserDataPermission>().FindAsync(existingPermission.Id);
        Assert.That(updatedPermission, Is.Not.Null);
        Assert.That(updatedPermission!.UserId, Is.EqualTo("UpdatedUser"));
    }

    [Test]
    public async Task DeleteAsync_ShouldRemovePermission_WhenPermissionExists()
    {
        // Arrange
        var permissionToDelete = _mockData!.First();

        // Act
        await _repository!.DeleteAsync(permissionToDelete.Id, CancellationToken.None);

        // Assert
        var deletedPermission = await _context.Set<UserDataPermission>().FindAsync(permissionToDelete.Id);
        Assert.That(deletedPermission, Is.Null);
    }

    [Test]
    public async Task GetByUserIdsAndProjectAsync_ShouldReturnList_WhenPermissionExists()
    {
        // Arrange
        var userIds = new[] { UserId1, UserId2, UserId3 };

        // Act
        var result = await _repository!.GetByUserIdsAndProjectAsync(userIds, _SecondProject, CancellationToken.None);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2));
    }
}
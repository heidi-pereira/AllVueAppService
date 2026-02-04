namespace UserManagement.Tests.Infrastructure.Repositories.UserFeaturePermissions;

[TestFixture]
public class PermissionFeatureRepositoryTests
{
    private const string FeatureOneName = "Feature1";
    private const string FeatureTwoName = "Feature2";
    private MetaDataContext? _mockContext;
    private PermissionFeatureRepository? _repository;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<MetaDataContext>()
            .UseInMemoryDatabase(databaseName: "TestDb")
            .Options;

        _mockContext = new MetaDataContext(options);

        var mockData = new List<PermissionFeature>
        {
            new PermissionFeature { Id = 1, Name = FeatureOneName },
            new PermissionFeature { Id = 2, Name = FeatureTwoName }
        };
        _mockContext.PermissionFeatures.AddRange(mockData);
        await _mockContext.SaveChangesAsync();

        _repository = new PermissionFeatureRepository(_mockContext);
    }

    [TearDown]
    public void TearDown()
    {
        _mockContext!.Database.EnsureDeleted();
        _mockContext.Dispose();
        _mockContext = null;
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllPermissionFeatures()
    {
        // Act
        var result = await _repository!.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.First().Name, Is.EqualTo(FeatureOneName));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnPermissionFeature_WhenFeatureExists()
    {
        // Act
        var result = await _repository!.GetByIdAsync(1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Id, Is.EqualTo(1));
        Assert.That(result.Name, Is.EqualTo(FeatureOneName));
    }

    [Test]
    public void GetByIdAsync_ShouldThrowKeyNotFoundException_WhenFeatureDoesNotExist()
    {
        // Act & Assert
        Assert.ThrowsAsync<KeyNotFoundException>(async () => await _repository!.GetByIdAsync(99, CancellationToken.None));
    }
}
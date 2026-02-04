using BrandVue.EntityFramework.MetaData.FeatureToggle;
using NSubstitute;
using Vue.Common.FeatureFlags;

namespace Test.Vue.Common.Features;

[TestFixture]
public class FeatureToggleServiceTests
{
    private IFeatureQueryService _featureQueryService;
    private IFeatureManagementService _featureManagementService;
    private FeatureToggleService _service;

    private readonly List<Feature> _userFeatures = new()
    {
        new Feature { Id = 1, Name = "UserFeature1", DocumentationUrl = "http://savanta.com", FeatureCode = FeatureCode.unknown, IsActive = true },
        new Feature { Id = 2, Name = "UserFeature2", DocumentationUrl = "http://savanta.com", FeatureCode = (FeatureCode)1, IsActive = true }
    };

    [SetUp]
    public void SetUp()
    {
        _featureQueryService = Substitute.For<IFeatureQueryService>();
        _featureManagementService = Substitute.For<IFeatureManagementService>();
        _service = new FeatureToggleService(_featureQueryService, _featureManagementService);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenFeatureQueryServiceIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureToggleService(
            null,
            _featureManagementService));
        Assert.That(ex.ParamName, Is.EqualTo("featureQueryService"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenFeatureManagementServiceIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureToggleService(
            _featureQueryService,
            null));
        Assert.That(ex.ParamName, Is.EqualTo("featureManagementService"));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldThrowArgumentNullException_WhenServiceThrows()
    {
        // Arrange
        _featureQueryService.When(x => x.GetEnabledFeaturesForCurrentUserAsync(Arg.Any<CancellationToken>()))
            .Do(x => throw new ArgumentNullException("UserId"));

        // Act & Assert
        var ex = await AssertThrowsAsync<ArgumentNullException>(() => _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None));
        Assert.That(ex.ParamName, Is.EqualTo("UserId"));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldReturnUserFeatures_WhenUserExists()
    {
        // Arrange
        _featureQueryService.GetEnabledFeaturesForCurrentUserAsync(Arg.Any<CancellationToken>())
            .Returns(_userFeatures);

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(_userFeatures));
        await _featureQueryService.Received(1).GetEnabledFeaturesForCurrentUserAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldReturnTrue_WhenFeatureIsEnabled()
    {
        // Arrange
        _featureQueryService.IsFeatureEnabledAsync(FeatureCode.unknown, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.IsFeatureEnabledAsync(FeatureCode.unknown, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        await _featureQueryService.Received(1).IsFeatureEnabledAsync(FeatureCode.unknown, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldReturnFalse_WhenFeatureIsNotEnabled()
    {
        // Arrange
        _featureQueryService.IsFeatureEnabledAsync(FeatureCode.unknown, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _service.IsFeatureEnabledAsync(FeatureCode.unknown, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
        await _featureQueryService.Received(1).IsFeatureEnabledAsync(FeatureCode.unknown, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveUserFeaturesAsync_ShouldCallManagementService()
    {
        // Arrange
        var expectedUserFeature = new UserFeature { UserId = "user1", FeatureId = 1 };
        _featureManagementService.SaveUserFeaturesAsync("user1", 1, Arg.Any<CancellationToken>())
            .Returns(expectedUserFeature);

        // Act
        var result = await _service.SaveUserFeaturesAsync("user1", 1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUserFeature));
        await _featureManagementService.Received(1).SaveUserFeaturesAsync("user1", 1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveOrganisationFeaturesAsync_ShouldCallManagementService()
    {
        // Arrange
        var expectedOrgFeature = new OrganisationFeature { OrganisationId = "org1", FeatureId = 1 };
        _featureManagementService.SaveOrganisationFeaturesAsync("org1", 1, Arg.Any<CancellationToken>())
            .Returns(expectedOrgFeature);

        // Act
        var result = await _service.SaveOrganisationFeaturesAsync("org1", 1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOrgFeature));
        await _featureManagementService.Received(1).SaveOrganisationFeaturesAsync("org1", 1, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteOrganisationFeaturesAsync_ShouldCallManagementService()
    {
        // Arrange
        _featureManagementService.DeleteOrganisationFeaturesAsync("org1", 1, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.DeleteOrganisationFeaturesAsync("org1", 1, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        await _featureManagementService.Received(1).DeleteOrganisationFeaturesAsync("org1", 1, Arg.Any<CancellationToken>());
    }

    private static async Task<T> AssertThrowsAsync<T>(Func<Task> testCode) where T : Exception
    {
        try
        {
            await testCode();
            Assert.Fail("Expected exception of type {0} but no exception was thrown.", typeof(T).Name);
            return null;
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            Assert.Fail("Expected exception of type {0} but exception of type {1} was thrown.", typeof(T).Name, ex.GetType().Name);
            return null;
        }
    }
}

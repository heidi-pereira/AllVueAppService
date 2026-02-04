using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vue.Common.Auth;
using Vue.Common.AuthApi;
using Vue.Common.FeatureFlags;
using AuthServer.GeneratedAuthApi;

namespace Test.Vue.Common.Features;

[TestFixture]
public class FeatureQueryServiceTests
{
    private IUserFeaturesRepository _userFeaturesRepository;
    private IOrganisationFeaturesRepository _organisationFeaturesRepository;
    private IAuthApiClient _authApiClient;
    private IUserContext _userContext;
    private ILogger<FeatureQueryService> _logger;
    private FeatureQueryService _service;

    private readonly List<Feature> _userFeatures = new()
    {
        new Feature { Id = 1, Name = "UserFeature1", DocumentationUrl = "http://savanta.com", FeatureCode = FeatureCode.table_builder, IsActive = true },
        new Feature { Id = 2, Name = "UserFeature2", DocumentationUrl = "http://savanta.com", FeatureCode = FeatureCode.user_management, IsActive = true }
    };

    private readonly List<Feature> _organisationFeatures = new()
    {
        new Feature { Id = 3, Name = "OrgFeature1", DocumentationUrl = "http://savanta.com", FeatureCode = FeatureCode.data_export, IsActive = true },
        new Feature { Id = 4, Name = "OrgFeature2", DocumentationUrl = "http://savanta.com", FeatureCode = FeatureCode.unknown, IsActive = true }
    };

    private const string TestUserId = "user123";
    private const string TestCompanyShortCode = "testcompany";
    private const string TestOrganisationId = "org123";

    [SetUp]
    public void SetUp()
    {
        _userFeaturesRepository = Substitute.For<IUserFeaturesRepository>();
        _organisationFeaturesRepository = Substitute.For<IOrganisationFeaturesRepository>();
        _authApiClient = Substitute.For<IAuthApiClient>();
        _userContext = Substitute.For<IUserContext>();
        _logger = Substitute.For<ILogger<FeatureQueryService>>();

        _userContext.UserId.Returns(TestUserId);
        _userContext.AuthCompany.Returns(TestCompanyShortCode);

        _service = new FeatureQueryService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            _userContext,
            _authApiClient,
            _logger);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserFeaturesRepositoryIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureQueryService(
            null!,
            _organisationFeaturesRepository,
            _userContext,
            _authApiClient,
            _logger));
        Assert.That(ex.ParamName, Is.EqualTo("userFeaturesRepository"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenOrganisationFeaturesRepositoryIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureQueryService(
            _userFeaturesRepository,
            null!,
            _userContext,
            _authApiClient,
            _logger));
        Assert.That(ex.ParamName, Is.EqualTo("organisationFeaturesRepository"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserContextIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureQueryService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            null!,
            _authApiClient,
            _logger));
        Assert.That(ex.ParamName, Is.EqualTo("userContext"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenAuthApiClientIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureQueryService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            _userContext,
            null!,
            _logger));
        Assert.That(ex.ParamName, Is.EqualTo("authApiClient"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureQueryService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            _userContext,
            _authApiClient,
            null!));
        Assert.That(ex.ParamName, Is.EqualTo("logger"));
    }

    [Test]
    public void Constructor_ShouldCreateInstance_WhenAllDependenciesAreProvided()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new FeatureQueryService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            _userContext,
            _authApiClient,
            _logger));
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldThrowArgumentNullException_WhenFeatureNameIsEmpty()
    {
        // Act & Assert
        var ex = await AssertThrowsAsync<ArgumentException>(() => _service.IsFeatureEnabledAsync(FeatureCode.unknown, CancellationToken.None));
        Assert.That(ex.ParamName, Is.EqualTo("featureCode"));
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldReturnTrue_WhenFeatureIsEnabledForUser()
    {
        // Arrange
        var allFeatures = _userFeatures.Concat(_organisationFeatures);
        SetupGetEnabledFeaturesForCurrentUserAsync(allFeatures);

        // Act
        var result = await _service.IsFeatureEnabledAsync(FeatureCode.table_builder, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldReturnTrue_WhenFeatureIsEnabledForOrganisation()
    {
        // Arrange
        var allFeatures = _userFeatures.Concat(_organisationFeatures);
        SetupGetEnabledFeaturesForCurrentUserAsync(allFeatures);

        // Act
        var result = await _service.IsFeatureEnabledAsync(FeatureCode.data_export, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldReturnFalse_WhenFeatureIsNotEnabled()
    {
        // Arrange
        var allFeatures = _userFeatures.Concat(_organisationFeatures);
        SetupGetEnabledFeaturesForCurrentUserAsync(allFeatures);

        // Act
        var result = await _service.IsFeatureEnabledAsync(FeatureCode.llm_insights, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsFeatureEnabledAsync_ShouldReturnFalse_WhenNoFeaturesAreEnabled()
    {
        // Arrange
        SetupGetEnabledFeaturesForCurrentUserAsync(Enumerable.Empty<Feature>());

        // Act
        var result = await _service.IsFeatureEnabledAsync(FeatureCode.open_ends, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldThrowArgumentNullException_WhenUserIdIsNull()
    {
        // Arrange
        _userContext.UserId.Returns((string?)null);

        // Act & Assert
        var ex = await AssertThrowsAsync<ArgumentNullException>(() => 
            _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None));
        Assert.That(ex.ParamName, Is.EqualTo("UserId"));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldThrowArgumentNullException_WhenUserIdIsEmpty()
    {
        // Arrange
        _userContext.UserId.Returns(string.Empty);

        // Act & Assert
        var ex = await AssertThrowsAsync<ArgumentNullException>(() => 
            _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None));
        Assert.That(ex.ParamName, Is.EqualTo("UserId"));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldReturnUserFeatures_WhenCompanyNotFound()
    {
        // Arrange
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(_userFeatures);
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<CompanyModel>(new Exception("Company not found")));

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(_userFeatures));
        await _userFeaturesRepository.Received(1).GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>());
        await _authApiClient.Received(1).GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>());
        await _organisationFeaturesRepository.DidNotReceive().GetEnabledFeaturesForOrganisationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldReturnCombinedFeatures_WhenCompanyFound()
    {
        // Arrange
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(_userFeatures);
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>())
            .Returns(_organisationFeatures);

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        var expectedFeatures = _userFeatures.Union(_organisationFeatures);
        Assert.That(result, Is.EqualTo(expectedFeatures));
        
        await _userFeaturesRepository.Received(1).GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>());
        await _authApiClient.Received(1).GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>());
        await _organisationFeaturesRepository.Received(1).GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldReturnUserFeaturesOnly_WhenNoOrganisationFeatures()
    {
        // Arrange
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(_userFeatures);
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Feature>());

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(_userFeatures));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldReturnOrganisationFeaturesOnly_WhenNoUserFeatures()
    {
        // Arrange
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Feature>());
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>())
            .Returns(_organisationFeatures);

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(_organisationFeatures));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldReturnEmptyCollection_WhenNoFeatures()
    {
        // Arrange
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Feature>());
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Empty<Feature>());

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldHandleDuplicateFeatures_WhenUnionIsUsed()
    {
        // Arrange
        var duplicateFeature = new Feature { Id = 1, Name = "DuplicateFeature", DocumentationUrl = "http://savanta.com", FeatureCode = (FeatureCode)0, IsActive = true };
        var userFeaturesWithDuplicate = new List<Feature> { duplicateFeature };
        var orgFeaturesWithDuplicate = new List<Feature> { duplicateFeature };
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(userFeaturesWithDuplicate);
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>())
            .Returns(orgFeaturesWithDuplicate);

        // Act
        var result = await _service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Count(), Is.EqualTo(1)); // Union should remove duplicates
        Assert.That(result.First().Name, Is.EqualTo("DuplicateFeature"));
    }

    [Test]
    public async Task GetEnabledFeaturesForCurrentUserAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, cancellationToken)
            .Returns(_userFeatures);
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, cancellationToken)
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, cancellationToken)
            .Returns(_organisationFeatures);

        // Act
        await _service.GetEnabledFeaturesForCurrentUserAsync(cancellationToken);

        // Assert
        await _userFeaturesRepository.Received(1).GetEnabledFeaturesForUserAsync(TestUserId, cancellationToken);
        await _authApiClient.Received(1).GetCompanyByShortcode(TestCompanyShortCode, cancellationToken);
        await _organisationFeaturesRepository.Received(1).GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, cancellationToken);
    }

    private void SetupGetEnabledFeaturesForCurrentUserAsync(IEnumerable<Feature> features)
    {
        var company = new CompanyModel { Id = TestOrganisationId, ShortCode = TestCompanyShortCode };
        
        _userFeaturesRepository.GetEnabledFeaturesForUserAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(_userFeatures);
        _authApiClient.GetCompanyByShortcode(TestCompanyShortCode, Arg.Any<CancellationToken>())
            .Returns(company);
        _organisationFeaturesRepository.GetEnabledFeaturesForOrganisationAsync(TestOrganisationId, Arg.Any<CancellationToken>())
            .Returns(_organisationFeatures);
    }

    private static async Task<T> AssertThrowsAsync<T>(Func<Task> testCode) where T : Exception
    {
        try
        {
            await testCode();
            Assert.Fail("Expected exception of type {0} but no exception was thrown.", typeof(T).Name);
            return null!;
        }
        catch (T ex)
        {
            return ex;
        }
        catch (Exception ex)
        {
            Assert.Fail("Expected exception of type {0} but exception of type {1} was thrown.", typeof(T).Name, ex.GetType().Name);
            return null!;
        }
    }
}

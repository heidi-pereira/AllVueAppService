using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using NSubstitute;
using Vue.Common.Auth;
using Vue.Common.FeatureFlags;

namespace Test.Vue.Common.Features;

[TestFixture]
public class FeatureManagementServiceTests
{
    private IUserFeaturesRepository _userFeaturesRepository;
    private IOrganisationFeaturesRepository _organisationFeaturesRepository;
    private IUserContext _userContext;
    private FeatureManagementService _service;

    private const string TestUserId = "user123";
    private const string TestOrganisationId = "org123";
    private const string TestCurrentUserId = "currentUser123";
    private const int TestFeatureId = 42;

    [SetUp]
    public void SetUp()
    {
        _userFeaturesRepository = Substitute.For<IUserFeaturesRepository>();
        _organisationFeaturesRepository = Substitute.For<IOrganisationFeaturesRepository>();
        _userContext = Substitute.For<IUserContext>();

        _userContext.UserId.Returns(TestCurrentUserId);

        _service = new FeatureManagementService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            _userContext);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserFeaturesRepositoryIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureManagementService(
            null!,
            _organisationFeaturesRepository,
            _userContext));
        Assert.That(ex.ParamName, Is.EqualTo("userFeaturesRepository"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenOrganisationFeaturesRepositoryIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureManagementService(
            _userFeaturesRepository,
            null!,
            _userContext));
        Assert.That(ex.ParamName, Is.EqualTo("organisationFeaturesRepository"));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserContextIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new FeatureManagementService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            null!));
        Assert.That(ex.ParamName, Is.EqualTo("userContext"));
    }

    [Test]
    public void Constructor_ShouldCreateInstance_WhenAllDependenciesAreProvided()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => new FeatureManagementService(
            _userFeaturesRepository,
            _organisationFeaturesRepository,
            _userContext));
    }

    [Test]
    public async Task SaveUserFeaturesAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var expectedUserFeature = new UserFeature 
        { 
            UserId = TestUserId, 
            FeatureId = TestFeatureId,
            UpdatedByUserId = TestCurrentUserId,
            UpdatedDate = DateTime.UtcNow
        };

        _userFeaturesRepository.SaveUserFeatureAsync(TestUserId, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(expectedUserFeature);

        // Act
        var result = await _service.SaveUserFeaturesAsync(TestUserId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUserFeature));
        await _userFeaturesRepository.Received(1).SaveUserFeatureAsync(
            TestUserId, 
            TestFeatureId, 
            TestCurrentUserId, 
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveUserFeaturesAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var expectedUserFeature = new UserFeature { UserId = TestUserId, FeatureId = TestFeatureId };

        _userFeaturesRepository.SaveUserFeatureAsync(TestUserId, TestFeatureId, TestCurrentUserId, cancellationToken)
            .Returns(expectedUserFeature);

        // Act
        await _service.SaveUserFeaturesAsync(TestUserId, TestFeatureId, cancellationToken);

        // Assert
        await _userFeaturesRepository.Received(1).SaveUserFeatureAsync(TestUserId, TestFeatureId, TestCurrentUserId, cancellationToken);
    }

    [Test]
    public async Task SaveUserFeaturesAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        var expectedUserFeature = new UserFeature 
        { 
            UserId = TestUserId, 
            FeatureId = TestFeatureId,
            UpdatedByUserId = TestCurrentUserId,
            UpdatedDate = DateTime.UtcNow
        };

        _userFeaturesRepository.SaveUserFeatureAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedUserFeature);

        // Act
        var result = await _service.SaveUserFeaturesAsync(TestUserId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUserFeature));
        Assert.That(result.UserId, Is.EqualTo(TestUserId));
        Assert.That(result.FeatureId, Is.EqualTo(TestFeatureId));
        Assert.That(result.UpdatedByUserId, Is.EqualTo(TestCurrentUserId));
    }

    [TestCase("")]
    [TestCase(null)]
    public async Task SaveUserFeaturesAsync_ShouldHandleNullOrEmptyUserId(string? userId)
    {
        // Arrange
        var expectedUserFeature = new UserFeature { UserId = userId!, FeatureId = TestFeatureId };
        _userFeaturesRepository.SaveUserFeatureAsync(userId!, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(expectedUserFeature);

        // Act
        var result = await _service.SaveUserFeaturesAsync(userId!, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUserFeature));
        await _userFeaturesRepository.Received(1).SaveUserFeatureAsync(userId!, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>());
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public async Task SaveUserFeaturesAsync_ShouldHandleVariousFeatureIds(int featureId)
    {
        // Arrange
        var expectedUserFeature = new UserFeature { UserId = TestUserId, FeatureId = featureId };
        _userFeaturesRepository.SaveUserFeatureAsync(TestUserId, featureId, TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(expectedUserFeature);

        // Act
        var result = await _service.SaveUserFeaturesAsync(TestUserId, featureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedUserFeature));
        await _userFeaturesRepository.Received(1).SaveUserFeatureAsync(TestUserId, featureId, TestCurrentUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveOrganisationFeaturesAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        var expectedOrgFeature = new OrganisationFeature 
        { 
            OrganisationId = TestOrganisationId, 
            FeatureId = TestFeatureId,
            UpdatedByUserId = TestCurrentUserId,
            UpdatedDate = DateTime.UtcNow
        };

        _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(expectedOrgFeature);

        // Act
        var result = await _service.SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOrgFeature));
        await _organisationFeaturesRepository.Received(1).SaveOrganisationFeaturesAsync(
            TestOrganisationId, 
            TestFeatureId, 
            TestCurrentUserId, 
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveOrganisationFeaturesAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        var expectedOrgFeature = new OrganisationFeature { OrganisationId = TestOrganisationId, FeatureId = TestFeatureId };

        _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, TestCurrentUserId, cancellationToken)
            .Returns(expectedOrgFeature);

        // Act
        await _service.SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, cancellationToken);

        // Assert
        await _organisationFeaturesRepository.Received(1).SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, TestCurrentUserId, cancellationToken);
    }

    [Test]
    public async Task SaveOrganisationFeaturesAsync_ShouldReturnRepositoryResult()
    {
        // Arrange
        var expectedOrgFeature = new OrganisationFeature 
        { 
            OrganisationId = TestOrganisationId, 
            FeatureId = TestFeatureId,
            UpdatedByUserId = TestCurrentUserId,
            UpdatedDate = DateTime.UtcNow
        };

        _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedOrgFeature);

        // Act
        var result = await _service.SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOrgFeature));
        Assert.That(result.OrganisationId, Is.EqualTo(TestOrganisationId));
        Assert.That(result.FeatureId, Is.EqualTo(TestFeatureId));
        Assert.That(result.UpdatedByUserId, Is.EqualTo(TestCurrentUserId));
    }

    [TestCase("")]
    [TestCase(null)]
    public async Task SaveOrganisationFeaturesAsync_ShouldHandleNullOrEmptyOrganisationId(string? organisationId)
    {
        // Arrange
        var expectedOrgFeature = new OrganisationFeature { OrganisationId = organisationId!, FeatureId = TestFeatureId };
        _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(organisationId!, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(expectedOrgFeature);

        // Act
        var result = await _service.SaveOrganisationFeaturesAsync(organisationId!, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOrgFeature));
        await _organisationFeaturesRepository.Received(1).SaveOrganisationFeaturesAsync(organisationId!, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>());
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public async Task SaveOrganisationFeaturesAsync_ShouldHandleVariousFeatureIds(int featureId)
    {
        // Arrange
        var expectedOrgFeature = new OrganisationFeature { OrganisationId = TestOrganisationId, FeatureId = featureId };
        _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(TestOrganisationId, featureId, TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(expectedOrgFeature);

        // Act
        var result = await _service.SaveOrganisationFeaturesAsync(TestOrganisationId, featureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.EqualTo(expectedOrgFeature));
        await _organisationFeaturesRepository.Received(1).SaveOrganisationFeaturesAsync(TestOrganisationId, featureId, TestCurrentUserId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteOrganisationFeaturesAsync_ShouldCallRepositoryWithCorrectParameters()
    {
        // Arrange
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        await _organisationFeaturesRepository.Received(1).DeleteOrganisationFeaturesAsync(
            TestOrganisationId, 
            TestFeatureId, 
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeleteOrganisationFeaturesAsync_ShouldUseCancellationToken()
    {
        // Arrange
        var cancellationToken = new CancellationToken();
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, cancellationToken)
            .Returns(true);

        // Act
        await _service.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, cancellationToken);

        // Assert
        await _organisationFeaturesRepository.Received(1).DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, cancellationToken);
    }

    [Test]
    public async Task DeleteOrganisationFeaturesAsync_ShouldReturnTrue_WhenDeletionSucceeds()
    {
        // Arrange
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeleteOrganisationFeaturesAsync_ShouldReturnFalse_WhenDeletionFails()
    {
        // Arrange
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _service.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
    }

    [TestCase("")]
    [TestCase(null)]
    public async Task DeleteOrganisationFeaturesAsync_ShouldHandleNullOrEmptyOrganisationId(string? organisationId)
    {
        // Arrange
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(organisationId!, TestFeatureId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _service.DeleteOrganisationFeaturesAsync(organisationId!, TestFeatureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.False);
        await _organisationFeaturesRepository.Received(1).DeleteOrganisationFeaturesAsync(organisationId!, TestFeatureId, Arg.Any<CancellationToken>());
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MaxValue)]
    public async Task DeleteOrganisationFeaturesAsync_ShouldHandleVariousFeatureIds(int featureId)
    {
        // Arrange
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(TestOrganisationId, featureId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _service.DeleteOrganisationFeaturesAsync(TestOrganisationId, featureId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.True);
        await _organisationFeaturesRepository.Received(1).DeleteOrganisationFeaturesAsync(TestOrganisationId, featureId, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Service_ShouldMaintainUserContextConsistency_AcrossMultipleCalls()
    {
        // Arrange
        var userFeature = new UserFeature { UserId = TestUserId, FeatureId = TestFeatureId };
        var orgFeature = new OrganisationFeature { OrganisationId = TestOrganisationId, FeatureId = TestFeatureId };

        _userFeaturesRepository.SaveUserFeatureAsync(Arg.Any<string>(), Arg.Any<int>(), TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(userFeature);
        _organisationFeaturesRepository.SaveOrganisationFeaturesAsync(Arg.Any<string>(), Arg.Any<int>(), TestCurrentUserId, Arg.Any<CancellationToken>())
            .Returns(orgFeature);
        _organisationFeaturesRepository.DeleteOrganisationFeaturesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _service.SaveUserFeaturesAsync(TestUserId, TestFeatureId, CancellationToken.None);
        await _service.SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);
        await _service.DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, CancellationToken.None);

        // Assert
        await _userFeaturesRepository.Received(1).SaveUserFeatureAsync(TestUserId, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>());
        await _organisationFeaturesRepository.Received(1).SaveOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, TestCurrentUserId, Arg.Any<CancellationToken>());
        await _organisationFeaturesRepository.Received(1).DeleteOrganisationFeaturesAsync(TestOrganisationId, TestFeatureId, Arg.Any<CancellationToken>());

        // Verify that the same current user ID was used for all operations that require it
        var _ = _userContext.Received(2).UserId; // Called twice - once for save user, once for save org
    }

    [Test]
    public async Task Service_ShouldHandleRepositoryExceptions_Gracefully()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Repository error");
        _userFeaturesRepository.When(x => x.SaveUserFeatureAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>()))
            .Do(x => throw expectedException);

        // Act & Assert
        var actualException = await AssertThrowsAsync<InvalidOperationException>(() =>
            _service.SaveUserFeaturesAsync(TestUserId, TestFeatureId, CancellationToken.None));

        Assert.That(actualException.Message, Is.EqualTo("Repository error"));
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

using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon.DataPopulation;
using Vue.Common.Auth;

namespace Test.BrandVue.SourceData.Measures;

[TestFixture]
public class InMemoryRepositoryUpdatingMetricConfigurationRepositoryTests
{
    private InMemoryRepositoryUpdatingMetricConfigurationRepository _sut;
    private IMetricConfigurationRepository _mockPersistentRepository;
    private ILoadableMetricRepository _loadableMetricRepository;
    private ILoadableQuestionTypeLookupRepository _questionTypeLookupRepository;
    private IProductContext _productContext;

    [SetUp]
    public void SetUp()
    {
        _mockPersistentRepository = Substitute.For<IMetricConfigurationRepository>();
        var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
        _loadableMetricRepository = new MetricRepository(userPermissionsService);
        _productContext = new ProductContext("drinks");
        _questionTypeLookupRepository = new QuestionTypeLookupRepository(_loadableMetricRepository, new FallbackSubsetRepository());

        _sut = new InMemoryRepositoryUpdatingMetricConfigurationRepository(
            _mockPersistentRepository,
            _loadableMetricRepository,
            Substitute.For<IMetricFactory>(),
            _questionTypeLookupRepository,
            _productContext,
            Substitute.For<IVariableConfigurationRepository>(),
            Substitute.For<IVariableEntityLoader>(),
            Substitute.For<IFieldExpressionParser>(),
            Substitute.For<ILoggerFactory>()
        );
    }

    [Test]
    public void Update_ExistingMeasure_UpdatesAllRepositories()
    {
        // Arrange
        var updatedConfig = CreateMetricConfiguration(1, "TestMetric", "NewMapping");
        var previousConfig = CreateMetricConfiguration(1, "TestMetric", "OldMapping");
        var measure = new Measure {Name = "TestMetric" };

        SetupExistingMeasure(previousConfig, measure);

        // Act
        _sut.Update(updatedConfig);

        // Assert
        VerifyPersistentRepositoryUpdated(updatedConfig);
        Assert.That(measure.Name, Is.EqualTo(updatedConfig.Name));
    }

    [Test]
    public void Update_NonExistentMeasure_OnlyUpdatesPersistentRepository()
    {
        // Arrange
        var updatedConfig = CreateMetricConfiguration(1, "TestMetric", "NewMapping");
        var previousConfig = CreateMetricConfiguration(1, "TestMetric", "OldMapping");

        SetupNonExistentMeasure(previousConfig);

        // Act
        _sut.Update(updatedConfig);

        // Assert
        VerifyPersistentRepositoryUpdated(updatedConfig);
        Assert.That(_loadableMetricRepository.GetAllForCurrentUser(), Does.Not.Contain(updatedConfig.Name));
    }

    [Test]
    public void Delete_ExistingMeasure_RemovesFromAllRepositories()
    {
        // Arrange
        var config = CreateMetricConfiguration(1, "TestMetric", "Mapping");
        var measure = new Measure() { Name = "TestMetric" };

        SetupExistingMeasure(config, measure);

        // Act
        _sut.Delete(config);

        // Assert
        VerifyPersistentRepositoryDeleted(config);
        Assert.That(_loadableMetricRepository.GetAllForCurrentUser(), Does.Not.Contain(config.Name));
    }

    [Test]
    public void Delete_NonExistentMeasure_OnlyDeletesFromPersistentRepository()
    {
        // Arrange
        var config = CreateMetricConfiguration(1, "TestMetric", "Mapping");

        SetupNonExistentMeasure(config);

        // Act
        _sut.Delete(config);

        // Assert
        VerifyPersistentRepositoryDeleted(config);
        Assert.That(_loadableMetricRepository.GetAllForCurrentUser(), Does.Not.Contain(config.Name));
    }

    private MetricConfiguration CreateMetricConfiguration(int id, string name, string filterValueMapping)
        => new() { Id = id, Name = name, FilterValueMapping = filterValueMapping };

    private void SetupExistingMeasure(MetricConfiguration config, Measure measure)
    {
        _mockPersistentRepository.Get(config.Id).Returns(config);
        _loadableMetricRepository.TryAdd(measure.Name, measure);
    }

    private void SetupNonExistentMeasure(MetricConfiguration config)
        => _mockPersistentRepository.Get(config.Id).Returns(config);

    private void VerifyPersistentRepositoryDeleted(MetricConfiguration config)
        => _mockPersistentRepository.Received(1).Delete(config);

    private void VerifyPersistentRepositoryUpdated(MetricConfiguration config)
        => _mockPersistentRepository.Received(1).Update(config);


}

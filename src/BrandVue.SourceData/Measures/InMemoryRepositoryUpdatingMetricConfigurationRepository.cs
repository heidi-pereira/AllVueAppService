using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Measures;

internal class InMemoryRepositoryUpdatingMetricConfigurationRepository : IMetricConfigurationRepository
{
    private readonly IMetricConfigurationRepository _persistentRepository;
    private readonly ILoadableMetricRepository _loadableMetricRepository;
    private readonly IMetricFactory _metricFactory;
    private readonly ILoadableQuestionTypeLookupRepository _questionTypeLookupRepository;
    private readonly IProductContext _productContext;
    private readonly IVariableConfigurationRepository _variableConfigurationRepository;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<InMemoryRepositoryUpdatingMetricConfigurationRepository> _logger;
    private readonly IVariableEntityLoader _variableEntityLoader;
    private readonly IFieldExpressionParser _fieldExpressionParser;

    public InMemoryRepositoryUpdatingMetricConfigurationRepository(IMetricConfigurationRepository persistentRepository,
        ILoadableMetricRepository loadableMetricRepository,
        IMetricFactory metricFactory,
        ILoadableQuestionTypeLookupRepository questionTypeLookupRepository,
        IProductContext productContext,
        IVariableConfigurationRepository variableConfigurationRepository,
        IVariableEntityLoader variableEntityLoader,
        IFieldExpressionParser fieldExpressionParser,
        ILoggerFactory loggerFactory)
    {
        _persistentRepository = persistentRepository;
        _loadableMetricRepository = loadableMetricRepository;
        _metricFactory = metricFactory;
        _questionTypeLookupRepository = questionTypeLookupRepository;
        _productContext = productContext;
        _variableConfigurationRepository = variableConfigurationRepository;
        _variableEntityLoader = variableEntityLoader;
        _fieldExpressionParser = fieldExpressionParser;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<InMemoryRepositoryUpdatingMetricConfigurationRepository>();
    }

    public IReadOnlyCollection<MetricConfiguration> GetAll() => _persistentRepository.GetAll();
    public MetricConfiguration Get(int id) => _persistentRepository.Get(id);

    public MetricConfiguration Get(string name) => _persistentRepository.Get(name);

    public void Create(MetricConfiguration metricConfiguration, bool shouldValidate = true)
    {
        _persistentRepository.Create(metricConfiguration, shouldValidate);
        var measure = _metricFactory.CreateMetric(metricConfiguration);
        AddToInMemoryRepository(measure);
        _questionTypeLookupRepository.AddOrUpdate(measure);
        AddFilterValueMappingVariable(measure);
    }

    public void Update(MetricConfiguration metricConfiguration, bool shouldValidate = false)
    {
        var previousConfig = _persistentRepository.Get(metricConfiguration.Id);
        var filterValueMappingHasChanged = previousConfig.FilterValueMapping != metricConfiguration.FilterValueMapping;
        _persistentRepository.Update(metricConfiguration);
        if (_loadableMetricRepository.TryGet(previousConfig.Name, out var measureToUpdate))
        {
            if (filterValueMappingHasChanged)
            {
                RemoveFilterValueMappingVariable(measureToUpdate);
            }
            _loadableMetricRepository.RenameMeasure(measureToUpdate, metricConfiguration.Name);
            _metricFactory.LoadMetric(metricConfiguration, measureToUpdate);
            _questionTypeLookupRepository.AddOrUpdate(measureToUpdate);
            if (filterValueMappingHasChanged)
            {
                AddFilterValueMappingVariable(measureToUpdate);
            }
        }
    }

    public void Delete(MetricConfiguration metricConfiguration)
    {
        _persistentRepository.Delete(metricConfiguration);
        if (_loadableMetricRepository.TryGet(metricConfiguration.Name, out var measureToRemove))
        {
            _questionTypeLookupRepository.Remove(measureToRemove);
            _loadableMetricRepository.Remove(metricConfiguration.Name);
        }
    }

    public void Delete(int metricConfigurationId)
    {
        var metricConfig = _persistentRepository.Get(metricConfigurationId);
        Delete(metricConfig);
    }

    private bool AddToInMemoryRepository(Measure objectId) => _loadableMetricRepository.TryAdd(objectId.Name, objectId);

    private void RemoveFilterValueMappingVariable(Measure measure)
    {
        if (measure.FilterValueMappingVariableConfiguration != null)
        {
            _fieldExpressionParser.Delete(measure.FilterValueMappingVariableConfiguration);
            _variableEntityLoader.DeleteEntityForVariable(measure.FilterValueMappingVariableConfiguration);
            measure.FilterValueMappingVariable = null;
            measure.FilterValueMappingVariableConfiguration = null;
        }
    }

    private void AddFilterValueMappingVariable(Measure updatedMeasure)
    {
        var filterValueMappingParser = new FilterValueMappingVariableParser(_productContext, _variableConfigurationRepository, _loggerFactory.CreateLogger<FilterValueMappingVariableParser>());
        var variableConfig = filterValueMappingParser.CreateVariableConfigurationOrNull(updatedMeasure);
        if (variableConfig != null)
        {
            try
            {
                _variableEntityLoader.CreateOrUpdateEntityForVariable(variableConfig);
                var variable = _fieldExpressionParser.DeclareOrUpdateVariable(variableConfig);
                updatedMeasure.FilterValueMappingVariable = variable;
                updatedMeasure.FilterValueMappingVariableConfiguration = variableConfig;
            }
            catch (Exception x)
            {
                _logger.LogWarning(x, $"Failed to create FilterValueMapping variable for measure {updatedMeasure?.Name} ({updatedMeasure?.FilterValueMapping})");
            }
        }
    }
}
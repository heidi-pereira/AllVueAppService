using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Import;

namespace BrandVue.SourceData.Variable;

/// <remarks>
/// There are currently race conditions where an inconsistent set of metadata could be retrieved between updating various repositories, especially during an update operation
/// </remarks>
internal class InMemoryRepositoryUpdatingVariableConfigurationRepository : IVariableConfigurationRepository
{
    private readonly IVariableConfigurationRepository _persistentRepository;
    private readonly IVariableEntityLoader _variableEntityLoader;
    private readonly IFieldExpressionParser _fieldExpressionParser;

    public InMemoryRepositoryUpdatingVariableConfigurationRepository(
        IVariableConfigurationRepository persistentRepository, IVariableEntityLoader variableEntityLoader,
        IFieldExpressionParser fieldExpressionParser)
    {
        _persistentRepository = persistentRepository;
        _variableEntityLoader = variableEntityLoader;
        _fieldExpressionParser = fieldExpressionParser;
    }

    public IReadOnlyCollection<VariableConfiguration> GetAll() => _persistentRepository.GetAll();
    public IReadOnlyCollection<VariableConfiguration> GetBaseVariables() => _persistentRepository.GetBaseVariables();

    public VariableConfiguration Get(int variableConfigurationId) => _persistentRepository.Get(variableConfigurationId);

    public VariableConfiguration GetByIdentifier(string variableIdentifier) => _persistentRepository.GetByIdentifier(variableIdentifier);

    public VariableConfiguration Create(VariableConfiguration variableConfiguration,
        IReadOnlyCollection<string> overrideVariableDependencyIdentifiers)
    {
        _variableEntityLoader.CreateOrUpdateEntityForVariable(variableConfiguration);
        _fieldExpressionParser.DeclareOrUpdateVariable(variableConfiguration);
        var configuration = _persistentRepository.Create(variableConfiguration, overrideVariableDependencyIdentifiers);
        return configuration;
    }

    public void Delete(VariableConfiguration variableConfiguration)
    {
        _persistentRepository.Delete(variableConfiguration);
        _fieldExpressionParser.Delete(variableConfiguration);
        _variableEntityLoader.DeleteEntityForVariable(variableConfiguration);
    }

    public void Update(VariableConfiguration variableConfiguration)
    {
        var previousConfig = _persistentRepository.Get(variableConfiguration.Id);
        _persistentRepository.Update(variableConfiguration);
        var previousVariable = _fieldExpressionParser.GetDeclaredVariableOrNull(previousConfig.Identifier);
        if (previousConfig.Identifier != variableConfiguration.Identifier)
        {
            _fieldExpressionParser.Delete(previousConfig);
        }
        _variableEntityLoader.CreateOrUpdateEntityForVariable(variableConfiguration);
        _fieldExpressionParser.DeclareOrUpdateVariable(variableConfiguration);
        foreach (var dependentVariable in GetTransitivelyDependingOn(previousConfig))
        {
            _fieldExpressionParser.DeclareOrUpdateVariable(dependentVariable);
        }
    }

    public void UpdateMany(IEnumerable<VariableConfiguration> variableConfigurations)
    {
        var previousConfigs = variableConfigurations.Select(vc => _persistentRepository.Get(vc.Id)).ToDictionary(vc => vc.Id);
        _persistentRepository.UpdateMany(variableConfigurations);
        foreach (var variableConfiguration in variableConfigurations)
        {
            Update(variableConfiguration);
        }
    }

    private static IEnumerable<VariableConfiguration> GetTransitivelyDependingOn(VariableConfiguration previousConfig) =>
        previousConfig.FollowMany(v => v.VariablesDependingOnThis.Select(x => x.Variable)).Skip(1);
}
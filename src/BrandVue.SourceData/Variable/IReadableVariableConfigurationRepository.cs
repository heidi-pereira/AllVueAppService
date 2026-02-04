using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Variable;

public interface IReadableVariableConfigurationRepository
{
    IReadOnlyCollection<VariableConfiguration> GetAll();
    IReadOnlyCollection<VariableConfiguration> GetBaseVariables();
    VariableConfiguration Get(int variableConfigurationId);
    VariableConfiguration GetByIdentifier(string variableIdentifier);
}
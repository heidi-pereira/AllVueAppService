using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Variable
{
    public interface IVariableConfigurationRepository : IReadableVariableConfigurationRepository
    {
        VariableConfiguration Create(VariableConfiguration variableConfiguration,
            IReadOnlyCollection<string> overrideVariableDependencyIdentifiers);
        void Delete(VariableConfiguration variableConfiguration);
        void Update(VariableConfiguration variableConfiguration);
        void UpdateMany(IEnumerable<VariableConfiguration> variableConfigurations);
    }
}

using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Variable
{
    public interface IVariableValidator
    {
        void Validate(VariableConfiguration variableConfiguration, out IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers, out IReadOnlyCollection<string> entityTypeNames, bool shouldVerify = true);
    }
}
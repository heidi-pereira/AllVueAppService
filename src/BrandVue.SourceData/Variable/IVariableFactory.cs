using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Variable
{
    public interface IVariableFactory
    {
        /// <summary>
        /// Creates a domain model from configuration model.
        /// </summary>
        IVariable<int?> GetDeclaredVariable(VariableConfiguration variableConfig);

        IReadOnlyCollection<string> ParseResultEntityTypeNames(VariableConfiguration variableConfig);

        VariableDefinition SanitizeVariableEntityTypeName(VariableConfiguration originalVariableConfiguration, VariableDefinition updatedVariableDefinition);
    }
}
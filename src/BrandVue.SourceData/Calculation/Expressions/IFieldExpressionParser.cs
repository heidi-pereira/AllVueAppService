using BrandVue.EntityFramework.MetaData.VariableDefinitions;

namespace BrandVue.SourceData.Calculation.Expressions
{
    public interface IFieldExpressionParser
    {
        Variable<bool> ParseUserBooleanExpression(string fieldDefinitionFilterExpression);
        Variable<int?> ParseUserNumericExpressionOrNull(string numericExpression);
        IReadOnlyCollection<string> ParseResultEntityTypeNames(string expression);
        IVariable<int?> DeclareOrUpdateVariable(VariableConfiguration variableConfiguration);
        IVariable<int?> CreateTemporaryVariable(VariableConfiguration variableConfiguration);
        IVariable<int?> GetDeclaredVariableOrNull(string variableIdentifier);
        public IVariable<int?> GetDeclaredVariableOrNull(ResponseFieldDescriptor responseFieldDescriptor);
        void Delete(VariableConfiguration variableConfiguration);
        IReadOnlyCollection<VariableInstanceModel> GetDeclaredVariableModels();
        IVariable<bool> GetDeclaredBooleanVariableOrNull(string variableIdentifier);
    }
}

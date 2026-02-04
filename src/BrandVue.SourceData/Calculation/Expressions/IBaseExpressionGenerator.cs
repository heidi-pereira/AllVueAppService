using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation.Expressions
{
    public interface IBaseExpressionGenerator
    {
        string GetAnsweredQuestionPythonExpression(VariableConfiguration variableConfig);
        Measure GetMeasureWithOverriddenBaseExpression(Measure measure, BaseExpressionDefinition baseExpressionOverride);
        string GetBaseVariablePythonExpression(int baseVariableId, bool includeResultTypes, IEnumerable<string> primaryEntityTypeNames);
        string BaseExpressionForAnyoneAskedThisQuestion(GroupedVariableDefinition groupedDefinition, bool includeResultTypes);
        string BaseExpressionForAnyoneAskedThisQuestion(SingleGroupVariableDefinition singleGroupDefinition, bool includeResultTypes);
    }
}

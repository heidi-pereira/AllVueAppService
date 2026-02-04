using System.Collections;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public static class VariableDefinitionPythonExtensions
    {
        public static string GetPythonExpression(this VariableDefinition variableDefinition)
        {
            if (!ContainsPythonExpression(variableDefinition))
                throw new Exception("Variable definition does not contain a Python expression!");

            switch (variableDefinition)
            {
                case FieldExpressionVariableDefinition fieldExpressionVariableDefinition:
                    return GetPythonExpression(fieldExpressionVariableDefinition);
                case BaseGroupedVariableDefinition { Groups: [var g], AggregationType: var aggregationType }:
                    return GetSingleGroupPythonExpression(g, aggregationType);
                case GroupedVariableDefinition groupedVariableDefinition:
                    return GetPythonExpression(groupedVariableDefinition);
                case SingleGroupVariableDefinition singleGroupVariableDefinition:
                    return GetPythonExpression(singleGroupVariableDefinition);
                default:
                    throw new Exception("Could not extract a Python expression from the variable definition!");
            }
        }

        public static bool ContainsPythonExpression(this VariableDefinition variableDefinition)
        {
            if (variableDefinition is FieldExpressionVariableDefinition)
            {
                return true;
            }

            if (variableDefinition is SingleGroupVariableDefinition singleComponentVariableDefinition)
            {
                return singleComponentVariableDefinition.Group.Component.ContainsPythonCondition();
            }

            if (variableDefinition is not GroupedVariableDefinition groupDefinition)
            {
                return false;
            }

            return groupDefinition.Groups is { Count: > 0 } && groupDefinition.Groups.All(group => group.Component.ContainsPythonCondition());
        }

        private static string GetPythonExpression(FieldExpressionVariableDefinition variableDefinition)
        {
            //to enable functionality such as netting, these need to behave the same as other variables and return None instead of 0 for not answered
            if (string.IsNullOrWhiteSpace(variableDefinition.Expression) || variableDefinition.Expression.Contains("or None", StringComparison.OrdinalIgnoreCase))
            {
                return variableDefinition.Expression;
            }
            return $"({variableDefinition.Expression}) or None";
        }

        private static string GetPythonExpression(GroupedVariableDefinition variableDefinition)
        {
            var groups = variableDefinition.Groups;
            if (groups == null)
            {
                return "";
            }

            var pythonExpressions = groups.Select(variableGrouping => GetPythonExpressionFromVariableGrouping(variableDefinition.ToEntityTypeName, variableGrouping));
            string testExpression = string.Join(" or ", pythonExpressions);
            return $"result.{variableDefinition.ToEntityTypeName} if {testExpression} else None";
        }

        private static string GetPythonExpression(SingleGroupVariableDefinition singleGroupVariableDefinition)
        {
            return GetSingleGroupPythonExpression(singleGroupVariableDefinition.Group, singleGroupVariableDefinition.AggregationType);
        }

        private static string GetSingleGroupPythonExpression(VariableGrouping variableGrouping, AggregationType aggregationType)
        {
            var maxExpression = variableGrouping.Component.GetPythonMaxCondition(aggregationType);
            var conditionExpression = variableGrouping.Component.GetPythonCondition();
            return $"{maxExpression} if {conditionExpression} else None";
        }

        private static string GetPythonExpressionFromVariableGrouping(string parentTypeName, VariableGrouping variableGrouping)
        {
            var condition = variableGrouping.Component.GetPythonCondition();
            var toEntityInstanceId = variableGrouping.ToEntityInstanceId;
            return $"((result.{parentTypeName} == {toEntityInstanceId}) and ({condition}))";
        }
    }
}
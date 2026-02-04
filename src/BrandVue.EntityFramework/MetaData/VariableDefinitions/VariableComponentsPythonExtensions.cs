using BrandVue.EntityFramework.Exceptions;
using System.Linq;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    public static class VariableComponentsPythonExtensions
    {
        public static bool ContainsPythonCondition(this VariableComponent variableComponent)
        {
            return variableComponent is not DateRangeVariableComponent &&
                variableComponent is not SurveyIdVariableComponent;
        }

        public static string GetPythonCondition(this VariableComponent variableComponent)
        {
            switch (variableComponent)
            {
                case CompositeVariableComponent compositeVariableComponent:
                    return compositeVariableComponent.GetPythonCondition();
                case InclusiveRangeVariableComponent inclusiveRangeVariableComponent:
                    return inclusiveRangeVariableComponent.GetPythonCondition();
                case InstanceListVariableComponent instanceListVariableComponent:
                    return instanceListVariableComponent.GetPythonCondition();
                default:
                    throw new ArgumentOutOfRangeException(nameof(variableComponent), variableComponent?.GetType().Name, null);
            }
        }

        public static string GetPythonCondition(this VariableComponent variableComponent, bool includeResultTypes, IEnumerable<string> primaryEntityTypeNames)
        {
            switch (variableComponent)
            {
                case CompositeVariableComponent compositeVariableComponent:
                    return compositeVariableComponent.GetPythonCondition(includeResultTypes, primaryEntityTypeNames);
                case InclusiveRangeVariableComponent inclusiveRangeVariableComponent:
                    return inclusiveRangeVariableComponent.GetPythonCondition(includeResultTypes, primaryEntityTypeNames);
                case InstanceListVariableComponent instanceListVariableComponent:
                    return instanceListVariableComponent.GetPythonCondition(includeResultTypes, primaryEntityTypeNames);
                default:
                    throw new ArgumentOutOfRangeException(nameof(variableComponent), variableComponent?.GetType().Name, null);
            }
        }

        public static string CompositeVariableSeparatorToStringConverter(CompositeVariableSeparator separator)
        {
            switch (separator)
            {
                case CompositeVariableSeparator.And:
                    return "and";

                case CompositeVariableSeparator.Or:
                    return "or";
            }

            return string.Empty;
        }

        private static string GetPythonCondition(this InclusiveRangeVariableComponent vc)
        {
            var identifier = $"response.{GetFieldRetriever(vc)}({vc.GetResultTypes()})";
            return GetInclusiveRangePythonExpression(vc, identifier);
        }

        private static string GetPythonCondition(this InclusiveRangeVariableComponent vc, bool includeResultTypes, IEnumerable<string> primaryEntityTypeNames)
        {
            var intersectingTypes = vc.ResultEntityTypeNames.Intersect(primaryEntityTypeNames)
                .Select(t => $"{t}=result.{t}");
            var resultTypes = includeResultTypes && intersectingTypes.Any() ? string.Join(", ", intersectingTypes) : "";
            var identifier = $"response.{GetFieldRetriever(vc)}({resultTypes})";
            return GetInclusiveRangePythonExpression(vc, identifier);
        }

        private static string GetInclusiveRangePythonExpression(InclusiveRangeVariableComponent vc, string list)
        {
            return $"any({GetPythonConditionExpression(vc, "r")} for r in {list})";
        }

        private static string GetPythonConditionExpression(InclusiveRangeVariableComponent vc, string r)
        {
            var notCondition = vc.Inverted ? "not " : "";
            var equalityOperator = vc.Inverted ? "!=" : "==";
            var greaterThanOperator = vc.Inverted ? "<" : ">=";
            var lessThanOperator = vc.Inverted ? ">" : "<=";
            switch (vc.Operator)
            {
                case VariableRangeComparisonOperator.Between:
                    return vc.Min <= vc.Max ?
                        $"{notCondition}{vc.Min} <= {r} <= {vc.Max}" :
                        $"{notCondition}{vc.Max} <= {r} <= {vc.Min}";

                case VariableRangeComparisonOperator.Exactly:
                    if (vc.ExactValues?.Any() == true)
                    {
                        if (vc.ExactValues.Length == 1)
                        {
                            return $"{r} {equalityOperator} {vc.ExactValues[0]}";
                        }
                        return $"{notCondition}{r} in [{string.Join(',', vc.ExactValues)}]";
                    }
                    return $"{r} {equalityOperator} {vc.Min}";

                case VariableRangeComparisonOperator.GreaterThan:
                    return $"{r} {greaterThanOperator} {vc.Min}";

                case VariableRangeComparisonOperator.LessThan:
                    return $"{r} {lessThanOperator} {vc.Min}";

                default:
                    throw new ArgumentOutOfRangeException(nameof(vc.Operator), vc.Operator, null);
            }
        }

        public static string GetPythonMaxCondition(this VariableComponent variableComponent, AggregationType aggregationType)
        {
            return (aggregationType, variableComponent) switch
            {
                (AggregationType.MaxOfSingleReferenced, ISingleVariableComponent vc) =>
                    $"max(response.{vc.FromVariableIdentifier}({vc.GetResultTypes()}))",
                (AggregationType.MaxOfMatchingCondition, CompositeVariableComponent vc) =>
                    "max(a for a in [" + string.Join(", ", vc.CompositeVariableComponents.Select(v => v.GetPythonMaxCondition(aggregationType))) + "] if a != None)",
                (AggregationType.MaxOfMatchingCondition, InstanceListVariableComponent vc) =>
                    vc.GetPythonMaxMatchingCondition(),
                (AggregationType.MaxOfMatchingCondition, InclusiveRangeVariableComponent vc) =>
                    vc.GetPythonMaxMatchingCondition(),
                _ => throw new NotImplementedException($"Max is not supported for {aggregationType} on {variableComponent?.GetType().Name}")
            };
        }

        private static string GetPythonMaxMatchingCondition(this InstanceListVariableComponent vc)
        {
            var condition = GetMaxAnsweredWithInstanceIdsCondition(vc, vc.InstanceIds);
            string maxCondition = vc.Operator switch
            {
                InstanceVariableComponentOperator.Or => condition,
                InstanceVariableComponentOperator.And => condition,
                // I think more consistent would be to get the max answer except for the instances passed, but that's complex to calculate here and there's no actual use case
                InstanceVariableComponentOperator.Not => throw new NotImplementedException("Cannot take max when using not condition"),
                _ => throw new ArgumentOutOfRangeException(nameof(vc.Operator), vc.Operator, null),
            };
            return maxCondition;
        }

        private static string GetPythonMaxMatchingCondition(this InclusiveRangeVariableComponent vc)
        {
            var identifier = $"response.{GetFieldRetriever(vc)}({vc.GetResultTypes()})";
            return $"max((r for r in {identifier} if {GetPythonConditionExpression(vc, "r")}), default=None)";
        }

        private static string GetPythonCondition(this InstanceListVariableComponent vc) => vc.Operator switch
            {
            InstanceVariableComponentOperator.Or => GetAnsweredWithInstanceIdsCondition(vc, vc.InstanceIds),
                InstanceVariableComponentOperator.And =>
                string.Join(" and ", vc.InstanceIds.Select(instanceId => GetAnsweredWithInstanceIdsCondition(vc, instanceId.Yield()))),
                InstanceVariableComponentOperator.Not =>
                $"len(response.{vc.FromVariableIdentifier}()) and not {GetAnsweredWithInstanceIdsCondition(vc, vc.InstanceIds)}",
                _ => throw new ArgumentException(),
            };

        private static string GetPythonCondition(this InstanceListVariableComponent vc, bool includeResultTypes, IEnumerable<string> primaryEntityTypeNames) => vc.Operator switch
        {
            InstanceVariableComponentOperator.Or => GetAnsweredWithInstanceIdsCondition(vc, vc.InstanceIds, includeResultTypes, primaryEntityTypeNames),
                InstanceVariableComponentOperator.And =>
                string.Join(" and ", vc.InstanceIds.Select(instanceId => GetAnsweredWithInstanceIdsCondition(vc, instanceId.Yield(), includeResultTypes, primaryEntityTypeNames))),
                InstanceVariableComponentOperator.Not =>
                $"len(response.{vc.FromVariableIdentifier}()) and not {GetAnsweredWithInstanceIdsCondition(vc, vc.InstanceIds, includeResultTypes, primaryEntityTypeNames)}",
                _ => throw new ArgumentException(),
            };

        private static string GetAnsweredWithInstanceIdsCondition(InstanceListVariableComponent vc, IEnumerable<int> instanceIds)
        {
            var resultTypes = vc.ResultEntityTypeNames.Any() ? $", {vc.GetResultTypes()}" : "";
            return GetAnsweredWithInstanceIdsCondition(vc, instanceIds, resultTypes);
        }

        private static string GetAnsweredWithInstanceIdsCondition(InstanceListVariableComponent vc, IEnumerable<int> instanceIds, bool includeResultTypes, IEnumerable<string> primaryEntityTypeNames)
        {
            var intersectingTypes = vc.ResultEntityTypeNames.Intersect(primaryEntityTypeNames)
                .Select(t => $"{t}=result.{t}");
            var resultTypes = includeResultTypes && intersectingTypes.Any() ? $", {string.Join(", ", intersectingTypes)}" : "";
            return GetAnsweredWithInstanceIdsCondition(vc, instanceIds, resultTypes);
        }

        private static string GetAnsweredWithInstanceIdsCondition(InstanceListVariableComponent vc, IEnumerable<int> instanceIds, string resultTypes)
        {
            string answerCondition = vc.AnswerMinimum.HasValue ? $" and answer >= {vc.AnswerMinimum.Value}" : "";
            answerCondition += vc.AnswerMaximum.HasValue ? $" and answer <= {vc.AnswerMaximum.Value}" : "";
            return $"any((answer != None{answerCondition}) for answer in response.{vc.FromVariableIdentifier}({vc.FromEntityTypeName}=[{string.Join(",", instanceIds)}]{resultTypes}))";
        }

        private static string GetMaxAnsweredWithInstanceIdsCondition(InstanceListVariableComponent vc, IEnumerable<int> instanceIds)
        {
            var resultTypes = vc.ResultEntityTypeNames.Any() ? $", {vc.GetResultTypes()}" : "";
            return GetMaxAnsweredWithInstanceIdsCondition(vc, instanceIds, resultTypes);
        }

        private static string GetMaxAnsweredWithInstanceIdsCondition(InstanceListVariableComponent vc, IEnumerable<int> instanceIds, string resultTypes)
        {
            string answerCondition = vc.AnswerMinimum.HasValue ? $" and answer >= {vc.AnswerMinimum.Value}" : "";
            answerCondition += vc.AnswerMaximum.HasValue ? $" and answer <= {vc.AnswerMaximum.Value}" : "";
            return $"max((answer for answer in response.{vc.FromVariableIdentifier}({vc.FromEntityTypeName}=[{string.Join(",", instanceIds)}]{resultTypes}) if answer != None{answerCondition}), default=None)";
        }

        private static string GetPythonCondition(this CompositeVariableComponent vc)
        {
            var conditions = vc.CompositeVariableComponents.Select(c => c.GetPythonCondition());
            return $"({string.Join($" {CompositeVariableSeparatorToStringConverter(vc.CompositeVariableSeparator)} ", conditions)})";
        }

        private static string GetPythonCondition(this CompositeVariableComponent vc, bool includeResultTypes, IEnumerable<string> primaryEntityTypeNames)
        {
            var conditions = vc.CompositeVariableComponents.Select(c => c.GetPythonCondition(includeResultTypes, primaryEntityTypeNames));
            return $"({string.Join($" {CompositeVariableSeparatorToStringConverter(vc.CompositeVariableSeparator)} ", conditions)})";
        }
        public static string GetResultTypes(this ISingleVariableComponent vc) => GetResultTypes(vc.ResultEntityTypeNames);

        private static string GetResultTypes(List<string> resultEntityTypeNames)
        {
            if (!resultEntityTypeNames.Any())
            {
                return "";
            }

            return string.Join(", ", resultEntityTypeNames.Select(GetTypeEqualsResultType));
        }

        public static string GetTypeEqualsResultType(string t)
        {
            return $"{t}=result.{t}";
        }

        private static string GetFieldRetriever(ISingleVariableComponent vc) => vc.FromVariableIdentifier;
    }
}
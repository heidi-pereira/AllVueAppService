using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Variable;

namespace BrandVue.SourceData.Measures
{
    public class MetricBaseDescriptionGenerator : IMeasureBaseDescriptionGenerator
    {
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;

        public MetricBaseDescriptionGenerator(IReadableVariableConfigurationRepository variableConfigurationRepository)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
        }

        public static string BaseDefinitionTypeToString(BaseDefinitionType baseType)
        {
            return baseType switch
            {
                BaseDefinitionType.AllRespondents => "All respondents",
                BaseDefinitionType.SawThisQuestion => "Respondents who saw the question",
                BaseDefinitionType.SawThisChoice => "Respondents who saw the choice",
                _ => throw new InvalidOperationException("Base type not handled " + baseType)
            };
        }

        public string BaseExpressionDefinitionToString(BaseExpressionDefinition baseExpressionDefinition)
        {
            if (baseExpressionDefinition.BaseVariableId.HasValue)
            {
                return GetBaseDescriptionAndHasCustomBaseFromVariableId(baseExpressionDefinition.BaseVariableId.Value).Description;
            }

            return BaseDefinitionTypeToString(baseExpressionDefinition.BaseType);
        }

        public (string BaseDescription, bool HasCustomBase) GetBaseDescriptionAndHasCustomBase(Measure measure, Subset subset)
        {
            string baseDescription = "Respondents who saw this question";
            bool hasCustomBase = false;

            var primaryEntityCombination = measure.PrimaryFieldEntityCombination?.Select(t => t.Identifier).ToHashSet() ?? new HashSet<string>();
            var baseEntityCombination = measure.BaseEntityCombination?.Select(t => t.Identifier).ToHashSet() ?? new HashSet<string>();
            if (!primaryEntityCombination.IsSupersetOf(baseEntityCombination))
            {
                hasCustomBase = true;
                baseDescription = "Custom base";
                if (measure.BaseVariableConfigurationId.HasValue)
                {
                    var variable = _variableConfigurationRepository.Get(measure.BaseVariableConfigurationId.Value);
                    baseDescription += variable == null ? "" : $" ({variable.DisplayName})";
                }
                else if (measure.HasBaseExpression)
                {
                    baseDescription = GetBaseDescriptionForCustomFieldExpression(measure.BaseExpressionString);
                }
                else if (measure.BaseField != null)
                {
                    baseDescription += $" ({measure.BaseField.Name})";
                }
            }
            else if (measure.BaseVariableConfigurationId.HasValue)
            {
                (baseDescription, hasCustomBase) = GetBaseDescriptionAndHasCustomBaseFromVariableId(measure.BaseVariableConfigurationId.Value);
            }
            else if (measure.HasBaseExpression && measure.BaseExpression is not BooleanFromValueVariable)
            {
                hasCustomBase = true;
                baseDescription = GetBaseDescriptionForCustomFieldExpression(measure.BaseExpressionString);
            }
            else if (measure.HasDistinctBaseField)
            {
                var choiceSet = measure.BaseField.GetValueChoiceSetOrNull(subset.Id);
                if (choiceSet == null)
                {
                    return ("Custom base", false);
                }

                var choicesBySurveyChoiceId = choiceSet.ChoicesBySurveyChoiceId;
                var choiceIds = choicesBySurveyChoiceId.Keys.ToArray();

                if (measure.LegacyBaseValues.IsList && choiceIds.Any(id => !measure.LegacyBaseValues.Values.Contains(id)))
                {
                    hasCustomBase = true;
                    var separator = measure.LegacyBaseValues.Values.Length > 1 ? "is any of" : "is";
                    var baseValueNames = BaseValuesToChoiceNames(measure.LegacyBaseValues.Values, choicesBySurveyChoiceId);
                    baseDescription = $"Respondents where {measure.BaseField.Name} {separator} {string.Join(", ", baseValueNames)}";
                }

                if (measure.LegacyBaseValues.IsRange && choiceIds.Any(id => id < measure.LegacyBaseValues.Minimum.Value || id > measure.LegacyBaseValues.Maximum.Value))
                {
                    hasCustomBase = true;
                    var range = Enumerable.Range(measure.LegacyBaseValues.Minimum.Value, measure.LegacyBaseValues.Maximum.Value - measure.LegacyBaseValues.Minimum.Value + 1).ToArray();
                    var separator = range.Length > 1 ? "is any of" : "is";
                    var baseValueNames = BaseValuesToChoiceNames(range, choicesBySurveyChoiceId);
                    baseDescription = $"Respondents where {measure.BaseField.Name} {separator} {string.Join(", ", baseValueNames)}";
                }
            }

            return (baseDescription, hasCustomBase);
        }

        private (string Description, bool HasCustomBase) GetBaseDescriptionAndHasCustomBaseFromVariableId(int variableId)
        {
            var variable = _variableConfigurationRepository.Get(variableId);
            if (variable == null)
            {
                return ("Error", false);
            }
            else if (!variable.IsBaseVariable())
            {
                return ($"Custom base ({variable.DisplayName})", true);
            }
            return (variable.DisplayName, false);
        }

        private string GetBaseDescriptionForCustomFieldExpression(string fieldExpression)
        {
            string baseExpressionString = fieldExpression?.Trim() ?? "";
            if (baseExpressionString == "1" || baseExpressionString == "(1)")
            {
                return "Custom base (All respondents)";
            }
            else
            {
                return "Custom base";
            }
        }

        private static IEnumerable<string> BaseValuesToChoiceNames(IEnumerable<int> baseValues, IReadOnlyDictionary<int, Choice> surveyChoiceIdToChoice)
        {
            var matchedChoices = baseValues.Where(id => surveyChoiceIdToChoice.ContainsKey(id));
            if (matchedChoices.Any())
            {
                return matchedChoices.Select(id => surveyChoiceIdToChoice[id].GetDisplayName());
            }
            else
            {
                return baseValues.Select(id => id.ToString());
            }
        }
    }
}

using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.Migrations.MetaData;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BrandVue.SourceData.Calculation.Expressions
{
    public class FilterValueMappingVariableParser
    {
        public const string FILTER_VALUE_MAPPING_VARIABLE_SUFFIX = "_filterValueMapping_variable";
        private readonly IProductContext _productContext;
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;
        private readonly ILogger<FilterValueMappingVariableParser> _logger;

        public FilterValueMappingVariableParser(IProductContext productContext, IReadableVariableConfigurationRepository variableConfigurationRepository, ILogger<FilterValueMappingVariableParser> logger)
        {
            _productContext = productContext;
            _variableConfigurationRepository = variableConfigurationRepository;
            _logger = logger;
        }

        public static string GetFilterValueMappingVariableIdentifier(string measureName) =>
            $"{NameGenerator.EnsureValidPythonIdentifier(measureName)}{FILTER_VALUE_MAPPING_VARIABLE_SUFFIX}";

        public VariableConfiguration CreateVariableConfigurationOrNull(Measure measure)
        {
            try
            {
                if (!BreakMeasureRequiresLegacyCalculation(measure) && !measure.DisableFilter && IsValidFilterValueMapping(measure.FilterValueMapping)
                    && GetFromVariableIdentifier(measure) is { } fromVariableIdentifier)
                {
                    var primaryEntityCombinationCount = measure.PrimaryFieldEntityCombination.Count;
                    var filterValueMappingParts = FilterMeasures(measure.FilterValueMapping).ToArray();
                    
                    //ignoring multientity as they can't be used as breaks/filters so FilterValueMapping is useless on them for now
                    if (measure.EntityCombination.Count() <= 1 && filterValueMappingParts.Any())
                    {
                        var identifier = GetFilterValueMappingVariableIdentifier(measure.Name);
                        var groupedDefinition = new GroupedVariableDefinition
                        {
                            ToEntityTypeName = identifier,
                            ToEntityTypeDisplayNamePlural = identifier,
                            Groups = new List<VariableGrouping>()
                        };
                        var variable = new VariableConfiguration
                        {
                            Identifier = identifier,
                            DisplayName = $"{measure.DisplayName} (FilterValueMapping)",
                            ProductShortCode = _productContext.ShortCode,
                            SubProductId = _productContext.SubProductId,
                            VariablesDependingOnThis = new List<VariableDependency>(),
                            Definition = groupedDefinition
                        };

                        
                        if (primaryEntityCombinationCount == 0)
                        {
                            for (var i = 0; i < filterValueMappingParts.Length; i++)
                            {
                                var (name, values, invert, isRange) = filterValueMappingParts[i];
                                var group = new VariableGrouping
                                {
                                    ToEntityInstanceId = i,
                                    ToEntityInstanceName = string.IsNullOrWhiteSpace(name) ? $"Group {i+1}" : name,
                                    Component = GetInclusiveRangeComponent(values, invert, isRange, fromVariableIdentifier)
                                };
                                groupedDefinition.Groups.Add(group);
                            }
                        }
                        else if (primaryEntityCombinationCount == 1)
                        {
                            var fromEntityTypeName = measure.PrimaryFieldEntityCombination.Single().Identifier;
                            for (var i = 0; i < filterValueMappingParts.Length; i++)
                            {
                                var (name, values, invert, isRange) = filterValueMappingParts[i];
                                var group = new VariableGrouping
                                {
                                    ToEntityInstanceId = i,
                                    ToEntityInstanceName = string.IsNullOrWhiteSpace(name) ? $"Group {i+1}" : name,
                                    Component = GetInstanceListComponent(values, invert, isRange, fromVariableIdentifier, fromEntityTypeName)
                                };
                                groupedDefinition.Groups.Add(group);
                            }
                        }

                        return variable;
                    }
                }
            }
            catch (Exception x)
            {
                //invalid filtervaluemapping, continue without creating
                _logger.LogWarning($"{_productContext} Failed to create FilterValueMapping variable for measure {measure?.Name} ({measure?.FilterValueMapping}): {x.Message}");
            }
            return null;
        }

        public static bool BreakMeasureRequiresLegacyCalculation(Measure measure)
        {
            if (measure.Field2 != null)
            {
                return true;
            }
            if (measure.PrimaryVariable?.CanCreateForSingleEntity() != true)
            {
                return true;
            }
            // Range filter value mapping is not supported in breaks
            if (IsValidFilterValueMapping(measure.FilterValueMapping))
            {
                if (!measure.EntityCombination.Any())
                {
                    return true;
                }
                //There are lots of the '1:Yes|!1:No' filtervaluemappings that could be ignored potentially, but there's the possibility that !1 covers "null" as well which may change the behaviour. Again, we should define a variable properly to cover this case.
                //Note: this is currently ignoring remapped names, so originals will be shown in UI. We should stop using FilterValueMappings for this purpose anywhere that becomes an issue by just creating a new variable or editing the entity instances
                return FilterMeasures(measure.FilterValueMapping).Any(part => part.Invert || part.IsRange || part.Values.Length > 1);
            }
            return false;
        }

        private VariableComponent GetInclusiveRangeComponent(int[] values, bool invert, bool isRange, string fromVariableIdentifier)
        {
            if (isRange)
            {
                var min = values.Min();
                var max = values.Max();
                return new InclusiveRangeVariableComponent
                {
                    Min = min,
                    Max = max,
                    Operator = VariableRangeComparisonOperator.Between,
                    FromVariableIdentifier = fromVariableIdentifier,
                    Inverted = invert
                };
            }
            return new InclusiveRangeVariableComponent
            {
                ExactValues = values,
                Operator = VariableRangeComparisonOperator.Exactly,
                FromVariableIdentifier = fromVariableIdentifier,
                Inverted = invert
            };
        }

        private InstanceListVariableComponent GetInstanceListComponent(int[] values, bool invert, bool isRange, string fromVariableIndentifier, string fromEntityTypeName)
        {
            return new InstanceListVariableComponent
            {
                FromVariableIdentifier = fromVariableIndentifier,
                FromEntityTypeName = fromEntityTypeName,
                Operator = invert ? InstanceVariableComponentOperator.Not : InstanceVariableComponentOperator.Or,
                InstanceIds = isRange ? Enumerable.Range(values[0], values[1] - values[0] + 1).ToList() : values.ToList()
            };
        }

        private string GetFromVariableIdentifier(Measure measure)
        {
            if (measure.VariableConfigurationId.HasValue)
            {
                var variable = _variableConfigurationRepository.Get(measure.VariableConfigurationId.Value);
                if(variable != null && variable.Identifier != null)
                {
                    return variable.Identifier;
                }
                else
                {
                    _logger.LogWarning($"{_productContext} Unable to fetch variable identifier for {measure.Name} ({measure.VariableConfigurationId})");
                    return measure.PrimaryFieldDependencies.OnlyOrDefault()?.Name;
                }
            }
            return measure.PrimaryFieldDependencies.OnlyOrDefault()?.Name;
        }

        public record FilterValueMappingPart(string Name, int[] Values, bool Invert, bool IsRange);

        //
        // https://www.rexegg.com/regex-lookarounds.html
        //
        // using -ve lookahead buffer to terminate the strings
        //
        private const string _filterMeasureSpliterRegEx = @"([!-]?[\d,-]*):(?:(?!(\|[!-]?[\d,-]*):).)*";

        private static IEnumerable<string> SplitFilterMeasures(string filterMeasure)
        {
            RegexOptions options = RegexOptions.Multiline;
            var allMatches = Regex.Matches(filterMeasure, _filterMeasureSpliterRegEx, options);
            return allMatches.Select(x=> x.Value);
        }

        public static IEnumerable<FilterValueMappingPart> FilterMeasures(string filterMeasure)
        {
            return SplitFilterMeasures(filterMeasure).Select(ParseFilterValueMappingPart);
        }

        public static FilterValueMappingPart ParseFilterValueMappingPart(string filterValueMappingPart)
        {
            var split = filterValueMappingPart.Split(':', 2);
            var valueString = split[0];

            var name = split[1];
            var invert = false;
            var isRange = false;
            int[] values = null;

            if (valueString.StartsWith('!'))
            {
                invert = true;
                valueString = valueString[1..];
            }

            var valueStringWithoutLeadingNegative = valueString.StartsWith('-') ? valueString[1..] : valueString;
            if (valueString.Contains(',') || !valueStringWithoutLeadingNegative.Contains('-'))
            {
                values = valueString.Split(',').Select(int.Parse).ToArray();
            }
            else
            {
                isRange = true;
                var startsWithNegative = valueString.StartsWith('-');
                values = valueStringWithoutLeadingNegative.Split('-', 2).Select(int.Parse).ToArray();
                if (startsWithNegative)
                {
                    values[0] = -values[0];
                }
            }

            return new FilterValueMappingPart(name, values, invert, isRange);
        }

        internal static bool IsValidFilterValueMapping(string filterValueMapping) =>
            !string.IsNullOrWhiteSpace(filterValueMapping) && !filterValueMapping.StartsWith("Range", StringComparison.OrdinalIgnoreCase);
    }
}

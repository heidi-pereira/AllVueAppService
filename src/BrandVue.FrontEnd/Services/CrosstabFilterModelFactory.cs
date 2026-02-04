using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public class CrosstabFilterModelFactory
    {
        private readonly IEntityRepository _entityRepository;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;

        public CrosstabFilterModelFactory(IEntityRepository entityRepository, IQuestionTypeLookupRepository questionTypeLookupRepository)
        {
            _entityRepository = entityRepository;
            _questionTypeLookupRepository = questionTypeLookupRepository;
        }

        public IEnumerable<CompositeFilterModel> GetAllFiltersForMeasure(Measure measure, Subset subset, CrossMeasureFilterInstance[] instancesToFilterTo, bool multipleChoiceByValue)
        {
            //
            // Code needs to be refactored as per https://app.shortcut.com/mig-global/story/81822/refactor-code-requestadapter-getbreakinstances-crosstabfiltermodelfactory-getallfiltersformeasure
            //

            //TODO: Handle -1 case (our rows match a type of the column)
            var measureIsBasedOnSingleChoiceOrVariable = _questionTypeLookupRepository.GetForSubset(subset)
                .TryGetValue(measure.Name, out var questionType) && (questionType == MainQuestionType.SingleChoice || questionType == MainQuestionType.CustomVariable);

            return measure switch
            {
                var m when m.EntityCombination.Count() > 1 => 
                    throw new ArgumentException("CrossMeasures can't be multi-entity"),
                var m when m.EntityCombination.OnlyOrDefault() is {Identifier: {} entityType} && !measureIsBasedOnSingleChoiceOrVariable => 
                    GetMultipleChoiceFilters(m, entityType, subset, instancesToFilterTo, multipleChoiceByValue),
                var m when CanUseFilterValueMappingInstances(m, instancesToFilterTo) => 
                    GetMappedFilters(m, instancesToFilterTo, measure.EntityCombination.SingleOrDefault()?.Identifier, measure.EntityCombination.Any()),
                var m when m.EntityCombination.Any() => 
                    GetSingleEntityFilters(m, subset, instancesToFilterTo),
                var m when !string.IsNullOrWhiteSpace(m.FilterValueMapping) =>
                    GetMappedFilters(measure, instancesToFilterTo, null, false),

                _ => throw new ArgumentException("CrossMeasures without an entity type must have a filter mapping")
            };
        }
        
        private bool CanUseFilterValueMappingInstances(Measure measure, CrossMeasureFilterInstance[] instancesToFilterTo)
        {
            return 
                !string.IsNullOrWhiteSpace(measure.FilterValueMapping) 
                && (!instancesToFilterTo.Any() 
                    || instancesToFilterTo.Any(i => !string.IsNullOrWhiteSpace(i.FilterValueMappingName)))
                && measure.EntityCombination.Any();
        }
        
        private IEnumerable<CompositeFilterModel> GetMultipleChoiceFilters(Measure measure, string entityType,
            Subset subset, CrossMeasureFilterInstance[] instancesToFilterTo, bool multipleChoiceByValue)
        {
            if (multipleChoiceByValue &&
                !string.IsNullOrWhiteSpace(measure.FilterValueMapping) &&
                !measure.FilterValueMapping.StartsWith("Range", StringComparison.InvariantCultureIgnoreCase))
            {
                return GetMappedFilters(measure, instancesToFilterTo, entityType, false);
            }
            else
            {
                return GetMultipleChoiceFiltersByEntityInstance(measure, subset, instancesToFilterTo);
            }
        }

        private IEnumerable<CompositeFilterModel> GetMultipleChoiceFiltersByEntityInstance(Measure measure, Subset subset, CrossMeasureFilterInstance[] instancesToFilterTo)
        {
            var entityType = measure.EntityCombination.Single();
            var instances = _entityRepository.GetInstancesOf(entityType.Identifier, subset);
            var filterInstanceIds = instancesToFilterTo.Select(i => i.InstanceId).ToList();
            if (filterInstanceIds.Any())
            {
                instances = instances.Where(i => filterInstanceIds.ToList().Contains(i.Id)).ToList();
            }
            /*
             * When not based on single choice, we show a column for every instance of the given EntityType.
             * However, we can't rely on the FilterValueMapping to always choose values to filter to.
             *
             * For example, Brand affinity has the FilterValueMapping 7:Love|5,6:Like|4:Indifferent|1,2,3:Dislike|1:Hate
             *
             * Instead we use the trueVals of the metric (5|6|7 for the Brand affinity example)
             */
            (int[] trueValues, bool isRange) = GetFilterValuesFromTrueValues(measure);
            return instances.OrderBy(i => i.Id).Select(i =>
                new CompositeFilterModel(FilterOperator.And, new MeasureFilterRequestModel(measure.Name, GetEntityInstanceFromSingleOption(entityType.Identifier, i.Id), false, isRange, trueValues).Yield()) { Name = i.Name }
            );
        }

        private IEnumerable<CompositeFilterModel> GetMappedFilters(Measure measure, CrossMeasureFilterInstance[] instancesToFilterTo, string entityType, bool useInstanceIdValues)
        {
            var filterMappingString = measure.FilterValueMapping;
            if (filterMappingString.StartsWith("Range", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new ArgumentException("Can't create a crosstab break from a \"Range\" filter mapping");
            }

            var mappings = filterMappingString.Split('|');
            var orderedMappings = GetOrderedMappings(mappings);

            var mappingNamesToFilterTo = instancesToFilterTo.Where(i => !string.IsNullOrWhiteSpace(i.FilterValueMappingName)).Select(i => i.FilterValueMappingName);
            foreach (var mapping in orderedMappings)
            {
                var components = mapping.Split(':');
                if (components.Length >= 1)
                {
                    var name = string.Join(":", components.Skip(1));
                    if (!mappingNamesToFilterTo.Any() || mappingNamesToFilterTo.Contains(name))
                    {
                        var inverted = mapping.StartsWith("!");
                        var valueString = inverted ? components[0].Substring(1) : components[0];
                        var (values, isRange) = ParseValues(valueString);

                        if (useInstanceIdValues)
                        {
                            /*  The behaviour here may be bugged - the created filters expect both the entity ID and response value to be the same, which
                                is not always the case. A question could have both a choice set (entity IDs) and separate numerical answer value. I expect you would only want
                                to filter by one of these at a time (by entity ID or by response value, not by both matching each other).
                                If this behaviour is fixed, there is a test which used the old behaviour used for its results that can be updated:
                                CrosstabBenchmarks.SingleEntityCrosstabWithEntityBaseFieldBreak
                            */
                            values = isRange ? Enumerable.Range(values[0], 1 + values[1] - values[0]).ToArray() : values;
                            var measureFilters = values.Select(v => new MeasureFilterRequestModel(measure.Name, GetEntityInstanceFromSingleOption(entityType, v), inverted, false, new[] { v }));
                            yield return new CompositeFilterModel(inverted ? FilterOperator.And : FilterOperator.Or, measureFilters)
                                { Name = name };
                        }
                        else
                        {
                            yield return new CompositeFilterModel(FilterOperator.And,
                                    new MeasureFilterRequestModel(measure.Name,  entityType is null ? new Dictionary<string, int[]>() : GetEntityInstanceFromSingleOption(entityType, -1), inverted, isRange, values).Yield())
                                { Name = name };
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Cannot split {mapping}");
                }
            }
        }

        private static Dictionary<string, int[]> GetEntityInstanceFromSingleOption(string entityType, int v)
        {
            return new Dictionary<string,int[]> { {entityType,[v]}};
        }

        private static List<string> GetOrderedMappings(string[] mappings)
        {
            var scriptPositionAndMapping = new List<(int firstInstanceId, string mapping)>();
            foreach (var map in mappings)
            {
                var indicesAndValue = map.Split(':');
                var firstValidIndex = indicesAndValue[0].Replace("!", string.Empty).TakeWhile(char.IsDigit).ToArray();

                if (int.TryParse(firstValidIndex, out int scriptPosition))
                {
                    scriptPositionAndMapping.Add((scriptPosition, map));
                }
                else
                {
                    scriptPositionAndMapping.Add((mappings.Length, map));
                }
            }

            scriptPositionAndMapping.Sort((x, y) => x.firstInstanceId.CompareTo(y.firstInstanceId));
            return scriptPositionAndMapping.Select(s => s.mapping).ToList();
        }

        private (int[] Values, bool IsRange) ParseValues(string valueString)
        {
            if (!valueString.Contains(","))
            {
                if (valueString.Contains("-"))
                {
                    if (valueString.Count(c => c == '-') > 3)
                    {
                        throw new ArgumentException($"Invalid valuestring for filter value mapping: {valueString}");
                    }

                    var workingString = valueString.StartsWith('-') ? valueString.Substring(1) : valueString;

                    var resultValues = workingString.Split('-', 2).Select(int.Parse).ToArray();
                    if (valueString.StartsWith('-'))
                    {
                        resultValues[0] = -resultValues[0];
                    }

                    if (resultValues.Length == 2 && resultValues[1] < resultValues[0])
                    {
                        throw new ArgumentException($"Invalid valuestring for filter value mapping: {valueString}");
                    }

                    return (resultValues, resultValues.Length > 1);
                }
            }
            return (valueString.Split(',').Select(int.Parse).ToArray(), false);
        }

        private static (int[], bool) GetFilterValuesFromTrueValues(Measure measure)
        {
            if (measure.Field is null)
            {
                return (new int[] { int.MinValue, int.MaxValue }, true);
            }

            if (measure.LegacyPrimaryTrueValues.IsList)
            {
                return (PrimaryTrueValues: measure.LegacyPrimaryTrueValues.Values, false);
            }

            if (measure.LegacyPrimaryTrueValues.IsRange)
            {
                return (new int[] {  measure.LegacyPrimaryTrueValues.Minimum.Value, measure.LegacyPrimaryTrueValues.Maximum.Value }, true);
            }

            return (new int[] { 1, int.MaxValue }, true);
        }

        private IEnumerable<CompositeFilterModel> GetSingleEntityFilters(Measure measure, Subset subset, CrossMeasureFilterInstance[] instancesToFilterTo)
        {
            var entityType = measure.EntityCombination.Single();
            var instances = _entityRepository.GetInstancesOf(entityType.Identifier, subset);
            var filterInstanceIds = instancesToFilterTo.Select(i => i.InstanceId);
            if (filterInstanceIds.Any())
            {
                instances = instances.Where(i => filterInstanceIds.Contains(i.Id)).ToList();
            }
            //TODO: Dedupe OrderBy with one in RequestAdapter.CreateBreaks so they don't have to happen to line up
            return instances.OrderBy(i => i.Id).Select(i =>
                new CompositeFilterModel(FilterOperator.And, new MeasureFilterRequestModel(measure.Name, GetEntityInstanceFromSingleOption(entityType.Identifier, i.Id), false, false, new[] { i.Id }).Yield()) { Name = i.Name }
            );
        }
    }
}

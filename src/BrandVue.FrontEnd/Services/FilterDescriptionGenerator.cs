using BrandVue.Models;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using System.Text;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.Services
{
    public class FilterDescriptionGenerator
    {
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;

        public FilterDescriptionGenerator(IMeasureRepository measureRepository, IEntityRepository entityRepository)
        {
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;
        }

        public string GetDemographicFilterDescription(DemographicFilter demographicFilter)
        {
            if (demographicFilter.AgeGroups.Any() ||
                demographicFilter.Genders.Any() ||
                demographicFilter.Regions.Any() ||
                demographicFilter.SocioEconomicGroups.Any())
            {
                var description = new StringBuilder();

                if (demographicFilter.AgeGroups.Any())
                {
                    description.Append($"Age groups: {string.Join(", ", demographicFilter.AgeGroups)}, ");
                }

                if (demographicFilter.Genders.Any())
                {
                    description.Append($"Genders: {string.Join(", ", demographicFilter.Genders)}, ");
                }

                if (demographicFilter.Regions.Any())
                {
                    description.Append($"Regions: {string.Join(", ", demographicFilter.Regions)}, ");
                }

                if (demographicFilter.SocioEconomicGroups.Any())
                {
                    description.Append($"Socioeconomic groups: {string.Join(", ", demographicFilter.SocioEconomicGroups)}, ");
                }

                description.Remove(description.Length - 2, 2);
                return description.ToString();
            }

            return null;
        }

        public string GetCompositeFilterDescription(CompositeFilterModel filterModel, Subset subset)
        {
            if (filterModel.Filters.Any() || filterModel.CompositeFilters.Any())
            {
                var description = new StringBuilder();
                var filterSeparator = $" {FilterOperatorToString(filterModel.FilterOperator)} ";

                if (filterModel.Filters.Any())
                {
                    description.Append(string.Join(filterSeparator, GetGroupedMeasureFilterDescription(filterModel.Filters, subset, filterSeparator)));
                }

                if (filterModel.Filters.Any() && filterModel.CompositeFilters.Any())
                {
                    description.Append(filterSeparator);
                }

                if (filterModel.CompositeFilters.Any())
                {
                    description.Append(string.Join(filterSeparator, filterModel.CompositeFilters.Select(filter => $"({GetCompositeFilterDescription(filter, subset)})")));
                }

                return description.ToString();
            }
            return null;
        }

        private string GetGroupedMeasureFilterDescription(IEnumerable<MeasureFilterRequestModel> filters, Subset subset, string filterSeparator)
        {
            var firstFilter = filters.First();

            if (!filters.All(filter => filter.MeasureName == firstFilter.MeasureName))
            {
                return string.Join(filterSeparator, filters.Select(filter => GetMeasureFilterDescription(filter, subset)));
            }

            var measure = _measureRepository.Get(firstFilter.MeasureName);
            if (measure.EntityCombination.Any())
            {
                var entityType = measure.EntityCombination.Single();
                var entityInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset)
                    .ToDictionary(i => i.Id);
                return
                    $"{entityType.DisplayNameSingular} is {string.Join(filterSeparator, filters.Select(filter => GetMeasureFilterValuesDescription(filter, entityInstances)))}";
            }

            return FilterValuesToDescription(filters, measure, filterSeparator);
        }

        private string FilterValuesToDescription(IEnumerable<MeasureFilterRequestModel> filters, Measure measure, string filterSeparator)
        {
            var rawFilterValuesToDescriptionLookup = measure.FilterValueMapping.Trim().Split('|').Select(eachFilterValueMapping =>
            {
                var filterMappingParts = eachFilterValueMapping.Split(":");
                if (filterMappingParts.Length >= 2)
                {
                    return (filterMappingParts[0], filterMappingParts[1]);
                }
                return (eachFilterValueMapping, eachFilterValueMapping);
            }).ToDictionary(x => x.Item1, x => x.Item2);

            var descriptionForFilterValues = filters.Select(filter =>
            {
                var rawFilterValuesFromFilter = RawFilterValueFromFilterModel(filter);
                if (rawFilterValuesToDescriptionLookup.ContainsKey(rawFilterValuesFromFilter))
                {
                    return rawFilterValuesToDescriptionLookup[rawFilterValuesFromFilter];
                }
                return rawFilterValuesFromFilter;
            });
            return $"{measure.DisplayName} : {string.Join(filterSeparator, descriptionForFilterValues)}";
        }

        private string RawFilterValueFromFilterModel(MeasureFilterRequestModel model)
        {
            var rawFilterValue = string.Join(",", model.Values);
            if (model.Invert)
            {
                rawFilterValue = "!" + rawFilterValue;
            }
            return rawFilterValue;
        }

        private string GetMeasureFilterDescription(MeasureFilterRequestModel filter, Subset subset)
        {
            var measure = _measureRepository.Get(filter.MeasureName);
            var entityType = measure.EntityCombination.Single();
            var entityInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset).ToDictionary(i => i.Id);
            return $"{entityType.DisplayNameSingular} is {GetMeasureFilterValuesDescription(filter, entityInstances)}";
        }

        private string GetMeasureFilterValuesDescription(MeasureFilterRequestModel filter, IDictionary<int, EntityInstance> entityInstances)
        {
            var description = new StringBuilder();
            var values = filter.EntityInstances.Values.SelectMany(v => v).ToArray();

            if (filter.TreatPrimaryValuesAsRange)
            {
                var min = values.Min();
                var max = values.Max();
                values = Enumerable.Range(min, max - min + 1).ToArray();
            }

            if (filter.Invert)
            {
                description.Append("not ");
            }

            if (values.Length > 1)
            {
                description.Append("any of ");
            }

            var nameValues = InstanceIdsToNames(values, entityInstances);
            description.Append(string.Join(", ", nameValues));
            return description.ToString();
        }

        private string FilterOperatorToString(FilterOperator op)
        {
            return op switch
            {
                FilterOperator.And => "and",
                FilterOperator.Or => "or",
                _ => ""
            };
        }

        private IEnumerable<string> InstanceIdsToNames(IEnumerable<int> instanceIds, IDictionary<int, EntityInstance> idToInstance)
        {
            return instanceIds.Select(id => idToInstance.ContainsKey(id) ? idToInstance[id].Name : id.ToString());
        }
    }
}

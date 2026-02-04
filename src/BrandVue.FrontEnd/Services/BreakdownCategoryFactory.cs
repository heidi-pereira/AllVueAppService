using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public class BreakdownCategoryFactory : IBreakdownCategoryFactory
    {
        private readonly IQuotaCellDescriptionProvider _quotaCellDescriptionProvider;
        private readonly IDemographicFilterToQuotaCellMapper _demographicFilterToQuotaCellMapper;
        private readonly IFilterRepository _filters;
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;

        public BreakdownCategoryFactory(IQuotaCellDescriptionProvider quotaCellDescriptionProvider,
            IDemographicFilterToQuotaCellMapper demographicFilterToQuotaCellMapper,
            IFilterRepository filters,
            IMeasureRepository measureRepository,
            IEntityRepository entityRepository)
        {
            _quotaCellDescriptionProvider = quotaCellDescriptionProvider;
            _demographicFilterToQuotaCellMapper = demographicFilterToQuotaCellMapper;
            _filters = filters;
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;

        }

        public BreakdownCategory ByGender(DemographicFilter demographicFilter, Subset subset)
        {
            var descriptionFilterCollection = BreakdownCategoryByIdentifier(DefaultQuotaFieldGroups.Gender, demographicFilter, subset);
            return new BreakdownCategory(_demographicFilterToQuotaCellMapper, descriptionFilterCollection);
        }

        private (string Description, DemographicFilter Filter)[] BreakdownCategoryByIdentifier(string identififier, DemographicFilter demographicFilter, Subset subset)
        {
            return demographicFilter
                .GetValueLabelsForField(identififier)
                .Select(label =>
                {
                    var metricName = demographicFilter.MetricNameForNamedItem(subset.Id, identififier);
                    string description = string.Empty;
                    if (metricName != null && _measureRepository.TryGet(metricName, out var measure))
                    {
                        var requestedInstances = _entityRepository.CreateTargetInstances(subset, measure);
                        var entity = requestedInstances.OrderedInstances.SingleOrDefault(x => x.Id.ToString() == label);
                        description = entity?.Name?? $"Missing {label}";
                    }
                    else
                    {
                        description = _quotaCellDescriptionProvider.GetDescriptionForQuotaCellKey(subset, identififier, label);
                    }
                    var filter = demographicFilter.WithValuesFor(_filters, identififier, label);

                    return (Description: description,Filter: filter);
                 }).ToArray();
        }

        public BreakdownCategory BySegOrNull(DemographicFilter demographicFilter, Subset subset)
        {
            var descriptionFilterCollection = BreakdownCategoryByIdentifier(DefaultQuotaFieldGroups.Seg, demographicFilter, subset);

            return descriptionFilterCollection.Any(v => v.Description is null) ? null : new BreakdownCategory(_demographicFilterToQuotaCellMapper, descriptionFilterCollection);
        }

        public BreakdownCategory ByRegion(DemographicFilter demographicFilter, Subset subset)
        {
            var descriptionFilterCollection = BreakdownCategoryByIdentifier(DefaultQuotaFieldGroups.Region, demographicFilter, subset);
            return new BreakdownCategory(_demographicFilterToQuotaCellMapper, descriptionFilterCollection);
        }

        public BreakdownCategory ByAgeGroup(DemographicFilter demographicFilter, Subset subset)
        {
            var descriptionFilterCollection = BreakdownCategoryByIdentifier(DefaultQuotaFieldGroups.Age, demographicFilter, subset);
            return new BreakdownCategory(_demographicFilterToQuotaCellMapper, descriptionFilterCollection);
        }
    }
}
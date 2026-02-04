using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public class BreakdownCategory
    {
        private readonly IDemographicFilterToQuotaCellMapper _demographicFilterToQuotaCellMapper;
        private readonly IReadOnlyCollection<(string Description, DemographicFilter Filter)> _descriptionFilterCollection;

        public BreakdownCategory(IDemographicFilterToQuotaCellMapper demographicFilterToQuotaCellMapper, IReadOnlyCollection<(string Description, DemographicFilter Filter)> descriptionFilterCollection)
        {
            _demographicFilterToQuotaCellMapper = demographicFilterToQuotaCellMapper;
            _descriptionFilterCollection = descriptionFilterCollection;
        }

        public IEnumerable<(string CategoryDescription, IGroupedQuotaCells QuotaCells)> GetCategories(Subset subset) =>
            _descriptionFilterCollection
                .Select(c => (c.Description, _demographicFilterToQuotaCellMapper.MapWeightedQuotaCellsFor(subset, c.Filter)));
    }
}
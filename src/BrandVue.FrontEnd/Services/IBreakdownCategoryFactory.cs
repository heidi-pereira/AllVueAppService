using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    public interface IBreakdownCategoryFactory
    {
        BreakdownCategory ByGender(DemographicFilter demographicFilter, Subset subset);
        BreakdownCategory BySegOrNull(DemographicFilter demographicFilter, Subset subset);
        BreakdownCategory ByRegion(DemographicFilter demographicFilter, Subset subset);
        BreakdownCategory ByAgeGroup(DemographicFilter demographicFilter, Subset subset);
    }
}
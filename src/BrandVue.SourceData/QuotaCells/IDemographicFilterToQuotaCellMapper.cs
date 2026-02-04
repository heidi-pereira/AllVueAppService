using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CommonMetadata;

namespace BrandVue.SourceData.QuotaCells
{
    public interface IDemographicFilterToQuotaCellMapper
    {
        IGroupedQuotaCells MapWeightedQuotaCellsFor(Subset datasetSelector,
            DemographicFilter filter);

        IGroupedQuotaCells MapQuotaCellsFor(Subset datasetSelector,
            DemographicFilter filter, AverageDescriptor averageDescriptor);
    }
}
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal interface IWeightingAggregator
    {
        EntityWeightedTotalSeries[] Weight(Subset datasetSelector,
            AverageDescriptor average,
            IGroupedQuotaCells indexOrderedDesiredQuotaCells,
            EntityTotalsSeries[] unweightedResults);
    }
}
using System.Threading;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Pure calculation on data that must already be loaded into memory before use.
    /// </summary>
    public interface ITotalisationOrchestrator
    {
        /// <summary>Filter to respondents in base, return per entity, per weighting cell: count and metric value sum</summary>
        /// <returns>A result per requested instance in ascending instance id order</returns>
        EntityTotalsSeries[] Totalise(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            EntityWeightedDailyResults[] weightedAverages,
            CancellationToken cancellationToken);
    }
}
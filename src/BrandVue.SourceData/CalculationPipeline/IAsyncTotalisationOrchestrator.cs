using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Fully encapsulated the calculation
    /// No prior work needed, unlike <see cref="ITotalisationOrchestrator"/>
    /// </summary>
    public interface IAsyncTotalisationOrchestrator
    {
        /// <returns>A result per requested instance in ascending instance id order</returns>
        Task<EntityTotalsSeries[]> TotaliseAsync(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            EntityWeightedDailyResults[] weightedAverages,
            CancellationToken cancellationToken);
    }
}
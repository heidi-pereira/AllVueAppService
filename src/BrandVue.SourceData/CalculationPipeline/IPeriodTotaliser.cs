using System.Threading;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    public interface IPeriodTotaliser
    {
        /// <summary>
        /// Per entity: Per quota cell: counts and metric totals per day or month intermediate results (windowed totals per month/rollingperiod/wave)
        /// </summary>
        EntityTotalsSeries[] TotalisePerCell(IProfileResponseAccessor profileResponseAccessor,
            FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells indexOrderedDesiredQuotaCells,
            EntityWeightedDailyResults[] weightedAverages,
            IGroupedQuotaCells allQuotaCells,
            DateTimeOffset responsesStartDate,
            DateTimeOffset responsesEndDate, CancellationToken cancellationToken);
    }
}

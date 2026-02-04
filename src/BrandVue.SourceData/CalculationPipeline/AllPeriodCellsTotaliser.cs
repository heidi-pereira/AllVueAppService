using System.Threading;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class AllPeriodCellsTotaliser : BasePeriodCellsTotaliser
    {
        protected override EntityTotalsSeries[]
            CalculateInternal(IProfileResponseAccessor profileResponseAccessor,
                AverageDescriptor average,
                FilteredMetric filteredMetric,
                TargetInstances requestedInstances,
                IGroupedQuotaCells desiredQuotaCells,
                DateTimeOffset lastDayOfFirstResult,
                DateTimeOffset lastDayOfLastResult,
                double[] weightedAverages,
                IGroupedQuotaCells allQuotaCells,
                CancellationToken cancellationToken)
        {
            var dataPointIndex = 0;
            var results = requestedInstances.CreateEmptyResults(lastDayOfLastResult.Yield(), allQuotaCells.Cells.Count);

            var totalCalculator = TotalCalculatorFactory.Create(filteredMetric, requestedInstances.EntityType);

            CalculateForQuotaCells(profileResponseAccessor, average, filteredMetric, desiredQuotaCells, lastDayOfFirstResult, lastDayOfLastResult, weightedAverages, totalCalculator, results, dataPointIndex, cancellationToken);

            return results;
        }

        private static void CalculateForQuotaCells(IProfileResponseAccessor profileResponseAccessor,
            AverageDescriptor average,
            FilteredMetric filteredMetric, IGroupedQuotaCells desiredQuotaCells,
            DateTimeOffset startDate,
            DateTimeOffset endDate, double[] weightedAverages, IFilteredMetricTotaliser totaliser,
            EntityTotalsSeries[] results, int dataPointIndex, CancellationToken cancellationToken)
        {
            foreach (var (quotaCell, responses) in profileResponseAccessor.GetResponses(startDate, endDate, desiredQuotaCells))
            {
                cancellationToken.ThrowIfCancellationRequested();
                totaliser.TotaliseResponses(
                    filteredMetric,
                    quotaCell,
                    responses.Span,
                    average.IncludeResponseIds,
                    results,
                    dataPointIndex,
                    weightedAverages);
                InitialiseFirstTotals(quotaCell, results, dataPointIndex);
            }
        }

        private static void InitialiseFirstTotals(QuotaCell quotaCell, EntityTotalsSeries[] results, int dataPointIndex)
        {
            foreach (var unweightedResult in results)
            {
                InitialiseFirstTotals(unweightedResult.CellsTotalsSeries[dataPointIndex][quotaCell]);
            }
        }
    }
}
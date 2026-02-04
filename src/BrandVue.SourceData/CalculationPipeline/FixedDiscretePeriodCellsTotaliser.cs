using System.Threading;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Dates;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class FixedDiscretePeriodCellsTotaliser : BasePeriodCellsTotaliser
    {
        private readonly IDateBatcher _dateBatcher;

        public FixedDiscretePeriodCellsTotaliser(IDateBatcher dateBatcher) => _dateBatcher = dateBatcher;

        protected override EntityTotalsSeries[] CalculateInternal(IProfileResponseAccessor profileResponseAccessor,
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
            var firstDayOfFirstResult = _dateBatcher.GetBatchStartContaining(lastDayOfFirstResult);
            var numberOfBatches = _dateBatcher.GetNumberOfBatchEndsWithin(lastDayOfFirstResult, lastDayOfLastResult);

            var results = requestedInstances.CreateEmptyResults(
                lastDayOfFirstResult,
                _dateBatcher.GetBatchEndStrictlyAfter,
                numberOfBatches,
                allQuotaCells.Cells.Count);

            if (numberOfBatches == 0)
            {
                return results;
            }

            var totalCalculator = TotalCalculatorFactory.Create(filteredMetric, requestedInstances.EntityType);

            foreach (var (quotaCell, profiles) in profileResponseAccessor.GetResponses(desiredQuotaCells).WithinTimesInclusive(firstDayOfFirstResult, lastDayOfLastResult))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var remainingProfiles = profiles.Span;
                while (remainingProfiles.Length > 0)
                {
                    var batchEnd = _dateBatcher.GetBatchEndContaining(remainingProfiles[0].Timestamp);
                    var dataPointIndex = _dateBatcher.GetBatchIndex(lastDayOfFirstResult, batchEnd);

                    int batchProfileCount = CountWhileLessThanOrEqualToDate(remainingProfiles, batchEnd);
                    var batchProfiles = remainingProfiles.Slice(0, batchProfileCount);
                    remainingProfiles = remainingProfiles.Slice(batchProfileCount);

                    totalCalculator.TotaliseResponses(
                        filteredMetric,
                        quotaCell,
                        batchProfiles,
                        average.IncludeResponseIds,
                        results,
                        dataPointIndex,
                        weightedAverages);

                    BuildTotalsForAverage(results, dataPointIndex, quotaCell);
                }

                // Sanity check - This would indicate an issue in the above logic, which should be consuming all profiles within the time period
                if (remainingProfiles.Length > 0) throw new InvalidOperationException($"{remainingProfiles.Length} profiles out of bounds");
            }

            return results;
        }

        private static void BuildTotalsForAverage(EntityTotalsSeries[] results, int dataPointIndex, QuotaCell quotaCell)
        {
            for (int seriesIndex = 0, seriesCount = results.Length;
                 seriesIndex < seriesCount;
                 ++seriesIndex)
            {
                var resultsForInstance = results[seriesIndex].CellsTotalsSeries;
                var resultsForBatch = resultsForInstance[dataPointIndex];
                var resultForQuotaCell = resultsForBatch[quotaCell];

                if (resultForQuotaCell?.TotalForPeriodOnly.SampleSize > 0)
                {
                    resultForQuotaCell.TotalForAverage = resultForQuotaCell.TotalForPeriodOnly;
                    resultForQuotaCell.ResponseIdsForAverage = resultForQuotaCell.ResponseIdsForPeriod?.ToList(); //Defensive copy in case some later process mutates this
                }
            }
        }
    }
}
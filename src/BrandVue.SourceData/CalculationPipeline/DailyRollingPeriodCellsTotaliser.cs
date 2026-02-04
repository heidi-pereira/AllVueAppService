using System.Threading;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class DailyRollingPeriodCellsTotaliser : BasePeriodCellsTotaliser
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
            var numberOfDataPoints = (lastDayOfLastResult - lastDayOfFirstResult).Days + 1;

            var results = requestedInstances.CreateEmptyResults(
                lastDayOfFirstResult,
                IncrementByOneDay,
                numberOfDataPoints,
                allQuotaCells.Cells.Count);

            var totalCalculator = TotalCalculatorFactory.Create(filteredMetric, requestedInstances.EntityType);

            foreach (var populatedQuotaCell in profileResponseAccessor.GetResponses(desiredQuotaCells).WithinTimesInclusive(lastDayOfFirstResult, lastDayOfLastResult))
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool shouldContinueRollingAverage = false;
                DateTimeOffset currentDate = lastDayOfFirstResult;
                var remainingProfiles = populatedQuotaCell.Profiles.Span;
                for (int dataPointIndex = 0; dataPointIndex < numberOfDataPoints; ++dataPointIndex)
                {
                    if (shouldContinueRollingAverage)
                    {
                        currentDate = currentDate.AddDays(1);
                    }
                    else
                    {
                        if (remainingProfiles.Length == 0) break;
                        // Jump forward to where there's sample
                        currentDate = remainingProfiles[0].Timestamp;
                        dataPointIndex = (int)(currentDate - lastDayOfFirstResult).TotalDays;
                    }

                    int lengthOfBatch = CountWhileLessThanOrEqualToDate(remainingProfiles, currentDate);

                    if (lengthOfBatch > 0)
                    {
                        var forToday = remainingProfiles.Slice(0, lengthOfBatch);
                        remainingProfiles = remainingProfiles.Slice(lengthOfBatch);

                        totalCalculator.TotaliseResponses(
                            filteredMetric,
                            populatedQuotaCell.QuotaCell,
                            forToday,
                            average.IncludeResponseIds,
                            results,
                            dataPointIndex,
                            weightedAverages);
                    }

                    shouldContinueRollingAverage = BuildTotalsForAverage(average, results, dataPointIndex, populatedQuotaCell.QuotaCell);
                }

                // Sanity check - This would indicate an issue in the above logic, which should be consuming all profiles within the time period
                if (remainingProfiles.Length > 0) throw new InvalidOperationException($"{remainingProfiles.Length} profiles out of bounds");
            }

            return results;
        }

        private bool BuildTotalsForAverage(
            AverageDescriptor average,
            EntityTotalsSeries[] results,
            int dataPointIndex, QuotaCell quotaCell)
        {
            bool hadAnySample = false;
            for (int seriesIndex = 0, seriesCount = results.Length;
                 seriesIndex < seriesCount;
                 ++seriesIndex)
            {
                var series = results[seriesIndex].CellsTotalsSeries;
                var todaysResults = series[dataPointIndex];
                var previousResults = dataPointIndex > 0 ? series[dataPointIndex - 1] : null;
                var lastResultsFromPreviousPeriod = dataPointIndex >= average.NumberOfPeriodsInAverage
                    ? series[dataPointIndex - average.NumberOfPeriodsInAverage]
                    : null;

                var todaysQuotaCellResult = todaysResults[quotaCell];
                var previousQuotaCellResult = previousResults?[quotaCell];
                var lastPeriodQuotaCellResult = lastResultsFromPreviousPeriod?[quotaCell];

                var totalForAverage = new ResultSampleSizePair();
                var responseIds = average.IncludeResponseIds ? new List<int>() : null;

                if (previousQuotaCellResult != null)
                {
                    //Start from previous result
                    if(previousQuotaCellResult.TotalForAverage != null)
                    {
                        totalForAverage += previousQuotaCellResult.TotalForAverage;
                    }

                    AddResponseIds(responseIds, previousQuotaCellResult?.ResponseIdsForAverage);

                    //Remove the first result from that one
                    if (lastPeriodQuotaCellResult?.TotalForPeriodOnly != null)
                    {
                        totalForAverage -= lastPeriodQuotaCellResult.TotalForPeriodOnly;
                        if (lastPeriodQuotaCellResult.ResponseIdsForPeriod?.Count > 0) responseIds?.RemoveRange(0, lastPeriodQuotaCellResult.ResponseIdsForPeriod.Count);
                    }
                }

                // Add on the current result
                if (todaysQuotaCellResult?.TotalForPeriodOnly is { } t) totalForAverage += t;
                AddResponseIds(responseIds, todaysQuotaCellResult?.ResponseIdsForPeriod);

                if (totalForAverage.SampleSize > 0)
                {
                    todaysQuotaCellResult ??= todaysResults[quotaCell] = new Total();
                    todaysQuotaCellResult.TotalForAverage = totalForAverage;
                    todaysQuotaCellResult.ResponseIdsForAverage = responseIds;
                    hadAnySample = true; // Open window for rolling average, the next window may need to roll up sample from here
                }
            }

            return hadAnySample;
        }
    }
}

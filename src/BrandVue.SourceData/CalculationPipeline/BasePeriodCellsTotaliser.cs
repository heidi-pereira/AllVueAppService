using System.Threading;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Dates;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal abstract class BasePeriodCellsTotaliser : IPeriodTotaliser
    {
        public EntityTotalsSeries[] TotalisePerCell(IProfileResponseAccessor profileResponseAccessor,
            FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells indexOrderedDesiredQuotaCells,
            EntityWeightedDailyResults[] weightedAverages,
            IGroupedQuotaCells allQuotaCells,
            DateTimeOffset responsesStartDate,
            DateTimeOffset responsesEndDate,
            CancellationToken cancellationToken)
        {
            
            if (weightedAverages is not null && filteredMetric.Breaks?.Any() == true) throw new NotSupportedException("Calculating variance with breaks not yet supported");

            var results = new List<EntityTotalsSeries>();
            for (int periodIndex = 0; periodIndex < calculationPeriod.Periods.Length; periodIndex++)
            {
                CalculationPeriodSpan requestedPeriod = calculationPeriod.Periods[periodIndex];
                var firstResultDate = ResultDateCalculator.GetFirst(
                    average,
                    filteredMetric.Metric.StartDate,
                    responsesStartDate, requestedPeriod.StartDate).ToDateInstance();

                //
                //  ***Temporary Fix***
                //  See https://app.shortcut.com/mig-global/story/58728/fix-brandvue-overtime-charts-showing-data-points-from-the-future-attempt-3
                //  this will not work when the question was stop being asked sometime before the responseEndDate
                //

                var lastResultDate = ResultDateCalculator.GetLast(
                    responsesEndDate < requestedPeriod.EndDate ? responsesEndDate : requestedPeriod.EndDate,
                    average).ToDateInstance();

                EntityTotalsSeries[] spanResults;

                if ( (firstResultDate > lastResultDate) || (firstResultDate > responsesEndDate) )
                {
                    //  This will happen if we have, for example, a measure with a start date
                    //  too close to the end date (or even after the end date) to calculate
                    //  the required average: i.e., not enough data.
                    spanResults = requestedInstances.CreateEmptyResults(DateTimeOffset.MinValue, IncrementByOneDay);
                }
                else
                {
                    var weightedAveragesForPeriod = weightedAverages?.Select(x=>x.WeightedDailyResults[periodIndex].WeightedResult).ToArray();

                    spanResults = CalculateInternal(
                        profileResponseAccessor,
                        average,
                        filteredMetric,
                        requestedInstances,
                        indexOrderedDesiredQuotaCells,
                        firstResultDate,
                        lastResultDate,
                        weightedAveragesForPeriod,
                        allQuotaCells,
                        cancellationToken);
                }

                results.AddRange(RemoveOutlyingValues(spanResults, average));
            }

            return MergeResultsForDifferentPeriods(results);
        }

        private static EntityTotalsSeries[] RemoveOutlyingValues(EntityTotalsSeries[] results, AverageDescriptor average)
        {
            if (average.TotalisationPeriodUnit != TotalisationPeriodUnit.Day) return results;
            // For calculating the rolling average, we artificially extend our the start date so that we have enough days to roll up the average
            // Here, we remove those days as we no longer need them and they would result in too many/duplicate results.
            var unweightedResultsPerOutputResult = average.NumberOfPeriodsInAverage;
            return results.Select(r => new EntityTotalsSeries(
                        r.EntityInstance,
                        r.EntityType,
                        new CellsTotalsSeries(r.CellsTotalsSeries.Skip(unweightedResultsPerOutputResult - 1)
                            .ToArray())))
                    .ToArray();
        }

        private static EntityTotalsSeries[] MergeResultsForDifferentPeriods(IReadOnlyCollection<EntityTotalsSeries> results)
        {
            // In the case of over-time, there will only be one period.
            // In the case of period-on-period comparisons, there will be more than one period (potentially non-contiguous in the case of same period last year.
            // Merge all of these period period results into one
            var grouped = results.GroupBy(r => (r.EntityInstance, r.EntityType)).Select(groupedByInstance =>
                new EntityTotalsSeries(
                    groupedByInstance.Key.EntityInstance, 
                    groupedByInstance.Key.EntityType,
                    new CellsTotalsSeries(groupedByInstance.SelectMany(instanceResults=>instanceResults.CellsTotalsSeries).ToArray())))
                .ToArray();

            return grouped;
        }

        protected abstract EntityTotalsSeries[] CalculateInternal(IProfileResponseAccessor profileResponseAccessor,
            AverageDescriptor average,
            FilteredMetric filteredMetric,
            TargetInstances requestedInstances,
            IGroupedQuotaCells desiredQuotaCells,
            DateTimeOffset lastDayOfFirstResult,
            DateTimeOffset lastDayOfLastResult,
            double[] weightedAverages,
            IGroupedQuotaCells allQuotaCells,
            CancellationToken cancellationToken);

        protected DateTimeOffset IncrementByOneDay(DateTimeOffset source)
        {
            return source.AddDays(1).ToDateInstance();
        }

        protected static void InitialiseFirstTotals(Total quotaCellTotal)
        {
            if (quotaCellTotal != null)
            {
                quotaCellTotal.ResponseIdsForAverage = quotaCellTotal.ResponseIdsForPeriod;
                quotaCellTotal.TotalForAverage = quotaCellTotal.TotalForPeriodOnly;
            }
        }

        protected static void AddResponseIds(List<int> responseIds, IList<int> responseIdsForAverage)
        {
            if (responseIds is null || responseIdsForAverage is null) return;
            responseIds.AddRange(responseIdsForAverage);
        }

        protected static int CountWhileLessThanOrEqualToDate(ReadOnlySpan<IProfileResponseEntity> remainingProfiles, DateTimeOffset currentDate)
        {
            var lengthOfBatch = 0;
            while (lengthOfBatch < remainingProfiles.Length && remainingProfiles[lengthOfBatch].Timestamp <= currentDate)
            {
                lengthOfBatch++;
            }

            return lengthOfBatch;
        }
    }
}

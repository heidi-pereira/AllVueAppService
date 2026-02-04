using BrandVue.EntityFramework;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Models
{
    public static class LowSampleExtensions
    {
        private static readonly AppSettings AppSettings = new AppSettings();
        public static int LowSampleThreshold { get; } = AppSettings.GetGlobalSetting<int>("LowSampleForBrand");

        public static LowSampleSummary[] LowSampleSummaries(this IEnumerable<MetricWeightedDailyResult> results, DateTimeOffset subsetStartDate, string name)
                => results.Where(result => result.WeightedDailyResult.HasLowSample(subsetStartDate))
                        .Select(result =>new LowSampleSummary {Name = name, Metric = result.MetricName, DateTime = result.WeightedDailyResult.Date})
                        .ToArray();

        public static LowSampleSummary[] LowSampleSummaries(this IEnumerable<WeightedDailyResult> results, DateTimeOffset subsetStartDate, string name)
                => results.Where(result => result.HasLowSample(subsetStartDate))
                    .Select(result => new LowSampleSummary { Name = name})
                    .ToArray();

        private static LowSampleSummary[] LowSampleSummaries(this IEnumerable<WeightedDailyResult> results, DateTimeOffset subsetStartDate, EntityInstance entityInstance, string subsetId, string measure)
                => results.Where(result => result.HasLowSample(entityInstance, subsetId, subsetStartDate))
                        .Select(result => new LowSampleSummary {EntityInstanceId = entityInstance.Id, Metric = measure, DateTime = result.Date})
                        .ToArray();

        public static LowSampleSummary[] LowSampleSummaries(this BrokenDownResults[] resultsByMeasure, DateTimeOffset subsetStartDate, string name)
                => resultsByMeasure.Where(result => result.HasLowSample(subsetStartDate))
                    .Select(result=> new LowSampleSummary { Name = name, DateTime = result.Total[0].Date }) //Assume only 1 item in array eg only one average)
                    .ToArray();


        private static bool HasLowSample(this BrokenDownResults result, DateTimeOffset subsetStartDate)
            => result.ByAgeGroup.Any(x => x.WeightedDailyResults.Any(y=>y.HasLowSample(subsetStartDate)))
                                     || result.ByGender.Any(x => x.WeightedDailyResults.Any(y => y.HasLowSample(subsetStartDate)))
                                     || result.ByRegion.Any(x => x.WeightedDailyResults.Any(y => y.HasLowSample(subsetStartDate)))
                                     || result.BySocioEconomicGroup.Any(x => x.WeightedDailyResults.Any(y => y.HasLowSample(subsetStartDate))) ;

        public static LowSampleSummary[] LowSampleSummaries(this IEnumerable<EntityWeightedDailyResults> results, string subsetId, DateTimeOffset subsetStartDate)
            => results.Where(result => result.WeightedDailyResults.HasLowSample(result.EntityInstance, subsetId, subsetStartDate)).
                Select(result => new LowSampleSummary{EntityInstanceId = result.EntityInstance?.Id}).ToArray();

        public static LowSampleSummary[] LowSampleSummaries(this IEnumerable<EntityWeightedDailyResults> results, string subsetId, DateTimeOffset subsetStartDate, string measure)
            => results.Where(result => result.WeightedDailyResults.HasLowSample(result.EntityInstance, subsetId, subsetStartDate)).
                Select(result => new LowSampleSummary { EntityInstanceId = result.EntityInstance?.Id, DateTime = result.WeightedDailyResults[0]?.Date, Metric = measure }).ToArray();

        public static LowSampleSummary[] EntityInstanceIdsWithLowSample(this EntityCategoryResults results, string subsetId, DateTimeOffset subsetStartDate)
        {
            bool hasLowSample = results.Results.Any(result => result.WeightedDailyResults.HasLowSample(results.EntityInstance, subsetId, subsetStartDate));
            var instancesWithLowSample = hasLowSample ? new[] {results.EntityInstance} : Array.Empty<EntityInstance>();
            return FromEntityInstanceIds(instancesWithLowSample.DistinctEntityInstanceIds());
        }

        public static LowSampleSummary[] LowSampleSummaries(this BrokenDownResults[] results, string subsetId, DateTimeOffset subsetStartDate)
        {
            var items = results
                .SelectMany(result => result.Total
                    .Where((total, index) =>
                        result.ByAgeGroup.Any(r => r.WeightedDailyResults[index].HasLowSample(result.EntityInstance, subsetId, subsetStartDate)) ||
                        result.ByGender.Any(r => r.WeightedDailyResults[index].HasLowSample(result.EntityInstance, subsetId, subsetStartDate)) ||
                        result.ByRegion.Any(r => r.WeightedDailyResults[index].HasLowSample(result.EntityInstance, subsetId, subsetStartDate)) ||
                        result.BySocioEconomicGroup.Any(r => r.WeightedDailyResults[index].HasLowSample(result.EntityInstance, subsetId, subsetStartDate))
                    )
                    .Select(item => new { item.Date, MeasureName = result.Measure.Name, EntityInstance = result.EntityInstance })
            );

            return items.Select(item => item.EntityInstance != null
                ? new LowSampleSummary { EntityInstanceId = item.EntityInstance.Id, DateTime = item.Date }
                : new LowSampleSummary { DateTime = item.Date, Metric = item.MeasureName }).ToArray();
        }

        public static LowSampleSummary[] LowSampleSummaries(this MultiMetricSeries results, string[] measureNames, string subsetId, DateTimeOffset subsetStartDate) =>
            results.OrderedData.SelectMany((od, index) => od.LowSampleSummaries(subsetStartDate, results.EntityInstance, subsetId, measureNames[index])).ToArray();

        public static LowSampleSummary[] LowSampleSummaries(this MultiMetricSeries[] results, string[] measureNames, string subsetId, DateTimeOffset subsetStartDate) =>
            results.SelectMany(r => r.LowSampleSummaries(measureNames, subsetId, subsetStartDate)).ToArray();

        public static LowSampleSummary[] EntityInstancesWithLowSample(this WeightedDailyResult[][] results,
            EntityInstance[] entityInstances,
            string subsetId,
            DateTimeOffset subsetStartDate)
        {
            var result = new List<int>();
            for (int index = 0; index < entityInstances.Length; index++)
            {
                var entityInstanceId = entityInstances[index];
                foreach (var timePeriodResult in results)
                {
                    var weightedDailyResultForInstance = timePeriodResult[index];

                    if (weightedDailyResultForInstance.HasLowSample(entityInstanceId, subsetId, subsetStartDate))
                    {
                        result.Add(entityInstanceId.Id);
                        break;
                    }
                }
            }
            return FromEntityInstanceIds(result.ToArray());
        }

        public static LowSampleSummary[] EntityInstanceIdsWithLowSample(this IEnumerable<ScorecardPerformanceMetricResult> results,
            int entityInstanceId,
            IEntityRepository entityRepository, EntityType entityType, Subset subset, DateTimeOffset subsetStartDate)
            => FromEntityInstanceIds(results.SelectMany(result =>
                    result.PeriodResults.HasLowSample(entityInstanceId, entityRepository, entityType, subset, subsetStartDate) ? new[] {entityInstanceId} : Array.Empty<int>()
               ).Distinct().ToArray());

        public static LowSampleSummary[] EntityInstanceIdsWithLowSampleForAverage(this IEnumerable<ScorecardPerformanceCompetitorsMetricResult> results,
            int entityInstanceId,
            IEntityRepository entityRepository, EntityType entityType, Subset subset, DateTimeOffset subsetStartDate)
        {
            var entityInstance = entityRepository.TryGetInstance(subset, entityType.Identifier, entityInstanceId, out var instance) ? instance : null;
            return FromEntityInstanceIds(results.SelectMany(result =>
                result.CompetitorData
                    .Where(p => p.Result.HasLowSample(entityInstance, subset.Id,
                        subsetStartDate)).Select(p => p.EntityInstance)
            ).DistinctEntityInstanceIds().ToArray());
        }

        public static LowSampleSummary[] EntityInstanceIdsWithLowSample(this IEnumerable<ScorecardVsKeyCompetitorsMetricResults> results,
            int entityInstanceId,
            IEntityRepository entityRepository, EntityType entityType, Subset subset, DateTimeOffset subsetStartDate)
        {
            var entityInstance = entityRepository.TryGetInstance(subset, entityType.Identifier, entityInstanceId, out var instance) ? instance : null;
            return FromEntityInstanceIds(results.SelectMany(result =>
                (result.ActiveEntityResult.Current.HasLowSample(entityInstance,
                    subset.Id, subsetStartDate) || result.ActiveEntityResult.Previous.HasLowSample(entityInstance, subset.Id, subsetStartDate)
                    ? new[] { entityInstanceId }
                    : Array.Empty<int>()).Union(
                    result.KeyCompetitorResults.Where(p =>
                            p.Current.HasLowSample(entityInstance, subset.Id,
                                subsetStartDate) || p.Previous.HasLowSample(entityInstance, subset.Id, subsetStartDate))
                        .Select(p => p.EntityInstance).DistinctEntityInstanceIds())).Distinct().ToArray());
        }

        public static LowSampleSummary[] EntityInstanceIdsWithLowSample(this IEnumerable<ResultsForMeasure> results, string subsetId, DateTimeOffset subsetStartDate)
            => results.SelectMany(result => result.Data.LowSampleSummariesWithDates(subsetId, subsetStartDate)).ToArray();

        public static LowSampleSummary[] LowSampleSummariesWithDates(this IEnumerable<EntityWeightedDailyResults> results, string subsetId, DateTimeOffset subsetStartDate)
            => results.Where(result => result.WeightedDailyResults.HasLowSample(result.EntityInstance, subsetId, subsetStartDate)).
                Select(result => new LowSampleSummary { EntityInstanceId = result.EntityInstance?.Id, DateTime = result.WeightedDailyResults[0].Date }).ToArray();


        private static int[] DistinctEntityInstanceIds(this IEnumerable<EntityInstance> entityInstances)
        {
            return entityInstances
                .Where(b => b != null) //For market metrics, instance is null, just don't warn user about such cases for now until we've designed a UI
                .Select(b => b.Id).Distinct().ToArray();
        }

        private static bool HasLowSample(this IEnumerable<WeightedDailyResult> results, int? entityInstanceId,
            IEntityRepository entityRepository, EntityType entityType, Subset subset, DateTimeOffset subsetStartDate)
        {
            var entityInstance = entityInstanceId != null && entityRepository.TryGetInstance(subset, entityType.Identifier, entityInstanceId.Value, out var instance) ? instance : null;
            return results.Any(result =>
                result.HasLowSample(entityInstance, subset.Id, subsetStartDate));
        }

        private static bool HasLowSample(this IEnumerable<WeightedDailyResult> results, EntityInstance entityInstance, string subsetId, DateTimeOffset subsetStartDate) =>
            results.Any(result => result.HasLowSample(entityInstance, subsetId, subsetStartDate));

        private static LowSampleSummary[] FromEntityInstanceIds(int[] entityInstanceIds)
        {
            var summaries = new List<LowSampleSummary>();
            summaries.AddRange(entityInstanceIds.Select(x => new LowSampleSummary { EntityInstanceId = x }));
            return summaries.ToArray();
        }
        private static bool HasLowSample(this WeightedDailyResult result, DateTimeOffset subsetStartDate)
        {
            if (result == null)
            {
                return false;
            }
            return result.UnweightedSampleSize <= LowSampleThreshold && result.Date >= subsetStartDate;
        }

        private static bool HasLowSample(this WeightedDailyResult result, EntityInstance entityInstance, string subsetId, DateTimeOffset subsetStartDate)
        {
            if (result == null)
            {
                return false;
            }
            var startDateForEntity = entityInstance?.StartDateForSubset(subsetId);
            var minDateForSample = startDateForEntity ?? subsetStartDate;

            return result.UnweightedSampleSize <= LowSampleThreshold && result.Date >= minDateForSample;
        }
    }
}
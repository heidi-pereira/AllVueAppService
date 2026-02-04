using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;

namespace BrandVue.Models
{
    public static class HasDataExtensions
    {
        public static bool HasData(
            this ICollection<BrokenDownResults> results)
                => results.Any(r => r.Total.HasData(true));

        public static bool HasData(
            this ICollection<WeightedDailyResult> results,
            bool countLastDayOnly = false)
        {
            if (results == null)
            {
                return false;
            }

            if (countLastDayOnly)
            {
                return (results.LastOrDefault()?.UnweightedSampleSize ?? 0) > 0;
            }

            uint sampleSize = 0;
            return results.Where(
                current =>
                {
                    sampleSize += current?.UnweightedSampleSize ?? 0;
                    return sampleSize > 0;
                }).Any();
        }

        private static bool HasData(this WeightedDailyResult result)
        {
            return (result?.UnweightedSampleSize ?? 0) > 0;
        }

        public static bool HasData(this ICollection<CategoryResults> results)
            => results.Any(
                categoryResult =>
                    categoryResult.WeightedDailyResults.HasData());

        public static bool HasData(
                this ICollection<EntityWeightedDailyResults> results,
                bool countLastDayOnly = false)
            => results.Any(
                result
                    => result
                        .WeightedDailyResults
                        .HasData(countLastDayOnly));

        public static bool HasData(
                this ResultsForMeasure results,
                bool countLastDayOnly = false)
            => results.Data.HasData(countLastDayOnly);

        public static bool HasData(
                this MultiMetricSeries results,
                bool countLastDayOnly = false)
            => results.OrderedData.Any(
                resultsForMeasure
                    => resultsForMeasure.HasData(countLastDayOnly));

        public static bool HasData(
                this ICollection<MultiMetricSeries> results,
                bool countLastDayOnly = false)
            => results.Any(
                series
                    => series.HasData(countLastDayOnly));

        public static bool HasData(
            this ICollection<ScorecardPerformanceMetricResult> results)
            => results.Any(result => result.HasData());

        public static bool HasData(
            this ScorecardPerformanceMetricResult results)
            => results.PeriodResults.HasData();

        public static bool HasData(
            this ICollection<ScorecardPerformanceCompetitorsMetricResult> results)
            => results.Any(peerData => peerData.HasData());

        public static bool HasData(
            this ScorecardPerformanceCompetitorsMetricResult results)
            => results.CompetitorData.Any(
                peerData
                    => peerData.Result?.UnweightedSampleSize > 0);


        public static bool HasData(
            this ScorecardVsKeyCompetitorsMetricEntityResult result)
            => result.Current?.UnweightedSampleSize > 0
               || result.Previous?.UnweightedSampleSize > 0;

        public static bool HasData(
                this ICollection<ScorecardVsKeyCompetitorsMetricEntityResult> results)
            => results.Any(result => result.HasData());

        public static bool HasData(
                this ScorecardVsKeyCompetitorsMetricResults results)
            => results.ActiveEntityResult.HasData()
               || results.KeyCompetitorResults.HasData();

        public static bool HasData(
            this ICollection<ScorecardVsKeyCompetitorsMetricResults> results)
            => results.Any(result => result.HasData());

        public static bool HasData(this List<RankingTableResult> results) =>
            results.Any(r => r.CurrentWeightedDailyResult.HasData());

        public static bool HasData(this IList<RankingOvertimeResult> results) =>
            results.Count > 0;
    }
}
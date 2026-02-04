using BrandVue.SourceData.Averages;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal static class TargetInstancesResultExtensions
    {
        public static EntityTotalsSeries[] CreateEmptyResults(this TargetInstances requestedInstances,
            IEnumerable<(int? WaveInstanceId, DateTimeOffset EndDate, int QuotaCellCount)> periods)
        {
            if (requestedInstances.EntityType.IsProfile)
            {
                var series = CreateSeries(periods);
                return new[] {new EntityTotalsSeries(null, requestedInstances.EntityType, series)};
            }

            return requestedInstances
                .OrderedInstances
                .Select(i => new EntityTotalsSeries(i, requestedInstances.EntityType, CreateSeries(periods)))
                .ToArray();
        }

        public static EntityTotalsSeries[] CreateEmptyResults(
            this TargetInstances requestedInstances,
            DateTimeOffset startDate,
            Func<DateTimeOffset, DateTimeOffset> dateIncrementor,
            int numberOfDataPoints = 0,
            int numberOfQuotaCells = 0)
        {
            var dates = new List<DateTimeOffset>();
            var currentDate = startDate;

            for (int dataPointIndex = 0; dataPointIndex < numberOfDataPoints; ++dataPointIndex)
            {
                dates.Add(currentDate);
                currentDate = dateIncrementor(currentDate);
            }

            return requestedInstances.CreateEmptyResults(dates, numberOfQuotaCells);
        }

        public static EntityTotalsSeries[] CreateEmptyResults(this TargetInstances requestedInstances, IEnumerable<DateTimeOffset> periods, int numberOfQuotaCells) =>
            requestedInstances.CreateEmptyResults(periods.Select(p => (default(int?), p, numberOfQuotaCells)));

        private static CellsTotalsSeries CreateSeries(IEnumerable<(int? WaveInstanceId, DateTimeOffset EndDate, int QuotaCellCount)> periods)
        {
            var resultsByDate = periods
                .Select(period => new CellTotals(period.EndDate, period.WaveInstanceId))
                .ToArray();

            return new CellsTotalsSeries(resultsByDate);
        }
    }
}   

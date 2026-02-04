using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Replicates shortcut mqrep behaviour where WeightedSampleCount is used to weighted average individual months. This gives less representative results than using the base measure would.
    /// Creates one result per calendar period, e.g. 3 months become a single data point at the end of the quarter.
    /// </summary>
    public class RepCompatiblePeriodAggregator : BaseResultPeriodAggregator
    {
        private readonly AverageDescriptor _average;

        public RepCompatiblePeriodAggregator(AverageDescriptor average, ILogger<RepCompatiblePeriodAggregator> logger) : base(logger)
        {
            _average = average ?? throw new ArgumentNullException(
                "Cannot create RepCompatibleMonthlyMutator with null average.");
        }

        public override IList<WeightedDailyResult> AggregateIntoResults(Measure measure, IList<WeightedTotal> weightedDailyResults)
        {
            switch (_average.TotalisationPeriodUnit)
            {
                case TotalisationPeriodUnit.Month:
                    weightedDailyResults = AggregateOverPeriod(weightedDailyResults);
                    break;

                default:
                    break;
            }

            return CalculateResultsFromTotals(measure, weightedDailyResults);
        }

        private DateTimeOffset GetPeriodEnd(DateTimeOffset source)
        {
            switch (_average.MakeUpTo)
            {
                case MakeUpTo.QuarterEnd:
                    return source.GetLastDayOfQuarter();

                case MakeUpTo.HalfYearEnd:
                    return source.GetLastDayOfHalfYear();

                case MakeUpTo.CalendarYearEnd:
                    return source.GetLastDayOfYear();

                default:
                    throw new InvalidOperationException($@"Cannot mutate results for unsupported make up to date in {_average}");
            }
        }

        private IList<WeightedTotal> AggregateOverPeriod(IList<WeightedTotal> source)
        {
            var target = new List<WeightedTotal>();

            DateTimeOffset? currentPeriodEnd = null;
            WeightedTotal transmogrified = null;
            foreach (WeightedTotal result in source)
            {
                if (currentPeriodEnd == null)
                {
                    currentPeriodEnd = GetPeriodEnd(result.Date);
                    transmogrified = new WeightedTotal(
                        currentPeriodEnd.Value);
                }

                var newPeriodEnd = GetPeriodEnd(result.Date);

                if (newPeriodEnd != currentPeriodEnd)
                {
                    _logger.LogDebug("Adding {@IntermediateWeightedDailyResults} to final {@AverageMakeUpTo} results",
                        transmogrified, _average.MakeUpTo);

                    target.Add(transmogrified);

                    currentPeriodEnd = newPeriodEnd;

                    transmogrified = new WeightedTotal(
                        currentPeriodEnd.Value);
                }

                _logger.LogDebug("Including {@IntermediateWeightedDailyResults} in {@AverageMakeUpTo} results calculation",
                    result, _average.MakeUpTo);

                transmogrified.WeightedValueTotal += result.WeightedValueTotal;
                transmogrified.WeightedSampleCount += result.WeightedSampleCount;
                transmogrified.UnweightedSampleCount += result.UnweightedSampleCount;
                if (result.ResponseIdsForDay != null)
                {
                    transmogrified.ResponseIdsForDay.AddRange(result.ResponseIdsForDay);
                }
            }

            if (transmogrified != null
                && (target.Count == 0 || target[target.Count - 1] != transmogrified))
            {
                _logger.LogDebug("Adding {@IntermediateWeightedDailyResults} as final quarterly result",
                    transmogrified);

                target.Add(transmogrified);
            }

            return target;
        }
    }
}

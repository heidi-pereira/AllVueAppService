using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Calculation
{
    public class NoOpResultPeriodAggregator : BaseResultPeriodAggregator
    {
        public NoOpResultPeriodAggregator(ILogger<NoOpResultPeriodAggregator> logger) : base(logger) { }

        public override IList<WeightedDailyResult> AggregateIntoResults(
            Measure measure,
            IList<WeightedTotal> weightedDailyResults)
        {
            //  No! I will not transform the results
            //  in an unexpected or magical way!
            return CalculateResultsFromTotals(measure, weightedDailyResults);
        }
    }
}

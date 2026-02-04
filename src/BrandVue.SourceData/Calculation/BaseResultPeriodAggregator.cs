using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Calculation
{
    public abstract class BaseResultPeriodAggregator : IResultPeriodAggregator
    {
        protected readonly ILogger _logger;

        protected BaseResultPeriodAggregator(ILogger logger)
        {
            _logger = logger;
        }

        public abstract IList<WeightedDailyResult> AggregateIntoResults(
            Measure measure,
            IList<WeightedTotal> weightedDailyResults);

        protected IList<WeightedDailyResult> CalculateResultsFromTotals(
            Measure measure,
            IList<WeightedTotal> source)
        {
            var count = source.Count;
            var target = new List<WeightedDailyResult>(count);

            for (var index = 0; index < count; ++index)
            {
                var intermediate = source[index];
                var final = CreateWeightedResult(measure, intermediate);
                target.Add(final);
            }

            return target;
        }

        private static WeightedDailyResult CreateWeightedResult(Measure measure, WeightedTotal intermediate)
        {
            var childResults = intermediate.ChildResults?.Select(r => CreateWeightedResult(measure, r)).ToArray();
            var final = new WeightedDailyResult(intermediate.Date)
            {
                WeightedValueTotal = intermediate.WeightedValueTotal,
                UnweightedValueTotal = intermediate.UnweightedValueTotal,
                UnweightedSampleSize = intermediate.UnweightedSampleCount,
                WeightedSampleSize = intermediate.WeightedSampleCount,
                ResponseIdsForDay = intermediate.ResponseIdsForDay,
                ChildResults = childResults
            };

            if (intermediate.WeightedSampleCount > 0)
            {
                final.WeightedResult = Mutate(measure, final, intermediate);
            }
            else
            {
                final.WeightedResult = intermediate.WeightedValueTotal;
            }

            return final;
        }

        private static double Mutate(Measure measure, WeightedDailyResult final, WeightedTotal intermediate)
        {
            switch (measure.CalculationType)
            {
                case CalculationType.Average:
                case CalculationType.YesNo:
                    return
                        intermediate.WeightedValueTotal
                        / intermediate.WeightedSampleCount;

                case CalculationType.NetPromoterScore:
                    return
                        intermediate.WeightedValueTotal
                        / intermediate.WeightedSampleCount * 100.0;

                default:
                    throw new InvalidOperationException(
                        $@"Invalid calculation type {
                                measure.CalculationType
                            } when calculating weighted values for measure {
                                measure.Name}.");
            }
        }
    }
}

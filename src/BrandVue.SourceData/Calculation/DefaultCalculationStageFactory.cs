using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Calculation
{
    internal class DefaultCalculationStageFactory : ICalculationStageFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly BasicResultsScaler _scaler;
        private readonly BasicResultsNormaliser _normalizer;

        public DefaultCalculationStageFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            _scaler = new BasicResultsScaler();
            _normalizer = new BasicResultsNormaliser(loggerFactory.CreateLogger<BasicResultsNormaliser>());
        }

        public EntityWeightedDailyResults[] CreateFinalResult(Subset subset, AverageDescriptor average,
            CalculationPeriod calculationPeriod, Measure measure, EntityWeightedTotalSeries[] intermediates, TargetInstances[] instances)
        {
            //  If we're calculating quarterly values, and have chosen to weight each month
            //  individually within the quarter, the mutator will calculate the quarterly
            //  value as the average of the monthly values within the quarter, and the
            //  quarterly sample count will be the sum of the monthly sample counts.
            //
            //  If we're calculating quarterly values, and have chosen to weight over the
            //  whole quarter instead of weighting each month individually, the mutator
            //  will be a no-op.
            var periodAggregator = CreateResultPeriodAggregatorFor(average, calculationPeriod);
            var target = new EntityWeightedDailyResults[intermediates.Length];

            for (int index = 0, count = intermediates.Length; index < count; ++index)
            {
                var current = intermediates[index];

                var weightedResults = periodAggregator.AggregateIntoResults(
                    measure,
                    current.Series);

                weightedResults = _scaler.Scale(measure, weightedResults);

                weightedResults = _normalizer.Normalise(
                    measure,
                    current.EntityInstance,
                    weightedResults);

                target[index] = new EntityWeightedDailyResults(
                    current.EntityInstance,
                    weightedResults);

                var startDateResultsCleaner = new StartDateResultsCleaner();
                startDateResultsCleaner.RemoveResultsBeforeStartDate(target[index], subset, average);
                startDateResultsCleaner.RemoveResultsBeforeStartDateForTargetInstances(target[index], subset, average, instances);
            }
            return target;
        }

        private IResultPeriodAggregator CreateResultPeriodAggregatorFor(AverageDescriptor average,
            CalculationPeriod calculationPeriod)
        {
            switch (average.TotalisationPeriodUnit)
            {
                case TotalisationPeriodUnit.Day:
                case TotalisationPeriodUnit.All:
                    return new NoOpResultPeriodAggregator(_loggerFactory.CreateLogger<NoOpResultPeriodAggregator>());

                case TotalisationPeriodUnit.Month:
                    switch (average.MakeUpTo)
                    {
                        case MakeUpTo.QuarterEnd:
                        case MakeUpTo.HalfYearEnd:
                        case MakeUpTo.CalendarYearEnd:
                            //  TODO: Bart - average.WeightAcross - absolutely needed - but would we ever do AllPeriods for monthly?
                            //  TODO: Bart - average.AverageStrategy - mean of periods support
                            return new RepCompatiblePeriodAggregator(average, _loggerFactory.CreateLogger<RepCompatiblePeriodAggregator>());

                        //  TODO: Bart - need to support monthly over 12 months style;
                        //  i.e., we're making up to the end of every month with that
                        //  month plus 11 preceding months
                        case MakeUpTo.MonthEnd:
                            return new RepCompatibleMultiMonthPeriodAggregator(average, calculationPeriod.Periods.Length, _loggerFactory.CreateLogger<RepCompatibleMultiMonthPeriodAggregator>());

                        case MakeUpTo.Day:
                        default:
                            throw new InvalidOperationException(
                                $@"Cannot make up to anything other than quarter end, calendar year end, or month end for averages with a monthly totalisation period. Invalid average: {
                                    JsonConvert.SerializeObject(average)}");
                    }
                default:
                    throw new ArgumentException(
                        $@"Cannot create IWeightedResultMutator for unsupported totalisation period in {
                            average}");

            }
        }
    }
}

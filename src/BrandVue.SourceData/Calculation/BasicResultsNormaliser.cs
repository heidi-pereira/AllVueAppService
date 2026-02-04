using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Calculation
{
    public class BasicResultsNormaliser : IResultsNormaliser
    {
        private readonly ILogger _logger;

        public BasicResultsNormaliser(ILogger<BasicResultsNormaliser> logger)
        {
            _logger = logger;
        }

        public IList<WeightedDailyResult> Normalise(
            Measure measure,
            EntityInstance entityInstance,
            IList<WeightedDailyResult> weightedResults)
        {
            if (measure.PreNormalisationMinimum == null
                || measure.PreNormalisationMaximum == null
                || measure.Minimum == null
                || measure.Maximum == null
                || (measure.Minimum == measure.PreNormalisationMinimum
                    && measure.Maximum == measure.PreNormalisationMaximum))
            {
                return weightedResults;
            }

            var oldMinimum = measure.PreNormalisationMinimum.Value;
            var oldMaximum = measure.PreNormalisationMaximum.Value;
            var oldRangeSize = oldMaximum - oldMinimum;

            var newMinimum = measure.Minimum.Value;
            var newMaximum = measure.Maximum.Value;
            var newRangeSize = newMaximum - newMinimum;

            var multiplier = ((double)newRangeSize) / oldRangeSize;

            foreach (var result in weightedResults)
            {
                WarnIfOutOfBounds(measure, entityInstance, result, oldMinimum, oldMaximum, true);

                result.WeightedResult = newMinimum + (result.WeightedResult - oldMinimum) * multiplier;

                WarnIfOutOfBounds(measure, entityInstance, result, newMinimum, newMaximum, false);
            }

            return weightedResults;
        }

        private void WarnIfOutOfBounds(
            Measure measure,
            EntityInstance entityInstance,
            WeightedDailyResult result,
            double expectedMinimum,
            double expectedMaximum,
            bool isPreNormalisation)
        {
            var prePost = isPreNormalisation ? "Pre" : "Post";
            if ((result.UnweightedSampleSize > 0) && (result.WeightedResult < expectedMinimum || result.WeightedResult > expectedMaximum))
            {
                _logger.LogWarning("{NormalisationType}-normalisation value for measure '{MeasureName}' calculated for entityInstance '{BrandName}' " +
                                   "falls outside expected range {ExpectedMinimum} <= {value} <= {ExpectedMaximum}: #{SampleSize} {@WeightedDailyResult}",
                    prePost, measure.Name, entityInstance.Name, expectedMinimum, result.WeightedResult, expectedMaximum, result.UnweightedSampleSize, result);
            }
        }
    }
}

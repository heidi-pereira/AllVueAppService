using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation
{
    public class BasicResultsScaler : IResultsScaler
    {
        public IList<WeightedDailyResult> Scale(
            Measure measure,
            IList<WeightedDailyResult> weightedResults)
        {
            if (measure.ScaleFactor == null)
            {
                return weightedResults;
            }

            var scaleFactor = measure.ScaleFactor.Value;
            foreach (var result in weightedResults)
            {
                result.WeightedResult *= scaleFactor;
            }

            return weightedResults;
        }
    }
}

using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation
{
    interface IResultsScaler
    {
        IList<WeightedDailyResult> Scale(
            Measure measure,
            IList<WeightedDailyResult> weightedResults);
    }
}

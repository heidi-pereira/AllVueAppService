using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation
{
    /// <summary>
    /// Used for transforming native results. The current use case
    /// for this is for calculating quarterly measure values where
    /// the months must be weighted individually.
    /// </summary>
    public interface IResultPeriodAggregator
    {
        IList<WeightedDailyResult> AggregateIntoResults(
            Measure measure,
            IList<WeightedTotal> weightedDailyResults);
    }
}

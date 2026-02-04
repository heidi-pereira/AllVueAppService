using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation
{
    interface IResultsNormaliser
    {
        IList<WeightedDailyResult> Normalise(
            Measure measure,
            EntityInstance entityInstance,
            IList<WeightedDailyResult> weightedResults);
    }
}

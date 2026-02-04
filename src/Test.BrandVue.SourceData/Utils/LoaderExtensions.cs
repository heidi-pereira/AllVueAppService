using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;

namespace Test.BrandVue.SourceData.Utils
{
    public static class LoaderExtensions
    {
        public static async Task<EntityWeightedDailyResults[]> Calculate(this BrandVueDataLoader brandVueDataLoader, Subset subset, CalculationPeriod period, AverageDescriptor averageDescriptor,
            Measure measure, TargetInstances requestedInstances)
        {

            var desiredQuotaCells = brandVueDataLoader.RespondentRepositorySource.GetForSubset(subset).WeightedCellsGroup;
            var weighted = await brandVueDataLoader.Calculator.Calculate(FilteredMetric.Create(measure, [], subset, new AlwaysIncludeFilter()),
                period,
                averageDescriptor,
                requestedInstances, desiredQuotaCells, false, CancellationToken.None);
            return weighted;
        }
    }
}
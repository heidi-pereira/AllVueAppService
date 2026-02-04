using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation;

internal interface ICalculationStageFactory
{
    EntityWeightedDailyResults[] CreateFinalResult(Subset subset, AverageDescriptor average,
        CalculationPeriod calculationPeriod, Measure measure, EntityWeightedTotalSeries[] intermediates, TargetInstances[] instances);
}
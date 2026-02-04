using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline;

public interface ITextCountCalculator
{
    Task<EntityWeightedDailyResults[]> CalculateTextCounts(
        Subset datasetSelector,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        Measure measure,
        IGroupedQuotaCells quotaCells,
        IFilter filter,
        TargetInstances[] filterInstances,
        TargetInstances requestedInstances,
        CancellationToken cancellationToken);
}
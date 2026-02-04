using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline;

/// <summary>
/// Raw data MUST be loaded into memory before using this calculator
/// </summary>
internal class TotalisationOrchestrator : ITotalisationOrchestrator
{
    protected readonly IRespondentRepositorySource _respondentRepositorySource;
    private readonly IProfileResponseAccessor _profileResponseAccessor;

    public TotalisationOrchestrator(IRespondentRepositorySource respondentRepositorySource,
        IProfileResponseAccessor profileResponseAccessor)
    {
        _respondentRepositorySource = respondentRepositorySource;
        _profileResponseAccessor = profileResponseAccessor;
    }

    public EntityTotalsSeries[] Totalise(FilteredMetric filteredMetric,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        TargetInstances requestedInstances,
        IGroupedQuotaCells indexOrderedQuotaCells,
        EntityWeightedDailyResults[] weightedAverages, CancellationToken cancellationToken)
    {
        var totalizer = TotaliserFactory.Create(average);

        var allQuotaCells = _respondentRepositorySource.GetForSubset(filteredMetric.Subset).AllCellsGroup;
        var unweighted = totalizer.TotalisePerCell(
            _profileResponseAccessor, filteredMetric, calculationPeriod, average, requestedInstances,
            indexOrderedQuotaCells, weightedAverages, allQuotaCells, _profileResponseAccessor.StartDate,
            _profileResponseAccessor.EndDate, cancellationToken);

        return unweighted;
    }
}
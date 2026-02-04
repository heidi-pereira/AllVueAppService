using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.CalculationPipeline;

public class InMemoryTotalisationOrchestrator : IAsyncTotalisationOrchestrator
{
    protected readonly IRespondentRepositorySource _respondentRepositorySource;
    private readonly IDataPresenceGuarantor _dataPresenceGuarantor;
    protected readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<InMemoryTotalisationOrchestrator> _logger;

    public InMemoryTotalisationOrchestrator(IRespondentRepositorySource respondentRepositorySource, IDataPresenceGuarantor dataPresenceGuarantor, IProfileResponseAccessorFactory profileResponseAccessorFactory, ILoggerFactory loggerFactory)
    {
        _respondentRepositorySource = respondentRepositorySource;
        _dataPresenceGuarantor = dataPresenceGuarantor;
        _profileResponseAccessorFactory = profileResponseAccessorFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<InMemoryTotalisationOrchestrator>();
    }

    /// <returns>A result per requested instance in ascending instance id order</returns>
    public async Task<EntityTotalsSeries[]> TotaliseAsync(FilteredMetric filteredMetric,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        TargetInstances requestedInstances,
        IGroupedQuotaCells quotaCells,
        EntityWeightedDailyResults[] weightedAverages,
        CancellationToken cancellationToken)
    {
        /*
         * The data side is multidimensional with respect to entity type. Profile fields have no entity association there - profile fields are requested with an empty TargetInstances array.
         * However the calculation side is one dimensional and so needs the requestedInstances of Profile type. We could make it multidimensional in future.
         * Results are returned only for the requestedInstances so this is currently needed for calculating profile results too.
         */
        IEnumerable<IDataTarget> targetInstances = requestedInstances.EntityType.IsProfile ?
            Enumerable.Empty<TargetInstances>()
            : new List<TargetInstances>(filteredMetric.FilterInstances) { requestedInstances };

        await _dataPresenceGuarantor.EnsureDataIsLoaded(_respondentRepositorySource.GetForSubset(filteredMetric.Subset), filteredMetric.Subset,
            filteredMetric.Metric, calculationPeriod, average, filteredMetric.Filter, targetInstances.ToArray(), filteredMetric.Breaks, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Calculating {MeasureTypeName} measure, {MeasureName}, for {BrandNames} in {@Subset} " +
                "with period {@CalculationPeriod} and filter {@Filter} for quota cells {@QuotaCells}",
                string.Concat(filteredMetric.Metric.EntityCombination.Select(t => t.DisplayNamePlural)),
                filteredMetric.Metric.Name, requestedInstances.GetLoggableInstanceString(),
                filteredMetric.Subset,
                calculationPeriod,
                filteredMetric.Filter,
                quotaCells);
        }

        quotaCells = quotaCells.FilterUnnecessary(filteredMetric.Filter);

        var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(filteredMetric.Subset);
        var unweightedInMemoryCalculator = new TotalisationOrchestrator(_respondentRepositorySource, profileResponseAccessor);
        var entityCalculationParallelizer = new EntityTotalisationParallelizer(unweightedInMemoryCalculator, _loggerFactory.CreateLogger<MetricCalculationOrchestrator>());
        var unweightedResults
            = entityCalculationParallelizer.Totalise(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, weightedAverages, cancellationToken);
        return unweightedResults;

    }
}
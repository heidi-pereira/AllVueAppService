using System.Diagnostics;
using System.Threading;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.CalculationPipeline;

internal class EntityTotalisationParallelizer : ITotalisationOrchestrator
{
    //  This is a guestimate - should really be benchmarked to see where we'll
    //  get the most benefit, or possibly even tuned dynamically. The latter
    //  feels like extra complexity we don't need for the time being.
    private const int ThresholdNumberOfInstancesForParallelisation = 8;
    private static int _numberOfConcurrentCalculations;
    private static readonly int NumberOfCpuCores = Math.Max(1, Environment.ProcessorCount);

    private readonly ILogger _logger;
    private readonly ITotalisationOrchestrator _fromMemoryUnweightedInMemoryUnweightedCalculator;

    public EntityTotalisationParallelizer(ITotalisationOrchestrator fromMemoryUnweightedInMemoryUnweightedCalculator, ILogger logger)
    {
        _fromMemoryUnweightedInMemoryUnweightedCalculator = fromMemoryUnweightedInMemoryUnweightedCalculator;
        _logger = logger;
    }

    public EntityTotalsSeries[] Totalise(FilteredMetric filteredMetric,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        TargetInstances requestedInstances,
        IGroupedQuotaCells quotaCells,
        EntityWeightedDailyResults[] weightedAverages, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var concurrencyCount = Interlocked.Increment(
            ref _numberOfConcurrentCalculations);

        try
        {
            if (requestedInstances == null || requestedInstances.OrderedInstances.Length <= ThresholdNumberOfInstancesForParallelisation
                                           || concurrencyCount >= NumberOfCpuCores
                                           || !filteredMetric.Metric.EntityCombination.Any())
            {
                return _fromMemoryUnweightedInMemoryUnweightedCalculator.Totalise(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, weightedAverages, cancellationToken);
            }

            var idealNumberOfChunks = Math.Min(
                //  This means we'll not get more than a single chunk until we're calculating
                //  for twice the number of threshold brands. This is OK because this whole
                //  bit is intended to improve performance when somebody wants a *lot* of brands
                //  calculating for in the Over Time view - e.g., when they've done a Select
                //  All in the brand chooser.
                requestedInstances.OrderedInstances.Length / ThresholdNumberOfInstancesForParallelisation,
                NumberOfCpuCores - concurrencyCount + 1);

            if (idealNumberOfChunks < 2)
            {
                return _fromMemoryUnweightedInMemoryUnweightedCalculator.Totalise(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, weightedAverages, cancellationToken);
            }

            return CalculateUnweightedInParallel(calculationPeriod,
                average,
                requestedInstances,
                quotaCells,
                idealNumberOfChunks,
                weightedAverages, filteredMetric, cancellationToken);
        }
        finally
        {
            Interlocked.Decrement(ref _numberOfConcurrentCalculations);

            stopwatch.Stop();
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Took {ElapsedTicks} ticks to calculate unweighted {MeasureName} results for {BrandNames}.",
                    stopwatch.ElapsedTicks, filteredMetric.Metric.Name, requestedInstances.GetLoggableInstanceString());
            }
        }
    }

    private EntityTotalsSeries[] CalculateUnweightedInParallel(CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        TargetInstances requestedInstances,
        IGroupedQuotaCells quotaCells,
        int idealNumberOfChunks,
        EntityWeightedDailyResults[] weightedAverages, FilteredMetric filteredMetric,
        CancellationToken cancellationToken)
    {
        var numberOfInstancesInEachChunk
            = requestedInstances.OrderedInstances.Length / idealNumberOfChunks
              + requestedInstances.OrderedInstances.Length % idealNumberOfChunks;

        var chunkedInstances
            = SliceIntoChunksForParallelProcessing(
                requestedInstances,
                numberOfInstancesInEachChunk);

        Interlocked.Add(
            ref _numberOfConcurrentCalculations,
            chunkedInstances.Length - 1);

        try
        {
            var chunkedResults = chunkedInstances.AsParallel().AsOrdered().Select(chunkOfInstances =>
            {
                var singleThreadedFilteredMeasure = FilteredMetric.Create(filteredMetric.Metric, filteredMetric.FilterInstances, filteredMetric.Subset, filteredMetric.Filter);
                singleThreadedFilteredMeasure.Breaks = filteredMetric.Breaks?.Select(x => x.DeepClone()).ToArray();
                return _fromMemoryUnweightedInMemoryUnweightedCalculator.Totalise(singleThreadedFilteredMeasure, calculationPeriod, average,
                    chunkOfInstances, quotaCells, weightedAverages, cancellationToken);
            });

            var stitchedResults
                = StitchChunkedResultsFromParallelisationBackIntoSingleArray(
                    requestedInstances,
                    chunkedResults);

            return stitchedResults;
        }
        finally
        {
            Interlocked.Add(
                ref _numberOfConcurrentCalculations,
                -(chunkedInstances.Length - 1));
        }
    }

    private static TargetInstances[] SliceIntoChunksForParallelProcessing(
        TargetInstances instances,
        int numberPerChunk)
    {
        var chunkedInstances = new EntityInstance[
            instances.OrderedInstances.Length / numberPerChunk
            + (instances.OrderedInstances.Length % numberPerChunk == 0 ? 0 : 1)][];
        for (
            int firstIndex = 0, chunkIndex = 0;
            firstIndex < instances.OrderedInstances.Length;
            firstIndex += numberPerChunk, ++chunkIndex)
        {
            var target = new EntityInstance[Math.Min(
                numberPerChunk,
                instances.OrderedInstances.Length - firstIndex)];
            chunkedInstances[chunkIndex] = target;
            instances.OrderedInstances.CopyTo(
                firstIndex,
                target,
                0,
                target.Length);
        }

        return chunkedInstances.Select(entityInstances => new TargetInstances(instances.EntityType, entityInstances)).ToArray();
    }

    private static EntityTotalsSeries[] StitchChunkedResultsFromParallelisationBackIntoSingleArray(
        TargetInstances instances,
        IEnumerable<EntityTotalsSeries[]> chunkedResults)
    {
        var stitchedResults = new EntityTotalsSeries[instances.OrderedInstances.Length];
        int index = 0;
        foreach (var chunkOfResults in chunkedResults)
        {
            Array.Copy(
                chunkOfResults,
                0,
                stitchedResults,
                index,
                chunkOfResults.Length);
            index += chunkOfResults.Length;
        }

        return stitchedResults;
    }
}
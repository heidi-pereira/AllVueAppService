using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline;

internal class UncertaintyCalculator
{
    private readonly IAsyncTotalisationOrchestrator _totalisationOrchestrator;
    protected readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
    protected readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;

    public UncertaintyCalculator(IProfileResponseAccessorFactory profileResponseAccessorFactory, IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository, IAsyncTotalisationOrchestrator totalisationOrchestrator)
    {
        _profileResponseAccessorFactory = profileResponseAccessorFactory;
        _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
        _totalisationOrchestrator = totalisationOrchestrator;
    }

    public async Task CalculateSignificance(Subset datasetSelector,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        Measure measure,
        IGroupedQuotaCells quotaCells,
        IFilter filter,
        EntityWeightedDailyResults[] entityWeightedResults,
        TargetInstances[] filterInstances,
        TargetInstances requestedInstances,
        CancellationToken cancellationToken)
    {
        await CalculateStandardDeviation(datasetSelector, calculationPeriod, average, measure, quotaCells, filter,
            entityWeightedResults, filterInstances, requestedInstances, cancellationToken);

        if (entityWeightedResults.All(result => result.WeightedDailyResults.Count < 2))
        {
            // Only can calculate significance when we have at least two results to compare.
            return;
        }


        foreach (var entityWeightedResult in entityWeightedResults)
        {
            // Only can calculate significance when we have at least two results to compare.
            if (entityWeightedResult.WeightedDailyResults.Count < 2)
            {
                continue;
            }

            for (int periodIndex = 1;
                 periodIndex < entityWeightedResult.WeightedDailyResults.Count;
                 periodIndex++)
            {
                var currentWeightedDailyResult = entityWeightedResult.WeightedDailyResults[periodIndex];
                var previousWeightedDailyResult = entityWeightedResult.WeightedDailyResults[periodIndex-1];
                if (previousWeightedDailyResult.UnweightedSampleSize != 0)
                {
                    currentWeightedDailyResult.Tscore = SignificanceCalculator.CalculateTScore(measure,currentWeightedDailyResult, previousWeightedDailyResult);
                    currentWeightedDailyResult.Significance =SignificanceCalculator.CalculateSignificance(currentWeightedDailyResult.Tscore.Value, SigConfidenceLevel.NinetyFive);
                }
            }
        }
    }

    private async Task CalculateStandardDeviation(Subset datasetSelector, CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        Measure measure, IGroupedQuotaCells quotaCells, IFilter filter,
        EntityWeightedDailyResults[] entityFinalWeighted, TargetInstances[] filterInstances,
        TargetInstances requestedInstances, CancellationToken cancellationToken)
    {

        switch (measure.CalculationType)
        {
            case CalculationType.Average:
                await CalculateStandardDeviationForAverageType(datasetSelector, calculationPeriod, average, measure, quotaCells, filter, entityFinalWeighted, filterInstances, requestedInstances, cancellationToken);
                break;
            case CalculationType.YesNo:
                // We use client-side standard-error calculation for this, so this is not required.
                return;
            default:
                return;
        }
    }

    private async Task CalculateStandardDeviationForAverageType(Subset subset, CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        Measure measure, IGroupedQuotaCells quotaCells, IFilter filter,
        EntityWeightedDailyResults[] entityFinalWeighted, TargetInstances[] filterInstances,
        TargetInstances requestedInstances, CancellationToken cancellationToken)
    {
        // See: https://en.wikipedia.org/wiki/Weighted_arithmetic_mean#Weighted_sample_variance
        var filteredMeasure = FilteredMetric.Create(measure, filterInstances, subset, filter);
        var unweightedWithVariance = (await _totalisationOrchestrator.TotaliseAsync(filteredMeasure, calculationPeriod, average, requestedInstances, quotaCells, entityFinalWeighted, cancellationToken));

        var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(subset);

        for (int entityInstanceIndex = 0; entityInstanceIndex < unweightedWithVariance.Length; entityInstanceIndex++)
        {
            if (unweightedWithVariance[entityInstanceIndex].CellsTotalsSeries.Count > 0 &&
                entityFinalWeighted[entityInstanceIndex].WeightedDailyResults.Count > 0)
            {
                // For quarters, unweighted is broken down by months and rolled up to quarter hence this is not 3.
                var monthStep = unweightedWithVariance[entityInstanceIndex].CellsTotalsSeries.Count /
                                entityFinalWeighted[entityInstanceIndex].WeightedDailyResults.Count;

                for (int periodIndex = 0;
                     periodIndex < entityFinalWeighted[entityInstanceIndex].WeightedDailyResults.Count;
                     periodIndex++)
                {
                    double sumOfVariancesByBrandPeriod = 0;
                    double sumOfSampleCount = 0;

                    var periodWeights = new IReadOnlyDictionary<QuotaCell, double>[monthStep];
                    for (var monthOffset = 0; monthOffset < monthStep; monthOffset++)
                    {
                        var dateTimeOffset = unweightedWithVariance[entityInstanceIndex].CellsTotalsSeries[periodIndex * monthStep + monthOffset].Date;
                        periodWeights[monthOffset] = WeightGeneratorForRequestedPeriod.Generate(subset, profileResponseAccessor, _quotaCellReferenceWeightingRepository, average, quotaCells, dateTimeOffset);
                        if (periodWeights[monthOffset] == null)
                        {
                            return;
                        }
                    }

                    for (var monthOffset = 0; monthOffset < monthStep; monthOffset++)
                    {
                        foreach (var (quotaCell, weight) in periodWeights[monthOffset])
                        {
                            var byQuotaCell = unweightedWithVariance[entityInstanceIndex]
                                .CellsTotalsSeries[periodIndex * monthStep + monthOffset][quotaCell];

                            if (byQuotaCell != null)
                            {
                                sumOfVariancesByBrandPeriod += byQuotaCell.TotalForAverage.Variance * weight;
                                sumOfSampleCount += byQuotaCell.TotalForAverage.SampleSize * weight;
                            }
                        }
                    }

                    var variance = sumOfVariancesByBrandPeriod / sumOfSampleCount;
                    entityFinalWeighted[entityInstanceIndex].WeightedDailyResults[periodIndex].Variance =
                        variance;

                    var standardDeviation = Math.Sqrt(variance);
                    entityFinalWeighted[entityInstanceIndex].WeightedDailyResults[periodIndex].StandardDeviation =
                        standardDeviation;
                }
            }
        }
    }
}
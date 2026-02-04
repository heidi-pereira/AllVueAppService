using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Utils;
using Microsoft.EntityFrameworkCore;
using BrandVue.EntityFramework.Answers;

namespace BrandVue.SourceData.CalculationPipeline;

public abstract class BaseTextCountCalculator : ITextCountCalculator
{
    protected readonly IAsyncTotalisationOrchestrator _resultsCalculator;
    protected readonly IMeasureRepository _measureRepository;
    protected readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
    protected readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;

    protected BaseTextCountCalculator(
        IProfileResponseAccessorFactory profileResponseAccessorFactory,
        IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
        IMeasureRepository measureRepository,
        IAsyncTotalisationOrchestrator resultsCalculator)
    {
        _profileResponseAccessorFactory = profileResponseAccessorFactory;
        _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
        _measureRepository = measureRepository;
        _resultsCalculator = resultsCalculator;
    }

    public async Task<EntityWeightedDailyResults[]> CalculateTextCounts(
        Subset datasetSelector,
        CalculationPeriod calculationPeriod,
        AverageDescriptor average,
        Measure measure,
        IGroupedQuotaCells quotaCells,
        IFilter filter,
        TargetInstances[] filterInstances,
        TargetInstances requestedInstances,
        CancellationToken cancellationToken)
    {
        const double significantNumber = 3.0;

        var averageWithIncludeResponseIdsOverride = average.ShallowCopy();
        averageWithIncludeResponseIdsOverride.IncludeResponseIds = true;

        var baseMeasureFieldName = measure.BaseFieldDependencies.SingleOrDefault()?.Name;
        var baseMeasureOrNull = _measureRepository
            .GetAllMeasuresWithDisabledPropertyFalseForSubset(datasetSelector)
            .FirstOrDefault(m => m.PrimaryFieldDependencies?.OnlyOrDefault()?.Name == baseMeasureFieldName);

        var baseMeasure = baseMeasureOrNull ?? throw new Exception($"Cannot find measure with field name of {measure.BaseField.Name}");

        var unweightedInstanceResults = await _resultsCalculator.TotaliseAsync(
            FilteredMetric.Create(baseMeasure, filterInstances, datasetSelector, filter),
            calculationPeriod,
            averageWithIncludeResponseIdsOverride,
            requestedInstances,
            quotaCells,
            null,
            cancellationToken);

        var asyncResults = IterateTextCountsAsync(
            datasetSelector,
            calculationPeriod,
            measure,
            quotaCells,
            filterInstances,
            unweightedInstanceResults,
            averageWithIncludeResponseIdsOverride,
            significantNumber);

        return await asyncResults.ToArrayAsync(cancellationToken);
    }

    private async IAsyncEnumerable<EntityWeightedDailyResults> IterateTextCountsAsync(
        Subset datasetSelector,
        CalculationPeriod calculationPeriod,
        Measure measure,
        IGroupedQuotaCells quotaCells,
        TargetInstances[] filterInstances,
        EntityTotalsSeries[] unweightedInstanceResults,
        AverageDescriptor averageWithIncludeResponseIdsOverride,
        double significantNumber)
    {
        var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(datasetSelector);
        var responseFieldDescriptor = measure.PrimaryFieldDependencies.SingleOrDefault()
            ?? throw new Exception($"Cannot find text field for {measure.Name}");
        var fieldDefinitionModel = responseFieldDescriptor.GetDataAccessModel(datasetSelector.Id);

        foreach (var unweightedInstanceResult in unweightedInstanceResults)
        {
            var responseIdsWithWeights = unweightedInstanceResult.CellsTotalsSeries
                .SelectMany(dailyResult => WeightGeneratorForRequestedPeriod.ResponseWeightsForDay(
                    _quotaCellReferenceWeightingRepository,
                    datasetSelector,
                    averageWithIncludeResponseIdsOverride,
                    quotaCells,
                    profileResponseAccessor,
                    dailyResult))
                .ToArray();

            var resultFilterInstances = filterInstances
                .Select(f => (f.EntityType, Id: f.SortedEntityInstanceIds.Single()))
                .ToList();

            if (unweightedInstanceResult.EntityInstance is not null)
            {
                resultFilterInstances.Add((
                    unweightedInstanceResult.EntityType,
                    unweightedInstanceResult.EntityInstance.Id));
            }

            var filterValues = fieldDefinitionModel.OrderedEntityColumns
                .Join(resultFilterInstances,
                    c => c.EntityType,
                    f => f.EntityType,
                    (c, f) => (Location: c.DbLocation, f.Id))
                .ToArray();

            // Call the abstract method that each implementation will provide
            var weightedTextCounts = await GetWeightedTextCountsAsync(
                responseIdsWithWeights,
                fieldDefinitionModel.UnsafeSqlVarCodeBase,
                filterValues);

            var unweightedResponseCount = (uint)weightedTextCounts.Sum(r => r.UnweightedResult);
            var weightedResponseCount = weightedTextCounts.Sum(r => r.Result);

            var weightedDailyResults = weightedTextCounts
                .CleanTextAndRegroup()
                .ApplyExclusionList(measure.ExcludeList)
                .Where(wc => wc.UnweightedResult >= significantNumber)
                .Select(w => new WeightedDailyResult(calculationPeriod.Periods[0].EndDate)
                {
                    WeightedResult = w.Result,
                    Text = w.Text,
                    UnweightedSampleSize = (uint)w.UnweightedResult
                })
                .OrderByDescending(r => r.WeightedResult)
                .ToList();

            yield return new EntityWeightedDailyResults(
                unweightedInstanceResult.EntityInstance,
                weightedDailyResults,
                unweightedResponseCount,
                weightedResponseCount);
        }
    }

    protected abstract Task<WeightedWordCount[]> GetWeightedTextCountsAsync(
        ResponseWeight[] responseWeights,
        string varCodeBase,
        IReadOnlyCollection<(DbLocation Location, int Id)> filters);
}
using System.Collections.Immutable;
using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.Models;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace BrandVue.Services
{
    /// <summary>
    /// The <see cref="IMetricCalculationOrchestrator"/> aims for a minimal API, this provides convenience methods to use it in common ways.
    /// </summary>
    public class ConvenientCalculator : IConvenientCalculator
    {
        private readonly IMetricCalculationOrchestrator _calculator;
        private readonly IMeasureRepository _measureRepository;
        private readonly InitialWebAppConfig _initialWebAppConfig;
        private readonly IFilterFactory _filterFactory;
        private readonly IEntityRepository _entityRepository;

        public ConvenientCalculator(IMetricCalculationOrchestrator calculator, IMeasureRepository measureRepository, InitialWebAppConfig initialWebAppConfig,
            IFilterFactory filterFactory, IEntityRepository entityRepository)
        {
            _calculator = calculator;
            _measureRepository = measureRepository;
            _initialWebAppConfig = initialWebAppConfig;
            _filterFactory = filterFactory;
            _entityRepository = entityRepository;
        }

        public async Task<ResultsForMeasure[]> GetCuratedResultsForAllMeasures(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            return await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                IFilter filter = _filterFactory.CreateFilterForMeasure(pam.FilterModel, measure, pam.Subset);
                var filteredMeasure = FilteredMetric.Create(measure, pam.FilterInstances, pam.Subset, filter);
                filteredMeasure.Breaks = pam.Breaks?.Select(b => b.DeepClone()).ToArray();
                var weightedResults = await _calculator.Calculate(filteredMeasure, pam.CalculationPeriod, pam.Average, pam.RequestedInstances, pam.QuotaCells, pam.IncludeSignificance, cancellationToken);

                var count = weightedResults.Length;
                var resultsModels = new EntityWeightedDailyResults[count];
                for (int index = 0; index < count; ++index)
                {
                    var current = weightedResults[index];
                    resultsModels[index] = new EntityWeightedDailyResults(
                        current.EntityInstance,
                        current.WeightedDailyResults);
                }

                var measureResults = new ResultsForMeasure()
                {
                    Measure = measure,
                    Data = resultsModels,
                    NumberFormat = measure.NumberFormatString
                };

                return measureResults;
            }, cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        }

        public async Task<(Measure Measure, EntityWeightedDailyResults[] PerEntityInstanceResults)[]>
            GetCuratedMarketResultsForAllMeasures(ResultsProviderParameters pam, CancellationToken cancellationToken)
        {
            return await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                var perEntityInstanceResults = await CalculateWeightedForMeasure(pam, cancellationToken, measure);
                return (measure, perEntityInstanceResults);
            }, cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        }

        public async
            Task<(Measure Measure, IList<WeightedDailyResult> AveragedResults, EntityWeightedDailyResults[]
                PerEntityInstanceResults)[]> GetCuratedMarketAverageResultsForAllMeasures(ResultsProviderParameters pam,
                CancellationToken cancellationToken)
        {
            var unbiasedRelativeSizeResults = await GetMarketAverageWeightingsByBaseMeasureId(pam, cancellationToken);

            return await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                var perEntityInstanceResults = await CalculateWeightedForMeasure(pam, cancellationToken, measure);
                var unbiasedRelativeSizeWeightings = unbiasedRelativeSizeResults[measure.MarketAverageBaseMeasure];
                var marketAverageResults = _calculator.CalculateMarketAverage(perEntityInstanceResults, pam.Subset,
                    _initialWebAppConfig.LowSampleForBrand,
                    pam.AverageType,
                    pam.QuestionType,
                    pam.EntityMeanMaps,
                    unbiasedRelativeSizeWeightings);
                return (measure, marketAverageResults, perEntityInstanceResults);
            }, cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        }

        public async Task<(Measure Measure, WeightedDailyResult WeightedDailyResult)[]> GetAverageMentionsResultForAllMeasures(ResultsProviderParameters pam, CancellationToken cancellationToken)
        {
            return await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                var filter = _filterFactory.CreateFilterForMeasure(pam.FilterModel, measure, pam.Subset);

                var requestedInstances = pam.RequestedInstances;
                if(measure.OriginalMetricName != null)
                {
                    requestedInstances = RemoveNetInstances(pam, measure);
                }

                var filteredMetric = FilteredMetric.Create(measure, pam.FilterInstances, pam.Subset, filter);
                var weightedDailyResult = await _calculator.CalculateAverageMentions(filteredMetric, pam.CalculationPeriod, pam.Average, requestedInstances, pam.QuotaCells, cancellationToken);

                return (measure, weightedDailyResult);
            }, cancellationToken).ToArrayAsync(cancellationToken: cancellationToken);
        }


        public async Task<WeightedDailyResult[]> GetNumericRespondentAverageResult(ResultsProviderParameters pam,
            ResponseFieldDescriptor field, CancellationToken cancellationToken)
        {
            var filter = _filterFactory.CreateFilterForMeasure(pam.FilterModel, pam.PrimaryMeasure, pam.Subset);
            var filteredMetric = FilteredMetric.Create(pam.PrimaryMeasure, pam.FilterInstances, pam.Subset, filter);
            return await _calculator.CalculateNumericResponseAverage(filteredMetric, pam.CalculationPeriod, pam.Average, pam.RequestedInstances, pam.QuotaCells, pam.AverageType, field, cancellationToken);
        }

        private TargetInstances RemoveNetInstances(ResultsProviderParameters pam, Measure measure)
        {
            var originalMeasure = _measureRepository.Get(measure.OriginalMetricName);

            var allTypes = originalMeasure.EntityCombination.Select(e => e.Identifier);
            var filterTypes = pam.FilterInstances.Select(f => f.EntityType.Identifier);
            var originalEntityType = allTypes.First(e => !filterTypes.Contains(e));
            var originalInstances = _entityRepository.GetInstancesOf(originalEntityType, pam.Subset);
            var ids = originalInstances.Where(b => pam.RequestedInstances.SortedEntityInstanceIds.Contains(b.Id));
            return new TargetInstances(pam.RequestedInstances.EntityType, ids);
        }

        public async Task<Dictionary<string, EntityWeightedDailyResults[]>> GetMarketAverageWeightingsByBaseMeasureId(
            ResultsProviderParameters pam, CancellationToken cancellationToken)
        {
            var results = await pam.Measures.Select(m => m.MarketAverageBaseMeasure).Distinct().AsAsyncParallel().AsOrdered().SelectAwait(async m =>
                new{BaseMeasureId = m, Weightings = await CalculateMarketWeightings(pam, m, cancellationToken) },
                cancellationToken
                ).ToDictionaryAsync(r => r.BaseMeasureId, r => r.Weightings, cancellationToken);
            return results;
        }

        public async Task<Dictionary<string, UnweightedTotals>>
            GetMarketAverageUnweightedWeightingsByBaseMeasureId(ResultsProviderParameters pam,
                CancellationToken cancellationToken)
        {
            var results = await pam.Measures.Select(m => m.MarketAverageBaseMeasure).Distinct().AsAsyncParallel().AsOrdered()
                .SelectAwait(async m =>
                    new { BaseMeasureId = m, Weightings = await CalculateUnweightedMarketWeightings(pam, m, cancellationToken) },
                    cancellationToken
                ).ToDictionaryAsync(r => r.BaseMeasureId, r => r.Weightings, cancellationToken);
            return results;
        }

        private async Task<EntityWeightedDailyResults[]> CalculateMarketWeightings(ResultsProviderParameters pam,
            string marketAverageBaseMeasureId, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(marketAverageBaseMeasureId) || marketAverageBaseMeasureId == Measure.UseEqualWeightingMeasureId
                ? null
                : await CalculateWeightedForMeasure(pam, cancellationToken, _measureRepository.Get(marketAverageBaseMeasureId));
        }

        private async Task<UnweightedTotals> CalculateUnweightedMarketWeightings(
            ResultsProviderParameters pam, string marketAverageBaseMeasureId, CancellationToken cancellationToken)
        {
            return string.IsNullOrWhiteSpace(marketAverageBaseMeasureId) || marketAverageBaseMeasureId == Measure.UseEqualWeightingMeasureId
                ? null
                : await CalculateUnweightedForMeasure(pam, cancellationToken, _measureRepository.Get(marketAverageBaseMeasureId));
        }

        public async Task<EntityWeightedDailyResults[]> CalculateWeightedForMeasure(ResultsProviderParameters pam,
            CancellationToken cancellationToken,
            Measure measureOverride = null,
            IGroupedQuotaCells quotaCellOverride = null)
        {
            var measure = measureOverride ?? pam.PrimaryMeasure;
            Subset subset = pam.Subset;
            IFilter filter = _filterFactory.CreateFilterForMeasure(pam.FilterModel, measure, pam.Subset);
            TargetInstances[] filterInstances = pam.FilterInstances;
            return await _calculator.Calculate(FilteredMetric.Create(measure, filterInstances, subset, filter), pam.CalculationPeriod, pam.Average, pam.RequestedInstances, pam.QuotaCells ?? quotaCellOverride, pam.IncludeSignificance, cancellationToken);
        }

        public async Task<UnweightedTotals> CalculateUnweightedForMeasure(ResultsProviderParameters pam,
            CancellationToken cancellationToken,
            Measure measureOverride = null, IGroupedQuotaCells quotaCellOverride = null)
        {
            var measure = measureOverride ?? pam.PrimaryMeasure;
            Subset subset = pam.Subset;
            TargetInstances[] filterInstances = pam.FilterInstances;
            IFilter filter = _filterFactory.CreateFilterForMeasure(pam.FilterModel, measure, pam.Subset);
            return await _calculator.CalculateUnweightedTotals(FilteredMetric.Create(measure, filterInstances, subset, filter), pam.CalculationPeriod, pam.Average, pam.RequestedInstances, pam.QuotaCells ?? quotaCellOverride, cancellationToken);
        }

        public async Task<int[]> CalculateRespondentIdsForMeasure(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            var averageWithResponseIds = pam.Average.ShallowCopy();
            averageWithResponseIds.IncludeResponseIds = true;
            Subset subset = pam.Subset;
            Measure measure = pam.PrimaryMeasure;
            TargetInstances[] filterInstances = pam.FilterInstances;
            IFilter filter = _filterFactory.CreateFilterForMeasure(pam.FilterModel, pam.PrimaryMeasure, pam.Subset);
            var data = await _calculator.CalculateUnweightedTotals(FilteredMetric.Create(measure, filterInstances, subset, filter), pam.CalculationPeriod, averageWithResponseIds, pam.RequestedInstances, pam.QuotaCells, cancellationToken);

            var responseIds =
                data.Unweighted.SelectMany(x => x.CellsTotalsSeries.SelectMany(y => y.CellResultsWithSample.SelectMany(z => z.ResponseIdsForAverage)))
                .Distinct().ToList();
            return responseIds.ToArray();
        }

        public EntityCategoryResults[] CategoryResultsForAccumulator(UnweightedTotals unweighted)
        {
            if (unweighted.Measure.EntityCombination.Any())
            {
                return CreateEntityCategoryResults(unweighted.RequestedInstances.OrderedInstances);
            }
            return CreateNoEntityCategoryResults();
        }

        public async Task<EntityCategoryResults[]> WeightCategoryWithoutSignificance(
            UnweightedTotals unweighted,
            BreakdownCategory breakdownCategory, CancellationToken cancellationToken)
        {
            var categoryResultsAccumulator = CategoryResultsForAccumulator(unweighted);
            foreach (var (categoryDescription, quotaCells) in breakdownCategory.GetCategories(unweighted.Subset))
            {
                var weighted = await _calculator.CalculateWeightedFromUnweighted(unweighted, calculateSignificance: false, cancellationToken, filteredCells: quotaCells);
                AddCategoryResults(weighted, categoryDescription, categoryResultsAccumulator);
            }
            return categoryResultsAccumulator;
        }

        public async Task<CategoryResults[]> MarketAverageForCategory(UnweightedTotals measureResults,
            UnweightedTotals relativeSizeResults,
            BreakdownCategory breakdownCategory,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps,
            CancellationToken cancellationToken)
        {
            return await breakdownCategory.GetCategories(measureResults.Subset).ToAsyncEnumerable().SelectAwait(async categoryDetails =>
            {
                var averagedResults = await CalculateMarketAverage(measureResults,
                    relativeSizeResults,
                    measureResults.Subset,
                    averageType,
                    questionType,
                    entityMeanMaps,
                    cancellationToken,
                    categoryDetails.QuotaCells);
                return new CategoryResults(categoryDetails.CategoryDescription, averagedResults);
            }).ToArrayAsync(cancellationToken);
        }

        public async Task<IList<WeightedDailyResult>> CalculateMarketAverage(
            UnweightedTotals totalsForMeasure,
            UnweightedTotals marketAverageWeightingsForMeasure,
            Subset pamSubset,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps,
            CancellationToken cancellationToken,
            IGroupedQuotaCells filteredCells = null)
        {
            var weightedResultsForMeasure = await _calculator.CalculateWeightedFromUnweighted(totalsForMeasure, false, cancellationToken, filteredCells);
            var weightedWeightingsForMeasure = marketAverageWeightingsForMeasure == null ? null :
                await _calculator.CalculateWeightedFromUnweighted(marketAverageWeightingsForMeasure, false, cancellationToken, filteredCells);
            return _calculator.CalculateMarketAverage(weightedResultsForMeasure,
                pamSubset,
                _initialWebAppConfig.LowSampleForBrand,
                averageType, 
                questionType,
                entityMeanMaps,
                weightedWeightingsForMeasure);
        }

        private static void AddCategoryResults(EntityWeightedDailyResults[] weighted,
            string categoryDescription,
            EntityCategoryResults[] categoryResultsAccumulator)
        {
            for (int index = 0, size = weighted.Length; index < size; ++index)
            {
                var resultSet = weighted[index];
                var categoryResultsForEntityInstance = categoryResultsAccumulator[index];
                categoryResultsForEntityInstance.Results.Add(new CategoryResults(
                    categoryDescription,
                    resultSet.WeightedDailyResults));
            }
        }

        private static EntityCategoryResults[] CreateNoEntityCategoryResults()
        {
            var results = new EntityCategoryResults[1];
            results[0] = new EntityCategoryResults(new EntityInstance());
            return results;
        }

        private static EntityCategoryResults[] CreateEntityCategoryResults(ImmutableArray<EntityInstance> entityInstances)
        {
            var length = entityInstances.Length;
            var results = new EntityCategoryResults[length];
            for (var index = 0; index < length; ++index)
            {
                results[index] = new EntityCategoryResults(entityInstances[index]);
            }
            return results;
        }
    }
}
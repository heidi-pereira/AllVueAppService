using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Threading;

namespace BrandVue.Services
{
    public partial class PipelineResultsProvider : IResultsProvider
    {
        private readonly IMetricCalculationOrchestrator _calculator;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IBreakdownCategoryFactory _breakdownCategoryFactory;
        private readonly IRequestAdapter _requestAdapter;
        private readonly IConvenientCalculator _convenientCalculator;
        private readonly IBreakdownResultsProvider _breakdownResultsProvider;
        private readonly IProductContext _productContext;
        private readonly IResponseRepository _textResponseRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IProfileResultsCalculator _profileResultsCalculator;
        private readonly IMeasureRepository _measureRepository;
        private readonly RequestScope _requestScope;
        private readonly IFilterRepository _filters;
        private readonly IMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;
        private readonly AppSettings _settings;
        private readonly ILogger<PipelineResultsProvider> _logger;

        public PipelineResultsProvider(
            IMetricCalculationOrchestrator calculator,
            ISubsetRepository subsetRepository,
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IBreakdownCategoryFactory breakdownCategoryFactory,
            IConvenientCalculator convenientCalculator,
            IRequestAdapter requestAdapter,
            IBreakdownResultsProvider breakdownResultsProvider,
            IProductContext productContext,
            IResponseRepository textResponseRepository,
            IHeatmapResponseRepository heatmapResponseRepository,
            IEntityRepository entityRepository,
            IProfileResultsCalculator profileResultsCalculator,
            IMeasureRepository measureRepository,
            AppSettings settings,
            RequestScope requestScope,
            IFilterRepository filters,
            IMetricConfigurationRepository metricConfigurationRepository,
            IVariableConfigurationRepository variableConfigurationRepository,
            ILogger<PipelineResultsProvider> logger
            )
        {
            _calculator = calculator;
            _subsetRepository = subsetRepository;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _breakdownCategoryFactory = breakdownCategoryFactory;
            _convenientCalculator = convenientCalculator;
            _requestAdapter = requestAdapter;
            _breakdownResultsProvider = breakdownResultsProvider;
            _productContext = productContext;
            _textResponseRepository = textResponseRepository;
            _entityRepository = entityRepository;
            _profileResultsCalculator = profileResultsCalculator;
            _measureRepository = measureRepository;
            _settings = settings;
            _requestScope = requestScope;
            _filters = filters;
            _metricConfigurationRepository = metricConfigurationRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _logger = logger;
        }

        public Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            return GetCuratedResultsForAllMeasures(pam, cancellationToken);
        }

        public Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            return GetCuratedResultsForAllMeasures(pam, cancellationToken);
        }

        private async Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            var curatedResultsForAllMeasures = await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken);
            var entityWeightedDailyResults = curatedResultsForAllMeasures[0].Data;

            var sampleSizeWeightedDailyResults =
                pam.SampleSizeEntityInstanceId.HasValue
                    ? entityWeightedDailyResults
                        .SingleOrDefault(b => b.EntityInstance.Id == pam.SampleSizeEntityInstanceId)
                        ?.WeightedDailyResults?.ToArray()
                    : entityWeightedDailyResults.FirstOrDefault()?.WeightedDailyResults?.ToArray();

            bool hasData = entityWeightedDailyResults.HasData();

            entityWeightedDailyResults =
                entityWeightedDailyResults.OrderByFocusEntityInstanceAndThenAlphabeticByEntityInstanceName(
                    pam.FocusEntityInstanceId);
            var measureResults = new CuratedResultsForExport
            {
                Data = curatedResultsForAllMeasures,
                HasData = hasData,
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : entityWeightedDailyResults.LowSampleSummaries(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate),
                SampleSizeMetadata = sampleSizeWeightedDailyResults?.GetSampleSizeMetadata(),
            };
            return measureResults;
        }

        public Task<BreakdownResults> GetBreakdown(MultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            return _breakdownResultsProvider.GetBreakdown(pam, model.DemographicFilter, cancellationToken);
        }

        public async Task<BreakdownByAgeResults> GetBreakDownByAge(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, true);

            var unweighted = await _convenientCalculator.CalculateUnweightedForMeasure(pam, cancellationToken);

            var byAgeGroup = (await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByAgeGroup(model.DemographicFilter, pam.Subset), cancellationToken)).Single();

            var totalResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken)).Single().Data.Single()
                .WeightedDailyResults.ToArray();

            bool hasData = totalResults.HasData() || byAgeGroup.Results.HasData();

            return new BreakdownByAgeResults
            {
                EntityInstance = byAgeGroup.EntityInstance,
                ByAgeGroup = byAgeGroup.Results,
                Total = totalResults,
                SampleSizeMetadata = totalResults.GetSampleSizeMetadata(),
                HasData = hasData,
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : byAgeGroup.EntityInstanceIdsWithLowSample(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
            };
        }


        public Task<RankingTableResults> GetRankingTableResult(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            return GetRankingTableResults(pam, cancellationToken);
        }

        public Task<RankingTableResults> GetRankingTableResult(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            return GetRankingTableResults(pam, cancellationToken);
        }

        public async Task<AverageResultWithPrevious> GetAverageResultWithPrevious(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            var results = (await GetOverTimeAverageResults(pam, cancellationToken)).Single().Results;
            return new AverageResultWithPrevious(pam, results);
        }

        public async Task<AverageResultWithPrevious> GetAverageResultWithPrevious(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            var results = (await GetOverTimeAverageResults(pam, cancellationToken)).Single().Results;
            return new AverageResultWithPrevious(pam, results);
        }

        private async Task<RankingTableResults> GetRankingTableResults(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            pam.IncludeSignificance = true;

            var (results, lowSampleSummary, sampleSizeMetadata) = await GetRankingResultsByDate(pam, cancellationToken);

            var currentPeriodEndDate = pam.CalculationPeriod.EndDate;
            var currentPeriodResults = results
                .SingleOrDefault(x => x.Date == currentPeriodEndDate)?.Results ?? new List<RankingOvertimeResult>();

            var previousPeriodEndDate = pam.CalculationPeriod.Periods.First().EndDate;
            var lastPeriodResults = results
                .SingleOrDefault(x => x.Date == previousPeriodEndDate)?.Results ?? new List<RankingOvertimeResult>();

            var rankedResults = currentPeriodResults.Select(x =>
            {
                var lastPeriodItem = lastPeriodResults.SingleOrDefault(
                    y => y.EntityInstance?.Id == x.EntityInstance?.Id);

                return new RankingTableResult(
                    x.EntityInstance,
                    x.Rank,
                    lastPeriodItem?.Rank,
                    x.WeightedDailyResult,
                    lastPeriodItem?.WeightedDailyResult,
                    x.MultipleSameRank);
            }).ToList();

            return new RankingTableResults(rankedResults)
            {
                HasData = rankedResults.HasData(),
                LowSampleSummary = lowSampleSummary,
                SampleSizeMetadata = sampleSizeMetadata
            };
        }

        public Task<RankingOvertimeResults> GetRankingOvertimeResult(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            return GetRankingOvertimeResults(pam, cancellationToken);
        }

        private async Task<RankingOvertimeResults> GetRankingOvertimeResults(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            pam.IncludeSignificance = true;

            var (results, lowSampleSummary, sampleSizeMetadata) = await GetRankingResultsByDate(pam, cancellationToken);

            return new RankingOvertimeResults(results)
            {
                HasData = results.Any(r => r.Results.HasData()),
                LowSampleSummary = lowSampleSummary,
                SampleSizeMetadata = sampleSizeMetadata
            };
        }

        private async
            Task<(List<RankingOvertimeResultsByDate> results, LowSampleSummary[] lowSampleSummary, SampleSizeMetadata
                sampleSizeMetadata)> GetRankingResultsByDate(ResultsProviderParameters pam,
                CancellationToken cancellationToken)
        {
            var weightedResults = await _convenientCalculator.CalculateWeightedForMeasure(pam, cancellationToken);

            var finalResults = new List<RankingOvertimeResultsByDate>();
            int periodCount = weightedResults[0].WeightedDailyResults.Count;

            for (int i = 0; i < periodCount; i++)
            {
                var resultsForPeriod = weightedResults.Select(r => (r.EntityInstance, WeightedDailyResult: r.WeightedDailyResults[i]));
                var orderedList = GetOrderedResultsForMeasure(resultsForPeriod, pam.PrimaryMeasure).ToList();

                var currentDate = orderedList[0].WeightedDailyResult.Date;

                var groupedRankedResults = orderedList.Select(o => o.WeightedDailyResult.WeightedResult)
                    .Select((r, ix) => new { Result = r, Index = ix + 1 })
                    .GroupBy(r => r.Result)
                    .Select(r => new { Result = r.Key, Rank = r.Min(z => z.Index), Multiple = r.Count() > 1 })
                    .ToDictionary(r => r.Result, r => r);
                var rankedResultsForPeriod = orderedList.Select(r =>
                {
                    var rankingResult = groupedRankedResults[r.WeightedDailyResult.WeightedResult];
                    return new RankingOvertimeResult(r.EntityInstance, rankingResult.Rank, r.WeightedDailyResult, rankingResult.Multiple);
                }).ToList();

                finalResults.Add(new RankingOvertimeResultsByDate(currentDate, rankedResultsForPeriod));
            }

            var sampleSizeMetadata = pam.FocusEntityInstanceId == null ?
                weightedResults.GetSampleSizeMetadata() :
                GetFocusEntitySampleSizeMetadata(pam, weightedResults);

            var lowSampleSummary = pam.DoMeasuresIncludeMarketMetric ?
                new LowSampleSummary[] { } :
                weightedResults.LowSampleSummaries(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate);

            return (finalResults, lowSampleSummary, sampleSizeMetadata);
        }

        private IEnumerable<(EntityInstance EntityInstance, WeightedDailyResult WeightedDailyResult)> GetOrderedResultsForMeasure(
            IEnumerable<(EntityInstance EntityInstance, WeightedDailyResult WeightedDailyResult)> results, Measure measure)
        {
            return measure.DownIsGood 
                ? results.OrderBy(r => r.WeightedDailyResult.WeightedResult) 
                : results.OrderByDescending(r => r.WeightedDailyResult.WeightedResult);
        }

        public async Task<StackedResults> GetStackedResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);

            var measureResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken))
                .Select(r => (Measure: r.Measure, Result: r.Data)).ToArray();

            var orderedInstanceIds = GetOrderedInstanceIds();

            var orderedEntityInstanceComparer
                = Comparer<EntityInstance>.Create((b1, b2) =>
                    Array.IndexOf(orderedInstanceIds, b2.Id)
                        .CompareTo(
                            Array.IndexOf(
                                orderedInstanceIds, b1.Id)
                        ) * (int)model.OrderingDirection);

            var sampleSize = measureResults.Max(mr =>
            {
                return pam.SampleSizeEntityInstanceId.HasValue
                    ? mr.Result.Where(r => r.EntityInstance.Id == pam.SampleSizeEntityInstanceId).Max(r => r.WeightedDailyResults.LastOrDefault()?.UnweightedSampleSize ?? 0)
                    : mr.Result.Max(r => r.WeightedDailyResults.LastOrDefault()?.UnweightedSampleSize ?? 0);
            });

            var measures = measureResults.Select(r =>
            {
                var orderedEntityResults = r.Result
                    .OrderBy(b => b.EntityInstance, orderedEntityInstanceComparer)
                    .ToArray();
                var SampleSizeEntityInstanceId = pam.SampleSizeEntityInstanceId.HasValue
                    ? r.Result.SingleOrDefault(br => br.EntityInstance.Id == pam.SampleSizeEntityInstanceId)
                    : r.Result.FirstOrDefault();
                return new StackedMeasureResult
                {
                    Name = r.Measure.Name,
                    Data = orderedEntityResults,
                    SampleSizeMetadata = SampleSizeEntityInstanceId?.WeightedDailyResults?.ToArray()
                        .GetSampleSizeMetadata()
                };
            }).ToArray();

            return new StackedResults
            {
                Measures = measures,
                SampleSizeMetadata = measures.FirstOrDefault()?.SampleSizeMetadata,
                HasData = sampleSize > 0,
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : measureResults.SelectMany(r => r.Result).LowSampleSummariesWithDates(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
            };

            int[] GetOrderedInstanceIds()
            {
                // Order by sum of matching measures, then brand name
                var ordered = measureResults
                    .Where(m => model.Ordering.Contains(m.Measure.Name))
                    .SelectMany(r => r.Result)
                    .GroupBy(r => r.EntityInstance.Id)
                    .OrderBy(r => r.Sum(b => b.WeightedDailyResults.LastOrDefault()?.WeightedResult ?? 0))
                    .ThenByDescending(b => b.First().EntityInstance.Name)
                    .Select(r => r.Key)
                    .ToArray();

                // No ordering matched measures so we fall back to ordering by brand name only
                if (!ordered.Any() && measureResults.Any())
                {
                    ordered = measureResults
                        .First()
                        .Result
                        .OrderByDescending(r => r.EntityInstance.Name)
                        .Select(r => r.EntityInstance.Id)
                        .ToArray();
                }

                return ordered;
            }
        }

        public async Task<StackedAverageResults> GetStackedAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            var measureResults = await _convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(pam, cancellationToken);

            var sampleSize = measureResults.Max(mr =>
            {
                return mr.AveragedResults.Max(r => r.UnweightedSampleSize);
            });
            var subsetStartDate = _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate;
            var measures = measureResults.Select(r =>
            {
                var orderedEntityResults = r.AveragedResults
                    .OrderBy(b => b.Text)
                    .ToArray();
                return new StackedAverageResult
                {
                    Name = r.Measure.Name,
                    Data = orderedEntityResults,
                    LowSampleSummary = r.AveragedResults.LowSampleSummaries(subsetStartDate, "Average")
                };
            }).ToArray();
            return new StackedAverageResults
            {
                Measures = measures,
                SampleSizeMetadata = measures.FirstOrDefault()?.SampleSizeMetadata,
                HasData = sampleSize > 0,
                LowSampleSummary = measures.SelectMany(m => m.LowSampleSummary).ToArray(),
            };
        }

        public async Task<StackedMultiEntityResults> GetStackedResultsForMultipleEntities(
            StackedMultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            var pams = _requestAdapter.CreateParametersForCalculation(model);
            var resultsPerInstance = await pams.ToAsyncEnumerable().SelectAwait(async pam => await GetInstanceResultFor(pam, cancellationToken)).ToArrayAsync(cancellationToken);

            var representativeParameters = pams.First();

            return new StackedMultiEntityResults
            {
                ResultsPerInstance = resultsPerInstance,
                HasData = resultsPerInstance.Any(r => r.HasData),
                SampleSizeMetadata = resultsPerInstance.GetSampleSizeMetadata(),
                LowSampleSummary = resultsPerInstance.SelectMany(r => r.Data.LowSampleSummaries(representativeParameters.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(representativeParameters.Subset).StartDate, r.FilterInstance.Name)).Distinct().ToArray()
            };

            async Task<StackedInstanceResult> GetInstanceResultFor(ResultsProviderParameters pam,
                CancellationToken cancellationToken)
            {
                var weightedResults = await _convenientCalculator.CalculateWeightedForMeasure(pam, cancellationToken);
                return new StackedInstanceResult
                {
                    FilterInstance = pam.FilterInstances.Single().OrderedInstances.Single(),
                    Data = weightedResults,
                    HasData = weightedResults.HasData(),
                };
            }
        }

        public async Task<IEnumerable<OverTimeAverageResults>> GetAverageForStackedMultiEntityCharts(
            StackedMultiEntityRequestModel model, AverageType averageType, CancellationToken cancellationToken)
        {
            var pams = _requestAdapter.CreateParametersForCalculation(model);

            foreach (var pam in pams)
            {
                pam.AverageType = averageType;
            }
            return await pams.ToAsyncEnumerable().SelectAwait(async pam =>
            {
                var results = (await GetOverTimeAverageResults(pam, cancellationToken)).Single().Results;
                foreach (var result in results.WeightedDailyResults)
                {
                    result.Text = pam.FilterInstances.Single().OrderedInstances.Single().Name;
                }
                return results;
            }).ToArrayAsync(cancellationToken);
        }

        public async Task<SplitMetricResults> GetSplitMetricResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var splitMetricPam = _requestAdapter.CreateParametersForCalculation(model, true);
            var results = await model.AdditionalMeasureFilters.AsAsyncParallel().AsOrdered().SelectAwait(async filter =>
            {
                var pam = _requestAdapter.CreateParametersForCalculation(model, true, filter);

                return (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken))
                    .Select(r => (Label: r.Measure.Name, Result: r.Data.Single().WeightedDailyResults.LastOrDefault())).ToArray();

            }, cancellationToken).ToArrayAsync(cancellationToken);

            return SplitMetricResults(splitMetricPam, results);
        }

        public async Task<SplitMetricResults> GetSplitMetricResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var splitMetricPam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            var results = await model.AdditionalMeasureFilters.ToAsyncEnumerable().SelectAwait(async filter =>
            {
                var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model, filter);

                return (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken)).Single().Data
                    .Select(r => (Label: r.EntityInstance.Name, Result: r.WeightedDailyResults.LastOrDefault())).ToArray();
            }).ToArrayAsync(cancellationToken);

            return SplitMetricResults(splitMetricPam, results);
        }

        private string GetVarCode(string measureName) => _metricConfigurationRepository.GetAll().Single(m => m.Name == measureName).VarCode;

        public async Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters(
            CuratedResultsModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> lazyGroupedFiltersPerBreak,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks = null)
        {
            if (_measureRepository.RequiresLegacyBreakCalculation(_settings, crossMeasureBreaks))
            {
                return await GetGroupedCompetitionResultsWithCrossbreakFiltersLegacy(model, lazyGroupedFiltersPerBreak, cancellationToken, crossMeasureBreaks);
            }

            var calculationParameters = _requestAdapter.CreateParametersForCalculation(model, crossMeasures: crossMeasureBreaks);
            
            return await GroupedCompetitionResultsWithCrossbreakFilters(calculationParameters, lazyGroupedFiltersPerBreak, crossMeasureBreaks, cancellationToken);
        }

        private Task<GroupedCrossbreakCompetitionResults> GroupedCompetitionResultsWithCrossbreakFilters(
            ResultsProviderParameters calculationParameters,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> lazyGroupedFiltersPerBreak,
            CrossMeasure[] crossMeasureBreaks, CancellationToken cancellationToken)
        {
            var groupedFiltersPerBreak =
                lazyGroupedFiltersPerBreak.Select(b => (b.GroupName, Filters: b.Filters.ToArray())).ToArray();

            var breakGroups = groupedFiltersPerBreak
                .Select(b =>
                {
                    var crossMeasureSignificanceComparandName = crossMeasureBreaks
                        ?.FirstOrDefault(c => GetVarCode(c.MeasureName) == b.GroupName)
                        ?.SignificanceFilterInstanceComparandName;
                    var significanceComparandName =
                        b.Filters.FirstOrDefault(b => b.Name == crossMeasureSignificanceComparandName)?.Name ??
                        b.Filters.First().Name;
                    return (b.Filters, b.GroupName, SignificanceComparandName: significanceComparandName);
                }).SelectMany(b => b.Filters, (b, fi) =>
                    (BreakName: b.GroupName, BreakInstance: new BreakInstance(fi.Name, b.SignificanceComparandName))
                ).ToArray();
            return GetCompetitionResultsWithBreaks(calculationParameters, breakGroups, cancellationToken);
        }

        public async Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFiltersLegacy(
            CuratedResultsModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks = null)
        {
            return new GroupedCrossbreakCompetitionResults
            {
                GroupedBreakResults = await breaks.ToAsyncEnumerable().SelectAwait(async grouping =>
                {
                    var comparandName = crossMeasureBreaks?.FirstOrDefault(c => GetVarCode(c.MeasureName) == grouping.GroupName)
                        ?.SignificanceFilterInstanceComparandName ?? null;
                    return new GroupedBreakResults
                    {
                        GroupName = grouping.GroupName,
                        BreakResults = await GetCompetitionResultsWithCrossbreakFiltersLegacy(model, grouping.Filters, comparandName, cancellationToken)
                    };
                }).ToArrayAsync(cancellationToken)
            };
        }

        public async Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters(
            MultiEntityRequestModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> lazyGroupedFiltersPerBreak,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks = null)
        {
            if (_measureRepository.RequiresLegacyBreakCalculation(_settings, crossMeasureBreaks))
            {
                return await GroupedCompetitionResultsWithCrossbreakFiltersLegacy(model, lazyGroupedFiltersPerBreak, crossMeasureBreaks, cancellationToken);
            }

            var calculationParameters = _requestAdapter.CreateParametersForCalculation(model, crossMeasureBreaks: crossMeasureBreaks);

            return await GroupedCompetitionResultsWithCrossbreakFilters(calculationParameters, lazyGroupedFiltersPerBreak, crossMeasureBreaks, cancellationToken);
        }

        public async Task<GroupedCrossbreakCompetitionResults> GroupedCompetitionResultsWithCrossbreakFiltersLegacy(
            MultiEntityRequestModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks,
            CrossMeasure[] crossMeasureBreaks, CancellationToken cancellationToken)
        {
            return new GroupedCrossbreakCompetitionResults
            {
                GroupedBreakResults = await breaks.ToAsyncEnumerable().SelectAwait(async grouping =>
                {
                    var comparandName = crossMeasureBreaks?.FirstOrDefault(c => GetVarCode(c.MeasureName) == grouping.GroupName)
                        ?.SignificanceFilterInstanceComparandName ?? null;
                    return new GroupedBreakResults
                    {
                        GroupName = grouping.GroupName,
                        BreakResults = await GetCompetitionResultsWithCrossbreakFiltersLegacy(model, grouping.Filters, comparandName, cancellationToken)
                    };
                }).ToArrayAsync(cancellationToken)
            };
        }

        private Task<CrossbreakCompetitionResults> GetCompetitionResultsWithCrossbreakFiltersLegacy(
            CuratedResultsModel model,
            IEnumerable<CompositeFilterModel> breaks,
            string comparandName, CancellationToken cancellationToken)
        {
            return GetCompetitionResultsWithCrossbreakFiltersLegacy(breaks,
                model.FilterModel,
                model.IncludeSignificance,
                comparandName,
                (filter) => _requestAdapter.CreateParametersForCalculation(model, filter), cancellationToken);
        }

        private Task<CrossbreakCompetitionResults> GetCompetitionResultsWithCrossbreakFiltersLegacy(
            MultiEntityRequestModel model,
            IEnumerable<CompositeFilterModel> breaks,
            string comparandName, CancellationToken cancellationToken)
        {
            return GetCompetitionResultsWithCrossbreakFiltersLegacy(breaks, model.FilterModel, model.IncludeSignificance, comparandName, 
                (filter) => _requestAdapter.CreateParametersForCalculation(model, filter), cancellationToken);
        }

        public record BreakInstance(string Name, string SignificanceComparandName);

        private async Task<GroupedCrossbreakCompetitionResults> GetCompetitionResultsWithBreaks(
            ResultsProviderParameters calculationParameters,
            (string BreakName, BreakInstance BreakInstance)[] breakInstances,
            CancellationToken cancellationToken)
        {
            var breakEntityInstances = GetEntityInstancesIfSingleBreakForSingleEntities(calculationParameters);

            var overallResultPerRequestedInstance = (await _convenientCalculator.CoalesceSingleDataPointPerEntityMeasure(calculationParameters, cancellationToken)).Single().Data
                .Select(x =>
                {
                    var weightedDailyResults = x.WeightedResult.RootAndLeaves().Skip(1).ToArray();
                    if (breakInstances.Length != weightedDailyResults.Length)
                    {
                        throw new InvalidOperationException("Missing break results");
                    }

                    return (x.EntityInstance, Results:
                        weightedDailyResults
                            .Zip(breakInstances, (wr, cc) => (ColumnId: cc, WeightedResult: wr)));
                })
                .SelectMany(x => x.Results, (perEntity, result) => (perEntity.EntityInstance, Break: result.ColumnId, DailyResult: result.WeightedResult))
                .GroupBy(r => r.Break.BreakName, e => (e.EntityInstance, e.Break.BreakInstance, e.DailyResult))
                .Select(g => GetSingleGroupCompetitionResultsWithBreaks(g, calculationParameters, breakEntityInstances));


            return new GroupedCrossbreakCompetitionResults
            {
                GroupedBreakResults = overallResultPerRequestedInstance
                    .Select(b => new GroupedBreakResults { BreakResults = b.crossbreakCompetitionResults, GroupName = b.Key })
                    .ToArray()
            };
        }

        private List<EntityInstance> GetEntityInstancesIfSingleBreakForSingleEntities(ResultsProviderParameters calculationParameters)
        {
            var breakEntityInstances = new List<EntityInstance>();

            if (calculationParameters.Breaks.Length == 1)
            {
                var breaks = calculationParameters.Breaks;
                if(breaks.Count() != 1)
                {
                    _logger.LogError($"Expected single break but found {breaks.Count()} when getting entity instances for {calculationParameters.PrimaryMeasure.Name}");
                    return breakEntityInstances;
                }

                if (breaks.Single().Variable.FieldDependencies.Count() != 1)
                {
                    _logger.LogError($"Expected single field dependency but found {breaks.Single().Variable.FieldDependencies.Count()} when getting entity instances for {calculationParameters.PrimaryMeasure.Name}");
                    return breakEntityInstances;
                }

                var variable = _variableConfigurationRepository.GetByIdentifier(breaks.Single().Variable.FieldDependencies.Single().Name);
                if (variable == null)
                {
                    _logger.LogError($"Unable to find variable with identifier {breaks.Single().Variable.FieldDependencies.Single().Name} when getting entity instances for {calculationParameters.PrimaryMeasure.Name}");
                    return breakEntityInstances;
                }

                var breakMeasures = _measureRepository.GetAll().Where(m => m.VariableConfigurationId == variable.Id);
                if (breakMeasures.Count() != 1)
                {
                    _logger.LogError($"Expected single break measure but found {breakMeasures.Count()} when getting entity instances for {calculationParameters.PrimaryMeasure.Name}");
                    return breakEntityInstances;
                }

                var breakMeasure = breakMeasures.Single();
                if (breakMeasure.EntityCombination.Count() == 1)
                {
                    breakEntityInstances = _entityRepository.GetInstancesOf(breakMeasure.EntityCombination.Single().Identifier, calculationParameters.Subset).ToList();
                }
                else
                {
                    _logger.LogError($"Expected single entity combination but found {breakMeasure.EntityCombination.Count()} when getting entity combination for {calculationParameters.PrimaryMeasure.Name}");
                    return breakEntityInstances;
                }
            }
            return breakEntityInstances;
        }

        private (string Key, CrossbreakCompetitionResults crossbreakCompetitionResults) GetSingleGroupCompetitionResultsWithBreaks(
            IGrouping<string, (EntityInstance EntityInstance, BreakInstance BreakInstance, WeightedDailyResult DailyResult)> results,
            ResultsProviderParameters calculationParameters,
            IEnumerable<EntityInstance> entityInstances)
        {
            var competitionBreakResults = results.GroupBy(x => x.BreakInstance)
                .Select(bi =>
                {
                    var breakEntityInstanceId = entityInstances.FirstOrDefault(e => e.Identifier == bi.Key.Name)?.Id;
                    var breakResults = new BreakResults
                    {
                        BreakName = bi.Key.Name,
                        BreakEntityInstanceId = breakEntityInstanceId,
                        EntityResults = bi.Select(r =>
                            new EntityWeightedDailyResults(r.EntityInstance, new[] { r.DailyResult })).ToArray(),
                    };

                    return new CompetitionBreak
                    {
                        BreakResults = breakResults,
                        PrimaryMeasure = calculationParameters.PrimaryMeasure,
                        SignificanceComparand = bi.Key.SignificanceComparandName
                    };
                }).ToArray();

            if (calculationParameters.IncludeSignificance)
            {
                MutateResultsToIncludeSignificance(competitionBreakResults, calculationParameters.SigConfidenceLevel);
            }

            var breakResultsArray = competitionBreakResults.Select(c => c.BreakResults).ToArray();

            var showSampleForMainInstance = !_productContext.IsAllVue;
            var sampleSizeEntityInstanceId =
                showSampleForMainInstance ? calculationParameters.SampleSizeEntityInstanceId : null;
            var crossbreakCompetitionResults = new CrossbreakCompetitionResults
            {
                InstanceResults = breakResultsArray,
                HasData = breakResultsArray.Any(r => r.EntityResults.HasData()),
                SampleSizeMetadata = breakResultsArray.GetSampleSizeMetadata(sampleSizeEntityInstanceId),
                LowSampleSummary = breakResultsArray.SelectMany(r =>
                        r.EntityResults.LowSampleSummaries(calculationParameters.Subset.Id,
                            _profileResponseAccessorFactory.GetOrCreate(calculationParameters.Subset).StartDate)).Distinct()
                    .ToArray()
            };
            return (results.Key, crossbreakCompetitionResults);
        }

        private async Task<CrossbreakCompetitionResults> GetCompetitionResultsWithCrossbreakFiltersLegacy(
            IEnumerable<CompositeFilterModel> breaks,
            CompositeFilterModel filterModel,
            bool includeSignificance,
            string comparandName,
            Func<CompositeFilterModel, ResultsProviderParameters> createParametersForCalculation,
            CancellationToken cancellationToken)
        {
            var results = await breaks.ToAsyncEnumerable().SelectAwait(async filter =>
            {
                var combinedFilterModel = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), new[] { filterModel, filter });
                var pam = createParametersForCalculation(combinedFilterModel);
                var entityResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken)).Single().Data;
                var results = new BreakResults
                {
                    BreakName = filter.Name,
                    EntityResults = entityResults,
                };
                var significanceComparand = breaks.FirstOrDefault(b => b.Name == comparandName)?.Name ?? breaks.First().Name;

                return new CompetitionBreak() {
                    BreakResults = results,
                    FilterModel = filterModel,
                    PrimaryMeasure = pam.PrimaryMeasure,
                    SignificanceComparand = significanceComparand
                };
            }).ToArrayAsync(cancellationToken);

            if (includeSignificance)
            {
                MutateResultsToIncludeSignificance(results);
            }

            var representativeParameters = createParametersForCalculation(results.First().FilterModel);
            var instanceResults = results.Select(r => r.BreakResults).ToArray();

            var showSampleForMainInstance = !_productContext.IsAllVue;
            var sampleSizeEntityInstanceId = showSampleForMainInstance ? representativeParameters.SampleSizeEntityInstanceId : null;

            return new CrossbreakCompetitionResults
            {
                InstanceResults = instanceResults,
                HasData = instanceResults.Any(r => r.EntityResults.HasData()),
                SampleSizeMetadata = instanceResults.GetSampleSizeMetadata(sampleSizeEntityInstanceId),
                LowSampleSummary = instanceResults.SelectMany(r => r.EntityResults.LowSampleSummaries(representativeParameters.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(representativeParameters.Subset).StartDate)).Distinct().ToArray()
            };
        }

        private static void MutateResultsToIncludeSignificance(IEnumerable<CompetitionBreak> results, SigConfidenceLevel sigConfidenceLevel = SigConfidenceLevel.NinetyFive)
        {
            foreach (var result in results)
            {
                var primaryMeasure = result.PrimaryMeasure;

                for (var index = 0; index < result.BreakResults.EntityResults.Length; index++)
                {
                    var entityResult = result.BreakResults.EntityResults[index].WeightedDailyResults[0];
                    var comparand = results.First(r => r.BreakResults.BreakName == r.SignificanceComparand);
                    var comparandResult = comparand.BreakResults.EntityResults[index].WeightedDailyResults[0];

                    MutateResultToIncludeSignificance(primaryMeasure, entityResult, comparandResult, comparand.SignificanceComparand, sigConfidenceLevel);
                }
            }
        }

        public static void MutateResultToIncludeSignificance(
            Measure primaryMeasure,
            WeightedDailyResult result,
            WeightedDailyResult comparandResult,
            string comparandName,
            SigConfidenceLevel sigConfidenceLevel)
        {
            result.Tscore = SignificanceCalculator.CalculateTScore(primaryMeasure, result, comparandResult);
            if (result.Tscore != null)
            {
                result.Significance = SignificanceCalculator.CalculateSignificance((double)result.Tscore, sigConfidenceLevel);

                if (result.Significance != Significance.None)
                {
                    var roundedDifference = (int)(Math.Round(result.WeightedResult - comparandResult.WeightedResult, 2) * 100);
                    result.SigificanceHelpText = $"{roundedDifference}% vs {comparandName}";
                }
            }
        }

        private SplitMetricResults SplitMetricResults(ResultsProviderParameters splitMetricPam,
            (string Label, WeightedDailyResult Result)[][] results)
        {
            int[] orderedResultsIndices = { };
            if (results.Length == 2)
            {
                orderedResultsIndices = Enumerable.Range(0, results[0].Length)
                    .OrderByDescending(i => results[0][i].Result?.WeightedResult - results[1][i].Result?.WeightedResult)
                    .ToArray();
            }
            else if (results.Length != 0)
            {
                orderedResultsIndices = Enumerable.Range(0, results[0].Length)
                    .OrderByDescending(i => results.Sum(r => r[i].Result.WeightedResult)).ToArray();
            }

            string[] orderedMetrics = { };
            WeightedDailyResult[][] orderedResults = { };
            var containsAnyMarketMetricOrNoResults = true;
            if (results.Length != 0)
            {
                orderedMetrics = results.First().Select((r, i) => new { result = r, indx = i })
                    .OrderBy(r => Array.IndexOf(orderedResultsIndices, r.indx)).Select(r => r.result.Label).ToArray();
                orderedResults = results.Select(rs =>
                    rs.Select((r, i) => new { result = r, indx = i })
                        .OrderBy(r => Array.IndexOf(orderedResultsIndices, r.indx)).Select(r => r.result.Result)
                        .ToArray()).ToArray();
                containsAnyMarketMetricOrNoResults = splitMetricPam.DoMeasuresIncludeMarketMetric;
            }

            var hasData = orderedResults.Any(weighted => weighted.HasData());

            var sampleSizeMetadata = splitMetricPam.GetSampleSizeMetadata(results, orderedResults, _entityRepository);

            return new SplitMetricResults
            {
                OrderedMeasures = orderedMetrics,
                OrderedResults = orderedResults,
                SampleSizeMetadata = sampleSizeMetadata,
                HasData = hasData,
                LowSampleSummary =
                    containsAnyMarketMetricOrNoResults || !splitMetricPam.LowSampleEntityInstanceId.HasValue
                        ? new LowSampleSummary[] { }
                        : orderedResults.EntityInstancesWithLowSample(OrderedEntities(splitMetricPam.EntityInstances, orderedResultsIndices),
                            splitMetricPam.Subset.Id,
                            _profileResponseAccessorFactory.GetOrCreate(splitMetricPam.Subset).StartDate)
            };
        }

        private static EntityInstance[] OrderedEntities(ImmutableArray<EntityInstance> entityInstances, int[] orderedResultsIndices)
        {
            var entityInstancesWithIndex = entityInstances.Select((result, index) => new { result, index });
            var orderedEntities = entityInstancesWithIndex.OrderBy(r => Array.IndexOf(orderedResultsIndices, r.index))
                .Select(x => x.result).ToArray();
            return orderedEntities;
        }

        public async Task<StackedProfileResults> GetStackedProfileResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, true);
            var resultsByMeasure = await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                var unweighted = await _convenientCalculator.CalculateUnweightedForMeasure(pam, cancellationToken, measure);

                var byAgeGroup = await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByAgeGroup(model.DemographicFilter, pam.Subset), cancellationToken);

                var byGender = await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByGender(model.DemographicFilter, pam.Subset), cancellationToken);

                var byRegion = await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, _breakdownCategoryFactory.ByRegion(model.DemographicFilter, pam.Subset), cancellationToken);

                var segCategory = _breakdownCategoryFactory.BySegOrNull(model.DemographicFilter, pam.Subset);
                var bySeg = segCategory == null
                    ? null
                    : await _convenientCalculator.WeightCategoryWithoutSignificance(unweighted, segCategory, cancellationToken);

                var weighted =
                    await _calculator.CalculateWeightedFromUnweighted(unweighted,
                        calculateSignificance: pam.IncludeSignificance, cancellationToken);

                return new BrokenDownResults(
                    measure,
                    pam.EntityInstances.Single(),
                    byAgeGroup.Single().Results,
                    byGender.Single().Results,
                    byRegion.Single().Results,
                    bySeg?.Single().Results,
                    weighted.Single().WeightedDailyResults);
            }, cancellationToken).ToArrayAsync(cancellationToken);

            var hasData = resultsByMeasure.HasData();
            return new StackedProfileResults
            {
                Data = resultsByMeasure,
                SampleSizeMetadata = resultsByMeasure.GetSampleSizeMetadata(),
                HasData = hasData,
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : resultsByMeasure.LowSampleSummaries(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
            };
        }

        public Task<BreakdownResults> GetBreakdownAverageResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            return GetBreakdownAverageResults(pam, model.DemographicFilter, cancellationToken);
        }

        public Task<BreakdownResults> GetBreakdownAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, alwaysIncludeActiveBrand: false);
            return GetBreakdownAverageResults(pam, model.DemographicFilter, cancellationToken);
        }

        private async Task<BreakdownResults> GetBreakdownAverageResults(ResultsProviderParameters pam,
            DemographicFilter demographicFilter, CancellationToken cancellationToken)
        {
            var marketAverageWeightings = await _convenientCalculator.GetMarketAverageUnweightedWeightingsByBaseMeasureId(pam, cancellationToken);
            var resultsByMeasure = await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                var unweightedWeightingsForMeasure = marketAverageWeightings[measure.MarketAverageBaseMeasure];
                var unweightedResultsForMeasure = await _convenientCalculator.CalculateUnweightedForMeasure(pam, cancellationToken, measure);

                var byAgeGroup = await _convenientCalculator.MarketAverageForCategory(unweightedResultsForMeasure,
                    unweightedWeightingsForMeasure,
                    _breakdownCategoryFactory.ByAgeGroup(demographicFilter, pam.Subset),
                    pam.AverageType,
                    pam.QuestionType,
                    pam.EntityMeanMaps,
                    cancellationToken);

                var byGender = await _convenientCalculator.MarketAverageForCategory(unweightedResultsForMeasure,
                    unweightedWeightingsForMeasure,
                    _breakdownCategoryFactory.ByGender(demographicFilter, pam.Subset),
                    pam.AverageType,
                    pam.QuestionType,
                    pam.EntityMeanMaps,
                    cancellationToken);

                var byRegion = await _convenientCalculator.MarketAverageForCategory(unweightedResultsForMeasure,
                    unweightedWeightingsForMeasure,
                    _breakdownCategoryFactory.ByRegion(demographicFilter, pam.Subset),
                    pam.AverageType,
                    pam.QuestionType,
                    pam.EntityMeanMaps,
                    cancellationToken);

                var segCategory = _breakdownCategoryFactory.BySegOrNull(demographicFilter, pam.Subset);
                var bySeg = segCategory == null
                    ? null
                    : await _convenientCalculator.MarketAverageForCategory(unweightedResultsForMeasure,
                    unweightedWeightingsForMeasure,
                    segCategory,
                    pam.AverageType,
                    pam.QuestionType,
                    pam.EntityMeanMaps,
                    cancellationToken);

                var measureTotals = await _convenientCalculator.CalculateMarketAverage(unweightedResultsForMeasure,
                    unweightedWeightingsForMeasure,
                    pam.Subset,
                    pam.AverageType,
                    pam.QuestionType,
                    pam.EntityMeanMaps,
                    cancellationToken);

                var entityInstance = new EntityInstance();

                return new BrokenDownResults(
                    measure,
                    entityInstance,
                    byAgeGroup,
                    byGender,
                    byRegion,
                    bySeg,
                    measureTotals);
            }, cancellationToken).ToArrayAsync(cancellationToken);

            return new BreakdownResults
            {
                Data = resultsByMeasure,
                SampleSizeMetadata = resultsByMeasure.GetSampleSizeMetadata(),
                HasData = resultsByMeasure.HasData(),
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : resultsByMeasure.LowSampleSummaries(_profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate, "Average"),
            };
        }


        public async Task<WordleResults> GetWordleResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            var results = await _convenientCalculator.CalculateWeightedForMeasure(pam, cancellationToken);
            var maxResult = results.MaxBy(w => w.UnweightedResponseCount);
            return new WordleResults
            {
                Results = results,
                SampleSizeMetadata = new SampleSizeMetadata
                {
                    SampleSize = new UnweightedAndWeightedSample
                    {
                        Unweighted = maxResult?.UnweightedResponseCount ?? 0,
                        Weighted = maxResult?.WeightedResponseCount ?? 0
                    },
                    CurrentDate = results.FirstOrDefault()?.WeightedDailyResults.FirstOrDefault()?.Date
                },
                HasData = results.Any(result => result.WeightedDailyResults.Count > 0)
            };
        }

        public async Task<RawTextResults> GetRawTextResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, alwaysIncludeActiveBrand: false);
            var measure = pam.Measures.First();
            var fieldDefinitionModel = measure.PrimaryFieldDependencies.Single().GetDataAccessModel(model.SubsetId);

            var resultFilterInstances = pam.RequestedInstances.OrderedInstances.Any() ? new[] { (pam.RequestedInstances.EntityType, Id: pam.RequestedInstances.SortedEntityInstanceIds.Single()) } : Array.Empty<(EntityType EntityType, int Id)>();

            var filterValues = fieldDefinitionModel.OrderedEntityColumns.Join(resultFilterInstances, c => c.EntityType, f => f.EntityType, (c, f) => (Location: c.DbLocation, f.Id)).ToArray();
            var responseIds = await _convenientCalculator.CalculateRespondentIdsForMeasure(pam, cancellationToken);

            var surveyIds = pam.Subset.SurveyIdToSegmentNames.Keys.ToArray();
            var rawText = _textResponseRepository.GetRawTextTrimmed(responseIds, fieldDefinitionModel.UnsafeSqlVarCodeBase, filterValues, surveyIds);

            return new RawTextResults()
            {
                Text = rawText.Select(t => t.Text).ToArray(),
                SampleSizeMetadata = new SampleSizeMetadata
                {
                    SampleSize = new UnweightedAndWeightedSample
                    {
                        Unweighted = rawText.Length,
                        Weighted = rawText.Length
                    }
                },
                HasData = rawText.Any()
            };
        }

        public ResultsProviderParameters ResultsProviderParameters(CuratedResultsModel modelCuratedResultsModel)
        {
            return _requestAdapter.CreateParametersForCalculation(modelCuratedResultsModel);
        }

        public ResultsProviderParameters ResultsProviderParametersMultiEntity(MultiEntityRequestModel model)
        {
            return _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
        }

        public async Task<MultiMetricResults> GetMultiMetricResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);

            var measureResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken))
                .Select(r => (Measure: r.Measure, Result: r.Data)).ToArray();

            if (pam.AreMeasuresOrderedByResult)
            {
                Array.Sort(
                    measureResults,
                    ((Measure, EntityWeightedDailyResults[]) result1,
                        (Measure, EntityWeightedDailyResults[]) result2) =>
                    {
                        var entityResult1 = result1.Item2[pam.MultiMetricEntityInstanceIndex];
                        var entityResult2 = result2.Item2[pam.MultiMetricEntityInstanceIndex];

                        if (entityResult1.WeightedDailyResults.Any() && entityResult2.WeightedDailyResults.Any())
                            return -entityResult1.WeightedDailyResults.LastOrDefault().WeightedResult
                                .CompareTo(entityResult2.WeightedDailyResults.LastOrDefault().WeightedResult);
                        return (entityResult1.EntityInstance?.Name ?? String.Empty)
                            .CompareTo(entityResult2.EntityInstance?.Name ?? String.Empty);
                    });
            }

            var orderedMeasures = new string[measureResults.Length];

            MultiMetricSeries multiMetricResults;
            MultiMetricSeries[] comparisonSeries;
            if (measureResults.All(m => m.Result.LastOrDefault()?.EntityInstance == null))
            {
                multiMetricResults = new MultiMetricSeries
                {
                    EntityInstance = null,
                    OrderedData = new WeightedDailyResult[measureResults.Length][]
                };

                comparisonSeries = new MultiMetricSeries[0];
            }
            else
            {
                multiMetricResults = new MultiMetricSeries
                {
                    EntityInstance = pam.EntityInstances[pam.MultiMetricEntityInstanceIndex],
                    OrderedData = new WeightedDailyResult[measureResults.Length][]
                };

                comparisonSeries = new MultiMetricSeries[pam.EntityInstances.Length - 1];
            }

            double sampleSize = 0.0;
            double weightedSample = 0;

            for (int measureIndex = 0; measureIndex < measureResults.Length; ++measureIndex)
            {
                var resultsForMeasure = measureResults[measureIndex];
                orderedMeasures[measureIndex] = resultsForMeasure.Measure.Name;

                for (int resultsIndex = 0; resultsIndex < resultsForMeasure.Result.Length; ++resultsIndex)
                {
                    var resultsForEntityInstance = resultsForMeasure.Result[resultsIndex];

                    if (resultsIndex == pam.MultiMetricEntityInstanceIndex)
                    {
                        var current = resultsForEntityInstance.WeightedDailyResults.LastOrDefault();
                        if ((current?.UnweightedSampleSize ?? 0) > sampleSize)
                        {
                            sampleSize = current?.UnweightedSampleSize ?? 0;
                            weightedSample = current?.WeightedSampleSize ?? 0;
                        }

                        multiMetricResults.OrderedData[measureIndex] =
                            resultsForEntityInstance.WeightedDailyResults.ToArray();
                    }
                    else
                    {
                        var isLess = resultsIndex < pam.MultiMetricEntityInstanceIndex;
                        MultiMetricSeries comparisonEntityResults =
                            comparisonSeries[isLess ? resultsIndex : resultsIndex - 1];

                        if (comparisonEntityResults == null)
                        {
                            comparisonEntityResults = new MultiMetricSeries
                            {
                                EntityInstance = resultsForEntityInstance.EntityInstance,
                                OrderedData = new WeightedDailyResult[measureResults.Length][]
                            };
                            comparisonSeries[isLess ? resultsIndex : resultsIndex - 1] = comparisonEntityResults;
                        }

                        comparisonEntityResults.OrderedData[measureIndex] =
                            resultsForEntityInstance.WeightedDailyResults.ToArray();
                    }
                }
            }

            var hasData = sampleSize > 0 || multiMetricResults.HasData() || comparisonSeries.HasData();
            Dictionary<string, UnweightedAndWeightedSample> sampleSizeByMetric = null;
            if (hasData && multiMetricResults.OrderedData.Any())
            {
                if (multiMetricResults.OrderedData.Any(x => (x.LastOrDefault()?.UnweightedSampleSize ?? sampleSize) != sampleSize))
                {
                    sampleSizeByMetric = new Dictionary<string, UnweightedAndWeightedSample>();
                    for (int index = 0; index < multiMetricResults.OrderedData.Length; index++)
                    {
                        var result = multiMetricResults.OrderedData[index].LastOrDefault();
                        if (result != null)
                        {
                            sampleSizeByMetric[orderedMeasures[index]] = new UnweightedAndWeightedSample
                            {
                                Unweighted = result.UnweightedSampleSize,
                                Weighted = result.WeightedSampleSize
                            };
                        }
                    }
                }
            }
            var finalResults = new MultiMetricResults
            {
                OrderedMeasures = orderedMeasures,
                ActiveSeries = multiMetricResults,
                ComparisonSeries = comparisonSeries.OrderReverseAlphabeticByEntityInstanceName(),
                SampleSizeMetadata = new SampleSizeMetadata
                {
                    SampleSize = new UnweightedAndWeightedSample
                    {
                        Unweighted = sampleSize,
                        Weighted = weightedSample
                    },
                    CurrentDate = multiMetricResults.OrderedData.FirstOrDefault()?.GetSampleSizeMetadata().CurrentDate,
                    SampleSizeByMetric = sampleSizeByMetric,
                },
                HasData = hasData,
                LowSampleSummary = Array.Empty<LowSampleSummary>()
            };

            if (!pam.DoMeasuresIncludeMarketMetric)
            {
                var subsetStartDate = _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate;
                var activeBrandSummary = multiMetricResults.LowSampleSummaries(orderedMeasures,
                    pam.Subset.Id, subsetStartDate);
                var comparisonBrandSummary = comparisonSeries.LowSampleSummaries(orderedMeasures,
                    pam.Subset.Id, subsetStartDate);
                finalResults.LowSampleSummary = activeBrandSummary.Concat(comparisonBrandSummary).ToArray();
            }

            return finalResults;
        }

        public async Task<MultiMetricAverageResults> GetMultiMetricAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, alwaysIncludeActiveBrand: false);
            var averageResults = (await _convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(pam, cancellationToken))
                .Select(x => new MetricWeightedDailyResult()
                { MetricName = x.Measure.Name, WeightedDailyResult = x.AveragedResults.LastOrDefault() })
                .ToArray();

            return new MultiMetricAverageResults
            {
                Average = averageResults,
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : averageResults.LowSampleSummaries(_profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate, "Average"),
                HasData = true,
            };
        }
        
        public async Task<FunnelResults> GetFunnelResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model,
                alwaysIncludeActiveBrand: true);
            if (pam.CalculationPeriod.Periods.Length > 1) //This API Only supports a single period
            {
                pam.CalculationPeriod.Periods = new[] { pam.CalculationPeriod.Periods.Last() };
            }

            var averagesAndResultsForAllMetrics = await _convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(pam, cancellationToken);

            var metricResultsForEntities = averagesAndResultsForAllMetrics
                .SelectMany(averagesAndResultsForMetric => averagesAndResultsForMetric.PerEntityInstanceResults,
                    (averagesAndResultsForMetric, perEntityInstanceResult) => (
                        perEntityInstanceResult.EntityInstance,
                        MetricResult: new MetricWeightedDailyResult
                        {
                            MetricName = averagesAndResultsForMetric.Measure.Name,
                            WeightedDailyResult = perEntityInstanceResult.WeightedDailyResults.Single()
                        }
                    )
                ).GroupBy(metricResultsForEntity => metricResultsForEntity.EntityInstance)
                .Select(allMetricResultsByEntity => new MetricResultsForEntity
                {
                    EntityInstance = allMetricResultsByEntity.Key,
                    MetricResults = allMetricResultsByEntity.Select(mr => mr.MetricResult).ToArray()
                })
                .OrderBy(r => r.EntityInstance.Name)
                .ToArray();

            var sampleSizeMetaData = GetSampleSizeMetaData(metricResultsForEntities, pam);

            var hasData = metricResultsForEntities.Any(resultsForEntity =>
                resultsForEntity.MetricResults.Any(metricResultForEntity =>
                    metricResultForEntity.WeightedDailyResult.UnweightedSampleSize > 0));

            var entitiesWithAnyLowSampleMetric = metricResultsForEntities
                .Where(metricResultsForEntity => metricResultsForEntity.MetricResults.Any(metricResultForEntity =>
                    metricResultForEntity.WeightedDailyResult.UnweightedSampleSize <= LowSampleExtensions.LowSampleThreshold))
                .Select(metricResultForEntity => new LowSampleSummary
                {
                    EntityInstanceId = metricResultForEntity.EntityInstance.Id
                })
                .ToArray();

            var averagePerMeasures = averagesAndResultsForAllMetrics
                .Select(r => new MetricWeightedDailyResult()
                {
                    MetricName = r.Measure.Name,
                    WeightedDailyResult = r.AveragedResults.Single()
                }).ToArray();

            return new FunnelResults
            {
                Results = metricResultsForEntities,
                MarketAveragePerMeasures = averagePerMeasures,
                HasData = hasData,
                SampleSizeMetadata = sampleSizeMetaData,
                LowSampleSummary = entitiesWithAnyLowSampleMetric
            };
        }

        private static SampleSizeMetadata GetSampleSizeMetaData(MetricResultsForEntity[] metricResultsForEntities,
            ResultsProviderParameters pam)
        {
            if (pam.FocusEntityInstanceId.HasValue)
            {
                var focusResult = metricResultsForEntities
                    .Single(m => m.EntityInstance.Id == pam.FocusEntityInstanceId)
                    .MetricResults
                    .First()
                    .WeightedDailyResult;

                return new SampleSizeMetadata
                {
                    SampleSize = new UnweightedAndWeightedSample
                    {
                        Unweighted = focusResult.UnweightedSampleSize,
                        Weighted = focusResult.WeightedSampleSize
                    },
                    CurrentDate = focusResult.Date
                };
            }

            var sampleSizeByEntity = metricResultsForEntities
                .ToDictionary(r => r.EntityInstance.Name, r => new UnweightedAndWeightedSample
                {
                    Unweighted = r.MetricResults.First().WeightedDailyResult.UnweightedSampleSize,
                    Weighted = r.MetricResults.First().WeightedDailyResult.WeightedSampleSize
                });

            var anyResult = metricResultsForEntities
                .First()
                .MetricResults
                .First()
                .WeightedDailyResult;

            return new SampleSizeMetadata
            {
                SampleSizeByEntity = sampleSizeByEntity,
                CurrentDate = anyResult.Date
            };
        }

        public async Task<ScorecardPerformanceResults> GetScorecardPerformanceResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var focusInstancePam = _requestAdapter.CreateParametersForCalculation(model);
            focusInstancePam.IncludeSignificance = true;
            AssertIsValidScorecardAverage(focusInstancePam);

            var results = (await _convenientCalculator.GetCuratedResultsForAllMeasures(focusInstancePam, cancellationToken))
                .Select(r => new ScorecardPerformanceMetricResult
                {
                    MetricName = r.Measure.Name,
                    PeriodResults = r.Data.Single().WeightedDailyResults,
                }).ToArray();

            bool hasData = results.HasData();

            return new ScorecardPerformanceResults
            {
                MetricResults = results,
                SampleSizeMetadata = results.GetSampleSizeMetadata(),
                HasData = hasData,
                LowSampleSummary = focusInstancePam.LowSampleEntityInstanceId.HasValue
                    ? results.EntityInstanceIdsWithLowSample(focusInstancePam.LowSampleEntityInstanceId.Value,
                        focusInstancePam.EntityRepository, focusInstancePam.RequestedInstances.EntityType,
                        focusInstancePam.Subset, _profileResponseAccessorFactory.GetOrCreate(focusInstancePam.Subset).StartDate)
                    : new LowSampleSummary[] { }
            };
        }

        public async Task<ScorecardPerformanceCompetitorResults> GetScorecardPerformanceResultsAverage(
            CuratedResultsModel model, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            AssertIsValidScorecardAverage(pam);

            var results = (await _convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(pam, cancellationToken)).Select(fullResults =>
            {
                var competitorData = fullResults.PerEntityInstanceResults.Select(result =>
                    new ScorecardPerformanceCompetitorDataResult
                    {
                        EntityInstance = result.EntityInstance,
                        Result = result.WeightedDailyResults
                            .LastOrDefault() //To be consistent, should really have returned multiple results if that's what's asked for
                    }).ToArray();

                return new ScorecardPerformanceCompetitorsMetricResult()
                {
                    MetricName = fullResults.Measure.Name,
                    CompetitorData = competitorData,
                    CompetitorAverage =
                        fullResults.AveragedResults.LastOrDefault()?.WeightedResult ??
                        0 //To be consistent, should really have returned multiple results if that's what's asked for
                };
            }).ToArray();
            return new ScorecardPerformanceCompetitorResults
            {
                MetricResults = results,
                HasData = results.HasData(),
                LowSampleSummary = pam.LowSampleEntityInstanceId.HasValue
                    ? results.EntityInstanceIdsWithLowSampleForAverage(pam.LowSampleEntityInstanceId.Value,
                        pam.EntityRepository, pam.RequestedInstances.EntityType, pam.Subset,
                        _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
                    : new LowSampleSummary[] { }
            };
        }

        public async Task<ScorecardVsKeyCompetitorsResults> GetScorecardVsKeyCompetitorsResults(
            CuratedResultsModel model, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            pam.IncludeSignificance = true;
            AssertIsValidScorecardAverage(pam);

            var results = await pam.Measures.AsAsyncParallel().AsOrdered().SelectAwait(async measure =>
            {
                var allBrandResults = await _convenientCalculator.CalculateWeightedForMeasure(pam, cancellationToken, measure);

                var activeBrandResults = allBrandResults[pam.ScorecardEntityInstanceIndex];
                var activeBrandResult = new ScorecardVsKeyCompetitorsMetricEntityResult
                {
                    EntityInstance = activeBrandResults.EntityInstance,
                    Current = activeBrandResults.WeightedDailyResults.SingleOrDefault(weightedResult =>
                        weightedResult.Date == pam.CalculationPeriod.EndDate),
                    Previous = activeBrandResults.WeightedDailyResults.OrderByDescending(x => x.Date).FirstOrDefault(
                        weightedResult =>
                            weightedResult.Date < pam.CalculationPeriod.EndDate)
                };

                var peerData = new ScorecardVsKeyCompetitorsMetricEntityResult[pam.EntityInstances.Length - 1];

                for (var instanceIndex = 0; instanceIndex < pam.EntityInstances.Length; ++instanceIndex)
                {
                    if (instanceIndex == pam.ScorecardEntityInstanceIndex)
                    {
                        continue;
                    }

                    var peerBrandResults = allBrandResults[instanceIndex];
                    var scorecardForPeer = new ScorecardVsKeyCompetitorsMetricEntityResult
                    {
                        EntityInstance = peerBrandResults.EntityInstance,
                        Current = peerBrandResults.WeightedDailyResults.SingleOrDefault(weightedResult =>
                            weightedResult.Date == pam.CalculationPeriod.EndDate),
                        Previous = peerBrandResults.WeightedDailyResults.OrderByDescending(x => x.Date).FirstOrDefault(
                            weightedResult =>
                                weightedResult.Date < pam.CalculationPeriod.EndDate)
                    };

                    peerData[
                            instanceIndex > pam.ScorecardEntityInstanceIndex
                                ? instanceIndex - 1
                                : instanceIndex]
                        = scorecardForPeer;
                }

                Array.Sort(
                    peerData,
                    (result1, result2)
                        => StringComparer.OrdinalIgnoreCase.Compare(
                            result1.EntityInstance.Name,
                            result2.EntityInstance.Name));

                return new ScorecardVsKeyCompetitorsMetricResults
                {
                    MetricName = measure.Name,
                    ActiveEntityResult = activeBrandResult,
                    KeyCompetitorResults = peerData
                };
            }, cancellationToken).ToArrayAsync(cancellationToken);

            bool hasData = results.HasData();
            return new ScorecardVsKeyCompetitorsResults
            {
                MetricResults = results,
                SampleSizeMetadata = results.GetSampleSizeMetadata(),
                HasData = hasData,
                LowSampleSummary = pam.LowSampleEntityInstanceId.HasValue
                    ? results.EntityInstanceIdsWithLowSample(pam.LowSampleEntityInstanceId.Value, pam.EntityRepository,
                        pam.RequestedInstances.EntityType, pam.Subset, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
                    : new LowSampleSummary[] { }
            };
        }

        public async Task<CategoryResult[]> GetProfileResultsForMultipleEntities(MultiEntityProfileModel profileModel, CancellationToken cancellationToken)
        {
            var period = profileModel.Period;
            var measures = profileModel.MeasureNames.Select(mn => _measureRepository.Get(mn)).ToArray();
            var databaseCalculationMeasures = measures.Where(m => SuitableForDatabaseAssistance(profileModel, m)).ToArray();

            // Use metric configuration repository to pick up variables
            var metricConfigurations = profileModel.MeasureNames.Select(mn => _metricConfigurationRepository.Get(mn)).ToArray();
            var calculationEngineMeasures = metricConfigurations.Where(m => databaseCalculationMeasures.All(dm => dm.Name != m.Name));

            var databaseMeasureResults = _profileResultsCalculator
                .GetResults(databaseCalculationMeasures, profileModel.SubsetId, period.ComparisonDates, period.Average, profileModel.DataRequest.EntityInstanceIds,
                    profileModel.ActiveEntityId, _requestScope.Organization);

            var calculationEngineResults = await calculationEngineMeasures
                .ToAsyncEnumerable().SelectAwait(async m => await CreateCategoryResults(m.Name,
                    profileModel.SubsetId,
                    profileModel.Period,
                    profileModel.DataRequest,
                    profileModel.ActiveEntityId,
                    profileModel.IncludeMarketAverage,
                    profileModel.OverriddenBaseVariableIds,
                    false,
                    SigConfidenceLevel.NinetyFive,
                    cancellationToken)).ToArrayAsync(cancellationToken);
            return databaseMeasureResults.Concat(calculationEngineResults.SelectMany(x => x)).ToArray();
        }

        private static void AssertIsValidScorecardAverage(ResultsProviderParameters resultsProviderParameters)
        {
            if (resultsProviderParameters.Average.TotalisationPeriodUnit != TotalisationPeriodUnit.Month && resultsProviderParameters.Average.MakeUpTo != MakeUpTo.WeekEnd)
            {
                throw new ArgumentOutOfRangeException(nameof(resultsProviderParameters.Average.TotalisationPeriodUnit));
            }
        }

        private bool SuitableForDatabaseAssistance(MultiEntityProfileModel profileModel, Measure m) =>
            _settings.UseDatabaseAssistedCalculationsForAudiences &&
            profileModel.DataRequest.EntityInstanceIds.Contains(profileModel.ActiveEntityId) && //TODO: We didn't get time to handle the database query where the active brand is to be excluded from the average
            SuitableForDatabaseAssistance(m, profileModel.SubsetId);

        /// <summary>
        /// This is currently quite restrictive and there's no reason with a few simple changes the <see cref="ProfileResultsCalculator"/> could handle other combinations
        /// but for now it's nice to keep it simple until we work more on it.
        /// </summary>
        internal static bool SuitableForDatabaseAssistance(Measure m, string subsetId) =>
            m.CalculationType == CalculationType.YesNo &&
            m.EntityCombination.Count() == 2 &&
            !m.IsUsingFieldExpressions &&
            m.HasDistinctBaseField &&
            m.BaseField.EntityCombination.OnlyOrDefault() is { IsBrand: true } &&
            m.Field.EntityCombination.OnlyOrDefault() is { IsBrand: false } &&
            m.Field.GetDataAccessModel(subsetId).QuestionModel.MasterType == "CHECKBOX";

        private async Task<CategoryResult[]> CreateCategoryResults(string measureName,
            string subsetId,
            Period period,
            EntityInstanceRequest dataRequest,
            int activeEntityId,
            bool includeMarketAverage,
            [CanBeNull] int[] overriddenBaseVariableIds,
            bool includeSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            CancellationToken cancellationToken)
        {
            var overrideBaseExpressions = new List<BaseExpressionDefinition>();
            if (overriddenBaseVariableIds != null)
            {
                foreach (var baseVariableId in overriddenBaseVariableIds)
                {
                    var baseVariable = _variableConfigurationRepository.Get(baseVariableId);
                    if (baseVariable == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(baseVariableId), baseVariableId, $"Base variable id '{baseVariableId}' not found.");
                    }

                    overrideBaseExpressions.Add(new BaseExpressionDefinition
                    {
                        BaseVariableId = baseVariable.Id,
                        BaseMeasureName = measureName,
                        BaseType = BaseDefinitionType.AllRespondents
                    });
                }
            }

            var pam = CreateParametersForCalculation(measureName,
                subsetId,
                period,
                dataRequest,
                overrideBaseExpressions.ToArray(),
                includeSignificance,
                sigConfidenceLevel);
            var subset = _subsetRepository.Get(subsetId);
            if (!pam.PrimaryMeasure.EntityCombination.Any(e => e.IsBrand))
            {
                throw new ArgumentOutOfRangeException("Measure", measureName, $@"Measure '{measureName}' is not a brand metric.");
            }

            var entityCount = pam.PrimaryMeasure.EntityCombination.Count();
            if (entityCount == 1)
            {
                //This is the brand only case
                return await GetBrandVectorResult(pam, subset, activeEntityId, includeMarketAverage, cancellationToken);
            }

            //This is the multi entity case
            var otherEntityTypes = pam.PrimaryMeasure.EntityCombination.Where(e => !e.IsBrand);
            var instancesOfAllTypes = otherEntityTypes.Select(type => pam.EntityRepository.GetInstancesOf(type.Identifier, pam.Subset).Select(instance => (Type: type, Instance: instance)));
            var cartesianProduct = instancesOfAllTypes.CartesianProduct(_settings.MaxCartesianProductSize);
            var results = new List<CategoryResult>();
            foreach (var instances in cartesianProduct)
            {
                var filters = instances.Select(i => new EntityInstanceRequest(i.Type.Identifier, new []{ i.Instance.Id})).ToArray();
                var pamWithFilter = CreateParametersForCalculation(measureName,
                    subsetId,
                    period,
                    dataRequest,
                    overrideBaseExpressions.ToArray(),
                    includeSignificance,
                    sigConfidenceLevel,
                    filters);
                var entityInstanceName = string.Join(", ", instances.Select(i => i.Instance.Name));
                results.AddRange(await GetBrandVectorResult(pamWithFilter, subset, activeEntityId, includeMarketAverage, cancellationToken, entityInstanceName));
            }
            return results.ToArray();
        }

        private ResultsProviderParameters CreateParametersForCalculation(string measureName,
            string subsetId,
            Period period,
            EntityInstanceRequest dataRequest,
            BaseExpressionDefinition[] overrideBaseExpressions,
            bool includeSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            EntityInstanceRequest[] filterBy = null)
        {
            var multiRequestModel = new MultiEntityRequestModel(measureName,
                subsetId,
                period,
                dataRequest,
                filterBy,
                new DemographicFilter(_filters),
                new CompositeFilterModel(),
                Array.Empty<MeasureFilterRequestModel>(),
                overrideBaseExpressions,
                includeSignificance,
                sigConfidenceLevel);

            return _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(multiRequestModel);
        }

        private async Task<CategoryResult[]> GetBrandVectorResult(ResultsProviderParameters pam, Subset subset,
            int activeEntityId,
            bool includeMarketAverages, CancellationToken cancellationToken, string entityInstanceName = null)
        {
            if (includeMarketAverages)
            {
                var curatedMarketAverageResultsForAllMeasures = await _convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(pam, cancellationToken);
                return await curatedMarketAverageResultsForAllMeasures.ToAsyncEnumerable().SelectAwait(async x =>
                    await GetCategoryResult(pam, subset, activeEntityId, entityInstanceName, x.PerEntityInstanceResults,
                        x.Measure, cancellationToken, x.AveragedResults.Single().WeightedResult)).ToArrayAsync(cancellationToken);
            }
            else
            {
                var curatedMarketResultsForAllMeasures = await _convenientCalculator.GetCuratedMarketResultsForAllMeasures(pam, cancellationToken);

                return await curatedMarketResultsForAllMeasures.ToAsyncEnumerable().SelectAwait(async x =>
                    await GetCategoryResult(pam, subset, activeEntityId, entityInstanceName, x.PerEntityInstanceResults,
                        x.Measure, cancellationToken)).ToArrayAsync(cancellationToken);
            }
        }

        private async Task<CategoryResult> GetCategoryResult(ResultsProviderParameters pam, Subset subset,
            int activeEntityId,
            string entityInstanceName,
            EntityWeightedDailyResults[] perEntityInstanceResults, Measure measure, CancellationToken cancellationToken,
            double? averageResult = null)
        {
            //PERF: If the focus instance is in the average then just pluck it out of the market average results
            if (pam.RequestedInstances.SortedEntityInstanceIds.Contains(activeEntityId))
            {
                var pluckedWeightedDailyResult = perEntityInstanceResults
                    .Single(r => r.EntityInstance.Id == activeEntityId)
                    .WeightedDailyResults.Single();
                return new CategoryResult(measure.Name, entityInstanceName, pluckedWeightedDailyResult,
                    averageResult, measure.BaseVariableConfigurationId);
            }

            //We need to get the focus instance result separately
            var focusInstance =
                pam.EntityRepository.TryGetInstance(subset, pam.RequestedInstances.EntityType.Identifier, activeEntityId,
                    out var instance)
                    ? instance
                    : throw new BadRequestException("The focused instance was not valid");
            pam.RequestedInstances = new TargetInstances(pam.RequestedInstances.EntityType, focusInstance.Yield());
            var weightedResults = (await _convenientCalculator.CalculateWeightedForMeasure(pam, cancellationToken)).Single();
            var weightedDailyResult = weightedResults.WeightedDailyResults.Single();
            return new CategoryResult(measure.Name, entityInstanceName, weightedDailyResult, averageResult,
                measure.BaseVariableConfigurationId);
        }

        public Task<CompetitionResults> GetCompetitionResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var pams = _requestAdapter.CreateCalculationParametersPerPeriod(model);
            return GetCompetitionResultsInternal(pams, cancellationToken);
        }

        private async Task<CompetitionResults> GetCompetitionResultsInternal(
            IReadOnlyCollection<ResultsProviderParameters> pams, CancellationToken cancellationToken)
        {
            if (pams.Count == 0)
            {
                throw new ArgumentException("Must provide at least one result providers parameters instance");
            }

            if (pams.Any(p => p.CalculationPeriod.Periods.Length != 1))
            {
                throw new ArgumentException("Parameters for competition result must contain only one period");
            }

            var periodResults = await pams.ToAsyncEnumerable().SelectAwait(async p =>
            {
                var periodResult = (await _convenientCalculator.GetCuratedResultsForAllMeasures(p, cancellationToken)).Single();
                return new PeriodResult
                {
                    Period = p.CalculationPeriod.Periods.Single(),
                    ResultsPerEntity = periodResult.Data
                };
            }).ToArrayAsync(cancellationToken);

            var latestPeriodResults = periodResults.OrderByDescending(r => r.Period.EndDate).First().ResultsPerEntity;
            var representativeParameters = pams.First();

            return new CompetitionResults
            {
                PeriodResults = periodResults,
                HasData = periodResults.Any(r => r.ResultsPerEntity.HasData(true)),

                SampleSizeMetadata = representativeParameters.FocusEntityInstanceId == null ? latestPeriodResults.GetSampleSizeMetadata() : GetFocusEntitySampleSizeMetadata(representativeParameters, latestPeriodResults),

                LowSampleSummary = representativeParameters.DoMeasuresIncludeMarketMetric
                    ? Array.Empty<LowSampleSummary>()
                    : periodResults.SelectMany(r => r.ResultsPerEntity.LowSampleSummariesWithDates(representativeParameters.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(representativeParameters.Subset).StartDate)).Distinct().ToArray()
            };
        }

        public Task<OverTimeResults> GetOverTimeResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            var resultsProviderParameters = _requestAdapter.CreateParametersForCalculation(model);
            return GetOverTimeResults(resultsProviderParameters, cancellationToken);
        }

        public Task<OverTimeResults> GetUnorderedOverTimeResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var resultsProviderParameters = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            return GetOverTimeResults(resultsProviderParameters, cancellationToken);
        }

        private async Task<OverTimeResults> GetOverTimeResults(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            var entityWeightedDailyResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken))[0].Data;

            var sampleSizeMetadata = pam.FocusEntityInstanceId == null ?
                entityWeightedDailyResults.GetSampleSizeMetadata() :
                GetFocusEntitySampleSizeMetadata(pam, entityWeightedDailyResults);

            entityWeightedDailyResults = entityWeightedDailyResults.OrderByFocusEntityInstanceAndThenAlphabeticByEntityInstanceName(pam.FocusEntityInstanceId);

            return new OverTimeResults
            {
                EntityWeightedDailyResults = entityWeightedDailyResults,
                SampleSizeMetadata = sampleSizeMetadata,
                HasData = entityWeightedDailyResults.HasData(),
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? []
                    : entityWeightedDailyResults.LowSampleSummaries(pam.Subset.Id, _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate)
            };
        }

        private static SampleSizeMetadata GetFocusEntitySampleSizeMetadata(ResultsProviderParameters pam,
            IEnumerable<EntityWeightedDailyResults> entityWeightedDailyResults)
        {
            var weightedResultsForSampleSize = entityWeightedDailyResults.FirstOrDefault(b => b.EntityInstance?.Id == pam.SampleSizeEntityInstanceId)
                ?? entityWeightedDailyResults.First();
            return weightedResultsForSampleSize.WeightedDailyResults.ToArray().GetSampleSizeMetadata();
        }

        public async Task<OverTimeSingleAverageResultsForMetric[]> GetOverTimeAverageResults(CuratedResultsModel model,
            AverageType averageType, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, alwaysIncludeActiveBrand: false);
            pam.AverageType = averageType;
            return await GetOverTimeAverageResults(pam, cancellationToken);
        }

        public async Task<OverTimeAverageResults> GetUnorderedOverTimeAverageResults(MultiEntityRequestModel model,
            AverageType averageType, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            pam.AverageType = averageType;
            return (await GetOverTimeAverageResults(pam, cancellationToken)).Single().Results;
        }

        public async Task<OverTimeSingleAverageResultsForMetric[]> GetUnorderedOverTimeAverageResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculationWithAdditionalFilter(model);
            return await GetOverTimeAverageResults(pam, cancellationToken);
        }

        private async Task<OverTimeSingleAverageResultsForMetric[]> GetOverTimeAverageResults(ResultsProviderParameters pam,
            CancellationToken cancellationToken)
        {
            OverTimeSingleAverageResultsForMetric[] results = default;
            if (pam.AverageType == AverageType.Mentions)
            {
                results = (await _convenientCalculator.GetAverageMentionsResultForAllMeasures(pam, cancellationToken)).Select(r =>
                    OverTimeSingleAverageResultsForMeasure(pam, [r.WeightedDailyResult], r.Measure)).ToArray();
            }
            else if (pam.Measures.Count == 1 && pam.PrimaryMeasure.IsNumericVariable)
            {
                results = [OverTimeSingleAverageResultsForMeasure(pam, await _convenientCalculator.GetNumericRespondentAverageResult(pam, pam.PrimaryMeasure.NumericVariableField, cancellationToken), pam.PrimaryMeasure)];
            }
            else
            {
                results = (await _convenientCalculator.GetCuratedMarketAverageResultsForAllMeasures(pam, cancellationToken)).Select(r =>
                    OverTimeSingleAverageResultsForMeasure(pam, r.AveragedResults, r.Measure)).ToArray();
            }

            return results;
        }

        private OverTimeSingleAverageResultsForMetric OverTimeSingleAverageResultsForMeasure(ResultsProviderParameters pam, IList<WeightedDailyResult> weightedDailyResults, Measure measure)
        {
            return new OverTimeSingleAverageResultsForMetric()
            {
                Results = new OverTimeAverageResults
                {
                    AverageType = pam.AverageType,
                    WeightedDailyResults = weightedDailyResults.ToArray(),
                    LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                        ? []
                        : weightedDailyResults.LowSampleSummaries(_profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate, "Average"),
                    HasData = true,
                }, Measure = measure
            };
        }

        public Task<OverTimeResults> GetOverviewResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            pam.IncludeSignificance = true;
            return GetOverTimeResults(pam, cancellationToken);
        }

        public async Task<ImpactMapResults> GetImpactMapResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model);
            var curatedResultsForAllMeasures = _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken);
            var currentPeriod = (await curatedResultsForAllMeasures).Select(x => new ResultsForMeasure
            {
                Measure = x.Measure,
                Data = x.Data.Select(b => new EntityWeightedDailyResults(b.EntityInstance,
                    b.WeightedDailyResults.Where(w => w.Date == pam.CalculationPeriod.EndDate).ToList())).ToArray()
            }).ToArray();


            var previousPeriod = (await curatedResultsForAllMeasures).Select(x => new ResultsForMeasure
            {
                Measure = x.Measure,
                Data = x.Data.Select(b => new EntityWeightedDailyResults(b.EntityInstance,
                    pam.CalculationPeriod.Periods.Length > 1
                        ? b.WeightedDailyResults.Where(w => w.Date == pam.CalculationPeriod.Periods.First().EndDate)
                            .ToList()
                        : new List<WeightedDailyResult>())).ToArray()
            }).ToArray();


            var entityInstancePredicate = new Func<ResultsForMeasure, EntityWeightedDailyResults>(r =>
                pam.SampleSizeEntityInstanceId.HasValue
                    ? r.Data.Single(w => w.EntityInstance.Id == pam.SampleSizeEntityInstanceId)
                    : r.Data.First());

            var metaForEachMetric = currentPeriod
                .Select(b => new
                {
                    b.Measure.Name,
                    SampleSizeMeta = entityInstancePredicate(b).WeightedDailyResults.ToArray().GetSampleSizeMetadata()
                })
                .ToArray();

            var sampleSizeMeta = new SampleSizeMetadata
            {
                SampleSize = metaForEachMetric.Max(r => r.SampleSizeMeta.SampleSize),
                SampleSizeByMetric = metaForEachMetric.ToDictionary(m => m.Name, m => m.SampleSizeMeta.SampleSize),
                CurrentDate = metaForEachMetric[0].SampleSizeMeta.CurrentDate
            };

            bool hasData
                = sampleSizeMeta.SampleSize.Unweighted > 0
                  || currentPeriod[0].HasData(true)
                  || currentPeriod[1].HasData(true)
                  || previousPeriod[0].HasData(true)
                  || previousPeriod[1].HasData(true);

            var subsetStartDate = _profileResponseAccessorFactory.GetOrCreate(pam.Subset).StartDate;
            return new ImpactMapResults
            {
                Data = currentPeriod[0].Data.Select((b, i) => new
                    EntityMetricMap
                {
                    EntityInstance = b.EntityInstance,
                    Current = new MetricMapData
                    {
                        Metric1 = currentPeriod[0].Data[i].WeightedDailyResults.LastOrDefault(),
                        Metric2 = currentPeriod[1].Data[i].WeightedDailyResults.LastOrDefault()
                    },
                    Previous = new MetricMapData
                    {
                        Metric1 = previousPeriod[0].Data[i].WeightedDailyResults.LastOrDefault(),
                        Metric2 = previousPeriod[1].Data[i].WeightedDailyResults.LastOrDefault()
                    }
                }).ToArray(),
                SampleSizeMetadata = sampleSizeMeta,
                HasData = hasData,
                LowSampleSummary = pam.DoMeasuresIncludeMarketMetric
                    ? new LowSampleSummary[] { }
                    : currentPeriod.EntityInstanceIdsWithLowSample(pam.Subset.Id, subsetStartDate)
                        .Union(previousPeriod.EntityInstanceIdsWithLowSample(pam.Subset.Id, subsetStartDate))
                        .ToArray()
            };
        }

        public async Task<BrandSampleResults> GetBrandSampleResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            var pam = _requestAdapter.CreateParametersForCalculation(model, onlyUseFocusInstance: true);

            var measureResults = (await _convenientCalculator.GetCuratedResultsForAllMeasures(pam, cancellationToken))
                .Select(r => (Measure: r.Measure, Result: r.Data)).ToArray();

            var brandSampleResults = new BrandSampleResults
            {
                MonthSelectedEndDate = model.Period.ComparisonDates[0].EndDate,
                BrandSampleMetricResults = measureResults.Select(m => new BrandSampleMetricResult
                {
                    Metric = m.Measure.Name,
                    WeightedDailyResult = m.Result.Single().WeightedDailyResults.LastOrDefault()
                }).ToArray()
            };
            return brandSampleResults;
        }


    }

    public class OverTimeSingleAverageResultsForMetric
    {
        public OverTimeAverageResults Results { get; set; }
        public Measure Measure { get; set; }
    }

    public class OverTimeAverageResultsForMetric
    {
        public (string AverageName, OverTimeAverageResults Results)[] Results { get; set; }
        public Measure Measure { get; set; }
    }
}
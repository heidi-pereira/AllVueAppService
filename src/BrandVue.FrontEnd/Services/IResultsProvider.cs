using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using System.Threading;

namespace BrandVue.Services
{
    public interface IResultsProvider
    {
        Task<BreakdownResults> GetBreakdown(MultiEntityRequestModel model, CancellationToken cancellationToken);
        Task<BreakdownByAgeResults> GetBreakDownByAge(CuratedResultsModel modelWithOnlyFocusInstance,
            CancellationToken cancellationToken);
        Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<RankingTableResults> GetRankingTableResult(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<RankingTableResults> GetRankingTableResult(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<RankingOvertimeResults> GetRankingOvertimeResult(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<MultiMetricResults> GetMultiMetricResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<FunnelResults> GetFunnelResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<ScorecardPerformanceResults> GetScorecardPerformanceResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<ScorecardVsKeyCompetitorsResults> GetScorecardVsKeyCompetitorsResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<CompetitionResults> GetCompetitionResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<OverTimeResults> GetOverTimeResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<OverTimeResults> GetUnorderedOverTimeResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<ImpactMapResults> GetImpactMapResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<OverTimeResults> GetOverviewResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<BrandSampleResults> GetBrandSampleResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<StackedResults> GetStackedResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<SplitMetricResults> GetSplitMetricResults(MultiEntityRequestModel models,
            CancellationToken cancellationToken);
        Task<SplitMetricResults> GetSplitMetricResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters(
            CuratedResultsModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks = null);

        Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters(
            MultiEntityRequestModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks = null);
        Task<StackedProfileResults> GetStackedProfileResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<OverTimeSingleAverageResultsForMetric[]> GetOverTimeAverageResults(CuratedResultsModel model, AverageType averageType,
            CancellationToken cancellationToken);
        Task<OverTimeSingleAverageResultsForMetric[]> GetUnorderedOverTimeAverageResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<OverTimeAverageResults> GetUnorderedOverTimeAverageResults(MultiEntityRequestModel model,
            AverageType averageType, CancellationToken cancellationToken);
        Task<MultiMetricAverageResults> GetMultiMetricAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<BreakdownResults> GetBreakdownAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<BreakdownResults> GetBreakdownAverageResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<WordleResults> GetWordleResults(CuratedResultsModel model, CancellationToken cancellationToken);
        Task<RawTextResults> GetRawTextResults(CuratedResultsModel model, CancellationToken cancellationToken);
        ResultsProviderParameters ResultsProviderParameters(CuratedResultsModel modelCuratedResultsModel);
        ResultsProviderParameters ResultsProviderParametersMultiEntity(MultiEntityRequestModel model);
        Task<ScorecardPerformanceCompetitorResults> GetScorecardPerformanceResultsAverage(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<StackedMultiEntityResults> GetStackedResultsForMultipleEntities(StackedMultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<CategoryResult[]> GetProfileResultsForMultipleEntities(MultiEntityProfileModel profileModel,
            CancellationToken cancellationToken);
        Task<IEnumerable<OverTimeAverageResults>> GetAverageForStackedMultiEntityCharts(
            StackedMultiEntityRequestModel model, AverageType averageType, CancellationToken cancellationToken);
        Task<AverageResultWithPrevious> GetAverageResultWithPrevious(MultiEntityRequestModel model,
            CancellationToken cancellationToken);
        Task<AverageResultWithPrevious> GetAverageResultWithPrevious(CuratedResultsModel model,
            CancellationToken cancellationToken);
        Task<StackedAverageResults> GetStackedAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken);
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;

namespace Test.BrandVue.FrontEnd.Mocks
{
    public class FakeResultsProvider : IResultsProvider
    {
        public Task<BreakdownResults> GetBreakdown(MultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<BreakdownByAgeResults> GetBreakDownByAge(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static CuratedResultsForExport CuratedResultsForAllMeasures()
        {
            var results = new List<WeightedDailyResult>
            {
                new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.1, UnweightedSampleSize = 10 }
            };
            return new CuratedResultsForExport
            {
                Data = new[]
                {
                    new ResultsForMeasure
                    {
                        Data = new[]
                        {
                            new EntityWeightedDailyResults(new EntityInstance
                            {
                                Id = 1,
                                Name = "blorgfester ltd."
                            }, results)
                        },
                        NumberFormat = "0.00",
                    }
                }
            };
        }

        public Task<CuratedResultsForExport> GetCuratedResultsForAllMeasures(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(CuratedResultsForAllMeasures());
        }

        public Task<RankingTableResults> GetRankingTableResult(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<RankingTableResults> GetRankingTableResult(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<RankingOvertimeResults> GetRankingOvertimeResult(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MultiMetricResults> GetMultiMetricResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ProductConfigurationResult> GetProductConfiguration(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ApplicationConfigurationResult GetApplicationConfiguration(string subsetId)
        {
            throw new NotImplementedException();
        }

        public Task<FunnelResults> GetFunnelResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ScorecardPerformanceResults> GetScorecardPerformanceResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ScorecardPerformanceCompetitorResults> GetScorecardPerformanceResultsAverage(
            CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<StackedMultiEntityResults> GetStackedResultsForMultipleEntities(
            StackedMultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CategoryResult[]> GetProfileResultsForMultipleEntities(MultiEntityProfileModel profileModel, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ScorecardVsKeyCompetitorsResults> GetScorecardVsKeyCompetitorsResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<CompetitionResults> GetCompetitionResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OverTimeResults> GetOverTimeResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<OverTimeResults> GetUnorderedOverTimeResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return new OverTimeResults
            {
                EntityWeightedDailyResults = (await GetCuratedResultsForAllMeasures(model, cancellationToken)).Data[0].Data, SampleSizeMetadata = new SampleSizeMetadata
                {
                    SampleSize = new UnweightedAndWeightedSample
                    {
                        Unweighted = 10.0,
                        Weighted = 10.0
                    }
                }
            };
        }

        public Task<ImpactMapResults> GetImpactMapResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OverTimeResults> GetOverviewResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        public Task<BrandSampleResults> GetBrandSampleResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<StackedResults> GetStackedResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<SplitMetricResults> GetSplitMetricResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<SplitMetricResults> GetSplitMetricResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public GroupedCrossbreakCompetitionResults GetGroupedCompetitionResultsWithCrossbreakFilters(CuratedResultsModel model, IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks)
        {
            throw new NotImplementedException();
        }

        public GroupedCrossbreakCompetitionResults GetGroupedCompetitionResultsWithCrossbreakFilters(MultiEntityRequestModel model, IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks)
        {
            throw new NotImplementedException();
        }

        public Task<StackedProfileResults> GetStackedProfileResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public OverTimeAverageResults GetOverTimeAverageResults(CuratedResultsModel model)
        {
            throw new NotImplementedException();
        }

        public Task<OverTimeSingleAverageResultsForMetric[]> GetUnorderedOverTimeAverageResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<MultiMetricAverageResults> GetMultiMetricAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<BreakdownResults> GetBreakdownAverageResults(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<BreakdownResults> GetBreakdownAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<WordleResults> GetWordleResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<RawTextResults> GetRawTextResults(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public ResultsProviderParameters ResultsProviderParameters(CuratedResultsModel modelCuratedResultsModel)
        {
            throw new NotImplementedException();
        }

        public ResultsProviderParameters ResultsProviderParametersMultiEntity(MultiEntityRequestModel model)
        {
            throw new NotImplementedException();
        }

        public Task<OverTimeSingleAverageResultsForMetric[]> GetOverTimeAverageResults(CuratedResultsModel model,
            AverageType averageTypes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<OverTimeAverageResults> GetUnorderedOverTimeAverageResults(MultiEntityRequestModel model,
            AverageType averageType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OverTimeAverageResults>> GetAverageForStackedMultiEntityCharts(
            StackedMultiEntityRequestModel model, AverageType averageType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AverageResultWithPrevious> GetAverageResultWithPrevious(MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<AverageResultWithPrevious> GetAverageResultWithPrevious(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<StackedAverageResults> GetStackedAverageResults(CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public CrossbreakCompetitionResults GetResults(CuratedResultsModelWithCrossbreaks model, IReadOnlyList<string> orderedLeafBreakIds)
        {
            throw new NotImplementedException();
        }

        public Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters(
            CuratedResultsModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks)
        {
            throw new NotImplementedException();
        }

        public GroupedCrossbreakCompetitionResults GetGroupedCompetitionResultsWithCrossbreakFilters2(CuratedResultsModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks, CrossMeasure[] crossMeasureBreaks = null)
        {
            throw new NotImplementedException();
        }

        public Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters(
            MultiEntityRequestModel model,
            IEnumerable<(string GroupName, IEnumerable<CompositeFilterModel> Filters)> breaks,
            CancellationToken cancellationToken,
            CrossMeasure[] crossMeasureBreaks = null)
        {
            throw new NotImplementedException();
        }
    }
}

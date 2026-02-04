using Aspose.Slides;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using System.Threading;
using Aspose.Slides.Charts;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class MultiBreakColumnChart : BasePowerpointChart, IPowerpointChart
    {
        private readonly SavedReport _report;

        public MultiBreakColumnChart(PowerpointBaseChartDependencies dependencies, SavedReport report) : base(dependencies)
        {
            _report = report;
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var breaks = part.OverrideReportBreaks ? part?.Breaks : _report.Breaks?.ToArray();
            var (entityTypeConfig, isMultiEntity) = MultiEntitySplitByTypeFilterInstanceCheck(measure, part);

            if (!part.MultiBreakSelectedEntityInstance.HasValue)
            {
                throw new InvalidOperationException("Multi-break must have a primary filter instance");
            }

            var commonChartData = chartExportData as CommonChartData;

            var primaryFilterInstance = part.MultiBreakSelectedEntityInstance.Value;
            var results = isMultiEntity ?
                await GetGroupedCrossbreakCompetitionResultsMultiEntity(
                    commonChartData,
                    entityTypeConfig.SplitByEntityType,
                    GetFilterInstances(entityTypeConfig, subset),
                    primaryFilterInstance,
                    breaks,
                    chartExportData.SortOrder,
                    chartExportData.SigDiffOptions,
                    part.ShowTop,
                    cancellationToken)
                :
                await GetGroupedCrossbreakCompetitionResults(
                    commonChartData,
                    primaryFilterInstance,
                    breaks,
                    chartExportData.SortOrder,
                    chartExportData.SigDiffOptions,
                    part.ShowTop,
                    cancellationToken);
            var lastGroupedResult = results.GroupedBreakResults.Last();
            var categories = results.GroupedBreakResults.Select(group =>
            {
                var subCategories = group.BreakResults.InstanceResults.Select(r => new Category(r.BreakName)).ToList();
                if (group != lastGroupedResult)
                {
                    subCategories.Add(new Category(""));
                }
                return new Category(group.GroupName, subCategories.ToArray());
            }).ToArray();
            var primaryEntityInstanceName = results.GroupedBreakResults.FirstOrDefault()?
                .BreakResults.InstanceResults.FirstOrDefault()?
                .EntityResults.FirstOrDefault()?
                .EntityInstance?.Name ?? "";
            var points = results.GroupedBreakResults.SelectMany(group =>
            {
                var points = group.BreakResults.InstanceResults.Select(r =>
                {
                    var result = r.EntityResults.Single().WeightedDailyResults.Single();
                    var displayedSignificance = ReportPowerpointHelper.GetDisplayedSignificance(result.Significance, chartExportData.SigDiffOptions.DisplaySignificanceDifferences);
                    return new Point(result.WeightedResult,
                        result.UnweightedSampleSize,
                        displayedSignificance);
                }).ToList();
                if (group != lastGroupedResult)
                {
                    points.Add(null);
                }
                return points;
            }
            );
            var series = new[] { new Series($"{measure.DisplayName}: {primaryEntityInstanceName}", points) };
            var footerAverages = Array.Empty<AverageResult[]>();

            RenderSlide(slide,
                ChartType.ClusteredColumn,
                part,
                measure,
                subset,
                chartExportData.FilterModel,
                chartExportData.BaseExpressionOverride,
                chartExportData.DecimalPlaces,
                chartExportData.HighlightLowSample,
                chartExportData.HideDataLabels,
                chartExportData.QuestionTypeLookup,
                entityTypeConfig,
                results.GroupedBreakResults.First().BreakResults,
                ref categories,
                ref series,
                footerAverages,
                chartExportData.SigDiffOptions,
                chartExportData.LowSampleThreshold,
                new[] { primaryEntityInstanceName });
        }

        private async Task<GroupedCrossbreakCompetitionResults> GetGroupedCrossbreakCompetitionResults(CommonChartData requestData,
            int primaryFilterInstanceId,
            CrossMeasure[] breaks,
            ReportOrder reportOrder,
            SigDiffOptions sigDiffOptions,
            int? showTopN, CancellationToken cancellationToken)
        {
            var curatedResultsModel = GetCuratedResultsModel(requestData,
                new[] { primaryFilterInstanceId },
                sigDiffOptions);
            var breakFilters = _crosstabResultsProvider.GetGroupedFlattenedBreaks(breaks, curatedResultsModel.SubsetId);
            var results = await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(curatedResultsModel, breakFilters, cancellationToken, breaks);

            SortCrossbreakCompetitionResults(reportOrder, results);
            ShowTopForCrossbreakCompetitionResults(showTopN, results);
            return results;
        }

        private async Task<GroupedCrossbreakCompetitionResults> GetGroupedCrossbreakCompetitionResultsMultiEntity(
            CommonChartData requestData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int primaryFilterInstanceId,
            CrossMeasure[] breaks,
            ReportOrder sortOrder,
            SigDiffOptions sigDiffOptions,
            int? showTopN, CancellationToken cancellationToken)
        {
            var multiEntityRequestModel = GetMultiEntityRequestModel(requestData, splitByEntityTypeName, filterBy,
                new[] { primaryFilterInstanceId }, sigDiffOptions);
            var breakFilters = _crosstabResultsProvider.GetGroupedFlattenedBreaks(breaks, multiEntityRequestModel.SubsetId);
            var results = await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(multiEntityRequestModel, breakFilters, cancellationToken);
            SortCrossbreakCompetitionResults(sortOrder, results);
            ShowTopForCrossbreakCompetitionResults(showTopN, results);
            return results;
        }

        private void SortCrossbreakCompetitionResults(ReportOrder order, GroupedCrossbreakCompetitionResults results)
        {
            //result ordering by default ScriptOrderDesc
            switch (order)
            {
                case ReportOrder.ResultOrderDesc:
                case ReportOrder.ResultOrderAsc:
                    foreach (var group in results.GroupedBreakResults)
                    {
                        group.BreakResults.InstanceResults = group.BreakResults.InstanceResults.OrderByDescending(
                            groupResult => groupResult.EntityResults[0].WeightedDailyResults[0].WeightedResult).ToArray();

                    }
                    break;
            }

            //ascending vs descending
            switch (order)
            {
                case ReportOrder.ResultOrderAsc:
                case ReportOrder.ScriptOrderAsc:
                    foreach (var group in results.GroupedBreakResults)
                    {
                        group.BreakResults.InstanceResults = group.BreakResults.InstanceResults.Reverse().ToArray();
                    }
                    break;
            }
        }

        protected void ShowTopForCrossbreakCompetitionResults(int? showTopN, GroupedCrossbreakCompetitionResults results)
        {
            if (showTopN.HasValue)
            {
                foreach (var group in results.GroupedBreakResults)
                {
                    group.BreakResults.InstanceResults = group.BreakResults.InstanceResults.Take(showTopN.Value).ToArray();
                }
            }
        }
    }
}


using Aspose.Slides;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Dashboard;
using System.Threading;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class SplitStackedColumnChart : BasePowerpointChart, IPowerpointChart
    {
        private readonly SavedReport _report;

        public SplitStackedColumnChart(PowerpointBaseChartDependencies baseDependencies, SavedReport report) : base(baseDependencies)
        {
            _report = report;
        }

        public async Task AddChartToSlide(ISlide slide,
            ChartExportData chartExportData,
            CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var allBreaks = part.OverrideReportBreaks ? part?.Breaks : _report.Breaks?.ToArray();
            var singleBreak = allBreaks.FirstOrDefault();
            var breaks = singleBreak != null && singleBreak.FilterInstances.Any() ? singleBreak : null;

            var (entityTypeConfig, isMultiEntity) = MultiEntitySplitByTypeFilterInstanceCheck(measure, part);
            if(entityTypeConfig.FilterByEntityTypes.Length > 1)
            {
                entityTypeConfig.FilterByEntityTypes = [entityTypeConfig.FilterByEntityTypes.Single(f => f.Instance == chartExportData.filterByIndex)];
            }

            //stacked charts dont get sorted or use showTop
            var defaultSortOrder = ReportOrder.ScriptOrderDesc;
            int? defaultShowTop = null;

            var commonChartData = chartExportData as CommonChartData;

            var results = await GetCompetitionResults(part,
                commonChartData,
                breaks,
                chartExportData.SigDiffOptions,
                entityTypeConfig,
                isMultiEntity,
                defaultSortOrder,
                defaultShowTop,
                cancellationToken);

            var instanceResultsArray = results.InstanceResults.ToArray();
            var firstEntityResults = instanceResultsArray[0].EntityResults;

            var categories = instanceResultsArray.Select(r => new Category(r.BreakName)).ToArray();

            // Aspose and Highcharts seem to have the opposite idea about ordering in a stacked bar chart,
            // so we reverse the order of the series for the segments of the bars here:
            var series = firstEntityResults.Select((_, index) =>
            {
                var entityInstance = firstEntityResults[index].EntityInstance;
                var seriesName = entityInstance?.Name ?? measure.DisplayName;
                var meanCalculationValue = ReportPowerpointHelper.GetMeanCalculationValueOrNull(entityInstance?.Id, measure.EntityInstanceIdMeanCalculationValueMapping);
                var points = instanceResultsArray.Select(r => new Point(r.EntityResults[index].WeightedDailyResults[0].WeightedResult,
                    r.EntityResults[index].WeightedDailyResults[0].UnweightedSampleSize,
                    ReportPowerpointHelper.GetDisplayedSignificance(r.EntityResults[index].WeightedDailyResults[0].Significance,
                        chartExportData.SigDiffOptions.DisplaySignificanceDifferences))
                );
                return new Series(seriesName, points, entityInstance?.Id, null, meanCalculationValue);
            }).Reverse().ToArray();
            var footerAverages = Array.Empty<AverageResult[]>();

            if (part.AverageTypes?.Any() == true && (series.Length > 1 || measure.IsNumericVariable))
            {
                (_, _, footerAverages) = await AddAverageToSplitStackedColumnChart(part,
                    commonChartData,
                    breaks,
                    entityTypeConfig,
                    isMultiEntity,
                    series,
                    categories,
                    chartExportData.SigDiffOptions,
                    cancellationToken);
            }

            RenderSlide(slide,
                ChartType.StackedColumn,
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
                results,
                ref categories,
                ref series,
                footerAverages,
                chartExportData.SigDiffOptions, chartExportData.LowSampleThreshold);
        }

        private async Task<(Category[], Series[], AverageResult[][])> AddAverageToSplitStackedColumnChart(
            PartDescriptor part,
            CommonChartData requestData,
            CrossMeasure breaks,
            MultipleEntitySplitByAndFilterBy entityTypeConfig,
            bool isMultiEntity,
            Series[] dataSeries,
            Category[] categories,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                part.AverageTypes,
                requestData.Subset);

            var averageResults = isMultiEntity ?
                await GetAverageResultsWithCrossbreakFiltersMultiEntity(
                    requestData,
                    entityTypeConfig.SplitByEntityType,
                    GetFilterInstances(entityTypeConfig, requestData.Subset),
                    part.SelectedEntityInstances?.SelectedInstances,
                    breaks,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken)
                :
                await GetAverageResultsWithCrossbreakFilters(
                    requestData,
                    part.SelectedEntityInstances?.SelectedInstances,
                    breaks,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken);

            return (categories.ToArray(), dataSeries.ToArray(), averageResults.Select(a => GetAverageResultsFrom(a)).ToArray());
        }

        private async Task<IEnumerable<CrosstabAverageResults>> GetAverageResultsWithCrossbreakFiltersMultiEntity(
            CommonChartData requestData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            CrossMeasure breaks,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var multiEntityRequestModel = GetMultiEntityRequestModel(requestData, splitByEntityTypeName, filterBy,
                selectedEntityInstanceIds, sigDiffOptions);
            var results = new List<CrosstabAverageResults>();

            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var model = new AverageMultiEntityChartModel(multiEntityRequestModel, average, breaks);
                var data = await _crosstabResultsProvider.GetAverageForMultiEntityCharts(model, cancellationToken);
                results.Add(data);
            }

            return results;
        }

    }
}


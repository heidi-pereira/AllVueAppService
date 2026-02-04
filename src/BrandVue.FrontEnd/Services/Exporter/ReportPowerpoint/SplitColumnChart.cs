using Aspose.Slides;
using BrandVue.EntityFramework.MetaData.Reports;
using System.Threading;
using Aspose.Slides.Charts;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class SplitColumnChart : BasePowerpointChart, IPowerpointChart
    {
        private readonly SavedReport _report;

        public SplitColumnChart(PowerpointBaseChartDependencies dependencies, SavedReport report) : base(dependencies)
        {
            _report = report;
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var allBreaks = part.OverrideReportBreaks ? part?.Breaks : _report.Breaks?.ToArray();
            var singleBreak = allBreaks.FirstOrDefault();
            var breaks = singleBreak != null && singleBreak.FilterInstances.Any() ? singleBreak : null;

            var (entityTypeConfig, isMultiEntity) = MultiEntitySplitByTypeFilterInstanceCheck(measure, part);

            var commonChartData = chartExportData as CommonChartData;

            var results = await GetCompetitionResults(part,
                commonChartData,
                breaks,
                chartExportData.SigDiffOptions,
                entityTypeConfig,
                isMultiEntity,
                chartExportData.SortOrder,
                part.ShowTop,
                cancellationToken);

            var instanceResults = results.InstanceResults.ToArray();

            var categories = instanceResults[0].EntityResults.Select(r =>
                new Category(r.EntityInstance?.Name,
                    ReportPowerpointHelper.GetMeanCalculationValueOrNull(r.EntityInstance?.Id, measure.EntityInstanceIdMeanCalculationValueMapping)))
                .ToArray();

            var series = instanceResults.Select(r =>
            {
                var points = r.EntityResults.Select(er => new Point(er.WeightedDailyResults[0].WeightedResult,
                    er.WeightedDailyResults[0].UnweightedSampleSize,
                    ReportPowerpointHelper.GetDisplayedSignificance(er.WeightedDailyResults[0].Significance, chartExportData.SigDiffOptions.DisplaySignificanceDifferences)));
                return new Series(r.BreakName, points);
            }).ToArray();
            var footerAverages = Array.Empty<AverageResult[]>();

            if (part.AverageTypes?.Any() == true && (categories.Length > 1 || measure.IsNumericVariable))
            {
                (categories, series, footerAverages) = await GetAveragesForSplitColumnChart(part,
                    commonChartData,
                    breaks,
                    entityTypeConfig,
                    isMultiEntity,
                    categories,
                    series,
                    chartExportData.SigDiffOptions,
                    cancellationToken);
            }

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
                results,
                ref categories,
                ref series,
                footerAverages,
                chartExportData.SigDiffOptions, chartExportData.LowSampleThreshold);
        }
    }
}


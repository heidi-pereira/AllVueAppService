using Aspose.Slides;
using System.Threading;
using Aspose.Slides.Charts;
using BrandVue.SourceData.Dates;
using BrandVue.SourceData.Averages;
using BrandVue.Models;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class OvertimeChart : BasePowerpointChart, IPowerpointChart
    {
        private readonly AverageDescriptor _average;
        private readonly ChartType _chartType;

        public OvertimeChart(
            PowerpointBaseChartDependencies baseDependencies,
            AverageDescriptor average,
            ChartType chartType
        ) : base(baseDependencies)
        {
            _average = average;
            _chartType = chartType;
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;
            var subset = chartExportData.Subset;

            var (entityTypeConfig, isMultiEntity) = MultiEntitySplitByTypeFilterInstanceCheck(measure, part);

            var commonChartData = chartExportData as CommonChartData;

            var filterInstances = entityTypeConfig?.FilterByEntityTypes == null || entityTypeConfig.FilterByEntityTypes.Length == 0 ?
                Array.Empty<EntityInstanceRequest>() : GetFilterInstances(entityTypeConfig, subset);
            var results = await GetOvertimeResults(
                commonChartData,
                entityTypeConfig.SplitByEntityType,
                filterInstances,
                part.SelectedEntityInstances?.SelectedInstances,
                chartExportData.SigDiffOptions,
                cancellationToken);

            var categories = results.EntityWeightedDailyResults.First().WeightedDailyResults.Select(r => new Category(ResultDateFormatter.FormatDate(r.Date, _average.MakeUpTo))).ToArray();

            var series = results.EntityWeightedDailyResults.Select((entityResult, entityIndex) =>
            {
                var values = entityResult.WeightedDailyResults.Select(result =>
                {
                    if (result.UnweightedSampleSize == 0)
                    {
                        return null;
                    }

                    var displayedSignificance = ReportPowerpointHelper.GetDisplayedSignificance(result.Significance, chartExportData.SigDiffOptions.DisplaySignificanceDifferences);
                    return new Point(result.WeightedResult, result.UnweightedSampleSize, displayedSignificance);
                }).ToArray();
                var entityInstance = entityResult.EntityInstance;
                var meanCalculationValue = ReportPowerpointHelper.GetMeanCalculationValueOrNull(entityInstance?.Id, measure.EntityInstanceIdMeanCalculationValueMapping);
                var numericLabel = meanCalculationValue != null
                    ? meanCalculationValue
                    : entityInstance?.Id;
                var seriesName = entityInstance?.Name ?? measure.DisplayName;
                return new Series(seriesName, values, numericLabel);
            }).ToArray();

            var footerAverages = Array.Empty<AverageResult[]>();

            var numInstances = results.EntityWeightedDailyResults.Length;
            if (part.AverageTypes?.Any() == true && (numInstances > 1 || measure.IsNumericVariable))
            {
                var averages = await GetOvertimeAverages(
                    commonChartData,
                    entityTypeConfig.SplitByEntityType,
                    filterInstances,
                    part.SelectedEntityInstances?.SelectedInstances,
                    part.AverageTypes,
                    chartExportData.SigDiffOptions,
                    cancellationToken);
                footerAverages = averages.Select(averageResult => GetOvertimeAverageResultFrom(averageResult, _average)).ToArray();
            }
            RenderSlide(slide,
                _chartType,
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


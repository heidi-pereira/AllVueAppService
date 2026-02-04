using System.Threading;
using Aspose.Slides;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class ColumnChart : BasePowerpointChart, IPowerpointChart
    {
        public ColumnChart(PowerpointBaseChartDependencies dependencies) : base(dependencies)
        {
        }

        public async Task AddChartToSlide(ISlide slide, ChartExportData chartExportData, CancellationToken cancellationToken)
        {
            var measure = chartExportData.Measure;
            var part = chartExportData.Part;

            var (entityTypeConfig, isMultiEntity) = MultiEntitySplitByTypeFilterInstanceCheck(measure, part);

            var commonChartData = chartExportData as CommonChartData;

            var results = await GetCompetitionResults(
                commonChartData,
                entityTypeConfig.SplitByEntityType,
                isMultiEntity ? GetFilterInstances(entityTypeConfig, chartExportData.Subset) : null,
                chartExportData.Part.SelectedEntityInstances?.SelectedInstances,
                chartExportData.SortOrder,
                chartExportData.SigDiffOptions,
                chartExportData.Part.ShowTop,
                cancellationToken);

            var resultsPerEntity = results.PeriodResults.First().ResultsPerEntity;

            var categories = resultsPerEntity
                .Select(r => new Category(r.EntityInstance?.Name ?? measure.DisplayName,
                    ReportPowerpointHelper.GetMeanCalculationValueOrNull(r.EntityInstance?.Id, measure.EntityInstanceIdMeanCalculationValueMapping)))
                .ToArray();
            var points = resultsPerEntity.Select(r => new Point(r.WeightedDailyResults[0].WeightedResult,
                r.WeightedDailyResults[0].UnweightedSampleSize,
                ReportPowerpointHelper.GetDisplayedSignificance(r.WeightedDailyResults[0].Significance, chartExportData.SigDiffOptions.DisplaySignificanceDifferences))
            ).ToArray();
            var footerAverages = Array.Empty<AverageResult[]>();

            if (part.AverageTypes?.Any() == true && (resultsPerEntity.Length > 1 || measure.IsNumericVariable))
            {
                var filterBy = isMultiEntity ? GetFilterInstances(entityTypeConfig, chartExportData.Subset) : Array.Empty<EntityInstanceRequest>();
                (categories, points, footerAverages) = await AddAverageToColumnChart(categories,
                    points,
                    commonChartData,
                    entityTypeConfig.SplitByEntityType,
                    filterBy,
                    part.SelectedEntityInstances?.SelectedInstances,
                    part.AverageTypes,
                    isMultiEntity,
                    chartExportData.SigDiffOptions,
                    cancellationToken);
            }

            var series = new[] { new Series("Overall", points) };
            RenderSlide(slide,
                ChartType.ClusteredColumn,
                part,
                measure,
                chartExportData.Subset,
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
                chartExportData.SigDiffOptions,
                chartExportData.LowSampleThreshold);
        }

        private async Task<(Category[] categories, Point[] points, AverageResult[][] averageResults)>
            AddAverageToColumnChart(Category[] categories,
                Point[] points,
                CommonChartData requestData,
                string splitByEntityType,
                EntityInstanceRequest[] instanceRequests,
                int[] selectedInstances,
                AverageType[] averageTypes,
                bool isMultiEntity,
                SigDiffOptions sigDiffOptions,
                CancellationToken cancellationToken)
        {
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            var averageResults = isMultiEntity ?
                await GetAverageCompetitionResultsMultiEntity(
                    requestData,
                    splitByEntityType,
                    instanceRequests,
                    selectedInstances,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken)
                :
                await GetAverageCompetitionResultsSingleEntity(
                    requestData,
                    selectedInstances,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken);

            return (categories, points, averageResults.Select(a => GetAverageResultsFrom(a)).ToArray());
        }

        private async Task<IEnumerable<CrosstabAverageResults>> GetAverageCompetitionResultsSingleEntity(
            CommonChartData requestData,
            int[] selectedEntityInstanceIds,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var request = GetCuratedResultsModel(requestData, selectedEntityInstanceIds, sigDiffOptions);
            var results = new List<CrosstabAverageResults>();

            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var data = await _crosstabResultsProvider.GetOverTimeAverageResultsWithBreaks(request, Array.Empty<CrossMeasure>(), average, cancellationToken);
                results.Add(data);
            }
            return results;
        }

        private async Task<IEnumerable<CrosstabAverageResults>> GetAverageCompetitionResultsMultiEntity(CommonChartData requestData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var request = GetMultiEntityRequestModel(requestData, 
                splitByEntityTypeName, 
                filterBy, 
                selectedEntityInstanceIds,
                sigDiffOptions);
            var results = new List<CrosstabAverageResults>();

            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var model = new AverageMultiEntityChartModel(request, average);
                var data = await _crosstabResultsProvider.GetAverageForMultiEntityCharts(model, cancellationToken);
                results.Add(data);
            }

            return results;
        }
    }
}


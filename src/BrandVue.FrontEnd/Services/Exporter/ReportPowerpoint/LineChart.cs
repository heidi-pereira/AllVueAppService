using Aspose.Slides;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Dashboard;
using System.Threading;
using BrandVue.Services.Interfaces;
using Aspose.Slides.Charts;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Page;

namespace BrandVue.Services.Exporter.ReportPowerpoint
{
    public class LineChart : BasePowerpointChart, IPowerpointChart
    {
        private readonly IWaveResultsProvider _waveResultsProvider;

        private readonly CrossMeasure _waves;
        private readonly SavedReport _report;

        public LineChart(
            PowerpointBaseChartDependencies baseDependencies,
            IWaveResultsProvider waveResultsProvider,
            CrossMeasure waves,
            SavedReport report
        ) : base(baseDependencies)
        {
            _waveResultsProvider = waveResultsProvider;
            _waves = waves;
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

            var results = isMultiEntity ?
                await GetWaveComparisonResultsMultiEntity(
                    commonChartData,
                    entityTypeConfig.SplitByEntityType,
                    GetFilterInstances(entityTypeConfig, subset),
                    part.SelectedEntityInstances?.SelectedInstances,
                    _waves,
                    breaks,
                    chartExportData.SigDiffOptions,
                    cancellationToken)
                :
                await GetWaveComparisonResults(
                    commonChartData,
                    part.SelectedEntityInstances?.SelectedInstances,
                    _waves,
                    breaks,
                    chartExportData.SigDiffOptions,
                    cancellationToken);

            var categories = results.ComparisonResults.First().WaveResults.Select(r => new Category(r.WaveName)).ToArray();

            var series = results.ComparisonResults.SelectMany(resultsPerWave =>
            {
                return resultsPerWave.WaveResults.First().EntityResults.Select((_, entityIndex) =>
                {
                    var values = resultsPerWave.WaveResults.Select(r =>
                    {
                        var result = r.EntityResults[entityIndex].WeightedDailyResults[0];
                        if (result.UnweightedSampleSize == 0)
                        {
                            return null;
                        }

                        var displayedSignificance = ReportPowerpointHelper.GetDisplayedSignificance(result.Significance, chartExportData.SigDiffOptions.DisplaySignificanceDifferences);
                        return new Point(result.WeightedResult,
                            result.UnweightedSampleSize,
                            displayedSignificance);
                    }).ToArray();
                    var entityInstance = resultsPerWave.WaveResults.First().EntityResults[entityIndex].EntityInstance;
                   
                    int? numericLabel = null;

                    if (entityInstance?.Id != null)
                    {
                        var meanCalculationValue = ReportPowerpointHelper.GetMeanCalculationValueOrNull(entityInstance.Id, measure.EntityInstanceIdMeanCalculationValueMapping);
                        numericLabel = meanCalculationValue != null
                            ? meanCalculationValue
                            : entityInstance.Id;
                    }
                        
                    var seriesName = entityInstance?.Name ?? measure.DisplayName;
                    return new Series(seriesName, values, numericLabel, resultsPerWave.BreakName);
                });
            }).ToArray();
            var footerAverages = Array.Empty<AverageResult[]>();

            var numInstances = results.ComparisonResults.FirstOrDefault()?.WaveResults.FirstOrDefault()?.EntityResults.Length ?? 0;
            if (part.AverageTypes?.Any() == true && (numInstances > 1 || measure.IsNumericVariable))
            {
                (categories, series, footerAverages) = await AddAverageDataForLineChart(part, commonChartData,
                    _waves, breaks, entityTypeConfig, isMultiEntity, categories, series, chartExportData.SigDiffOptions, cancellationToken);
            }
            RenderSlide(slide,
                ChartType.Line,
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

        private async Task<WaveComparisonResults> GetWaveComparisonResults(CommonChartData requestData,
            int[] selectedEntityInstanceIds,
            CrossMeasure waves,
            CrossMeasure breaks,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            // See usages of MeasureRepositoryExtensions.RequiresLegacyBreakCalculation for examples of how to transition away from using this slow filter-based mechanism
            var waveFilters = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(waves, requestData.Subset.Id);
            var breakFilters = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(breaks, requestData.Subset.Id);
            var request = GetCuratedResultsModel(requestData, selectedEntityInstanceIds, sigDiffOptions);
            var comparandWave = waves.SignificanceFilterInstanceComparandName ?? waveFilters.First().Name;
            return await _waveResultsProvider.GetWaveComparisonResults(request, waveFilters, breakFilters, comparandWave, cancellationToken);
        }

        private async Task<WaveComparisonResults> GetWaveComparisonResultsMultiEntity(CommonChartData requestData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            CrossMeasure waves,
            CrossMeasure breaks,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            // See usages of MeasureRepositoryExtensions.RequiresLegacyBreakCalculation for examples of how to transition away from using this slow filter-based mechanism
            var multiEntityRequestModel = GetMultiEntityRequestModel(requestData, splitByEntityTypeName, filterBy,
                selectedEntityInstanceIds, sigDiffOptions);
            var waveFilters = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(waves, requestData.Subset.Id);
            var breakFilters = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(breaks, requestData.Subset.Id);
            var comparandWave = waves.SignificanceFilterInstanceComparandName ?? waveFilters.First().Name;
            var results = _waveResultsProvider.GetWaveComparisonResults(multiEntityRequestModel, waveFilters, breakFilters, comparandWave, cancellationToken);
            return await results;
        }
        private async Task<(Category[], Series[], AverageResult[][])> AddAverageDataForLineChart(PartDescriptor part,
            CommonChartData requestData,
            CrossMeasure waves,
            CrossMeasure breaks,
            MultipleEntitySplitByAndFilterBy entityTypeConfig,
            bool isMultiEntity,
            IEnumerable<Category> categories,
            IEnumerable<Series> dataSeries,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var averageTypesExcludingMentions = part.AverageTypes.Where(a => a != AverageType.Mentions).ToArray();
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypesExcludingMentions,
                requestData.Subset);

            var averageResults = isMultiEntity ?
                await GetWaveAverageResultsMultiEntity(
                    requestData,
                    entityTypeConfig.SplitByEntityType,
                    GetFilterInstances(entityTypeConfig, requestData.Subset),
                    part.SelectedEntityInstances?.SelectedInstances,
                    waves,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken)
                :
                await GetWaveAverageResults(
                    requestData,
                    part.SelectedEntityInstances?.SelectedInstances,
                    waves,
                    verifiedAverageTypes,
                    sigDiffOptions,
                    cancellationToken);

            return (categories.ToArray(), dataSeries.ToArray(), averageResults.Select(a => GetAverageResultsFrom(a)).ToArray());
        }
        private async Task<IEnumerable<CrosstabAverageResults>> GetWaveAverageResults(CommonChartData requestData,
            int[] selectedEntityInstanceIds,
            CrossMeasure waves,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var request = GetCuratedResultsModel(requestData, selectedEntityInstanceIds, sigDiffOptions);

            var averages = new List<CrosstabAverageResults>();
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var result = await _crosstabResultsProvider.GetOverTimeAverageResultsWithBreaks(request, new[] { waves }, average, cancellationToken);
                averages.Add(result);
            }
            return averages.ToArray();
        }

        private async Task<IEnumerable<CrosstabAverageResults>> GetWaveAverageResultsMultiEntity(CommonChartData requestData,
            string splitByEntityTypeName,
            EntityInstanceRequest[] filterBy,
            int[] selectedEntityInstanceIds,
            CrossMeasure waves,
            AverageType[] averageTypes,
            SigDiffOptions sigDiffOptions,
            CancellationToken cancellationToken)
        {
            var multiEntityRequestModel = GetMultiEntityRequestModel(requestData, splitByEntityTypeName, filterBy,
                selectedEntityInstanceIds, sigDiffOptions);

            var averages = new List<CrosstabAverageResults>();
            var verifiedAverageTypes = _exportAverageHelper.VerifyAverageTypesForMeasure(requestData.Measure,
                averageTypes,
                requestData.Subset);

            foreach (var average in verifiedAverageTypes)
            {
                var model = new AverageMultiEntityChartModel(multiEntityRequestModel, average, waves);
                var result = await _crosstabResultsProvider.GetAverageForMultiEntityCharts(model, cancellationToken);
                averages.Add(result);
            }

            return averages.ToArray();
        }
    }
}


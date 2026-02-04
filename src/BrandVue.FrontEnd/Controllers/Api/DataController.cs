using BrandVue.EntityFramework;
using BrandVue.Filters;
using BrandVue.Middleware;
using BrandVue.MixPanel;
using BrandVue.Models;
using BrandVue.Models.ExcelExport;
using BrandVue.PublicApi.ModelBinding;
using BrandVue.Services;
using BrandVue.Services.Exporter;
using BrandVue.Services.Heatmap;
using BrandVue.Services.Interfaces;
using BrandVue.SourceData.Calculation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Threading;
using Vue.AuthMiddleware;
using static BrandVue.MixPanel.MixPanel;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix(Route)]
    [TrialDateRestrictionWarner]
    [CacheControl(VaryOn = ["ClientVueApiVersion"])]
    public class DataController : ApiController
    {
        public const string Route = "api/data";
        private const string DOWNLOAD_TIME = "Download Time";
        private readonly IResultsProvider _resultsProvider;
        private readonly ICrosstabResultsProvider _crosstabResultsProvider;
        private readonly ISeleniumService _seleniumService;
        private readonly AppSettings _appSettings;
        private readonly IUserContext _userContext;
        private readonly IReportExportService _reportExporter;
        private readonly IExcelChartExportService _excelChartExportService;
        private readonly IResponseExportService _responseExportService;
        private readonly IProductContext _productContext;
        private readonly IWaveResultsProvider _waveResultsProvider;
        private readonly IHeatmapService _heatmapService;

        public DataController(IResultsProvider resultsProvider,
            ICrosstabResultsProvider crosstabResultsProvider,
            ISeleniumService seleniumService,
            IUserContext userContext,
            IReportExportService reportExporter,
            IExcelChartExportService excelChartExportService,
            AppSettings appSettings,
            IResponseExportService responseExportService,
            IProductContext productContext,
            IWaveResultsProvider waveResultsProvider,
            IHeatmapService heatmapService)
        {
            _resultsProvider = resultsProvider;
            _seleniumService = seleniumService;
            _appSettings = appSettings;
            _userContext = userContext;
            _crosstabResultsProvider = crosstabResultsProvider;
            _reportExporter = reportExporter;
            _excelChartExportService = excelChartExportService;
            _responseExportService = responseExportService;
            _productContext = productContext;
            _waveResultsProvider = waveResultsProvider;
            _heatmapService = heatmapService;
        }

        [Route("wordle")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]

        public Task<WordleResults> GetWordleResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetWordleResults(model, cancellationToken);
        }

        [Route("rawtext")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<RawTextResults> GetRawTextResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetRawTextResults(model, cancellationToken);
        }

        [Route("rawheatmap")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<RawHeatmapResults> GetRawHeatmapResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _heatmapService.GetRawHeatmapResults(model, cancellationToken);
        }

        [Route("heatmapimageoverlay")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<HeatmapImageResult> GetHeatmapImageOverlay([FromCompressedUri] HeatmapOverlayRequestModel model,
            CancellationToken cancellationToken)
        {
            return _heatmapService.GetHeatmapImageOverlay(model, cancellationToken);
        }

        [Route("stacked")]
        [HttpPost]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<StackedResults> GetStackedResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetStackedResults(model, cancellationToken);
        }

        [Route("stackedaverage")]
        [HttpPost]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<StackedAverageResults> GetStackedAverageResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetStackedAverageResults(model, cancellationToken);
        }


        [Route("stackedmultientity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<StackedMultiEntityResults> GetStackedResultsForMultipleEntities(
            [FromCompressedUri] StackedMultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetStackedResultsForMultipleEntities(model, cancellationToken);
        }

        [Route("overtimemultipleentity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<OverTimeResults> GetOverTimeResultsForMultipleEntities(
            [FromCompressedUri] MultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetUnorderedOverTimeResults(model, cancellationToken);
        }

        [Route("overtimeaverage")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<OverTimeAverageResults> GetOverTimeAverageResults([FromCompressedUri] AverageMultiEntityChartModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetUnorderedOverTimeAverageResults(model.RequestModel, model.AverageType, cancellationToken);
        }

        [Route("getAverageResultsWithBreaksMultiEntity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<CrosstabAverageResults[]> GetAverageResultsWithBreaksMultiEntity(
            [FromCompressedUri] MultiEntityOverTimeAverageResultsWithBreaksModel model,
            CancellationToken cancellationToken)
        {
            return _crosstabResultsProvider.GetAverageResultsWithBreaks(model.Model, model.AverageType, cancellationToken);
        }

        [Route("getAverageForMultiEntityCharts")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<CrosstabAverageResults> GetAverageForMultiEntityCharts(
            [FromCompressedUri] AverageMultiEntityChartModel model, CancellationToken cancellationToken)
        {
            return _crosstabResultsProvider.GetAverageForMultiEntityCharts(model, cancellationToken);
        }

        [Route("getAverageForStackedMultiEntityCharts")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<IEnumerable<OverTimeAverageResults>> GetAverageForStackedMultiEntityCharts(
            [FromCompressedUri] AverageStackedMultiEntityChartsModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetAverageForStackedMultiEntityCharts(model.StackedMultiEntityRequestModel, model.AverageType, cancellationToken);
        }

        [Route("profiledatamultipleentity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<CategoryResult[]> GetProfileResultsForMultipleEntities(
            [FromCompressedUri] MultiEntityProfileModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetProfileResultsForMultipleEntities(model, cancellationToken);
        }

        [Route("overview")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<OverTimeResults> GetOverviewResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetOverviewResults(model, cancellationToken);
        }

        [Route("competition")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<CompetitionResults> GetCompetitionResults([FromCompressedUri] MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetCompetitionResults(model, cancellationToken);
        }

        [Route("groupedCrossbreakCompetitionResults")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<GroupedCrossbreakCompetitionResults> GetGroupedCrossbreakCompetitionResults(
            [FromCompressedUri] CuratedResultsModelWithCrossbreaks model, CancellationToken cancellationToken)
        {
            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            return _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(model.CuratedResultsModel, breaks, cancellationToken, model.Breaks);
        }

        [Route("groupedCrossbreakCompetitionResultsMultiEntity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<GroupedCrossbreakCompetitionResults> GetGroupedCrossbreakCompetitionResultsMultiEntity(
            [FromCompressedUri] MultiEntityRequestModelWithCrossbreaks model, CancellationToken cancellationToken)
        {
            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            return _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(model.MultiEntityRequestModel,
                breaks,
                cancellationToken,
                model.Breaks);
        }

        [Route("groupedCrossbreakCompetitionResultsMultiEntityMultiBreakMultiFilter")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public async Task<IEnumerable<GroupedCrossbreakCompetitionResults>> GetGroupedCrossbreakCompetitionResultsMultiEntityMultiBreakMultiFilter(
        [FromCompressedUri] MultiEntityRequestModelWithCrossbreaks model, CancellationToken cancellationToken)
        {
            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            var filterBys = model.MultiEntityRequestModel.FilterBy;
            int maxConcurrency = 10;
            var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task<GroupedCrossbreakCompetitionResults>>();

            foreach (var filterBy in filterBys)
            {
                await semaphore.WaitAsync(cancellationToken);
                var clonedModel = new MultiEntityRequestModel(
                    measureName: model.MultiEntityRequestModel.MeasureName,
                    subsetId: model.MultiEntityRequestModel.SubsetId,
                    period: model.MultiEntityRequestModel.Period,
                    dataRequest: model.MultiEntityRequestModel.DataRequest,
                    filterBy: new[] { filterBy },
                    demographicFilter: model.MultiEntityRequestModel.DemographicFilter,
                    filterModel: model.MultiEntityRequestModel.FilterModel,
                    additionalMeasureFilters: model.MultiEntityRequestModel.AdditionalMeasureFilters,
                    baseExpressionOverrides: model.MultiEntityRequestModel.BaseExpressionOverrides,
                    includeSignificance: model.MultiEntityRequestModel.IncludeSignificance,
                    sigConfidenceLevel: model.MultiEntityRequestModel.SigConfidenceLevel,
                    focusEntityInstanceId: model.MultiEntityRequestModel.FocusEntityInstanceId);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        return await _resultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(
                            clonedModel, breaks, cancellationToken, model.Breaks);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            return await Task.WhenAll(tasks);
        }


        [Route("waveComparisonResults")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<WaveComparisonResults> GetWaveComparisonResults(
            [FromCompressedUri] WaveResultsModelWithCrossbreaks model, CancellationToken cancellationToken)
        {
            // See usages of MeasureRepositoryExtensions.RequiresLegacyBreakCalculation for examples of how to transition away from using this slow filter-based mechanism
            var waves = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(model.Waves, model.SubsetId);
            var breaks = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(model.Breaks, model.SubsetId);
            var comparandWave = model.Waves.SignificanceFilterInstanceComparandName ?? waves.First().Name;
            return _waveResultsProvider.GetWaveComparisonResults(model.CuratedResultsModel, waves, breaks, comparandWave, cancellationToken);
        }

        [Route("waveComparisonResultsMultiEntity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<WaveComparisonResults> GetWaveComparisonResultsMultiEntity(
            [FromCompressedUri] MultiEntityWaveResultsModelWithCrossbreaks model, CancellationToken cancellationToken)
        {
            // See usages of MeasureRepositoryExtensions.RequiresLegacyBreakCalculation for examples of how to transition away from using this slow filter-based mechanism
            var waves = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(model.Waves, model.SubsetId);
            var breaks = _crosstabResultsProvider.GetFlattenedBreaksForMeasure(model.Breaks, model.SubsetId);
            var comparandWave = model.Waves.SignificanceFilterInstanceComparandName;
            return _waveResultsProvider.GetWaveComparisonResults(model.MultiEntityRequestModel, waves, breaks, comparandWave, cancellationToken);
        }

        [Route("impactmap")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<ImpactMapResults> GetImpactMapResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetImpactMapResults(model, cancellationToken);
        }

        [Route("breakdown")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<BreakdownResults> Breakdown([FromCompressedUri] MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetBreakdown(model, cancellationToken);
        }

        [Route("crosstabResults")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public async Task<CrosstabResults[]> CrosstabResults([FromCompressedUri] CrosstabRequestModel model,
            CancellationToken cancellationToken)
        {
            return await _crosstabResultsProvider.GetCrosstabResults(model, cancellationToken);
        }

        [Route("crosstabVariableResults")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public async Task<CrosstabulatedResults[]> CrosstabResultsFromTemporaryVariables([FromCompressedUri] TemporaryVariableRequestModel model,
            CancellationToken cancellationToken)
        {
            return await _crosstabResultsProvider.ExperimentalCrosstabResults(model, cancellationToken);
        }

        [Route("crosstabExport")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public string ExportCrosstabResults([FromCompressedUri] CrosstabExportRequest model,
            CancellationToken cancellationToken)
        {
            return _reportExporter.StartDataPageTableExport(model, cancellationToken);
        }

        [Route("crosstabExportText")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public string ExportCrosstabTextResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _reportExporter.StartDataPageTextExport(model, cancellationToken);
        }

        [Route("exportReportTables")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public string ExportReportTables([FromCompressedUri] ReportExportRequest model,
            CancellationToken cancellationToken)
        {
            return _reportExporter.StartReportTableExport(model.SavedReportId, model.Period, model.SubsetId, model.DemographicFilter, model.FilterModel, cancellationToken);
        }

        [Route("checkExportProgress")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(IActionResult), StatusCodes.Status200OK)]
        [SubsetAuthorisation]
        public IActionResult CheckExportProgress([FromCompressedUri] AsyncExportTaskModel model)
        {
            var exportTask = _reportExporter.CheckExportResult(model.ExportKey);
            return exportTask.Status switch
            {
                ExportStatus.Complete => File(exportTask.ExportResult, exportTask.ContentType, exportTask.FileDownloadName),
                ExportStatus.Pending => Accepted(),
                _ => throw new Exception("Export failed"),
            };
        }

        [Route("clearExportResult")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public void ClearExportResult([FromCompressedUri] AsyncExportTaskModel model)
        {
            _reportExporter.ClearExportResult(model.ExportKey);
        }

        [Route("reportPowerpointExport")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<string> ExportReportPowerpoint([FromCompressedUri] ReportExportRequest model,
            CancellationToken cancellationToken)
        {
            return await _reportExporter.StartFullReportPowerpointExport(
                HttpContext.GetUrlIncludingSubProduct(),
                model.SavedReportId,
                model.SubsetId,
                model.Period,
                model.OverTimePeriod,
                model.DemographicFilter,
                model.FilterModel,
                model.UseGenerativeAi,
                cancellationToken);
        }

        [Route("reportChartPowerpointExport")]
        [CompressedGetOrPost]
        [CacheControl(NoStore = true)]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<string> ExportReportSingleChartPowerpoint([FromCompressedUri] ReportPartExportRequest model,
            CancellationToken cancellationToken)
        {
            return await _reportExporter.StartSingleChartPowerpointExport(
                HttpContext.GetUrlIncludingSubProduct(),
                model.SavedReportId,
                model.PartId,
                model.SubsetId,
                model.Period,
                model.OverTimePeriod,
                model.DemographicFilter,
                model.FilterModel,
                model.UseGenerativeAi,
                cancellationToken);
        }

        [Route("breakdownaverage")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<BreakdownResults> BreakdownAverage([FromCompressedUri] MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetBreakdownAverageResults(model, cancellationToken);
        }

        [Route("breakdownaveragesingleentity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<BreakdownResults> BreakdownAverageSingleEntity([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetBreakdownAverageResults(model, cancellationToken);
        }

        [Route("stackedprofile")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<StackedProfileResults> GetStackedProfileResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetStackedProfileResults(model, cancellationToken);
        }

        [Route("agebreakdown")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<BreakdownByAgeResults> BreakdownByAge([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetBreakDownByAge(model, cancellationToken);
        }

        [Route("rankedproducts")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<RankingTableResults> GetRankedBrands([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetRankingTableResult(model, cancellationToken);
        }

        [Route("averagewithprevious")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<AverageResultWithPrevious> GetAverageWithPrevious([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetAverageResultWithPrevious(model, cancellationToken);
        }

        [Route("averagewithpreviousmultientity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<AverageResultWithPrevious> GetAverageWithPreviousMultiEntity(
            [FromCompressedUri] MultiEntityRequestModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetAverageResultWithPrevious(model, cancellationToken);
        }

        [Route("rankingproductsovertime")]
        [SubsetAuthorisation]
        [CompressedGetOrPost]
        public Task<RankingOvertimeResults> GetRankingOvertime([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetRankingOvertimeResult(model, cancellationToken);
        }

        [Route("ranking")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<RankingTableResults> GetRankingTableResults([FromCompressedUri] MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetRankingTableResult(model, cancellationToken);
        }

        [Route("multimetricresults")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<MultiMetricResults> GetMultiMetricResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetMultiMetricResults(model, cancellationToken);
        }

        [Route("multimetricaverage")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<MultiMetricAverageResults> GetMultiMetricAverageResults(
            [FromCompressedUri] CuratedResultsModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetMultiMetricAverageResults(model, cancellationToken);
        }

        [Route("splitmetric")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<SplitMetricResults> GetSplitMetricResults([FromCompressedUri] MultiEntityRequestModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetSplitMetricResults(model, cancellationToken);
        }

        [Route("splitmetricsingleentity")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<SplitMetricResults> GetSplitMetricResultsSingleEntity([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetSplitMetricResults(model, cancellationToken);
        }

        [Route("brandsample")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<BrandSampleResults> GetBrandSampleResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetBrandSampleResults(model, cancellationToken);
        }

        [Route("funnel")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<FunnelResults> GetFunnelResults([FromCompressedUri] CuratedResultsModel model,
            CancellationToken cancellationToken)
        {
            return _resultsProvider.GetFunnelResults(model, cancellationToken);
        }

        [Route("scorecardperformance")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<ScorecardPerformanceResults> GetScorecardPerformanceResults(
            [FromCompressedUri] CuratedResultsModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetScorecardPerformanceResults(model, cancellationToken);
        }

        [Route("scorecardperformanceaverage")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<ScorecardPerformanceCompetitorResults> GetScorecardPerformanceResultsAverage(
            [FromCompressedUri] CuratedResultsModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetScorecardPerformanceResultsAverage(model, cancellationToken);
        }

        [Route("scorecardvskeycompetitors")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public Task<ScorecardVsKeyCompetitorsResults> GetScorecardVsKeyCompetitorsResults(
            [FromCompressedUri] CuratedResultsModel model, CancellationToken cancellationToken)
        {
            return _resultsProvider.GetScorecardVsKeyCompetitorsResults(model, cancellationToken);
        }

        [Route("excelexport")]
        [CompressedGetOrPost]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<FileStreamResult> ExportData([FromCompressedUri] ExcelExportModel model,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await getExcelExport(model, cancellationToken);
            stopwatch.Stop();
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.ExcelDownload,
                _userContext.UserId,
                GetClientIpAddress(),
                new Dictionary<string, object>
                {
                    { DOWNLOAD_TIME, stopwatch.ElapsedMilliseconds },
                    { SUBSET, model.CuratedResultsModel.SubsetId},
                }));
            return result;
        }

        [Route("excelexportmultientity")]
        [CompressedGetOrPost]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<FileStreamResult> ExportMultiEntityData([FromCompressedUri] ExcelExportMultipleEntitiesModel model,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await getExcelExport(model, cancellationToken);
            stopwatch.Stop();
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.ExcelDownload,
                _userContext.UserId,
                GetClientIpAddress(),
                new Dictionary<string, object>
                {
                    { DOWNLOAD_TIME, stopwatch.ElapsedMilliseconds },
                    { SUBSET, model.MultiEntityRequestModel.SubsetId},
                }));
            return result;
        }

        [Route("excelexportsplitmetric")]
        [CompressedGetOrPost]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<FileStreamResult> ExportSplitMetricData([FromCompressedUri] ExcelExportSplitMetricModel model,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await getExcelExport(model, cancellationToken);
            stopwatch.Stop();
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.ExcelDownload,
                _userContext.UserId,
                GetClientIpAddress(),
                new Dictionary<string, object>
                {
                    { DOWNLOAD_TIME, stopwatch.ElapsedMilliseconds },
                    { SUBSET, model.MultiEntityRequestModel.SubsetId},
                }));
            return result;
        }

        private async Task<FileStreamResult> getExcelExport(IExcelExportModel model,
            CancellationToken cancellationToken)
        {
            var exporter = model switch
            {
                ExcelExportModel excelExportModel => _excelChartExportService.CreateExporter(excelExportModel, cancellationToken),
                ExcelExportMultipleEntitiesModel excelExportMultipleEntitiesModel => _excelChartExportService.CreateExporterForMultiple(excelExportMultipleEntitiesModel, cancellationToken, cancellationToken),
                ExcelExportSplitMetricModel excelExportSplitMetricModel => _excelChartExportService.CreateExporterForSplitMetric(excelExportSplitMetricModel, cancellationToken),
                _ => throw new Exception("Unknown export model"),
            };
            return File((await exporter).ToStream(), ExportHelper.MimeTypes.Excel, CalculateFileDownloadName(model));
        }

        private string CalculateFileDownloadName(IExcelExportModel model)
        {
            return $"{model.Name}-{Enum.GetName(typeof(ViewTypeEnum), model.ViewType)} - Private.xlsx";
        }

        [Route("excelexportcategories")]
        [CompressedGetOrPost]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [SubsetAuthorisation]
        public async Task<IActionResult> ExportCategoriesData([FromCompressedUri] ExcelExportCategoryModel model)
        {
            var stopwatch = Stopwatch.StartNew();
            var excelExporter = await _excelChartExportService.CreateExporterForCategory(model);
            string fileDownloadName = FileHelpers.SanitizeFileName(
                $"{DateTime.UtcNow:yyyy-MM-dd} - ${model.PageName} - ${model.ActiveBrand} - ${Enum.GetName(typeof(CategorySortKey), model.SortKey)} - Private.xlsx");
            var result =  File(excelExporter.ToStream(), ExportHelper.MimeTypes.Excel, fileDownloadName);
            stopwatch.Stop();
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.ExcelDownload,
                _userContext.UserId,
                GetClientIpAddress(),
                new Dictionary<string, object>
                {
                    { DOWNLOAD_TIME, stopwatch.ElapsedMilliseconds },
                    { SUBSET, model.SubsetId},
                }));
            return result;
        }

        [Route("chartexport")]
        [CompressedGetOrPost]
        [SubsetAuthorisation]
        public async Task<IActionResult> ExportChart([FromCompressedUri] ExportChartModel model)
        {
            var stopwatch = Stopwatch.StartNew();
            var result  =  await ExportChart(model, _userContext.UserName, GetClientIpAddress());
            stopwatch.Stop();
            await TrackAsync(new TrackAsyncEventModel(
                VueEvents.ChartDownload,
                _userContext.UserId,
                GetClientIpAddress(),
                new Dictionary<string, object>
                {
                    { DOWNLOAD_TIME, stopwatch.ElapsedMilliseconds },
                    { SUBSET, model.SubsetId},
                }));
            return result;
        }

        [Route("exportResponses")]
        [CompressedGetOrPost]
        [Authorize(Policy = Constants.UserRoleOrAbove)]
        [AuthorizedToExportResponseLevelDataAttribute]
        public IActionResult ExportResponses()
        {
            var responseExporter = _responseExportService.ExportAllRespondents();
            string fileDownloadName = FileHelpers.SanitizeFileName($"{DateTime.UtcNow:yyyy-MM-dd} - {_productContext.ShortCodeAndSubproduct()} - All respondents - Private.xlsx");
            return File(responseExporter, ExportHelper.MimeTypes.Excel, fileDownloadName);
        }

        internal async Task<IActionResult> ExportChart([FromCompressedUri] ExportChartModel model, string userName, string remoteIpAddress)
        {
            string viewType = Enum.GetName(typeof(ViewTypeEnum), model.ViewType);

            var uri = new Uri(model.Url);

            var appBaseUrl = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : ":" + uri.Port)}{HttpContext.Request.PathBase}";

            if (!appBaseUrl.EndsWith("/"))
            {
                appBaseUrl += "/";
            }

            var optionalUserOrg = _appSettings.AppSettingsCollection["ReportingToPassAroundOrganisation"] == (true.ToString()) ? _userContext.UserOrganisation : null;
            var appUrl = model.Url.Substring(appBaseUrl.Length);
            byte[] chartScreenshot;

            try
            {
                chartScreenshot = await _seleniumService.ExportChart(
                    appBaseUrl,
                    appUrl,
                    model.Name,
                    viewType,
                    model.Width,
                    model.Height,
                    model.Metrics,
                    userName,
                    remoteIpAddress,
                    optionalUserOrg);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
            return File(chartScreenshot, "image/png", $"{model.Name}-{viewType} - Private.png");
        }
    }
}

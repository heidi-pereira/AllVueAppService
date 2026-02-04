using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Dashboard;
using BrandVue.EntityFramework;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Threading;
using BrandVue.SourceData.Utils;
using BrandVue.EntityFramework.Exceptions;
using System.IO;
using Vue.Common.FeatureFlags;

namespace BrandVue.Services.Exporter
{
    public interface IReportExportService
    {
        string GetReportFileName(SavedReport savedReport);
        string StartDataPageTableExport(CrosstabExportRequest model, CancellationToken cancellationToken);
        string StartDataPageTextExport(CuratedResultsModel model, CancellationToken cancellationToken);
        string StartReportTableExport(int savedReport, Period period, string subsetId,
            DemographicFilter demographicFilter, CompositeFilterModel filterModel, CancellationToken cancellationToken);

        AsyncExportTask CheckExportResult(string cacheKey);
        void ClearExportResult(string cacheKey);

        Task<string> StartSingleChartPowerpointExport(string baseUrl, int reportId, int partId, string subsetId,
            Period period, Period overTimePeriod, DemographicFilter demographicFilter, CompositeFilterModel filterModel,
            bool useGenerativeAi, CancellationToken cancellationToken);
        Task<string> StartFullReportPowerpointExport(string baseUrl, int reportId, string subsetId, Period period, Period overTimePeriod,
            DemographicFilter demographicFilter, CompositeFilterModel filterModel, bool useGenerativeAi, CancellationToken cancellationToken);
    }

    public class ReportExportService : IReportExportService
    {
        private readonly ISavedReportRepository _savedReportRepository;
        private readonly IProductContext _productContext;
        private readonly IReportTableExporter _reportTableExporter;
        private readonly IReportPowerpointExporter _reportPowerpointExporter;
        private readonly IAiLabelledReportPowerpointExporter _aiLabelledReportPowerpointExporter;
        private readonly IPagesRepository _pagesRepository;
        private readonly ILogger<ReportExportService> _logger;
        private readonly IUserContext _userContext;
        private readonly IProductConfigurationProvider _productConfigurationProvider;
        private readonly IExportFileCache _exportCache;
        private readonly IFeatureToggleService _featureToggleService;

        private IMemoryCache ExportCache => _exportCache.GetCache();
        private MemoryCacheEntryOptions ExportCacheEntryOptions => _exportCache.GetEntryOptions();

        private const string EXCEL_CONTENT_TYPE = ExportHelper.MimeTypes.Excel;
        private const string PPTX_CONTENT_TYPE = ExportHelper.MimeTypes.PowerPoint;

        public ReportExportService(IProductContext productContext,
            ISavedReportRepository savedBreaksRepository,
            IPagesRepository pagesRepository,
            IReportTableExporter reportTableExporter,
            IReportPowerpointExporter reportPowerpointExporter,
            IAiLabelledReportPowerpointExporter aiLabelledReportPowerpointExporter,
            IExportFileCache tableExportCache,
            ILogger<ReportExportService> logger,
            IUserContext userContext,
            IProductConfigurationProvider productConfigurationProvider,
            IFeatureToggleService featureToggleService)
        {
            _productContext = productContext;
            _savedReportRepository = savedBreaksRepository;
            _reportTableExporter = reportTableExporter;
            _reportPowerpointExporter = reportPowerpointExporter;
            _aiLabelledReportPowerpointExporter = aiLabelledReportPowerpointExporter;
            _pagesRepository = pagesRepository;
            _exportCache = tableExportCache;
            _logger = logger;
            _userContext = userContext;
            _productConfigurationProvider = productConfigurationProvider;
            _featureToggleService = featureToggleService;
        }

        public string GetReportFileName(SavedReport savedReport)
        {
            var page = _pagesRepository.GetPages().Single(page => savedReport.ReportPageId == page.Id);
            return $"{_productContext.SurveyName} - {page.Name} - Report - Private.xlsx";
        }

        private AsyncExportTask StartCachingExportResult(string fileDownloadName, string contentType)
        {
            string cacheKey;
            do
            {
                cacheKey = Guid.NewGuid().ToString("N");
            }
            while (ExportCache.TryGetValue(cacheKey, out var _) == true);

            var exportTask = new AsyncExportTask
            {
                CacheKey = cacheKey,
                FileDownloadName = fileDownloadName,
                ContentType = contentType,
                Status = ExportStatus.Pending,
                ExportResult = null,
            };
            ExportCache.Set(cacheKey, exportTask, ExportCacheEntryOptions);
            return exportTask;
        }

        private void QueueExportTask(string cacheKey, Func<Task<byte[]>> doExport)
        {
            try
            {
                _userContext.FreezeClaims();
                // Fire and forget this task so the request can return immediately and a separate request can check on it later (if the app pool isn't restarted in the meantime)
                // Hangfire or some other alternative would be preferable in case the app restarts: https://blog.stephencleary.com/2021/01/asynchronous-messaging-1-basic-distributed-architecture.html
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var completedExport = await doExport();
                        UpdateCachedExportTask(cacheKey, ExportStatus.Complete, completedExport);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Report export failed");
                        UpdateCachedExportTask(cacheKey, ExportStatus.Error, null);
                    }
                }, CancellationToken.None);
            }
            catch (Exception x)
            {
                _logger.LogError(x, $"Report export failed");
                UpdateCachedExportTask(cacheKey, ExportStatus.Error, null);
            }
        }

        private void UpdateCachedExportTask(string cacheKey, ExportStatus status, byte[] exportResult)
        {
            if (ExportCache.TryGetValue(cacheKey, out var cacheValue) && cacheValue is AsyncExportTask exportTask)
            {
                exportTask.Status = status;
                exportTask.ExportResult = exportResult;
            }
            else
            {
                throw new NotFoundException($"Export task for key {cacheKey} was not found");
            }
        }

        public AsyncExportTask CheckExportResult(string cacheKey)
        {
            if (ExportCache.TryGetValue(cacheKey, out var cacheValue) && cacheValue is AsyncExportTask exportTask)
            {
                return exportTask;
            }
            throw new NotFoundException($"Export result for key {cacheKey} was not found");
        }

        public void ClearExportResult(string cacheKey)
        {
            ExportCache.Remove(cacheKey);
        }

        public string StartDataPageTableExport(CrosstabExportRequest model, CancellationToken cancellationToken)
        {
            var authCompany = _userContext.AuthCompany;
            return StartDataPageTableExport(async () => await _reportTableExporter.Export(model, authCompany, cancellationToken), GetSuffixString(model.RequestModel.Period));
        }

        public string StartDataPageTextExport(CuratedResultsModel model, CancellationToken cancellationToken)
        {
            var authCompany = _userContext.AuthCompany;
            return StartDataPageTableExport(async () => await _reportTableExporter.ExportText(model, authCompany, cancellationToken), GetSuffixString(model.Period));
        }

        private string StartDataPageTableExport(Func<Task<MemoryStream>> GetExportStream, string periodSuffix = null)
        {
            var surveyNamePrefix = string.IsNullOrWhiteSpace(_productContext.SurveyName) ? "" : $"{_productContext.SurveyName} - ";
            var fileDownloadName = PathExtensions.ReplaceInvalidFilenameCharacters($"{surveyNamePrefix}CrosstabExport{periodSuffix} - Private.xlsx");
            var exportTask = StartCachingExportResult(fileDownloadName, ExportHelper.MimeTypes.Excel);
            async Task<byte[]> doExport()
            {
                var stream = await GetExportStream();
                return stream.ToArray();
            }
            QueueExportTask(exportTask.CacheKey, doExport);
            return exportTask.CacheKey;
        }

        private string GetSuffixString(Period period)
        {
            return $" {period.Average} {period.ComparisonDates.FirstOrDefault()?.ToString()}";
        }

        public string StartReportTableExport(int savedReportId, Period period, string subsetId,
            DemographicFilter demographicFilter, CompositeFilterModel filterModel, CancellationToken cancellationToken1)
        {
            var report = _savedReportRepository.GetById(savedReportId);
            var fileName = PathExtensions.ReplaceInvalidFilenameCharacters(GetReportFileName(report));
            var exportTask = StartCachingExportResult(fileName, EXCEL_CONTENT_TYPE);
            var authCompany = _userContext.AuthCompany;
            async Task<byte[]> doExport(CancellationToken cancellationToken)
            {
                var stream = await _reportTableExporter.Export(report, period, subsetId, demographicFilter, filterModel, authCompany, cancellationToken);
                return stream.ToArray();
            }
            QueueExportTask(exportTask.CacheKey, () => doExport(cancellationToken1));
            return exportTask.CacheKey;
        }

        public async Task<string> StartSingleChartPowerpointExport(string baseUrl, int reportId, int partId,
            string subsetId, Period period, Period overTimePeriod, DemographicFilter demographicFilter, CompositeFilterModel filterModel,
            bool useGenerativeAi, CancellationToken cancellationToken)
        {
            var (report, reportUrl, authCompany, appConfiguration) = GetPowerpointExportConfig(baseUrl, reportId, subsetId);
            var isOvertimeDataEnabled = await IsOvertimeDataFeatureEnabled(cancellationToken);
            var sigDiffOptions = new SigDiffOptions(report.HighlightSignificance,
                report.SigConfidenceLevel,
                report.DisplaySignificanceDifferences,
                CrosstabSignificanceType.CompareToTotal);
            async Task<byte[]> doExport()
            {
                var stream = useGenerativeAi
                    ? await _aiLabelledReportPowerpointExporter.ExportChart(
                        report, partId, subsetId, period, overTimePeriod, demographicFilter, filterModel, reportUrl, authCompany,
                        appConfiguration, isOvertimeDataEnabled, sigDiffOptions, cancellationToken)
                    : await _reportPowerpointExporter.ExportChart(
                        report, partId, subsetId, period, overTimePeriod, demographicFilter, filterModel, reportUrl, authCompany,
                        appConfiguration, isOvertimeDataEnabled, sigDiffOptions, cancellationToken);
                return stream.ToArray();
            }
            return StartPowerpointExport(doExport);
        }

        public async Task<string> StartFullReportPowerpointExport(string baseUrl, int reportId, string subsetId,
            Period period, Period overTimePeriod, DemographicFilter demographicFilter, CompositeFilterModel filterModel,
            bool useGenerativeAi, CancellationToken cancellationToken)
        {
            var (report, reportUrl, authCompany, appConfiguration) = GetPowerpointExportConfig(baseUrl, reportId, subsetId);
            var isOvertimeDataEnabled = await IsOvertimeDataFeatureEnabled(cancellationToken);
            var sigDiffOptions = new SigDiffOptions(report.HighlightSignificance,
                report.SigConfidenceLevel,
                report.DisplaySignificanceDifferences,
                CrosstabSignificanceType.CompareToTotal);
            async Task<byte[]> doExport(CancellationToken cancellationToken)
            {
                var stream = useGenerativeAi
                    ? await _aiLabelledReportPowerpointExporter.ExportReport(report, subsetId, period, overTimePeriod, demographicFilter,
                    filterModel, reportUrl, authCompany, appConfiguration, isOvertimeDataEnabled, sigDiffOptions, cancellationToken)
                    : await _reportPowerpointExporter.ExportReport(report, subsetId, period, overTimePeriod, demographicFilter,
                    filterModel, reportUrl, authCompany, appConfiguration, isOvertimeDataEnabled, sigDiffOptions, cancellationToken);
                return stream.ToArray();
            }
            return StartPowerpointExport(() => doExport(cancellationToken));
        }

        private (SavedReport Report, string ReportUrl, string AuthCompany, ApplicationConfigurationResult ApplicationConfiguration) GetPowerpointExportConfig(string baseUrl, int reportId, string subsetId)
        {
            var report = _savedReportRepository.GetById(reportId);
            var reportUrl = GetReportUrl(baseUrl, report);
            //these need to be snapshotted due to using HttpContext to get information about current user
            var authCompany = _userContext.AuthCompany;
            var appConfiguration = _productConfigurationProvider.GetApplicationConfiguration(subsetId);
            return (report, reportUrl, authCompany, appConfiguration);
        }

        private async Task<bool> IsOvertimeDataFeatureEnabled(CancellationToken cancellationToken)
        {
            var features = await _featureToggleService.GetEnabledFeaturesForCurrentUserAsync(cancellationToken);
            return features.Any(f => f.FeatureCode == EntityFramework.MetaData.FeatureToggle.FeatureCode.overtime_data);
        }

        private string StartPowerpointExport(Func<Task<byte[]>> doExport)
        {
            var surveyNamePrefix = string.IsNullOrWhiteSpace(_productContext.SurveyName) ? "" : $"{_productContext.SurveyName} - ";
            var fileDownloadName = PathExtensions.ReplaceInvalidFilenameCharacters($"{surveyNamePrefix}ReportExport - Private.pptx");
            var exportTask = StartCachingExportResult(fileDownloadName, PPTX_CONTENT_TYPE);
            QueueExportTask(exportTask.CacheKey, doExport);
            return exportTask.CacheKey;
        }

        public string GetReportUrl(string baseUrl, SavedReport report) => $"{baseUrl}/ui/reports/{report.ReportPage.Name}".ToLowerInvariant();
    }
}

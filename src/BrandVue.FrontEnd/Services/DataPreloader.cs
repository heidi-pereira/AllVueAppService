using BrandVue.Models;
using BrandVue.SourceData.Calculation;
using System.Threading;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Respondents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework;

namespace BrandVue.Services
{
    public class DataPreloadTaskStatus
    {
        public int TotalCount { get; }
        public int CompletedCount { get; set; }
        public List<string> Errors { get; }
        public bool IsCancelled { get; set; }
        public bool IsComplete => CompletedCount >= TotalCount;

        public DataPreloadTaskStatus(int totalCount, int completedCount, List<string> errors)
        {
            TotalCount = totalCount;
            CompletedCount = completedCount;
            Errors = errors;
            IsCancelled = false;
        }
    }

    public record DataPreloadTask(CancellationTokenSource CancellationTokenSource, DataPreloadTaskStatus Status);

    public class DataPreloader : IDataPreloader
    {

        public record ReportPreloadData(IRespondentRepository RespondentRepository,
            CalculationPeriod CalculationPeriod, Subset Subset, ParsedReport Report);

        private readonly ILogger<DataPreloader> _logger;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly ISavedReportService _savedReportService;
        private readonly IDataPresenceGuarantor _dataPresenceGuarantor;
        private readonly IDataPreloadTaskCache _taskCache;
        private readonly IProductContext _productContext;

        private IMemoryCache TaskCache => _taskCache.GetCache();
        private MemoryCacheEntryOptions TaskCacheEntryOptions => _taskCache.GetEntryOptions();

        internal string CacheKey => _productContext.SubProductId ?? _productContext.ShortCode;

        private readonly object _cacheLock = new();

        public DataPreloader(ILogger<DataPreloader> logger,
            ISubsetRepository subsetRepository,
            IRespondentRepositorySource respondentRepositorySource,
            ISavedReportService savedReportService,
            IDataPresenceGuarantor dataPresenceGuarantor,
            IDataPreloadTaskCache taskCache,
            IProductContext productContext)
        {
            _logger = logger;
            _subsetRepository = subsetRepository;
            _respondentRepositorySource = respondentRepositorySource;
            _savedReportService = savedReportService;
            _dataPresenceGuarantor = dataPresenceGuarantor;
            _taskCache = taskCache;
            _productContext = productContext;
        }

        public DataPreloadTaskStatus PreloadReportDataIntoMemory(CancellationToken cancellationToken)
        {
            var taskStatus = GetCurrentTaskStatus();
            if (taskStatus != null && !taskStatus.IsCancelled && !taskStatus.IsComplete)
            {
                return taskStatus;
            }

            var reports = GetReportsToPreload();

            var task = StartCachingExportResult(reports.Count());

            _ = Task.Run(async () => await LoadReportDataIntoMemory(reports, GetLinkedCancellationToken(cancellationToken, task.CancellationTokenSource.Token)));

            return task.Status;
        }

        private CancellationToken GetLinkedCancellationToken(CancellationToken internalToken, CancellationToken externalToken)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(internalToken, externalToken).Token;
        }

        public DataPreloadTaskStatus CheckTaskStatus()
        {
            var taskStatus = GetCurrentTaskStatus();
            if (taskStatus != null)
            {
               return taskStatus;
            }
            throw new NotFoundException($"Data preload task status for key {CacheKey} was not found");
        }

        public void ClearTaskStatus()
        {
            TaskCache.Remove(CacheKey);
        }

        public void CancelTask()
        {
            lock (_cacheLock)
            {
                if (TaskCache.TryGetValue(CacheKey, out var cacheValue) && cacheValue is DataPreloadTask preloadTask)
                {
                    preloadTask.Status.IsCancelled = true;
                    preloadTask.CancellationTokenSource.Cancel();
                }
            }
        }

        internal IEnumerable<ReportPreloadData> GetReportsToPreload()
        {
            var reports = new List<ReportPreloadData>();
            var savedReports = _savedReportService.GetAllReports();
            foreach (var subset in _subsetRepository)
            {
                var respondentRepository = _respondentRepositorySource.GetForSubset(subset);
                var calculationPeriod = new CalculationPeriod(respondentRepository.EarliestResponseDate,
                    respondentRepository.LatestResponseDate);

                reports.AddRange(
                    _savedReportService.ParseReportsForSubset(savedReports.Reports, subset)
                        .Select(report => new ReportPreloadData(respondentRepository, calculationPeriod, subset, report)));
            }

            return reports;
        }

        private async Task LoadReportDataIntoMemory(IEnumerable<ReportPreloadData> reports, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                var parallelOptions = new ParallelOptions { CancellationToken = cancellationToken };

                await Parallel.ForEachAsync(reports, parallelOptions, async (data, token) =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await _dataPresenceGuarantor.EnsureDataIsLoaded(data.RespondentRepository, data.Subset,
                                data.Report.Measure,
                                data.CalculationPeriod, data.Report.Average, new AlwaysIncludeFilter(),
                                data.Report.TargetInstances.ToArray(), data.Report.Breaks, token);

                            UpdateCachedTask();
                        }
                        catch (Exception ex)
                        {
                            var errorMessage = $"Error loading report data into memory for {data.Report.Measure.Name}";
                            _logger.LogError(ex, errorMessage);
                            UpdateCachedTask(errorMessage);
                        }
                    }
                });
            }
        }

        internal DataPreloadTask StartCachingExportResult(int taskCount)
        {
            var taskStatus = new DataPreloadTaskStatus(taskCount, 0, new List<string>());
            var task = new DataPreloadTask(new CancellationTokenSource(), taskStatus);
            TaskCache.Set(CacheKey, task, TaskCacheEntryOptions);
            return task;
        }

        internal void UpdateCachedTask(string errorMessage = null)
        {
            lock(_cacheLock)
            {
                var taskStatus = GetCurrentTaskStatus();
                if (taskStatus != null)
                {
                    taskStatus.CompletedCount++;
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        taskStatus.Errors.Add(errorMessage);
                    }
                }
                else
                {
                    throw new NotFoundException($"Data preload task for key {CacheKey} was not found");
                }
            }
        }

        private DataPreloadTaskStatus GetCurrentTaskStatus()
        {
            if (TaskCache.TryGetValue(CacheKey, out var cacheValue) && cacheValue is DataPreloadTask preloadTask)
            {
                return preloadTask.Status;
            }

            return null;
        }
    }
}
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.Services
{
    public interface ISavedReportService
    {
        ReportsForSurveyAndUser GetAllReports();
        ReportsForSurveyAndUser GetAllReportsForCurrentUser();
        int CreateReport(CreateNewReportRequest request);
        int CopyReport(CopySavedReportRequest request);
        void UpdateReportSettings(UpdateReportSettingsRequest request);
        void DeleteReport(int savedReportId);
        bool HasReportChanged(int savedReportId, string reportGuid);
        void AddParts(ModifyReportPartsRequest request);
        void UpdateParts(ModifyReportPartsRequest request);
        void DeletePart(DeleteReportPartRequest request);
        bool CheckReportPageNameAlreadyExists(string name, int? savedReportId);
        void UpdateReportModified(SavedReport report, string expectedGuid = null);
        IEnumerable<ParsedReport> ParseReportsForSubset(IEnumerable<Report> reports, Subset subset);
        void UpdatePartColours(int partId, string[] colours);
    }

    public class SavedReportService : ISavedReportService
    {
        private readonly ISavedReportRepository _savedReportRepository;
        private readonly IUserContext _userContext;
        private readonly IProductContext _productContext;
        private readonly IPagesRepository _pagesRepository;
        private readonly IPanesRepository _panesRepository;
        private readonly IPartsRepository _partsRepository;
        private readonly ISavedBreaksRepository _savedBreaksRepository;
        private readonly IMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IWeightingPlanService _weightingPlanService;
        private readonly IMeasureRepository _measureRepository;
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly IRequestAdapter _requestAdapter;
        private readonly IEntityRepository _entityRepository;

        public SavedReportService(ISavedReportRepository savedReportRepository,
            IUserContext userContext,
            IProductContext productContext,
            IPagesRepository pagesRepository,
            IPanesRepository panesRepository,
            IPartsRepository partsRepository,
            ISavedBreaksRepository savedBreaksRepository,
            IMetricConfigurationRepository metricConfigurationRepository,
            IVariableConfigurationRepository variableConfigurationRepository,
            IWeightingPlanService weightingPlanService,
            IMeasureRepository measureRepository,
            IAverageDescriptorRepository averageDescriptorRepository,
            IRequestAdapter requestAdapter,
            IEntityRepository entityRepository)
        {
            _savedReportRepository = savedReportRepository;
            _userContext = userContext;
            _productContext = productContext;
            _pagesRepository = pagesRepository;
            _panesRepository = panesRepository;
            _partsRepository = partsRepository;
            _savedBreaksRepository = savedBreaksRepository;
            _metricConfigurationRepository = metricConfigurationRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _weightingPlanService= weightingPlanService;
            _measureRepository = measureRepository;
            _averageDescriptorRepository = averageDescriptorRepository;
            _requestAdapter = requestAdapter;
            _entityRepository = entityRepository;
        }

        public ReportsForSurveyAndUser GetAllReports()
        {
            var savedReports = _savedReportRepository.GetAll();
            return CreateReportsForSurveyAndUser(savedReports, AllReportsAreAccessibleToCurrentUser);
        }

        public ReportsForSurveyAndUser GetAllReportsForCurrentUser()
        {
            var savedReports = _savedReportRepository.GetFor(_userContext.UserId);
            return CreateReportsForSurveyAndUser(savedReports, GetAccessFunctionForCurrentUser());
        }

        private bool AllReportsAreAccessibleToCurrentUser(int _)
        {
            return true;
        }

        private Func<int, bool> GetAccessFunctionForCurrentUser()
        {
            var userAccessibleMetricNames = _measureRepository.GetAllForCurrentUser().Select(metric => metric.Name).ToHashSet();
            var allMetricNames = _measureRepository.GetAll().Select(metric => metric.Name).ToHashSet();

            var inaccessibleMetricNames = allMetricNames.Where(varCode => !userAccessibleMetricNames.Contains(varCode)).ToHashSet();

            if (!inaccessibleMetricNames.Any())
            {
                return AllReportsAreAccessibleToCurrentUser;
            }

            var parts = _partsRepository.GetParts();
            var areAllPartsInPaneAccessibleToUser = parts
                .GroupBy(p => p.PaneId)
                .ToDictionary(g => g.Key, g => g.All(p => !inaccessibleMetricNames.Contains(p.Spec1)));

            var paneIdToPageId = GetPaneIdToPageIdDictionary();

            var areAllPartsInPageAccessibleToUser = areAllPartsInPaneAccessibleToUser
                .Select(p => (pageId: paneIdToPageId[p.Key], allAccessible: p.Value))
                .GroupBy(g => g.pageId)
                .ToDictionary(g => g.Key, g => g.All(p => p.allAccessible));

            return (int pageId) => areAllPartsInPageAccessibleToUser.GetValueOrDefault(pageId, false);
        }

        private Dictionary<string, int> GetPaneIdToPageIdDictionary()
        {
            var panes = _panesRepository.GetPanes();
            var pages = _pagesRepository.GetPages();

            var pageNameToPageId = pages.ToDictionary(page => page.Name, page => page.Id);
            var paneIdToPageId = panes.ToDictionary(pane => pane.Id, pane => pageNameToPageId[pane.PageName]);
            return paneIdToPageId;
        }

        private ReportsForSurveyAndUser CreateReportsForSurveyAndUser(IReadOnlyCollection<SavedReport> savedReports, Func<int, bool> userCanAccessReport)
        {
            var defaultReport = _savedReportRepository.GetDefault();

            //PagesController / PageHierarchyGenerator will provide the pages so this just needs to give the page id
            return new ReportsForSurveyAndUser
            {
                DefaultReportId = defaultReport?.Id,
                Reports = savedReports.Select(r => new Report
                {
                    SavedReportId = r.Id,
                    IsShared = r.IsShared,
                    PageId = r.ReportPageId,
                    ReportOrder = r.Order,
                    ModifiedDate = r.ModifiedDate,
                    ModifiedGuid = r.ModifiedGuid,
                    LastModifiedByUser = r.LastModifiedByUser,
                    DecimalPlaces = r.DecimalPlaces,
                    ReportType = r.ReportType,
                    Waves = r.Waves,
                    Breaks = r.Breaks,
                    IncludeCounts = r.IncludeCounts,
                    CalculateIndexScores = r.CalculateIndexScores,
                    HighlightLowSample = r.HighlightLowSample,
                    HighlightSignificance = r.HighlightSignificance,
                    SignificanceType = r.SignificanceType,
                    DisplaySignificanceDifferences = r.DisplaySignificanceDifferences,
                    SigConfidenceLevel = r.SigConfidenceLevel,
                    SinglePageExport = r.SinglePageExport,
                    IsDataWeighted = r.IsDataWeighted,
                    HideEmptyRows = r.HideEmptyRows,
                    HideEmptyColumns = r.HideEmptyColumns,
                    HideTotalColumn = r.HideTotalColumn,
                    HideDataLabels = r.HideDataLabels,
                    ShowMultipleTablesAsSingle = r.ShowMultipleTablesAsSingle,
                    BaseTypeOverride = r.BaseTypeOverride ?? BaseDefinitionType.SawThisQuestion,
                    BaseVariableId = r.BaseVariableId,
                    DefaultFilters = r.DefaultFilters,
                    OverTimeConfig = r.OverTimeConfig,
                    SubsetId = r.SubsetId,
                    UserHasAccess = userCanAccessReport(r.ReportPageId),
                    LowSampleThreshold = r.LowSampleThreshold
                })
            };
        }

        public int CreateReport(CreateNewReportRequest request)
        {
            var singlePageExport = false;
            var decimalPlaces = request.AdditionalReportSettings?.DecimalPlaces ?? 1;
            var includeCounts = request.AdditionalReportSettings?.IncludeCounts ?? true;
            var calculateIndexScores = request.AdditionalReportSettings?.CalculateIndexScores ?? false;
            var highlightLowSample = request.AdditionalReportSettings?.HighlightLowSample ?? true;
            var highlightSignificance = request.AdditionalReportSettings?.HighlightSignificance ?? false;
            var displaySignificanceDifferences = request.AdditionalReportSettings?.DisplaySignificanceDifferences ?? DisplaySignificanceDifferences.ShowBoth;
            var hideEmptyRows = false;
            var hideEmptyColumns = false;
            var hideTotalColumn = request.AdditionalReportSettings?.HideTotalColumn ?? false;
            var hideDataLabels = false;
            var showMultipleTablesAsSingle = request.AdditionalReportSettings?.ShowMultipleTablesAsSingle ?? false;
            var significanceType = request.AdditionalReportSettings?.SignificanceType ?? CrosstabSignificanceType.CompareWithinBreak;
            var baseTypeOverride = request.AdditionalReportSettings?.BaseTypeOverride ?? BaseDefinitionType.SawThisQuestion;
            int? baseVariableId = request.AdditionalReportSettings?.BaseVariableId;
            var breaks = request.AdditionalReportSettings?.Categories.ToList() ?? new List<CrossMeasure>();
            var filters = new List<DefaultReportFilter>();
            var isDataWeighted = request.AdditionalReportSettings?.WeightingEnabled ?? _weightingPlanService.HasValidWeightingForSubset(request.SubsetId);

            return CreateAndSaveReport(request.Page,
                request.IsShared,
                request.IsDefault,
                request.Order,
                request.ReportType,
                request.Waves,
                breaks,
                decimalPlaces,
                singlePageExport,
                includeCounts,
                calculateIndexScores,
                highlightLowSample,
                highlightSignificance,
                isDataWeighted,
                hideEmptyRows,
                hideEmptyColumns,
                hideTotalColumn,
                hideDataLabels,
                showMultipleTablesAsSingle,
                significanceType,
                displaySignificanceDifferences,
                SigConfidenceLevel.NinetyFive,
                baseTypeOverride,
                baseVariableId,
                filters,
                request.OverTimeConfig);
        }

        public int CopyReport(CopySavedReportRequest request)
        {
            var savedReport = _savedReportRepository.GetFor(_userContext.UserId)
                .Single(r => r.Id == request.ReportId);

            var page = request.ExistingPage;
            page.Id = 0;
            page.Name = request.NewName;
            page.DisplayName = request.NewDisplayName;
            foreach (var pane in page.Panes)
            {
                pane.Id = null;
                pane.PageName = request.NewName;
                foreach (var part in pane.Parts)
                {
                    part.PaneId = null;
                }
            }

            return CreateAndSaveReport(page,
                request.IsShared,
                request.IsDefault,
                savedReport.Order,
                savedReport.ReportType,
                savedReport.Waves,
                savedReport.Breaks,
                savedReport.DecimalPlaces,
                savedReport.SinglePageExport,
                savedReport.IncludeCounts,
                savedReport.CalculateIndexScores,
                savedReport.HighlightLowSample,
                savedReport.HighlightSignificance,
                savedReport.IsDataWeighted,
                savedReport.HideEmptyRows,
                savedReport.HideEmptyColumns,
                savedReport.HideTotalColumn,
                savedReport.HideDataLabels,
                savedReport.ShowMultipleTablesAsSingle,
                savedReport.SignificanceType,
                savedReport.DisplaySignificanceDifferences,
                savedReport.SigConfidenceLevel,
                savedReport.BaseTypeOverride,
                savedReport.BaseVariableId,
                savedReport.DefaultFilters,
                savedReport.OverTimeConfig,
                savedReport.LowSampleThreshold);
        }

        public void UpdateReportSettings(UpdateReportSettingsRequest request)
        {
            ValidateReport(request.IsShared, request.IsDefault);
            var savedReport = _savedReportRepository.GetById(request.SavedReportId);
            ValidatePageName(request.PageName, savedReport.ReportPageId);
            savedReport.IsShared = request.IsShared;
            savedReport.Order = request.Order;
            savedReport.DecimalPlaces = request.DecimalPlaces;
            savedReport.Waves = request.Waves;
            savedReport.Breaks = new List<CrossMeasure>(request.Breaks);
            savedReport.IncludeCounts = request.IncludeCounts;
            savedReport.CalculateIndexScores = request.CalculateIndexScores;
            savedReport.HighlightLowSample = request.HighlightLowSample;
            savedReport.IsDataWeighted = request.IsDataWeighted;
            savedReport.HighlightSignificance = request.HighlightSignificance;
            savedReport.DisplaySignificanceDifferences = request.DisplaySignificanceDifferences;
            savedReport.SignificanceType = request.SignificanceType;
            savedReport.SigConfidenceLevel = request.SigConfidenceLevel;
            savedReport.SinglePageExport = request.SinglePageExport;
            savedReport.HideEmptyRows = request.HideEmptyRows;
            savedReport.HideEmptyColumns = request.HideEmptyColumns;
            savedReport.HideTotalColumn = request.HideTotalColumn;
            savedReport.HideDataLabels = request.HideDataLabels;
            savedReport.ShowMultipleTablesAsSingle = request.ShowMultipleTablesAsSingle;
            savedReport.BaseTypeOverride = request.BaseTypeOverride;
            savedReport.BaseVariableId = request.BaseVariableId;
            savedReport.DefaultFilters = new List<DefaultReportFilter>(request.DefaultFilters);
            savedReport.ModifiedGuid = request.ModifiedGuid;
            savedReport.OverTimeConfig = request.OverTimeConfig;
            savedReport.SubsetId = request.SubsetId;
            savedReport.LowSampleThreshold = request.LowSampleThreshold;

            UpdateReportModified(savedReport, request.ModifiedGuid);
            _savedReportRepository.Update(savedReport);
            _savedReportRepository.UpdateReportIsDefault(savedReport.Id, request.IsDefault);
            _pagesRepository.UpdatePageName(savedReport.ReportPageId, request.PageDisplayName, request.PageName);
        }

        public void DeleteReport(int savedReportId)
        {
            var report = _savedReportRepository.GetById(savedReportId);
            _pagesRepository.ValidateCanDeletePage(report.ReportPageId);
            _savedReportRepository.Delete(savedReportId);
            _pagesRepository.DeletePage(report.ReportPageId);
        }

        public bool HasReportChanged(int savedReportId, string reportGuid)
        {
            try
            {
                var report = _savedReportRepository.GetById(savedReportId);
                return (_userContext.UserId != report.LastModifiedByUser && reportGuid != report.ModifiedGuid);
            }
            catch (NotFoundException)
            {
                return true;
            }
        }

        public void AddParts(ModifyReportPartsRequest request)
        {
            var report = _savedReportRepository.GetById(request.SavedReportId);
            UpdateReportModified(report, request.ExpectedGuid);
            _savedReportRepository.Update(report);
            _partsRepository.CreateParts(request.Parts);
        }

        public void UpdateParts(ModifyReportPartsRequest request)
        {
            var report = _savedReportRepository.GetById(request.SavedReportId);
            UpdateReportModified(report, request.ExpectedGuid);
            _savedReportRepository.Update(report);
            _partsRepository.UpdateParts(request.Parts);
        }

        public void DeletePart(DeleteReportPartRequest request)
        {
            var report = _savedReportRepository.GetById(request.SavedReportId);
            UpdateReportModified(report, request.ExpectedGuid);
            _savedReportRepository.Update(report);

            var part = _partsRepository.GetById(request.PartIdToDelete);
            var metric = _metricConfigurationRepository.Get(part.Spec1);
            if (!string.IsNullOrWhiteSpace(metric?.OriginalMetricName) && metric.VariableConfigurationId.HasValue)
            {
                //delete net variable
                var variable = _variableConfigurationRepository.Get(metric.VariableConfigurationId.Value);
                _metricConfigurationRepository.Delete(metric.Id);
                _variableConfigurationRepository.Delete(variable);
            }

            _partsRepository.DeletePart(request.PartIdToDelete);
            _partsRepository.UpdateParts(request.PartsToUpdate);
        }

        public bool CheckReportPageNameAlreadyExists(string name, int? savedReportId)
        {
            int? pageId = null;
            if (savedReportId.HasValue)
            {
                var report = _savedReportRepository.GetById(savedReportId.Value);
                pageId = report.ReportPageId;
            }
            return _pagesRepository.PageNameAlreadyExists(name, pageId);
        }

        private int CreateAndSaveReport(PageDescriptor page,
           bool isShared,
           bool isDefault,
           ReportOrder order,
           ReportType reportType,
           ReportWaveConfiguration waves,
           List<CrossMeasure> breaks,
           int decimalPlaces,
           bool singlePageExport,
           bool includeCounts,
           bool calculateIndexScores,
           bool highlightLowSample,
           bool highlightSignificance,
           bool isDataWeighted,
           bool hideEmptyRows,
           bool hideEmptyColumns,
           bool hideTotalColumn,
           bool hideDataLabels,
           bool showMultipleTablesAsSingle,
           CrosstabSignificanceType significanceType,
           DisplaySignificanceDifferences displaySignificanceDifferences,
           SigConfidenceLevel sigConfidenceLevel,
           BaseDefinitionType? baseTypeOverride,
           int? baseVariableId,
           List<DefaultReportFilter> defaultFilters,
           ReportOverTimeConfiguration overTimeConfig,
           int? lowSampleThreshold = null)
        {
            ValidateReport(isShared, isDefault);
            ValidatePageName(page.Name, page.Id);

            _pagesRepository.CreatePage(page);
            var savedReport = new SavedReport
            {
                SubProductId = _productContext.SubProductId,
                ProductShortCode = _productContext.ShortCode,
                IsShared = isShared,
                CreatedByUserId = _userContext.UserId,
                ReportPageId = page.Id,
                Order = order,
                DecimalPlaces = decimalPlaces,
                ReportType = reportType,
                Waves = waves,
                Breaks = breaks,
                SinglePageExport = singlePageExport,
                IncludeCounts = includeCounts,
                CalculateIndexScores = calculateIndexScores,
                HighlightLowSample = highlightLowSample,
                HighlightSignificance = highlightSignificance,
                DisplaySignificanceDifferences = displaySignificanceDifferences,
                IsDataWeighted = isDataWeighted,
                HideEmptyRows = hideEmptyRows,
                HideEmptyColumns = hideEmptyColumns,
                HideTotalColumn = hideTotalColumn,
                HideDataLabels = hideDataLabels,
                ShowMultipleTablesAsSingle = showMultipleTablesAsSingle,
                SignificanceType = significanceType,
                SigConfidenceLevel = sigConfidenceLevel,
                BaseTypeOverride = baseTypeOverride,
                BaseVariableId = baseVariableId,
                DefaultFilters = defaultFilters,
                OverTimeConfig = overTimeConfig,
                LowSampleThreshold = lowSampleThreshold
            };
            UpdateReportModified(savedReport);
            _savedReportRepository.Create(savedReport);

            if (isDefault)
            {
                _savedReportRepository.UpdateReportIsDefault(savedReport.Id, isDefault);
            }

            return savedReport.Id;
        }

        private void ValidateReport(bool isShared, bool isDefault)
        {
            if (isDefault && !isShared)
            {
                throw new BadRequestException("Cannot set a report that is not shared as default");
            }
        }

        private void ValidatePageName(string pageName, int pageId)
        {
            if (PageHierarchyGenerator.PROTECTED_PAGE_NAMES.Contains(pageName, StringComparer.OrdinalIgnoreCase) ||
                _pagesRepository.PageNameAlreadyExists(pageName, pageId))
            {
                throw new BadRequestException($"Report already exists with name: {pageName}");
            }
        }

        public void UpdateReportModified(SavedReport report, string expectedGuid = null)
        {
            if(expectedGuid != null)
            {
               ValidateGuid(report, expectedGuid);
            }

            report.ModifiedDate = DateTimeOffset.UtcNow;
            report.ModifiedGuid = Guid.NewGuid().ToString("N");
            report.LastModifiedByUser = _userContext.UserId;
        }

        private void ValidateGuid(SavedReport report, string expectedGuid)
        {
            if (report.LastModifiedByUser != _userContext.UserId && report.ModifiedGuid != expectedGuid)
            {
                throw new ReportOutOfDateException("Report was out of date");
            }
        }

        public IEnumerable<ParsedReport> ParseReportsForSubset(IEnumerable<Report> reports, Subset subset)
        {
            var parsedReports = new List<ParsedReport>();
            var pagesByPageId = _pagesRepository.GetPages().ToLookup(p => p.Id);
            var panesByPageName = _panesRepository.GetPanes().ToLookup(p => p.PageName);
            var partsByPaneId = _partsRepository.GetParts().ToLookup(p => p.PaneId);
            foreach (var savedReport in reports)
            {
                var page = pagesByPageId[savedReport.PageId].FirstOrDefault();
                if (page == null)
                {
                    continue;
                }

                foreach (var pane in panesByPageName[page.Name].ToArray())
                {
                    foreach (var part in partsByPaneId[pane.Id].ToArray())
                    {
                        var measure = _measureRepository.Get(part.Spec1);
                        foreach (var average in _averageDescriptorRepository)
                        {
                            var splitByInstances = TargetInstancesForEntityType(measure, subset,
                                part.MultipleEntitySplitByAndFilterBy?.SplitByEntityType);

                            var filterByInstances = new List<TargetInstances>();

                            if (part.MultipleEntitySplitByAndFilterBy?.FilterByEntityTypes != null)
                            {
                                foreach (var filterBy in part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes)
                                {
                                    var filterTargetInstances =
                                        TargetInstancesForEntityType(measure, subset, filterBy.Type);
                                    if (filterTargetInstances != null)
                                    {
                                        filterByInstances.Add(filterTargetInstances);
                                    }
                                }
                            }

                            var breaks = part.Breaks != null
                                ? _requestAdapter.CreateBreaks(part.Breaks, subset.Id)
                                : null;

                            parsedReports.Add(new ParsedReport(measure, average, splitByInstances, filterByInstances, breaks));
                        }
                    }
                }
            }

            return parsedReports;
        }

        private TargetInstances TargetInstancesForEntityType(Measure measure, Subset subset,
            string entityTypeIdentifier)
        {
            if (entityTypeIdentifier == null)
            {
                return null;
            }

            var entityType = measure.EntityCombination.First(entityType =>
                entityType.Identifier == entityTypeIdentifier);

            return entityType != null
                ? new TargetInstances(entityType,
                    _entityRepository.GetInstancesOf(entityType.Identifier, subset))
                : null;
        }

        public void UpdatePartColours(int partId, string[] colours)
        {
            var part = _partsRepository.GetById(partId);
            part.Colours = colours;
            _partsRepository.UpdatePart(part);
        }
    }
}

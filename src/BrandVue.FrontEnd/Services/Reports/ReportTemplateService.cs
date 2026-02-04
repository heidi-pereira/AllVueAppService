using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.SourceData.AutoGeneration;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Variable;
using BrandVue.Variable;

namespace BrandVue.Services.Reports
{
    public class ReportTemplateService : IReportTemplateService
    {
        private readonly ISavedReportRepository _savedReportRepository;
        private readonly IUserContext _userContext;
        private readonly IPartsRepository _partsRepository;
        private readonly IPagesRepository _pagesRepository;
        private readonly IPanesRepository _panesRepository;
        private readonly IMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IReportTemplateRepository _reportTemplateRepository;
        private readonly IVariableManager _variableManager;
        private readonly IProductContext _productContext;
        private readonly ISavedReportService _savedReportService;
        private TemplateImportLog _log;
        private IResponseFieldManager _responseFieldManager;
        private readonly IAverageConfigurationRepository _averageConfigurationRepository;

        const string PaneType = "ReportSubPage";
        const string PageType = "SubPage";

        public ReportTemplateService(ISavedReportRepository savedReportRepository,
            IUserContext userContext,
            IPagesRepository pagesRepository,
            IPanesRepository panesRepository,
            IPartsRepository partsRepository,
            IMetricConfigurationRepository metricConfigurationRepository,
            IVariableConfigurationRepository variableConfigurationRepository,
            IReportTemplateRepository reportTemplateRepository,
            IVariableManager variableManager,
            IProductContext productContext,
            ISavedReportService savedReportService,
            IAverageConfigurationRepository averageConfigurationRepository,
            IResponseFieldManager responseFieldManager)
        {
            _savedReportRepository = savedReportRepository;
            _userContext = userContext;
            _partsRepository = partsRepository;
            _pagesRepository = pagesRepository;
            _panesRepository = panesRepository;
            _metricConfigurationRepository = metricConfigurationRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _reportTemplateRepository = reportTemplateRepository;
            _variableManager = variableManager;
            _productContext = productContext;
            _savedReportService = savedReportService;
            _averageConfigurationRepository = averageConfigurationRepository;
            _responseFieldManager = responseFieldManager;
        }

        public async Task<ReportTemplate> SaveReportAsTemplate(ReportTemplateModel model)
        {
            var savedReport = _savedReportRepository.GetById(model.SavedReportId);
            if (savedReport == null)
                throw new NotFoundException($"Saved report with ID {model.SavedReportId} not found.");

            var template = GenerateTemplate(model, savedReport);
            return await _reportTemplateRepository.CreateAsync(template);
        }

        public IEnumerable<ReportTemplate> GetAllTemplatesForUser()
        {
            return _reportTemplateRepository.GetAllForUser();
        }

        public async Task<SavedReport> CreateReportFromTemplate(int templateId, string reportName)
        {
            _log = new TemplateImportLog();
            _log.AddLog(EventType.Report, $"Beginning import of template using id {templateId}", Severity.Info);

            var template = _reportTemplateRepository.GetTemplateById(templateId);
            if (template == null)
            {
                throw new NotFoundException($"Report template with ID {templateId} not found.");
            }

            CreateUserDefinedVariables(template);
            var allMetrics = _metricConfigurationRepository.GetAll();
            var pageId = CreatePagesPanesAndParts(template, allMetrics, reportName);
            var validatedWaves = ValidateWaves(template, allMetrics);
            var validatedBreaks = ValidateBreaks(template, allMetrics);
            var validatedOverTimeConfig = ValidatedOverTimeConfig(template, allMetrics);
            var validatedBaseVariableId = GetValidatedBaseVariableId(template);

            var savedReport = CreateReport(template, pageId, validatedWaves, validatedBreaks, validatedOverTimeConfig, validatedBaseVariableId);
            _savedReportRepository.Create(savedReport);
            return savedReport;
        }

        private SavedReport CreateReport(
            ReportTemplate template,
            int? pageId,
            ReportWaveConfiguration validatedWaves,
            IEnumerable<CrossMeasure> validatedBreaks,
            ReportOverTimeConfiguration validatedOverTimeConfig,
            int? validatedBaseVariableId)
        {
            var savedReport = new SavedReport
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                ReportPageId = pageId.Value,
                ModifiedDate = DateTimeOffset.UtcNow,
                ModifiedGuid = Guid.NewGuid().ToString(),
                LastModifiedByUser = _userContext.UserId,
                BaseVariableId = validatedBaseVariableId,
                Waves = validatedWaves,
                Breaks = validatedBreaks.ToList(),
                DefaultFilters = template.SavedReportTemplate.DefaultFilters,
                OverTimeConfig = validatedOverTimeConfig,
                SubsetId = template.SavedReportTemplate.SubsetId,
                IsShared = template.SavedReportTemplate.IsShared,
                Order = template.SavedReportTemplate.Order,
                DecimalPlaces = template.SavedReportTemplate.DecimalPlaces,
                ReportType = template.SavedReportTemplate.ReportType,
                HighlightSignificance = template.SavedReportTemplate.HighlightSignificance,
                SignificanceType = template.SavedReportTemplate.SignificanceType,
                DisplaySignificanceDifferences = template.SavedReportTemplate.DisplaySignificanceDifferences,
                SigConfidenceLevel = template.SavedReportTemplate.SigConfidenceLevel,
                IncludeCounts = template.SavedReportTemplate.IncludeCounts,
                CalculateIndexScores = template.SavedReportTemplate.CalculateIndexScores,
                HighlightLowSample = template.SavedReportTemplate.HighlightLowSample,
                IsDataWeighted = template.SavedReportTemplate.IsDataWeighted,
                HideEmptyRows = template.SavedReportTemplate.HideEmptyRows,
                HideEmptyColumns = template.SavedReportTemplate.HideEmptyColumns,
                HideTotalColumn = template.SavedReportTemplate.HideTotalColumn,
                HideDataLabels = template.SavedReportTemplate.HideDataLabels,
                ShowMultipleTablesAsSingle = template.SavedReportTemplate.ShowMultipleTablesAsSingle,
                BaseTypeOverride = template.SavedReportTemplate.BaseTypeOverride,
                CreatedByUserId = _userContext.UserId,
                SinglePageExport = template.SavedReportTemplate.SinglePageExport,
                LowSampleThreshold = template.SavedReportTemplate.LowSampleThreshold
            };
            _savedReportService.UpdateReportModified(savedReport);

            _log.AddLog(EventType.Report, $"Report created from template", Severity.Info);
            savedReport.TemplateImportLog = _log;
            return savedReport;
        }

        private int? GetValidatedBaseVariableId(ReportTemplate template)
        {
            if (template.BaseVariable == null || string.IsNullOrEmpty(template.BaseVariable.Identifier))
                return null;

            var matchedVariables = _variableConfigurationRepository.GetAll()
                .Where(v => v.Identifier == template.BaseVariable.Identifier &&
                            (v.Definition is BaseGroupedVariableDefinition ||
                             v.Definition is BaseFieldExpressionVariableDefinition))
                .ToList();

            if (!matchedVariables.Any())
            {
                _log.AddLog(EventType.Report, $"Base variable with identifier {template.BaseVariable.Identifier} not found, omitting from report", Severity.Warning);
                return null;
            }

            if (matchedVariables.Count > 1)
            {
                _log.AddLog(EventType.Report, $"Multiple base variables with identifier {template.BaseVariable.Identifier} found, omitting from report", Severity.Warning);
                return null;
            }

            return matchedVariables.Single().Id;
        }

        private ReportOverTimeConfiguration ValidatedOverTimeConfig(ReportTemplate template, IReadOnlyCollection<MetricConfiguration> allMetrics)
        {
            if (template.SavedReportTemplate.OverTimeConfig == null)
            {
                return null;
            }

            if (template.AverageConfiguration == null)
            {
                _log.AddLog(EventType.Report, "Over time configuration has a declared average, but this average is not defined", Severity.Error);
                return null;
            }

            var averageId = template.SavedReportTemplate.OverTimeConfig.AverageId;
            var existingAverage = _averageConfigurationRepository.GetAll()
                .SingleOrDefault(a => a.AverageId == averageId);

            if (existingAverage == null)
            {
                try
                {
                    SeedNewAverageConfiguration(template.AverageConfiguration);
                }
                catch (Exception ex)
                {
                    _log.AddLog(EventType.Report, $"Error seeding average descriptor: {ex.Message}", Severity.Error);
                    return null;
                }
            }
            else
            {
                _log.AddLog(EventType.Report, $"An average with identifier {averageId} already exists and will be used in over time configuration", Severity.Info);
            }

            return template.SavedReportTemplate.OverTimeConfig;
        }

        private void SeedNewAverageConfiguration(AverageConfiguration templateAverageConfiguration)
        {
            var averageConfiguration = new AverageConfiguration
            {
                AverageId = templateAverageConfiguration.AverageId,
                DisplayName = templateAverageConfiguration.DisplayName,
                Order = templateAverageConfiguration.Order,
                TotalisationPeriodUnit = templateAverageConfiguration.TotalisationPeriodUnit,
                NumberOfPeriodsInAverage = templateAverageConfiguration.NumberOfPeriodsInAverage,
                WeightingMethod = templateAverageConfiguration.WeightingMethod,
                WeightAcross = templateAverageConfiguration.WeightAcross,
                AverageStrategy = templateAverageConfiguration.AverageStrategy,
                MakeUpTo = templateAverageConfiguration.MakeUpTo,
                WeightingPeriodUnit = templateAverageConfiguration.WeightingPeriodUnit,
                IncludeResponseIds = templateAverageConfiguration.IncludeResponseIds,
                IsDefault = templateAverageConfiguration.IsDefault,
                AllowPartial = templateAverageConfiguration.AllowPartial,
                Group = templateAverageConfiguration.Group,
                Disabled = templateAverageConfiguration.Disabled,
                SubsetIds = templateAverageConfiguration.SubsetIds ?? Array.Empty<string>(),
                SubProductId = _productContext.SubProductId,
                ProductShortCode = _productContext.ShortCode
            };

            _averageConfigurationRepository.Create(averageConfiguration);
        }

        private IEnumerable<CrossMeasure> ValidateBreaks(ReportTemplate template, IReadOnlyCollection<MetricConfiguration> allMetrics)
        {
            if (template.SavedReportTemplate.Breaks == null || !template.SavedReportTemplate.Breaks.Any())
            {
                return Array.Empty<CrossMeasure>();
            }

            var validatedBreaks = template.SavedReportTemplate.Breaks;
            var breakNames = validatedBreaks.Select(b => b.MeasureName);
            foreach (var breakName in breakNames)
            {
                if (!allMetrics.Any(m => m.Name == breakName))
                {
                    _log.AddLog(EventType.Metric, $"Break metric {breakName} not found in the system, omitting breaks from report", Severity.Warning);
                    validatedBreaks = new List<CrossMeasure>();
                    break;
                }
            }
            return validatedBreaks;
        }

        private ReportWaveConfiguration ValidateWaves(ReportTemplate template, IReadOnlyCollection<MetricConfiguration> allMetrics)
        {
            if (template.SavedReportTemplate.Waves == null || template.SavedReportTemplate.Waves.Waves == null)
            {
                return null;
            }

            var validatedWaves = template.SavedReportTemplate.Waves;
            var waveMetric = template.SavedReportTemplate.Waves.Waves.MeasureName;
            if (waveMetric != null && !allMetrics.Any(m => m.Name == waveMetric))
            {
                _log.AddLog(EventType.Metric, $"Wave metric {waveMetric} not found in the system, omitting from report", Severity.Warning);
                validatedWaves = null;
            }
            return validatedWaves;
        }

        private ReportTemplate GenerateTemplate(ReportTemplateModel model, SavedReport savedReport)
        {
            var template = new ReportTemplate();
            template.TemplateDisplayName = model.TemplateDisplayName;
            template.TemplateDescription = model.TemplateDescription;

            ReportOverTimeConfiguration sanitizedOverTimeConfig = null;
            if (savedReport.OverTimeConfig != null)
            {
                template.AverageConfiguration = GenerateSanitizedAverageConfiguration(savedReport.OverTimeConfig);
                sanitizedOverTimeConfig = SanitizeOverTimeConfig(savedReport.OverTimeConfig, template.AverageConfiguration?.AverageId);
            }

            template.UserId = _userContext.UserId;
            template.SavedReportTemplate = new SavedReportTemplate(
                savedReport.IsShared,
                savedReport.Order,
                savedReport.DecimalPlaces,
                savedReport.ReportType,
                savedReport.Waves,
                savedReport.Breaks,
                savedReport.SinglePageExport,
                savedReport.HighlightSignificance,
                savedReport.SignificanceType,
                savedReport.DisplaySignificanceDifferences,
                savedReport.SigConfidenceLevel,
                savedReport.IncludeCounts,
                savedReport.CalculateIndexScores,
                savedReport.HighlightLowSample,
                savedReport.IsDataWeighted,
                savedReport.HideEmptyRows,
                savedReport.HideEmptyColumns,
                savedReport.HideTotalColumn,
                savedReport.HideDataLabels,
                savedReport.ShowMultipleTablesAsSingle,
                savedReport.BaseTypeOverride,
                savedReport.DefaultFilters,
                sanitizedOverTimeConfig,
                savedReport.SubsetId,
                savedReport.LowSampleThreshold
            );

            var userDefinedVariables = _variableConfigurationRepository.GetAll()
                .Where(v => IsCreatedByUser(v.Definition))
                .Select(_variableManager.ConvertToModel).ToArray();

            template.UserDefinedVariableDefinitions = userDefinedVariables.Select(SanitizeVariable);
            PopulateBaseVariableForTemplate(savedReport, template, userDefinedVariables);
            PopulatePartsForTemplate(savedReport, template);
            template.CreatedAt = DateTime.UtcNow;
            return template;
        }

        private ReportOverTimeConfiguration SanitizeOverTimeConfig(ReportOverTimeConfiguration overTimeConfig, string? sanitizedAverageId)
        {
            return new ReportOverTimeConfiguration
            {
                AverageId = sanitizedAverageId,
                Range = overTimeConfig.Range,
                CustomRange = overTimeConfig.CustomRange,
                SavedRanges = overTimeConfig.SavedRanges ?? Array.Empty<CustomDateRange>(),
            };
        }

        private AverageConfiguration GenerateSanitizedAverageConfiguration(ReportOverTimeConfiguration overTimeConfig)
        {
            if (string.IsNullOrEmpty(overTimeConfig.AverageId))
            {
                return null;
            }

            var storedAverage = _averageConfigurationRepository.GetAll()
                .Where(a => a.AverageId == overTimeConfig.AverageId)
                .SingleOrDefault();

            if (storedAverage == null)
            {
                return null;
            }

            var sanitizedAverage = new AverageConfiguration
            {
                AverageId = storedAverage.AverageId,
                DisplayName = storedAverage.DisplayName,
                Order = storedAverage.Order,
                TotalisationPeriodUnit = storedAverage.TotalisationPeriodUnit,
                NumberOfPeriodsInAverage = storedAverage.NumberOfPeriodsInAverage,
                WeightingMethod = storedAverage.WeightingMethod,
                WeightAcross = storedAverage.WeightAcross,
                AverageStrategy = storedAverage.AverageStrategy,
                MakeUpTo = storedAverage.MakeUpTo,
                WeightingPeriodUnit = storedAverage.WeightingPeriodUnit,
                IncludeResponseIds = storedAverage.IncludeResponseIds,
                IsDefault = storedAverage.IsDefault,
                AllowPartial = storedAverage.AllowPartial,
                Group = storedAverage.Group,
                SubsetIds = storedAverage.SubsetIds ?? Array.Empty<string>(),
                Disabled = storedAverage.Disabled

            };
            return sanitizedAverage;
        }

        private void PopulatePartsForTemplate(SavedReport savedReport, ReportTemplate template)
        {
            var reportParts = GetPartsForPage(savedReport.ReportPageId);
            var templateParts = new List<ReportTemplatePart>();
            foreach (var part in reportParts)
            {
                var partMetric = _metricConfigurationRepository.Get(part.Spec1);
                if (partMetric == null)
                    throw new NotFoundException($"Metric {part.Spec1} cannot be found");

                var templatePart = new ReportTemplatePart(
                        part.Spec1,
                        partMetric.VarCode,
                        part.Spec2,
                        part.DefaultSplitBy,
                        part.HelpText,
                        part.Ordering,
                        part.OrderingDirection,
                        part.Colours,
                        part.Filters,
                        part.Breaks,
                        part.OverrideReportBreaks,
                        part.ShowTop,
                        part.MultipleEntitySplitByAndFilterBy,
                        part.ReportOrder,
                        part.BaseExpressionOverride,
                        part.Waves,
                        part.SelectedEntityInstances,
                        part.AverageTypes,
                        part.MultiBreakSelectedEntityInstance,
                        part.DisplayMeanValues,
                        part.DisplayStandardDeviation,
                        part.CustomConfigurationOptions,
                        part.PartType,
                        part.ShowOvertimeData
                    );
                templateParts.Add(templatePart);
            }
            template.ReportTemplateParts = templateParts;
        }

        private PaneDescriptor[] GetPane(string pageName, PartDescriptor[] parts)
        {
            var pane = new PaneDescriptor()
            {
                PageName = pageName,
                Height = 500,
                PaneType = PaneType,
                View = -10,
                Parts = parts
            };
            return [pane];
        }

        private int? CreatePagesPanesAndParts(ReportTemplate template, IEnumerable<MetricConfiguration> allMetrics, string reportName)
        {
            var parts = SeedParts(template, allMetrics);
            var pageName = PagePanePartHelper.SanitizeUrl(reportName);
            var page = new PageDescriptor()
            {
                Name = pageName,
                DisplayName = reportName,
                PageType = PageType,                
                Panes = GetPane(pageName, parts.ToArray())
            };
            return _pagesRepository.CreatePage(page);
        }

        private List<PartDescriptor> SeedParts(ReportTemplate template, IEnumerable<MetricConfiguration> allMetrics)
        {
            var parts = new List<PartDescriptor>();
            foreach (var templatePart in template.ReportTemplateParts)
            {
                var metric = _metricConfigurationRepository.Get(templatePart.MetricName);
                if (metric == null || metric == default)
                {
                    metric = allMetrics.FirstOrDefault(metric => metric.VarCode == (templatePart.MetricVarcode));
                    if (metric == null || metric == default)
                    {
                        _log.AddLog(EventType.Metric, $"Metric {templatePart.MetricName} with varcode {templatePart.MetricVarcode} not found, skipping creation of {templatePart.HelpText}", Severity.Error);
                        continue;
                    }
                    else
                    {
                        _log.AddLog(EventType.Metric, $"Metric {templatePart.MetricName} not found by name, but found by varcode {templatePart.MetricVarcode}.", Severity.Warning);
                    }
                }

                var part = new PartDescriptor
                {
                    Spec1 = metric.Name,
                    Spec2 = templatePart.Position,
                    DefaultSplitBy = templatePart.DefaultSplitBy,
                    HelpText = templatePart.HelpText,
                    Ordering = templatePart.Ordering,
                    OrderingDirection = templatePart.OrderingDirection,
                    Colours = templatePart.Colours,
                    Filters = templatePart.Filters,
                    Breaks = templatePart.Breaks,
                    OverrideReportBreaks = templatePart.OverrideReportBreaks,
                    ShowTop = templatePart.ShowTop,
                    MultipleEntitySplitByAndFilterBy = templatePart.MultipleEntitySplitByAndFilterBy,
                    ReportOrder = templatePart.ReportOrder,
                    BaseExpressionOverride = templatePart.BaseExpressionOverride,
                    Waves = templatePart.Waves,
                    SelectedEntityInstances = templatePart.SelectedEntityInstances,
                    AverageTypes = templatePart.AverageTypes,
                    MultiBreakSelectedEntityInstance = templatePart.MultiBreakSelectedEntityInstance,
                    DisplayMeanValues = templatePart.DisplayMeanValues,
                    DisplayStandardDeviation = templatePart.DisplayStandardDeviation,
                    CustomConfigurationOptions = templatePart.CustomConfigurationOptions,
                    ShowOvertimeData = templatePart.ShowOvertimeData,
                    PartType = templatePart.PartType
                };
                parts.Add(part);
            }

            return parts;
        }

        private void CreateUserDefinedVariables(ReportTemplate template)
        {
            foreach (var templateVariable in template.UserDefinedVariableDefinitions)
            {
                try
                {
                    CreateVariable(template, templateVariable);
                }
                catch (Exception)
                {
                    _log.AddLog(EventType.Variable,
                        $"Something went wrong creating {templateVariable.DisplayName}, it will not be included in the report.",
                        Severity.Error);
                    continue;
                }
            }
        }

        private void CreateVariable(ReportTemplate template, VariableConfiguration templateVariable, int depth = 0)
        {
            var storedVariable = _variableConfigurationRepository.GetByIdentifier(templateVariable.Identifier);
            if (storedVariable != null)
            {
                return;
            }

            if (IsTrappedInRecursion(templateVariable, depth))
            {
                throw new InvalidOperationException($"Recursion depth exceeded while creating variable {templateVariable.Identifier}.");
            }

            var fieldsReferencedInVariable = GetFieldsReferencedInVariable(templateVariable.Definition);
            var userDefinedFields = template.UserDefinedVariableDefinitions
                .Select(v => v.Identifier)
                .ToArray();

            foreach (var field in fieldsReferencedInVariable)
            {
                if (userDefinedFields.Contains(field))
                {
                    var referencedVariable = template.UserDefinedVariableDefinitions.FirstOrDefault(v => v.Identifier == field);
                    //Create the referenced variable first
                    _log.AddLog(EventType.Variable,
                        $"{templateVariable.DisplayName} relies on variable {referencedVariable.DisplayName}, attempting to create this first.",
                        Severity.Info);
                    CreateVariable(template, referencedVariable, depth + 1);
                }
                if (!_responseFieldManager.TryGet(field, out var locatedField))
                {
                    _log.AddLog(EventType.Variable,
                        $"Unable to find field {field} referenced in variable {templateVariable.DisplayName}, it will not be included in the report.",
                        Severity.Error);
                    throw new NotFoundException($"Field {field} referenced in variable {templateVariable.DisplayName} not found.");
                }
            }

            _log.AddLog(EventType.Variable,
                $"Unable to find variable with identifier {templateVariable.Identifier}, attempting to create",
                Severity.Warning);

            CreateNewVariableAndMetaDataFromDefinition(templateVariable);
        }

        private bool IsTrappedInRecursion(VariableConfiguration templateVariable, int depth)
        {
            const int MaxRecursionDepth = 100;
            if (depth > MaxRecursionDepth)
            {
                _log.AddLog(EventType.Variable,
                    $"Recursion depth exceeded while creating variable {templateVariable.Identifier}. Skipping to prevent stack overflow.",
                    Severity.Error);
                return true;
            }

            return false;
        }

        private void CreateNewVariableAndMetaDataFromDefinition(VariableConfiguration templateVariable)
        {
            var model = new VariableConfigurationCreateModel()
            {
                Name = templateVariable.DisplayName,
                Definition = templateVariable.Definition,
                CalculationType = SourceData.Measures.CalculationType.YesNo,
                IdentifierOverride = templateVariable.Identifier,
            };

            _variableManager.ConstructVariableAndRelatedMetadata(model);
        }

        private IEnumerable<string> GetFieldsReferencedInVariable(VariableDefinition definition)
        {
            switch (definition)
            {
                case FieldExpressionVariableDefinition fieldExprDef:
                    _log.AddLog(EventType.Variable,
                        $"Unable to establish if {definition} references questions or variables, it will be included in the report but may cause errors",
                        Severity.Warning);
                    return [];

                case GroupedVariableDefinition groupedDef:
                    return groupedDef.Groups
                        .SelectMany(g => GetFieldsFromComponent(g.Component).Select(b => b))
                        .Distinct();

                case SingleGroupVariableDefinition singleGroupDef:
                    return GetFieldsFromComponent(singleGroupDef.Group.Component);

                default:
                    return [];
            }
        }

        private IEnumerable<string> GetFieldsFromComponent(VariableComponent component)
        {
            switch (component)
            {
                case InclusiveRangeVariableComponent inclusiveRange:
                    return [inclusiveRange.FromVariableIdentifier];

                case InstanceListVariableComponent instanceList:
                    return [instanceList.FromVariableIdentifier];

                case CompositeVariableComponent composite:
                    return composite.CompositeVariableComponents.SelectMany(c => GetFieldsFromComponent(c));

                case SurveyIdVariableComponent surveyId:
                case DateRangeVariableComponent dateRange:
                default:
                    return [];
            }
        }

        private static void PopulateBaseVariableForTemplate(SavedReport savedReport, ReportTemplate template, VariableConfigurationModel[] userDefinedVariables)
        {
            if (savedReport.BaseVariableId != null)
            {
                var baseVariable = userDefinedVariables.FirstOrDefault(v => v.Id == savedReport.BaseVariableId);

                if (baseVariable == null)
                    throw new NotFoundException("Report base variable cannot be found");

                var sanitizedBaseVariable = SanitizeVariable(baseVariable);
                template.BaseVariable = sanitizedBaseVariable;
            }
        }

        private List<PartDescriptor> GetPartsForPage(int pageId)
        {
            const int DefaultSpec2OrderValue = -1;
            var page = _pagesRepository.GetPages()
                .Single(x => x.Id == pageId);
            var paneIdsForPage = _panesRepository.GetPanes()
                .Where(pane => pane.PageName == page.Name)
                .Select(pane => pane.Id)
                .ToHashSet();
            var reportParts = _partsRepository.GetParts()
                .Where(part => paneIdsForPage.Contains(part.PaneId));
            return reportParts.OrderBy(x => int.TryParse(x.Spec2, out var value) ? value : DefaultSpec2OrderValue).ToList();
        }

        public static bool IsCreatedByUser(VariableDefinition definition)
        {
            if (definition is QuestionVariableDefinition)
                return false;
            if (definition is GroupedVariableDefinition grouped)
                return !grouped.ToEntityTypeName.StartsWith(AutoGenerationConstants.NumericIdentifier, StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private static VariableConfiguration SanitizeVariable(VariableConfigurationModel variable)
        {
            return new VariableConfiguration()
            {
                Definition = variable.Definition,
                DisplayName = variable.DisplayName,
                Identifier = variable.Identifier,
            };
        }
    }
}

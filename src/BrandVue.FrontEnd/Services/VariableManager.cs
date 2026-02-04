using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using BrandVue.Variable;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Utils;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;

namespace BrandVue.Services
{
    public class VariableManager : IVariableManager
    {
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IProductContext _productContext;
        private readonly IPartsRepository _partsRepository;
        private readonly IPagesRepository _pagesRepository;
        private readonly IPanesRepository _panesRepository;
        private readonly IVariableFactory _variableFactory;
        private readonly IVariableConfigurationFactory _variableConfigurationFactory;
        private readonly ISavedBreaksRepository _savedBreaksRepository;
        private readonly IVariableValidator _variableValidator;
        private readonly ISavedReportRepository _savedReportRepository;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;
        private readonly IFieldExpressionParser _fieldExpressionParser;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly ISavedReportService _savedReportService;
        private readonly IEntityRepository _entityRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly IClaimRestrictedSubsetRepository _claimRestrictedSubsetRepository;
        private readonly IMetricConfigurationFactory _metricConfigurationFactory;
        private readonly ILogger<VariableManager> _logger;

        public VariableManager(IVariableConfigurationRepository variableConfigurationRepository,
            IProductContext productContext,
            IMetricConfigurationRepository metricConfigurationRepository,
            IPartsRepository partsRepository,
            IVariableFactory variableFactory,
            IVariableConfigurationFactory variableConfigurationFactory,
            ISavedBreaksRepository savedBreaksRepository,
            IVariableValidator variableValidator,
            IPagesRepository pagesRepository,
            IPanesRepository panesRepository,
            ISavedReportRepository savedReportRepository,
            IBaseExpressionGenerator baseExpressionGenerator,
            IFieldExpressionParser fieldExpressionParser,
            IWeightingPlanRepository weightingPlanRepository,
            ISavedReportService savedReportService,
            IEntityRepository entityRepository,
            IMeasureRepository measureRepository,
            IClaimRestrictedSubsetRepository claimRestrictedSubsetRepository,
            IMetricConfigurationFactory metricConfigurationFactory,
            ILogger<VariableManager> logger)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
            _productContext = productContext;
            _metricConfigurationRepository = metricConfigurationRepository;
            _partsRepository = partsRepository;
            _variableFactory = variableFactory;
            _variableConfigurationFactory = variableConfigurationFactory;
            _savedBreaksRepository = savedBreaksRepository;
            _variableValidator = variableValidator;
            _pagesRepository = pagesRepository;
            _panesRepository = panesRepository;
            _savedReportRepository = savedReportRepository;
            _baseExpressionGenerator = baseExpressionGenerator;
            _fieldExpressionParser = fieldExpressionParser;
            _weightingPlanRepository = weightingPlanRepository;
            _savedReportService = savedReportService;
            _entityRepository = entityRepository;
            _measureRepository = measureRepository;
            _claimRestrictedSubsetRepository = claimRestrictedSubsetRepository;
            _metricConfigurationFactory = metricConfigurationFactory;
            _logger = logger;
        }

        public CreateVariableResultModel ConstructVariableAndRelatedMetadata(VariableConfigurationCreateModel model)
        {
            var identifier = model.IdentifierOverride ?? _variableConfigurationFactory.CreateIdentifierFromName(model.Name);
            var variableConfig = _variableConfigurationFactory.CreateVariableConfigFromParameters(model.Name,
                identifier,
                model.Definition,
                out var dependencyVariableInstanceIdentifiers,
                out _
                );

            var createdVariable = _variableConfigurationRepository.Create(variableConfig, dependencyVariableInstanceIdentifiers);
            MetricConfiguration createdMetric;
            try
            {
                var calculationType = model.CalculationType ?? CalculationType.YesNo;
                createdMetric = CreateNewMetricForVariable(createdVariable, calculationType);
                if (model.ReportSettings != null)
                {
                    var splitByEntityTypeName = variableConfig.Definition is GroupedVariableDefinition groupedDefinition ? groupedDefinition.ToEntityTypeName : null;
                    UpdateSavedReportWithNewVariable(model.ReportSettings, variableConfig, createdMetric);
                }
            }
            catch (Exception)
            {
                _variableConfigurationRepository.Delete(createdVariable);
                throw;
            }

            return new CreateVariableResultModel { UrlSafeMetricName = createdMetric.Name.SanitizeUrlSegment(), Metric = createdMetric };
        }

        public VariableConfigurationModel ConvertToModel(VariableConfiguration variableConfiguration)
        {
            return new VariableConfigurationModel
            {
                Id = variableConfiguration.Id,
                ProductShortCode = variableConfiguration.ProductShortCode,
                SubProductId = variableConfiguration.SubProductId,
                Identifier = variableConfiguration.Identifier,
                DisplayName = variableConfiguration.DisplayName,
                Definition = variableConfiguration.Definition
            };
        }

        public Measure ConstructTemporaryVariableSampleMeasure(VariableGrouping group)
        {
            var definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = "Variable for sample",
                ToEntityTypeDisplayNamePlural = "Variable for sample",
                Groups = new List<VariableGrouping> { group }
            };
            var baseExpression = _baseExpressionGenerator.BaseExpressionForAnyoneAskedThisQuestion(definition, false);
            var variable = ConstructTemporaryVariableForGroup(group);
            return ConstructTemporaryVariableMeasure(group.ToEntityInstanceName, variable, baseExpression);
        }

        public Measure ConstructTemporaryVariableSampleMeasure(FieldExpressionVariableDefinition definition)
        {
            try
            {
                var variable = _fieldExpressionParser.ParseUserNumericExpressionOrNull(definition.Expression);
                return ConstructTemporaryVariableMeasure("Variable for sample", variable, "1");
            }
            catch (Exception x)
            {
                throw new BadRequestException($"Parsing variable failed: {x.Message}");
            }
        }

        public Measure ConstructTemporaryVariableMeasure(GroupedVariableDefinition definition)
        {
            try
            {
                var variableConfig = _variableConfigurationFactory.CreateVariableConfigFromParameters("Temporary Variable Measure",
                    "temporary_variable_measure",
                    definition,
                    out var dependencyVariableInstanceIdentifiers,
                    out _
                );
                var variable = _fieldExpressionParser.CreateTemporaryVariable(variableConfig);
                var baseExpression = _baseExpressionGenerator.BaseExpressionForAnyoneAskedThisQuestion(definition, true);
                return ConstructTemporaryVariableMeasure("Temporary variable measure", variable, baseExpression);
            }
            catch (Exception x)
            {
                throw new BadRequestException($"Parsing variable failed: {x.Message}");
            }
        }

        private IVariable<int?> ConstructTemporaryVariableForGroup(VariableGrouping group)
        {
            return group.Component switch
            {
                DateRangeVariableComponent => new DataWaveProfileVariable(group),
                SurveyIdVariableComponent => new SurveyIdProfileVariable(group),
                _ => _fieldExpressionParser.ParseUserNumericExpressionOrNull(group.Component.GetPythonCondition())
            };
        }

        private Measure ConstructTemporaryVariableMeasure(string name, IVariable<int?> variable, string baseExpression)
        {
            var baseVariable = _fieldExpressionParser.ParseUserBooleanExpression(baseExpression);
            return new()
            {
                Name = name,
                PrimaryVariable = variable,
                BaseExpression = baseVariable
            };
        }

        public int CreateBaseVariable(VariableConfigurationCreateModel model)
        {
            if (!model.Definition.IsBaseVariable())
            {
                throw new BadRequestException($"Variable ({model.Definition.GetType().Name}) must be of {nameof(BaseGroupedVariableDefinition)} or {nameof(BaseFieldExpressionVariableDefinition)} type to be used as base variable");
            }

            var identifier = _variableConfigurationFactory.CreateIdentifierFromName(model.Name);
            var variableConfig = _variableConfigurationFactory.CreateVariableConfigFromParameters(model.Name,
                identifier,
                model.Definition,
                out var dependencyVariableInstanceIdentifiers,
                out var entityTypeNames);

            if (variableConfig.Definition is BaseFieldExpressionVariableDefinition baseExpressionDefinition)
            {
                baseExpressionDefinition.ResultEntityTypeNames = entityTypeNames;
            }

            var newBase = _variableConfigurationRepository.Create(variableConfig, dependencyVariableInstanceIdentifiers);

            if (model.ReportSettings != null && model.ReportSettings.AppendType == ReportVariableAppendType.Base)
            {
                var report = _savedReportRepository.GetById(model.ReportSettings.ReportIdToAppendTo);
                AppendBaseToReport(report, newBase, model.ReportSettings.SelectedPart);
            }

            return newBase.Id;
        }

        private void UpdateSavedReportWithNewVariable(VariableConfigurationReportSettings configuration,
            VariableConfiguration variableConfig,
            MetricConfiguration createdMetric)
        {
            var report = _savedReportRepository.GetById(configuration.ReportIdToAppendTo);
            var entityCombination = _variableFactory.ParseResultEntityTypeNames(variableConfig);
            switch (configuration.AppendType)
            {
                case ReportVariableAppendType.Filters:
                    AppendFiltersToReport(report, createdMetric, entityCombination);
                    break;
                case ReportVariableAppendType.Waves:
                    AppendWavesToReport(report, createdMetric, configuration.SelectedPart);
                    break;
                case ReportVariableAppendType.Breaks:
                    var isSingleEntity = entityCombination.Count == 1;
                    if (isSingleEntity)
                    {
                        if (report.ReportType == ReportType.Table)
                        {
                            AppendBreaksToTableReport(report, createdMetric, configuration.SelectedPart);
                        }
                        else
                        {
                            AppendBreaksToChartReport(report, variableConfig, createdMetric, configuration.SelectedPart);
                        }
                    }
                    break;
            }
        }

        private PaneDescriptor GetPaneForReport(SavedReport report)
        {
            return _panesRepository.GetPanes()
                .FirstOrDefault(pane => pane.PageName == report.ReportPage.Name);
        }

        private PartDescriptor GetReportPart(PaneDescriptor pane, string selectedPartSpec2)
        {
            return _partsRepository.GetParts()
                .FirstOrDefault(part => part.PaneId == pane?.Id && part.Spec2 == selectedPartSpec2);
        }

        private void AppendBreaksToChartReport(SavedReport report,
            VariableConfiguration variableConfig,
            MetricConfiguration createdMetric,
            string selectedPart)
        {
            var crossMeasure = new CrossMeasure
            {
                MeasureName = createdMetric.Name
            };

            if (variableConfig.Definition is GroupedVariableDefinition groupedVariableDefinition)
            {
                crossMeasure.FilterInstances = groupedVariableDefinition.Groups.Take(3)
                    .Select(group => new CrossMeasureFilterInstance
                    {
                        FilterValueMappingName = group.ToEntityInstanceName,
                        InstanceId = group.ToEntityInstanceId
                    }).ToArray();
            }

            if (selectedPart != null)
            {
                var pane = GetPaneForReport(report);
                var part = GetReportPart(pane, selectedPart);
                if (pane != null && part != null)
                {
                    var wasMultiBreak = part.Breaks?.Length > 1;
                    if (part.PartType == PartType.ReportsCardStackedMulti)
                    {
                        part.Breaks = new[] { crossMeasure };
                    }
                    else
                    {
                        part.Breaks = (part.Breaks ?? Array.Empty<CrossMeasure>()).Append(crossMeasure).ToArray();
                    }
                    var isNowMultiBreak = part.Breaks.Length > 1;

                    if (!wasMultiBreak && isNowMultiBreak)
                    {
                        var measure = _measureRepository.Get(part.Spec1);
                        var entityType = measure.EntityCombination.First();
                        var subsets = _claimRestrictedSubsetRepository.GetAllowed();
                        var subset = subsets.Any() ? subsets.First() : new Subset { Id = "All" };
                        var allInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset);
                        part.MultiBreakSelectedEntityInstance = allInstances.Any() ? allInstances.First().Id : 1;
                    }
                    else if(wasMultiBreak && !isNowMultiBreak)
                    {
                        part.MultiBreakSelectedEntityInstance = null;
                    }

                    _partsRepository.UpdatePart(part);
                }
            }
            else
            {
                report.Breaks = new[] { crossMeasure }.ToList();
                _savedReportService.UpdateReportModified(report);
                _savedReportRepository.Update(report);
            }
        }

        private void AppendBreaksToTableReport(SavedReport report, MetricConfiguration createdMetric, string selectedPart)
        {
            var crossMeasure = new CrossMeasure
            {
                MeasureName = createdMetric.Name
            };

            if (selectedPart != null)
            {
                var pane = GetPaneForReport(report);
                var part = GetReportPart(pane, selectedPart);
                if (pane != null && part != null)
                {
                    part.Breaks = (part.Breaks ?? Array.Empty<CrossMeasure>()).Append(crossMeasure).ToArray();
                    _partsRepository.UpdatePart(part);
                }
            }
            else
            {
                report.Breaks = report.Breaks ?? new List<CrossMeasure>();
                report.Breaks.Add(crossMeasure);
                _savedReportService.UpdateReportModified(report);
                _savedReportRepository.Update(report);
            }
        }

        private void AppendFiltersToReport(SavedReport report,
            MetricConfiguration createdMetric,
            IReadOnlyCollection<string> entityTypeNames)
        {
            if (entityTypeNames.Count <= 1)
            {
                report.DefaultFilters = report.DefaultFilters ?? new List<DefaultReportFilter>();
                report.DefaultFilters.Add(new DefaultReportFilter
                {
                    MeasureName = createdMetric.Name,
                    Filters = new List<DefaultReportFilterInstance>()
                });
                _savedReportService.UpdateReportModified(report);
                _savedReportRepository.Update(report);
            }
        }

        private void AppendWavesToReport(SavedReport report, MetricConfiguration createdMetric, string selectedPart)
        {
            var waveConfig = new ReportWaveConfiguration
            {
                WavesToShow = ReportWavesOptions.MostRecentNWaves,
                NumberOfRecentWaves = 3,
                Waves = new CrossMeasure
                {
                    MeasureName = createdMetric.Name
                }
            };
            if (selectedPart != null)
            {
                var pane = GetPaneForReport(report);
                var part = GetReportPart(pane, selectedPart);
                if (pane != null && part != null)
                {
                    part.Waves = waveConfig;
                    part.PartType = PartType.ReportsCardLine;
                    _partsRepository.UpdatePart(part);
                }
            }
            else
            {
                report.Waves = waveConfig;
                _savedReportService.UpdateReportModified(report);
                _savedReportRepository.Update(report);
            }
        }

        private void AppendBaseToReport(SavedReport report, VariableConfiguration baseVariable, string selectedPart)
        {
            if (selectedPart != null)
            {
                var pane = GetPaneForReport(report);
                var part = GetReportPart(pane, selectedPart);
                if (pane != null && part != null)
                {
                    part.BaseExpressionOverride = new BaseExpressionDefinition
                    {
                        BaseVariableId = baseVariable.Id,
                    };
                    _partsRepository.UpdatePart(part);
                }
            }
            else
            {
                report.BaseVariableId = baseVariable.Id;
                _savedReportService.UpdateReportModified(report);
                _savedReportRepository.Update(report);
            }
        }

        public MultipleEntitySplitByAndFilterBy GetSplitByAndFilterBy(IReadOnlyCollection<string> entityTypeNames, string splitByEntityTypeName = null)
        {
            var splitByAndFilterBy = new MultipleEntitySplitByAndFilterBy()
            {
                SplitByEntityType = "",
                FilterByEntityTypes = Array.Empty<EntityTypeAndInstance>()
            };

            if (entityTypeNames.Any())
            {
                var splitByType = entityTypeNames.SingleOrDefault(type => type.Equals(splitByEntityTypeName, StringComparison.OrdinalIgnoreCase)) ?? entityTypeNames.First();
                splitByAndFilterBy.SplitByEntityType = splitByType;
                splitByAndFilterBy.FilterByEntityTypes = entityTypeNames.Where(type => !type.Equals(splitByEntityTypeName, StringComparison.OrdinalIgnoreCase))
                    .Select(entityType => new EntityTypeAndInstance
                    {
                        Type = entityType,
                        Instance = null
                    }).ToArray();
            }
            return splitByAndFilterBy;
        }

        public void UpdateVariableGroupValuesForMetric(MetricConfiguration metricConfig, GroupedVariableDefinition groupedDefinition)
        {
            var values = string.Join(">", groupedDefinition.Groups.Min(g => g.ToEntityInstanceId), groupedDefinition.Groups.Max(g => g.ToEntityInstanceId));
            var filterValues = string.Join("|", groupedDefinition.Groups.Select(g => $"{g.ToEntityInstanceId}:{g.ToEntityInstanceName}"));
            metricConfig.BaseVals = values;
            metricConfig.TrueVals = values;
            metricConfig.FilterValueMapping = filterValues;
        }

        private MetricConfiguration CreateNewMetricForVariable(VariableConfiguration variable, CalculationType calculationType)
        {
            var newMetricForVariable = _metricConfigurationFactory.CreateNewMetricForVariable(variable, calculationType, _productContext);

            _metricConfigurationRepository.Create(newMetricForVariable, false);
            return newMetricForVariable;
        }

        public void UpdateVariable(int variableId,
            string newVariableName,
            VariableDefinition newVariableDefinition,
            CalculationType? calculationType)
        {
            //not modifying variable identifier as it could be referenced inside other variables
            //not modifying metric name or entityType name as it could be referenced by name in reports
            var existingVariable = _variableConfigurationRepository.Get(variableId);

            if (existingVariable.IsBaseVariable())
            {
                if (!newVariableDefinition.IsBaseVariable())
                {
                    throw new BadRequestException("Base variable cannot be updated to a non-base variable");
                }
            }
            else if (newVariableDefinition.IsBaseVariable())
            {
                throw new BadRequestException("Variable cannot be updated to a base variable");
            }

            var originalName = existingVariable.DisplayName;

            var newVariable = existingVariable with
            {
                DisplayName = newVariableName,
                Definition = _variableFactory.SanitizeVariableEntityTypeName(existingVariable, newVariableDefinition)
            };

            _variableValidator.Validate(newVariable, out var _, out var entityTypeNames);
            if (newVariable.Definition is BaseFieldExpressionVariableDefinition baseExpressionDefinition)
            {
                baseExpressionDefinition.ResultEntityTypeNames = entityTypeNames;
            }

            _variableConfigurationRepository.Update(newVariable);

            var metricsForVariable = _metricConfigurationRepository.GetAll()
                .Where(m => m.VariableConfigurationId == variableId)
                .ToArray();
            var metric = metricsForVariable.Length == 1 ? metricsForVariable.Single() :
                metricsForVariable.SingleOrDefault(m => m.Name == newVariable.Identifier);

            if (metric != null)
            {
                _metricConfigurationFactory.UpdateMetricForVariable(metric, newVariable, originalName, newVariableName, calculationType);
                _metricConfigurationRepository.Update(metric);

                if (newVariable.Definition is GroupedVariableDefinition definition)
                {
                    var reports = _savedReportRepository.GetAll();
                    foreach (var report in reports)
                    {
                        var defaultFiltersForMetric = report.DefaultFilters?.SingleOrDefault(f => f.MeasureName == metric.Name);
                        if (defaultFiltersForMetric != null)
                        {
                            foreach (var filter in defaultFiltersForMetric.Filters)
                            {
                                filter.Values = filter.Values.Where(v =>
                                    definition.Groups.Any(g => g.ToEntityInstanceId == v)).ToArray();
                            }

                            defaultFiltersForMetric.Filters = defaultFiltersForMetric.Filters.Where(filter => filter.Values.Length > 0).ToList();
                            _savedReportService.UpdateReportModified(report);
                            _savedReportRepository.Update(report);
                        }
                    }
                }
            }
        }

        public void DeleteVariableById(int variableConfigId)
        {
            var metricConfigsThatUseVariableId = _metricConfigurationRepository.GetAll()
                .Where(m => m.VariableConfigurationId == variableConfigId).ToList();

            var metricNames = metricConfigsThatUseVariableId.Select(m => m.Name).ToHashSet();
            CheckForVariableReferenceInBreaks(metricNames);
            CheckForVariableReferenceInReports(metricNames);
            CheckForVariableReferenceInWeightings(variableConfigId);

            var reports = _savedReportRepository.GetAll();
            CheckForVariableReferenceInFilters(metricConfigsThatUseVariableId, reports);
            CheckForVariableReferenceInWaves(metricConfigsThatUseVariableId, reports);

            foreach (var metric in metricConfigsThatUseVariableId)
            {
                _metricConfigurationRepository.Delete(metric.Id);
            }

            DeleteVariable(variableConfigId);
        }

        private void CheckForVariableReferenceInWaves(List<MetricConfiguration> metricConfigsThatUseVariableId, IEnumerable<SavedReport> reports)
        {
            var reportsWithFiltersThatUseVariable = reports.Where(r => r.DefaultFilters.Any(d => metricConfigsThatUseVariableId.Any(m => m.Name == d.MeasureName)));
            if(reportsWithFiltersThatUseVariable.Any())
            {
                throw new BadRequestException($"Variable is being used as a wave inside report {reportsWithFiltersThatUseVariable.First().ReportPage.DisplayName} and cannot be deleted");
            }
        }

        private void CheckForVariableReferenceInFilters(List<MetricConfiguration> metricConfigsThatUseVariableId, IEnumerable<SavedReport> reports)
        {
            var reportsUsingVariable = reports.Where(r => r.DefaultFilters.Any(d => metricConfigsThatUseVariableId.Any(m => m.Name == d.MeasureName)));
            if (reportsUsingVariable.Any())
            {
                throw new BadRequestException($"Variable is being used as a filter inside report {reportsUsingVariable.First().ReportPage.DisplayName} and cannot be deleted");
            }
        }

        private void CheckForVariableReferenceInWeightings(int variableConfigId)
        {
            var variable = _variableConfigurationRepository.Get(variableConfigId);
            var weightings = _weightingPlanRepository.GetWeightingPlans(_productContext.ShortCode, _productContext.SubProductId);
            var weightingsContainingVariable = weightings.Where(w => w.VariableIdentifier == variable.Identifier);
            var subsetsUsingWeighting = weightingsContainingVariable.Select(w => w.SubsetId);

            if (weightingsContainingVariable.Any())
            {
                throw new BadRequestException($"Variable can't be deleted, it is used for weighting in the following subsets: {string.Join(',', subsetsUsingWeighting)}");
            }
        }

        public void DeleteBaseVariableById(int variableConfigId)
        {
            CheckForBaseVariableReferenceInMetrics(variableConfigId);
            CheckForBaseVariableReferenceInReports(variableConfigId);
            DeleteVariable(variableConfigId);
        }

        private void DeleteVariable(int variableConfigId)
        {
            var variableToDelete = _variableConfigurationRepository.Get(variableConfigId);
            _variableConfigurationRepository.Delete(variableToDelete);
        }

        private void CheckForBaseVariableReferenceInReports(int variableConfigId)
        {
            var reportsUsingBaseVariable = _savedReportRepository.GetAll().Where(r => r.BaseVariableId == variableConfigId).ToArray();
            if (reportsUsingBaseVariable.Any())
            {
                var reportNames = string.Join(", ", reportsUsingBaseVariable.Select(r => r.ReportPage.DisplayName));
                var pluralized = reportsUsingBaseVariable.Length > 1 ? "reports" : "report";
                throw new BadRequestException($"Base can't be deleted, it is being used by {pluralized}: {reportNames}");
            }
            var parts = _partsRepository.GetParts().Where(p => p.BaseExpressionOverride != null);
            foreach (var part in parts)
            {
                var baseOverride = part.BaseExpressionOverride;
                if (baseOverride.BaseVariableId == variableConfigId)
                {
                    var pane = _panesRepository.GetPanes().Where(p => p.Id == part.PaneId).Single();
                    var report = _pagesRepository.GetPages().Where(p => p.Name == pane.PageName).Single();
                    var partType = part.PartType == PartType.ReportsTable ? "table" : "chart";
                    throw new BadRequestException($"Base can't be deleted, it is being used by {partType}: {part.Spec1} in report: {report.DisplayName}");
                }
            }
        }

        private void CheckForBaseVariableReferenceInMetrics(int variableConfigId)
        {
            var metricConfiguration = _metricConfigurationRepository.GetAll()
                .FirstOrDefault(m => m.BaseVariableConfigurationId == variableConfigId);
            if (metricConfiguration != null)
            {
                throw new BadRequestException($"Base can't be deleted, it is being used by metric: {metricConfiguration.VarCode}");
            }
        }

        private void CheckForVariableReferenceInReports(HashSet<string> metricNames)
        {
            var parts = _partsRepository.GetParts();
            var partsUsingVariable = parts.Where(p => p.Spec1 != null && metricNames.Any(name => p.Spec1.Equals(name)));

            if (partsUsingVariable.Count() != 0)
            {
                var offensivePart = partsUsingVariable.First();
                var offensivePane = _panesRepository.GetPanes().Where(p => p.Id == offensivePart.PaneId).First();
                var offensivePage = _pagesRepository.GetPages().Where(p => p.Name == offensivePane.PageName).First();
                throw new BadRequestException($"Variable is being referenced inside report {offensivePage.DisplayName} and cannot be deleted");
            }
        }

        private void CheckForVariableReferenceInBreaks(HashSet<string> metricNames)
        {
            var parts = _partsRepository.GetParts().Where(p => RecursiveCheckBreak(metricNames, p.Breaks)).Select(p => $"(id: {p.Id}, report id: {p.PaneId}, metric: {p.DefaultSplitBy})").ToList();
            string partsWarning = parts.Any() ? $"report parts: ({string.Join(", ", parts)})" : "";
            
            var reports = _savedReportRepository.GetAll().Where(r => RecursiveCheckBreak(metricNames, r.Breaks.ToArray())).Select(r => r.ReportPage.DisplayName).ToList();
            string reportsWarning = reports.Any() ? $"reports: ({string.Join(", ", reports)})" : "";
            
            var breaks = _savedBreaksRepository.GetAllForSubProduct().Where(b => metricNames.Contains(b.Name) || RecursiveCheckBreak(metricNames, b.Breaks.ToArray())).Select(b => b.Name).ToList();
            string breaksWarnings = breaks.Any() ? $"breaks: ({string.Join(", ", breaks)})" : "";
            
            if(!partsWarning.IsNullOrWhiteSpace() || !reportsWarning.IsNullOrWhiteSpace() || !breaksWarnings.IsNullOrWhiteSpace())
            {
                throw new BadRequestException($"Variable is being referenced inside {string.Join(", ", new List<string>{partsWarning, reportsWarning, breaksWarnings})} and cannot be deleted");
            }
        }

        public VariableWarningModel[] CheckVariableIsInUse(int variableId)
        {
            var variable = _variableConfigurationRepository.Get(variableId);
            var variableWarnings = new List<VariableWarningModel>();

            var metricsReferencingVariable = _metricConfigurationRepository.GetAll().Where(m => m.VariableConfigurationId == variable.Id)
                .Select(m => m.Name).ToList();
            var allParts = _partsRepository.GetParts();
            CheckUsedInChartOrTable(variableWarnings, metricsReferencingVariable, allParts);
            CheckUsedAsBaseVariable(variable, variableWarnings, allParts);
            CheckUsedInBreaks(variableWarnings, metricsReferencingVariable, allParts);
            CheckIfVariableIsUsedByAnotherVariable(variable, variableWarnings);
            CheckIfVariableIsUsedInWeighting(variable, variableWarnings);
            CheckIfVariableUsedInFilters(variableWarnings, metricsReferencingVariable);
            CheckIfVariableIsUsedInWave(variable, variableWarnings);
            return variableWarnings.ToArray();
        }

        private void CheckIfVariableIsUsedInWave(VariableConfiguration variable, List<VariableWarningModel> variableWarnings)
        {
            var metricsReferencingVariable = _metricConfigurationRepository.GetAll().Where(m => m.VariableConfigurationId == variable.Id)
                .Select(m => m.Name).ToList();
            var reportsWithWaves = _savedReportRepository.GetAll().Where(r => r.Waves != null);
            var reportsContainingVariableAsWave = reportsWithWaves.Where(r => metricsReferencingVariable.Contains(r.Waves.Waves.MeasureName))
                .Select(r => r.ReportPage.DisplayName).ToList();
            if(reportsContainingVariableAsWave.Any())
            {
                var warning = new VariableWarningModel()
                {
                    Names = reportsContainingVariableAsWave,
                    ObjectThatReferencesVariable = ObjectThatReferencesVariable.Wave
                };
                variableWarnings.Add(warning);
            }
        }

        private void CheckIfVariableUsedInFilters(List<VariableWarningModel> variableWarnings, List<string> metricConfigsThatUseVariableId)
        {
            var reports = _savedReportRepository.GetAll();
            var reportsWithFiltersThatUseVariable = reports.Where(r => r.DefaultFilters.Any(d => metricConfigsThatUseVariableId.Any(m => m == d.MeasureName)))
                .Select(r => r.ReportPage.DisplayName).ToList();

            if (reportsWithFiltersThatUseVariable.Any())
            {
                var warning = new VariableWarningModel()
                {
                    Names = reportsWithFiltersThatUseVariable,
                    ObjectThatReferencesVariable = ObjectThatReferencesVariable.Filter
                };

                variableWarnings.Add(warning);
            }
        }

        private void CheckIfVariableIsUsedInWeighting(VariableConfiguration variable, List<VariableWarningModel> variableWarnings)
        {
            var weightings = _weightingPlanRepository.GetWeightingPlans(_productContext.ShortCode,_productContext.SubProductId);
            var weightingsContainingVariable = weightings.Where(w => w.VariableIdentifier == variable.Identifier)
                .Select(w => w.SubsetId).ToList();

            if(weightingsContainingVariable.Any())
            {
                var warningModel = new VariableWarningModel
                {
                    ObjectThatReferencesVariable = ObjectThatReferencesVariable.Weighting,
                    Names = weightingsContainingVariable
                };
                variableWarnings.Add(warningModel);
            }
        }

        private bool RecursiveCheckBreak(ICollection<string> metricReferencingVariable, CrossMeasure[] breaks)
        {
            if (breaks == null)
            {
                return false;
            }

            foreach (var crossMeasure in breaks)
            {
                if (metricReferencingVariable.Contains(crossMeasure.MeasureName))
                {
                    return true;
                }

                if (RecursiveCheckBreak(metricReferencingVariable, crossMeasure.ChildMeasures))
                {
                    return true;
                }
            }

            return false;
        }

        private void CheckUsedInBreaks(List<VariableWarningModel> variableWarnings,
            List<string> metricsReferencingVariable,
            IReadOnlyCollection<PartDescriptor> allParts)
        {
            var partsUsingAsBreak = allParts.Where(p => RecursiveCheckBreak(metricsReferencingVariable, p.Breaks)).ToList();
            var reportsUsingBreaks = _savedReportRepository.GetAll().Where(r => r.Breaks != null && 
                RecursiveCheckBreak(metricsReferencingVariable, r.Breaks.ToArray())).ToArray();
            if (partsUsingAsBreak.Any() || reportsUsingBreaks.Any())
            {
                AddWarning(variableWarnings, partsUsingAsBreak, ObjectThatReferencesVariable.Break, reportsUsingBreaks);
            }
        }

        private void CheckUsedAsBaseVariable(VariableConfiguration variable, List<VariableWarningModel> variableWarnings, IReadOnlyCollection<PartDescriptor> allParts)
        {
            var partsUsingBaseVariable = allParts.Where(p => p.BaseExpressionOverride != null && p.BaseExpressionOverride.BaseVariableId == variable.Id).ToList();
            var reportsUsingBaseVariable = _savedReportRepository.GetAll().Where(r => r.BaseVariableId == variable.Id).ToArray();
            if (partsUsingBaseVariable.Any() || reportsUsingBaseVariable.Any())
            {
                AddWarning(variableWarnings, partsUsingBaseVariable, ObjectThatReferencesVariable.Base, reportsUsingBaseVariable);
            }
        }

        private void CheckUsedInChartOrTable(List<VariableWarningModel> variableWarnings,
            List<string> metricsReferencingVariable,
            IReadOnlyCollection<PartDescriptor> allParts)
        {
            var partsDisplayingVariable = allParts.Where(p => metricsReferencingVariable.Contains(p.Spec1)).ToList();
            if (partsDisplayingVariable.Any())
            {
                AddWarning(variableWarnings, partsDisplayingVariable, ObjectThatReferencesVariable.Report);
            }
        }

        private void AddWarning(
            List<VariableWarningModel> variableWarnings,
            IEnumerable<PartDescriptor> partsUsingVariable,
            ObjectThatReferencesVariable objectThatReferencesVariable,
            SavedReport[] reportsUsingVariable = null)
        {
            var warningModel = new VariableWarningModel
            {
                ObjectThatReferencesVariable = objectThatReferencesVariable
            };

            var reportPageNames = reportsUsingVariable?.Select(r => r.ReportPage.DisplayName);
            warningModel.Names = reportPageNames != null
                ? reportPageNames.Concat(GetReportsFromParts(partsUsingVariable)).Distinct() 
                : GetReportsFromParts(partsUsingVariable.Distinct());
            variableWarnings.Add(warningModel);
        }

        private IEnumerable<string> GetReportsFromParts(IEnumerable<PartDescriptor> parts)
        {
            var paneIds = parts.Select(r => r.PaneId).Distinct().ToArray();
            var pageNames = _panesRepository.GetPanes().Where(p => paneIds.Contains(p.Id)).Select(p => p.PageName).ToArray();
            return _pagesRepository.GetPages().Where(p => pageNames.Contains(p.Name)).Select(p => p.DisplayName);
        }

        private void CheckIfVariableIsUsedByAnotherVariable(VariableConfiguration variable, List<VariableWarningModel> variableWarnings)
        {
            if (variable.VariablesDependingOnThis != null && variable.VariablesDependingOnThis.Count() > 0)
            {
                var warningModel = new VariableWarningModel
                {
                    ObjectThatReferencesVariable = ObjectThatReferencesVariable.Variable
                };
                var names = new List<string>();
                foreach (var dependent in variable.VariablesDependingOnThis)
                {
                    var dependentVariable = _variableConfigurationRepository.Get(dependent.VariableId);
                    names.Add(dependentVariable?.DisplayName?? $"Unknown Variable ID: {dependent.VariableId}");
                }
                warningModel.Names = names.Order().ToArray();
                variableWarnings.Add(warningModel);
            }
        }

        public IEnumerable<CreateVariableResultModel> CreateFlattenedVariables(VariableConfigurationCreateModel model)
        {
            if (model.Definition is not GroupedVariableDefinition groupedDefinition)
            {
                throw new BadRequestException("Only grouped variable definitions can be converted to single entity");
            }

            var results = new List<CreateVariableResultModel>();
            foreach (var group in groupedDefinition.Groups)
            {
                var createModel = new VariableConfigurationCreateModel
                {
                    Name = group.ToEntityInstanceName,
                    Definition = new SingleGroupVariableDefinition
                    {
                        Group = group
                    },
                    CalculationType = CalculationType.YesNo
                };
                try
                {
                    var result = ConstructVariableAndRelatedMetadata(createModel);
                    if (result != null)
                    {
                        results.Add(result);
                    }
                }
                catch
                {
                    // Continue on error, skip failed variable creation
                    _logger.LogWarning($"Failed to create variable for {group.ToEntityInstanceName}");
                }
            }
            return results;
        }
    }
}
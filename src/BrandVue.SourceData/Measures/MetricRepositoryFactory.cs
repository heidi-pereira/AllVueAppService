using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Utils;
using BrandVue.SourceData.Variable;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using Vue.Common.Auth;

namespace BrandVue.SourceData.Measures
{
    internal class MetricRepositoryFactory
    {
        private readonly IBrandVueDataLoaderSettings _settings;
        private readonly IInstanceSettings _instanceSettings;
        private readonly IDbContextFactory<MetaDataContext> _metaDataContextFactory;
        private readonly IAnswerDbContextFactory _answersDbContext;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISubsetRepository _subsetRepository;
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly IMetricFactory _metricFactory;
        private readonly MetricConfigurationRepositorySql _metricConfigurationRepository;
        private readonly IProductContext _productContext;
        private readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly ILogger<MetricRepositoryFactory> _logger;
        private readonly IAllVueConfigurationRepository _allVueConfigurationRepository;
        private readonly IMetricConfigurationFactory _metricConfigurationFactory;
        private readonly Dictionary<int, bool> _questionIdHasAnswersLookup;
        private readonly IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;

        internal MetricRepositoryFactory(IBrandVueDataLoaderSettings settings,
            IInstanceSettings instanceSettings,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IAnswerDbContextFactory answersDbContextFactory,
            ILoggerFactory loggerFactory,
            ISubsetRepository subsetRepository,
            IResponseFieldManager responseFieldManager,
            IMetricFactory metricFactory,
            MetricConfigurationRepositorySql metricConfigurationRepository,
            IProductContext productContext,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            IVariableConfigurationRepository variableConfigurationFactory,
            IAllVueConfigurationRepository allVueConfigurationRepository,
            IMetricConfigurationFactory metricConfigurationFactory,
            Dictionary<int, bool> questionIdHasAnswersLookup)
        {
            _settings = settings;
            _instanceSettings = instanceSettings;
            _metaDataContextFactory = metaDataContextFactory;
            _answersDbContext = answersDbContextFactory;
            _loggerFactory = loggerFactory;
            _subsetRepository = subsetRepository;
            _responseFieldManager = responseFieldManager;
            _metricFactory = metricFactory;
            _metricConfigurationRepository = metricConfigurationRepository;
            _productContext = productContext;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
            _variableConfigurationRepository = variableConfigurationFactory;
            _logger = _loggerFactory.CreateLogger<MetricRepositoryFactory>();
            _allVueConfigurationRepository = allVueConfigurationRepository;
            _metricConfigurationFactory = metricConfigurationFactory;
            _questionIdHasAnswersLookup = questionIdHasAnswersLookup;
            _userDataPermissionsOrchestrator = userDataPermissionsOrchestrator;
        }

        public MetricRepository CreateAndPopulateMeasureRepository()
        {
            bool entirelyMapBasedConfiguration = !_settings.LoadConfigFromSql && !_instanceSettings.GenerateFromAnswersTable;
            if (entirelyMapBasedConfiguration)
            {
                return LoadFromMetricsTabOfMapFileIfMetricsTabExists(_metricFactory);
            }

            if (_instanceSettings.GenerateFromAnswersTable)
            {
                CreateMetricsForUnusedVariables();
                ConfirmStatusOfMetricsWithoutData();

                var allVueConfiguration = _allVueConfigurationRepository.GetOrCreateConfiguration();
                if (allVueConfiguration.CheckOrphanedMetricsForCanonicalVariables)
                {
                    EnsureQuestionVariablesHaveAssociatedMetric();
                    allVueConfiguration.CheckOrphanedMetricsForCanonicalVariables = false;
                    _allVueConfigurationRepository.UpdateConfiguration(allVueConfiguration);
                }
            }

            var measureRepositorySqlLoader = new MetricRepositorySqlLoader(_metaDataContextFactory, _productContext, _metricFactory, _userDataPermissionsOrchestrator, _loggerFactory);
            return measureRepositorySqlLoader.CreateMeasureRepository();
        }

        private void ConfirmStatusOfMetricsWithoutData()
        {
            if(!_productContext.IsAllVue)
            {
                return;
            }
            
            using var context = _metaDataContextFactory.CreateDbContext();
            var metricsWithoutData = context.MetricConfigurations
                .Where(mc => mc.ProductShortCode == _productContext.ShortCode && mc.SubProductId == _productContext.SubProductId)
                .Where(m => !m.HasData)
                .ToArray();

            if (metricsWithoutData?.Count() == 0)
            {
                return;
            }

            var allSubsets = _subsetRepository.ToArray();
            var allFields = _responseFieldManager.GetAllFields();

            foreach (var metric in metricsWithoutData)
            {
                var field = GetFieldFromMetric(allFields, metric);

                if (field != null)
                {
                    foreach (var subset in allSubsets)
                    {
                        var question = field.GetDataAccessModelOrNull(subset.Id);
                        if (question == null || question.QuestionModel == null)
                        {
                            break;
                        }

                        if (_questionIdHasAnswersLookup.TryGetValue(question.QuestionModel.QuestionId, out bool hasAnswers) && hasAnswers)
                        {
                            metric.HasData = true;
                            metric.EligibleForCrosstabOrAllVue = true;
                            _metricConfigurationRepository.Update(metric, false);
                            break;
                        }
                    }
                }
            }
        }

        private ResponseFieldDescriptor GetFieldFromMetric(ICollection<ResponseFieldDescriptor> allFields, MetricConfiguration metric)
        {
            ResponseFieldDescriptor field = null;

            if (metric.VariableConfigurationId != null
                && _variableConfigurationRepository.Get(metric.VariableConfigurationId.Value) is {} variable)
            {
                field = allFields.FirstOrDefault(f => f.Name == variable.Identifier);
            }
            else if (metric.Field != null)
            {
                field = allFields.FirstOrDefault(f => f.Name == metric.Field);
            }

            return field;
        }

        private void EnsureQuestionVariablesHaveAssociatedMetric()
        {
            var metrics = _metricConfigurationRepository.GetAll();
            var questionVariables = _variableConfigurationRepository.GetAll()
                .Where(v => v.Definition is QuestionVariableDefinition);

            var variableConfigurationIdsWithCanonicalMetric = metrics.Where(m => m.VariableConfigurationId != null).Select(m => m.VariableConfigurationId).ToHashSet();
            var metricsWithNoVariable = metrics.Where(m => m.VariableConfigurationId is null).ToArray();
            var metricsByVarCode = metricsWithNoVariable.Where(m => m.IsAutoGenerated == AutoGenerationType.CreatedFromField).ToLookup(m => m.VarCode);
            var metricsByField = metricsWithNoVariable.Where(m => m.IsAutoGenerated == AutoGenerationType.CreatedFromField).ToLookup(m => m.Field);

            var metricsByVarCodeForAutogeneratedOriginal = metricsWithNoVariable.Where(m => m.IsAutoGenerated == AutoGenerationType.Original).ToLookup(m => m.VarCode);
            var metricsByFieldForAutogeneratedOriginal = metricsWithNoVariable.Where(m => m.IsAutoGenerated == AutoGenerationType.Original).ToLookup(m => m.Field);
            _logger.LogInformation("{product} Converting metrics", _productContext);
            foreach (var variable in questionVariables)
            {
                if(variableConfigurationIdsWithCanonicalMetric.Contains(variable.Id))
                {
                   //if there is already an identified canonical variable, we don't need to do anything
                   continue;
                }

                var definition = variable.Definition as QuestionVariableDefinition;
                var metricsWithMatchingVarCode = metricsByVarCode[definition.QuestionVarCode].ToArray();
                if(metricsWithMatchingVarCode.Count() == 1)
                {
                    UpdateMetricWithVariableId(variable, metricsWithMatchingVarCode.Single());
                    continue;
                }

                var metricsWithMatchingField = metricsByField[variable.Identifier];
                if(metricsWithMatchingField.Count() == 1)
                {
                    UpdateMetricWithVariableId(variable, metricsWithMatchingField.Single());
                    continue;
                }

                if (!_productContext.IsAllVue)
                {
                    var metricsWithMatchingVarCodeOriginal = metricsByVarCodeForAutogeneratedOriginal[definition.QuestionVarCode].OrderBy(m => m.Id);
                    if (metricsWithMatchingVarCodeOriginal.Any())
                    {
                        UpdateMetricWithVariableId(variable, metricsWithMatchingVarCodeOriginal.First());
                        continue;
                    }

                    var metricsWithMatchingFieldOriginal = metricsByFieldForAutogeneratedOriginal[variable.Identifier].OrderBy(m => m.Id); ;
                    if (metricsWithMatchingFieldOriginal.Any())
                    {
                        UpdateMetricWithVariableId(variable, metricsWithMatchingFieldOriginal.First());
                        continue;
                    }
                }
                _logger.LogError("{product} Variable {variable} has been orphaned", _productContext.ShortCodeAndSubproduct(), variable.DisplayName);
            }

            if (_productContext.IsAllVue || _productContext.NonMapFileSurveyIds.Any())
            {
                var metricsWithoutVariables = _metricConfigurationRepository.GetAll()
                    .Where(x => !x.VariableConfigurationId.HasValue).ToArray();
                _logger.LogInformation("{product} Checking for metrics without variables {count}", _productContext, metricsWithoutVariables.Length);
                if (metricsWithoutVariables.Any())
                {
                    var metricNames = metricsWithoutVariables.Select(x => x.DisplayName).ToArray();
                    _logger.LogError("{product} Could not find variable for metrics {metrics}",
                        _productContext.ShortCodeAndSubproduct(), string.Join(",", metricNames));
                }
            }
            _logger.LogInformation("{product} Completed metric conversion", _productContext);
        }

        private void UpdateMetricWithVariableId(VariableConfiguration variable, MetricConfiguration metric)
        {
            metric.IsAutoGenerated = AutoGenerationType.CreatedFromField;
            metric.VariableConfigurationId = variable.Id;
            _metricConfigurationRepository.Update(metric, false);
        }

        private void CreateMetricsForUnusedVariables()
        {
            try
            {
                CreateInner();
            }
            catch (InvalidOperationException)
            {
                // The property 'MetricConfiguration.Id' has a temporary value while attempting to change the entity's state to 'Unchanged'.Either set a permanent value explicitly, or ensure that the database is configured to generate values for this property.
                // Not sure why this happens sometimes. Especially immediately after restoring a backup. Retry seems to fix it
                CreateInner();
            }

            void CreateInner()
            {
                var variables = _variableConfigurationRepository.GetAll();
                var usedIdentifiers = _metricConfigurationRepository.GetDirectlyUsedVariableIdentifiers(variables);
                var metrics = _metricConfigurationRepository.GetAll();
                var usedMeasureNames = metrics.Select(mc => mc.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var generatedMetricConfigs =
                    GenerateMetricConfigsForUnusedFields(usedIdentifiers, usedMeasureNames).ToArray();
                _metricConfigurationRepository.CreateMany(generatedMetricConfigs);

                metrics = metrics.Concat(generatedMetricConfigs).ToArray();
                usedMeasureNames.AddRange(generatedMetricConfigs.Select(m => m.Name));

                var newMetricsForVariables = GenerateMissingCanonicalMetricConfigs(metrics, usedMeasureNames, variables);
                _metricConfigurationRepository.CreateMany(newMetricsForVariables);
            }
        }

        private MetricConfiguration[] GenerateMissingCanonicalMetricConfigs(
            IReadOnlyCollection<MetricConfiguration> metrics, HashSet<string> usedMeasureNames,
            IReadOnlyCollection<VariableConfiguration> variables)
        {
            var directlyUsedVariableIds = metrics.Select(mc => mc.VariableConfigurationId)
                .ToHashSet();
            var primaryFields = metrics.Select(m => m.Field).Where(f => f is not null)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var variablesWithoutCanonicalMetric = variables
                .Where(v => !v.IsBaseVariable()
                            && !directlyUsedVariableIds.Contains(v.Id)
                            && !primaryFields.Contains(v.Identifier)
                );
            var newMetricsForVariables = variablesWithoutCanonicalMetric.Select(v =>
            {
                var newMetricForVariable = _metricConfigurationFactory.CreateNewMetricForVariable(v, CalculationType.YesNo,
                    _productContext);
                newMetricForVariable.DisableMeasure = true; // Visible only to admins in allvue
                return newMetricForVariable;
            }).Where(m => usedMeasureNames.Add(m.Name)).ToArray();
            return newMetricsForVariables;
        }

        private MetricRepository LoadFromMetricsTabOfMapFileIfMetricsTabExists(IMetricFactory metricFactory)
        {
            var measureRepository = new MetricRepository(_userDataPermissionsOrchestrator);

            // Load measures from the map file
            var measureLoader = new MapFileMetricLoader(measureRepository, _commonMetadataFieldApplicator, _loggerFactory.CreateLogger<MapFileMetricLoader>(), metricFactory, _productContext);
            measureLoader.LoadIfExists(_settings.MeasureMetadataFilepath);

            return measureRepository;
        }

        public IEnumerable<MetricConfiguration> GenerateMetricConfigsForUnusedFields(HashSet<string> usedFieldNames,
            HashSet<string> usedMeasureNames)
        {
            usedFieldNames = usedFieldNames.AsHashSet(StringComparer.OrdinalIgnoreCase);
            var allSubsets = _subsetRepository.ToArray();
            var allFields = _responseFieldManager.GetAllFields();
            var unusedFields = usedFieldNames != null
                ? allFields.Where(f => !usedFieldNames.Contains(f.Name))
                : allFields;

            var fieldsToMap = unusedFields.SelectMany(f => AccessModelAndSubsets(f, allSubsets)).ToArray();
            var askedBaseFields = fieldsToMap.Where(f => f.AccessModel.FieldType == FieldType.Asked);

            var askedBaseFieldsGroupedByVarCode = askedBaseFields.GroupBy(x => x.AccessModel.QuestionModel.VarCode).ToList();
            var askedBaseFieldsWithSingleDefinitionAcrossSubsets = askedBaseFieldsGroupedByVarCode.Where(x => x.Count() == 1);
            var askedBaseFieldsWithMultipleDefinitionsAcrossSubsets = askedBaseFieldsGroupedByVarCode.Where(x => x.Count() > 1).ToList();

            if (askedBaseFieldsWithMultipleDefinitionsAcrossSubsets.Any())
            {
                _logger.LogWarning(
                    $"Survey {_productContext.SubProductId ?? _productContext.ShortCode} : Ignoring VarCodes {string.Join(", ", askedBaseFieldsWithMultipleDefinitionsAcrossSubsets.Select(x => x.Key))} which have multiple field definitions across subsets");
            }

            var askedBaseFieldsByVarCode = askedBaseFieldsWithSingleDefinitionAcrossSubsets.ToDictionary(f => f.Single().AccessModel.QuestionModel.VarCode,
                f => f.Single().Field);

            var measureNameGenerator = new NameGenerator(fieldsToMap.Select(f => f.AccessModel.QuestionModel));

            var variables = _variableConfigurationRepository.GetAll();
            var fieldVariableMappings = fieldsToMap
                .Join(variables,
                    field => field.Field.Name,
                    variable => variable.Identifier,
                    (field, variable) => (field.Field, field.AccessModel, field.Subsets, Variable: variable ));

            foreach (var (field, accessModel, subsets, variable) in fieldVariableMappings.Where(f => f.AccessModel.FieldType == FieldType.Standard))
            {
                var questionModel = accessModel.QuestionModel;
                bool isCheckbox = questionModel.MasterType == "CHECKBOX"; //This should ideally have a special choice set of unchecked and checked. Relying on this string is bad.
                var measureName = measureNameGenerator.GenerateMeasureName(questionModel, usedMeasureNames);
                var hasAnswers = true;
                if(_productContext.IsAllVue)
                {
                    _questionIdHasAnswersLookup.TryGetValue(questionModel.QuestionId, out hasAnswers);
                }

                yield return ConvertToConfiguration(
                    measureName,
                    field,
                    questionModel,
                    isCheckbox,
                    askedBaseFieldsByVarCode,
                    accessModel.QuestionModel.VarCode,
                    subsets,
                    variable.Id,
                    hasAnswers,
                    accessModel.ScaleFactor);
            }
        }

        private MetricConfiguration ConvertToConfiguration(string measureName,
            ResponseFieldDescriptor field,
            Question question,
            bool isCheckbox,
            IDictionary<string, ResponseFieldDescriptor> askedBaseFieldsByVarCode,
            string varCode,
            Subset[] subsets,
            int canonicalVariableId,
            bool hasData,
            double? scaleFactor)
        {
            var configuration = new MetricConfiguration
            {
                Name = measureName,
                Field = field.Name,
                BaseField = field.Name,
                DisableMeasure = false,
                EligibleForCrosstabOrAllVue = hasData,
                HelpText = question.QuestionText,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                VarCode = question.VarCode,
                DisplayName = question.VarCode,
                DisableFilter = !_productContext.IsAllVue,
                VariableConfigurationId = canonicalVariableId,
                Subset = subsets != null ? string.Join("|", subsets.Select(s => s.Id)) : null, // null means available for all subsets
                IsAutoGenerated = AutoGenerationType.CreatedFromField,
                HasData = hasData,
                ScaleFactor = scaleFactor
            };
            if (question.MasterType == "HEATMAPIMAGE")
            {
                configuration.CalcType = CalculationTypeParser.AsString(CalculationType.Text);
                configuration.TrueVals=
                configuration.BaseVals = string.Join(">", int.MinValue + 1, int.MaxValue);
                configuration.FilterValueMapping = "Range:Range";
                if (question.OptionalData is HeatMapData heapMapOptions)
                {
                    //
                    //Place holder as to what to do here.....
                    //
                    configuration.Max = heapMapOptions.MaxClicks;
                    configuration.Min = heapMapOptions.AddClickRadiusInPixels;
                }
            }
            else if (isCheckbox)
            {
                configuration.CalcType = CalculationTypeParser.AsString(CalculationType.YesNo);
                configuration.TrueVals = "1";
                configuration.BaseVals = "-99|1";
                configuration.FilterValueMapping = "-99:No|1:Yes";
                configuration.NumFormat = MetricNumberFormatter.PercentageInputNumberFormat;
            }
            else if (question.AnswerChoiceSet != null)
            {
                configuration.CalcType = CalculationTypeParser.AsString(CalculationType.YesNo);
                var values = string.Join(">", question.AnswerChoiceSet.Choices.Min(c => c.SurveyChoiceId), question.AnswerChoiceSet.Choices.Max(c => c.SurveyChoiceId));
                configuration.BaseVals = configuration.TrueVals = values;
                configuration.FilterValueMapping = string.Join("|", question.AnswerChoiceSet.Choices.Select(c => $"{c.SurveyChoiceId}:{c.GetDisplayName()}"));
                configuration.NumFormat = MetricNumberFormatter.PercentageInputNumberFormat;

                if (askedBaseFieldsByVarCode.TryGetValue(varCode, out var baseField))
                {
                    configuration.BaseField = baseField.Name;
                }
            }
            else if (question.MasterType == "TEXTENTRY" && string.IsNullOrEmpty(question.NumberFormat))
            {
                //Text fields used as base look at AnswerValue for determining if there is an answer.
                //It appears historically the value is always -99 but this will handle any value
                configuration.CalcType = CalculationTypeParser.AsString(CalculationType.Text);
                configuration.BaseVals = string.Join(">", int.MinValue + 1, int.MaxValue);
            }
            else
            {
                configuration.CalcType = CalculationTypeParser.AsString(CalculationType.Average);
                int min = int.MinValue + 1;
                int max = int.MaxValue;
                if (question.MinimumValue.HasValue && question.MaximumValue.HasValue && question.MaximumValue.Value > question.MinimumValue.Value)
                {
                    min = question.MinimumValue.Value;
                    max = question.MaximumValue.Value;
                }
                var values = string.Join(">", min, max);
                configuration.BaseVals = configuration.TrueVals = values;
                configuration.FilterValueMapping = "Range:Range";
                configuration.NumFormat = MetricNumberFormatter.DecimalInputNumberFormat;
            }

            if (field.EntityCombination.Count > 1 || _productContext.DisableAutoMetricFiltering)
            {
                //Filter dialog doesn't handle multi-entity filters yet or explicitly disabled in app settings
                configuration.DisableFilter = true;
                configuration.DefaultSplitByEntityType = DetermineDefaultSplitBy(question, field);
            }

            return configuration;
        }

        public string DetermineDefaultSplitBy(Question question, ResponseFieldDescriptor field)
        {
            //we should be able to work out the relevant choicesets from here
            string name = question switch
            {
                var q when question.SectionChoiceSet != null
                    => q.SectionChoiceSet.Name,
                var q when question.PageChoiceSet != null
                    => q.PageChoiceSet.Name,
                var q when question.QuestionChoiceSet != null && (question.MasterType == "DROPBOX" || question.MasterType == "DROPZONE" || question.MasterType == "RADIO")
                    => q.QuestionChoiceSet.Name,
                var q when question.AnswerChoiceSet != null && question.MasterType == "COMBO"
                    => q.AnswerChoiceSet.Name,
                _ => null
            };

            if (name != null)
            {
                var sanitized = name.Humanize().Dehumanize();
                var filterBy = field.EntityCombination.FirstOrDefault(e => e.Identifier == sanitized);
                if (filterBy != null)
                {
                    return field.EntityCombination.FirstOrDefault(e => e.Identifier != sanitized)?.Identifier;
                }
            }

            return null;
        }

        private IEnumerable<(ResponseFieldDescriptor Field, FieldDefinitionModel AccessModel, Subset[] Subsets)> AccessModelAndSubsets(ResponseFieldDescriptor f, Subset[] allSubsets)
        {
            var accessModelAndSubsets = allSubsets.Select(s => (Subset: s, AccessModel: f.GetDataAccessModelOrNull(s.Id))).Where(t => t.AccessModel?.QuestionModel != null).ToArray();
            var subsetsByAccessModel = accessModelAndSubsets.ToLookup(t => t.AccessModel.OrderedEntityColumns, SequenceComparer<EntityFieldDefinitionModel>.ForImmutableArray());
            if (subsetsByAccessModel.OnlyOrDefault() is {} grouping)
            {
                var subsets = grouping.Select(t => t.Subset).ToArray();
                if (subsets.Length == allSubsets.Length) subsets = null; // Set to null meaning all in this case
                yield return (f, grouping.First().AccessModel, subsets);
            }
            else
            {
                _logger.LogWarning($"Skipping metric generation for {f} since there are {subsetsByAccessModel.Count} unique entity column definitions");
            }
        }
    }
}

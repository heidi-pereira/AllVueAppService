using System.IO;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Settings;
using Microsoft.Extensions.Logging;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Variable;
using Microsoft.EntityFrameworkCore;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.AutoGeneration;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.CalculationLogging;
using Vue.Common.FeatureFlags;
using BrandVue.SourceData.Snowflake;
using Vue.Common.Auth;

namespace BrandVue.SourceData.Import
{
    public class BrandVueDataLoader : IBrandVueDataLoader
    {
        private readonly IBrandVueDataLoaderSettings _settings;
        private readonly ILazyDataLoaderFactory _lazyDataLoaderFactory;
        protected readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly AnswersTableMetadataLoader _answersTableMetadataLoader;
        private readonly IVariableConfigurationRepository _writeableVariableConfigurationRepository;
        private readonly IReadableVariableConfigurationRepository _readableVariableConfigurationRepository;
        protected readonly ICommonMetadataFieldApplicator _commonMetadataFieldApplicator;
        protected readonly IDbContextFactory<MetaDataContext> _metaDataContextFactory;
        protected readonly IAnswerDbContextFactory _answersContextFactory;
        private IEntitySetConfigurationRepository _entitySetConfigurationRepository;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly SubsetRepositoryLoader _subsetRepositoryLoader;
        private ResponseFieldManager _responseFieldManager;
        private VariableEntityLoader _variableEntityLoader;
        private EntitySetConfigurationLoader _entitySetLoader;
        private IInvalidatableLoaderCache _invalidatableLoaderCache;
        private readonly IAllVueConfigurationRepository _allVueConfigurationRepository;
        private readonly ICalculationLogger _calculationLogger;
        private readonly IFeatureToggleService _featureToggleService;
        private readonly ISnowflakeRepository _snowflakeRepository;
        private readonly ITextCountCalculatorFactory _textCountCalculatorFactory;
        private AllVueConfiguration _allVueConfiguration;
        private DataPresenceGuarantor _dataPresenceGuarantor;
        public IRespondentDataLoader RespondentDataLoader { get; private set; }
        protected IProductContext ProductContext { get; }
        private readonly IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator;

        public BrandVueDataLoader(ILoggerFactory loggerFactory,
            IBrandVueDataLoaderSettings settings,
            ILazyDataLoaderFactory lazyDataLoaderFactory,
            IResponseRepository textResponseRepository,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IAnswerDbContextFactory answersContextFactory,
            IProductContext productContext,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            IInstanceSettings instanceSettings,
            IChoiceSetReader choiceSetReader,
            IWeightingPlanRepository weightingPlanRepository,
            IResponseWeightingRepository responseWeightingRepository,
            IInvalidatableLoaderCache invalidatableLoaderCache,
            IAllVueConfigurationRepository allVueConfigurationRepository,
            ICalculationLogger calculationLogger,
            IFeatureToggleService featureToggleService,
            ISnowflakeRepository snowflakeRepository,
            ITextCountCalculatorFactory textCountCalculatorFactory)
            : this(loggerFactory, settings, lazyDataLoaderFactory,
                new VariableConfigurationRepository(metaDataContextFactory, productContext),
                textResponseRepository,
                metaDataContextFactory,
                answersContextFactory,
                productContext,
                userDataPermissionsOrchestrator,
                commonMetadataFieldApplicator,
                instanceSettings,
                choiceSetReader,
                new AverageConfigurationRepository(metaDataContextFactory, productContext),
                new EntitySetConfigurationRepositorySql(metaDataContextFactory, productContext),
                weightingPlanRepository,
                responseWeightingRepository,
                invalidatableLoaderCache,
                allVueConfigurationRepository,
                calculationLogger,
                featureToggleService,
                snowflakeRepository,
                textCountCalculatorFactory
                )
        {
        }

        internal BrandVueDataLoader(ILoggerFactory loggerFactory,
            IBrandVueDataLoaderSettings settings,
            ILazyDataLoaderFactory lazyDataLoaderFactory,
            IVariableConfigurationRepository variableConfigurationRepository,
            IResponseRepository textResponseRepository,
            IDbContextFactory<MetaDataContext> metaDataContextFactory,
            IAnswerDbContextFactory answersContextFactory,
            IProductContext productContext,
            IUserDataPermissionsOrchestrator userDataPermissionsOrchestrator,
            ICommonMetadataFieldApplicator commonMetadataFieldApplicator,
            IInstanceSettings instanceSettings,
            IChoiceSetReader choiceSetReader,
            IAverageConfigurationRepository averageConfigurationRepository,
            IEntitySetConfigurationRepository entitySetConfigurationRepository,
            IWeightingPlanRepository weightingPlanRepository,
            IResponseWeightingRepository responseWeightingRepository,
            IInvalidatableLoaderCache invalidatableLoaderCache,
            IAllVueConfigurationRepository allVueConfigurationRepository,
            ICalculationLogger calculationLogger,
            IFeatureToggleService featureToggleService,
            ISnowflakeRepository snowflakeRepository,
            ITextCountCalculatorFactory textCountCalculatorFactory)
        {
            _loggerFactory = loggerFactory;
            ProductContext = productContext;
            _userDataPermissionsOrchestrator = userDataPermissionsOrchestrator;
            _logger = loggerFactory.CreateLogger<BrandVueDataLoader>();
            _settings = settings;
            _commonMetadataFieldApplicator = commonMetadataFieldApplicator;
            _lazyDataLoaderFactory = lazyDataLoaderFactory;
            _writeableVariableConfigurationRepository = variableConfigurationRepository;
            _readableVariableConfigurationRepository = variableConfigurationRepository;
            TextResponseRepository = textResponseRepository;
            InstanceSettings = instanceSettings;
            _answersTableMetadataLoader = new AnswersTableMetadataLoader(loggerFactory.CreateLogger<BrandVueDataLoader>(), InstanceSettings, choiceSetReader, variableConfigurationRepository, productContext, settings);
            _metaDataContextFactory = metaDataContextFactory;
            _answersContextFactory = answersContextFactory;
            AverageConfigurationRepository = averageConfigurationRepository;
            _entitySetConfigurationRepository = entitySetConfigurationRepository;
            _weightingPlanRepository = weightingPlanRepository;
            _invalidatableLoaderCache = invalidatableLoaderCache;
            _subsetRepositoryLoader = new SubsetRepositoryLoader(_settings, _loggerFactory, choiceSetReader, _commonMetadataFieldApplicator, _metaDataContextFactory, ProductContext);
            ResponseWeightingRepository = responseWeightingRepository;
            _allVueConfigurationRepository = allVueConfigurationRepository;
            _calculationLogger = calculationLogger;
            _featureToggleService = featureToggleService;
            _snowflakeRepository = snowflakeRepository;
            _textCountCalculatorFactory = textCountCalculatorFactory;
        }

        public ILazyDataLoader LazyDataLoader { get; private set; }
        public IResponseFieldManager ResponseFieldManager => _responseFieldManager;
        public IQuotaCellDescriptionProvider QuotaCellDescriptionProvider { get; private set; }
        public IQuotaCellReferenceWeightingRepository QuotaCellReferenceWeightingRepository { get; private set; }
        public IMetricCalculationOrchestrator Calculator { get; private set; }
        public ISubsetRepository SubsetRepository { get; private set; }
        public IFilterRepository FilterRepository { get; private set; }
        public IAverageConfigurationRepository AverageConfigurationRepository { get; private set; }
        public IAverageDescriptorRepository AverageDescriptorRepository { get; private set; }
        public IMeasureRepository MeasureRepository { get; private set; }
        public ILoadableEntityInstanceRepository EntityInstanceRepository { get; private set; }
        public ILoadableEntitySetRepository EntitySetRepository { get; private set; }
        public ILoadableEntityTypeRepository EntityTypeRepository { get; private set; }
        public IInstanceSettings InstanceSettings { get; }
        public IRespondentRepositorySource RespondentRepositorySource { get; private set; }
        public IMetricFactory MetricFactory { get; private set; }
        public IMetricConfigurationRepository MetricConfigurationRepository { get; private set; }
        public IVariableConfigurationRepository VariableConfigurationRepository { get; private set; }
        public IFieldExpressionParser FieldExpressionParser { get; private set; }
        public IQuestionTypeLookupRepository QuestionTypeLookupRepository { get; private set; }
        public IResponseRepository TextResponseRepository { get; private set; }
        public IProfileResultsCalculator ProfileResultsCalculator { get; private set; }
        public ISampleSizeProvider SampleSizeProvider { get; private set; }
        internal IBaseExpressionGenerator BaseExpressionGenerator { get; private set; }
        public IProfileResponseAccessorFactory ProfileResponseAccessorFactory { get; private set; }
        public IResponseWeightingRepository ResponseWeightingRepository { get; private set; }
        public IEntitySetConfigurationRepository EntitySetConfigurationRepository => _entitySetConfigurationRepository;
        public IFeatureToggleService FeatureToggleService => _featureToggleService;
        public ITextCountCalculatorFactory TextCountCalculatorFactory => _textCountCalculatorFactory;
        public ISnowflakeRepository SnowflakeRepository => _snowflakeRepository;

        public const string All = "All";

        private record PlansAndRespondentLoader (List<WeightingPlan> PlansForSubset, IResponseLevelQuotaCellLoader Loader);

        private IReadOnlyCollection<WeightingPlanConfiguration> GetLoaderWeightingPlansForSubset(string product,
            string subProductIdOrNull, string subsetId)
        {
            var plans = _weightingPlanRepository.GetLoaderWeightingPlansForSubset(ProductContext.ShortCode,
                ProductContext.SubProductId, subsetId);
            if ((!plans.Any()) && 
                (ResponseWeightingRepository.AreThereAnyRootResponseWeights(subsetId)) )
            {
                var myPlans = new List<WeightingPlanConfiguration>
                {
                    new() { SubsetId = subsetId, ProductShortCode = product, SubProductId = subProductIdOrNull }
                };
                plans = myPlans;
            }
            return plans;
        }
        public void LoadBrandVueData()
        {
            IRespondentRepositoryFactory respondentRepositoryFactory;
            
            Dictionary<Subset,  PlansAndRespondentLoader> subsetWeightingPlansLookup = null;
            if (_settings.FeatureFlagBrandVueLoadWeightingFromDatabase || ProductContext.IsAllVue)
            {
                subsetWeightingPlansLookup = SubsetRepository
                    .Where(s => !s.Disabled)
                    .Select(subset => (subset, WeightingPlans:
                            GetLoaderWeightingPlansForSubset(ProductContext.ShortCode, ProductContext.SubProductId, subset.Id)
                        ))
                    .Where(s => s.WeightingPlans.Any())
                    .ToDictionary(s => s.subset, s =>
                    {
                        var loader = new ResponseLevelQuotaCellLoader(_loggerFactory.CreateLogger<ResponseLevelQuotaCellLoader>(),ResponseWeightingRepository, s.subset);
                        var plans = s.WeightingPlans.ToAppModel(loader).ToList();
                        var validator = new ReferenceWeightingValidator();
                        var hasRootResponseLevelWeighting = loader.GetPossibleRootResponseWeightingsForSubset() != null;
                        var isValid = validator.IsValid(hasRootResponseLevelWeighting, plans, MeasureRepository, out var messages);

                        if (!isValid)
                        {
                            foreach (var message in ReferenceWeightingValidator.ConvertMessages(messages))
                            {
                                _logger.LogError($"{ProductContext.ShortCode} {ProductContext.SubProductId}: Invalid Weighting plan {s.subset.DisplayName} {message.Path} {message.MessageText}");
                            }
                            plans = new List<WeightingPlan>();
                        }

                        return new PlansAndRespondentLoader(plans, loader);
                    }
                );
            }

            if (_allVueConfiguration.AllowLoadFromMapFile && File.Exists(_settings.ProfilingFieldsMetadataFilepath) && 
                (subsetWeightingPlansLookup == null || (subsetWeightingPlansLookup.Count == 0 && !ProductContext.IsAllVue) || !ProductContext.GenerateFromSurveyIds))
            {
                //
                //This is legacy weighting used by WGSN & Test Code
                //
                var fromJsonFileReferenceWeightingRepository = new JsonReferenceWeightingFactory(_settings, _loggerFactory.CreateLogger<JsonReferenceWeightingFactory>());
                // Side effect: loads into the factory
                var weightingExistencePerSubset = SubsetRepository.ToDictionary(subset => subset, subset => fromJsonFileReferenceWeightingRepository.LoadOrNullIfNotExists(subset));

                var quotaFieldMapperCollection = new MapFileQuotaCellDescriptionProvider(CategoryMappingFactory.CreateFrom(_settings, _loggerFactory), SubsetRepository);
                QuotaCellDescriptionProvider = quotaFieldMapperCollection;

                respondentRepositoryFactory = new FieldsOnlyRespondentRepositoryFactory(
                    ResponseFieldManager,
                    LazyDataLoader,
                    _loggerFactory.CreateLogger<FieldsOnlyRespondentRepositoryFactory>(),
                    _settings.AppSettings,
                    ProductContext,
                    quotaFieldMapperCollection,
                    weightingExistencePerSubset);

                (ProfileResponseAccessorFactory, RespondentRepositorySource, SampleSizeProvider, BaseExpressionGenerator) = CreateCommon(respondentRepositoryFactory, MetricConfigurationRepository);
                QuotaCellReferenceWeightingRepository = fromJsonFileReferenceWeightingRepository;
            }
            else
            {
                var subsetWeightingMeasuresDictionary = CreateSubsetWeightingMeasuresDictionary(subsetWeightingPlansLookup);

                QuotaCellDescriptionProvider = new QuotaCellDescriptionProvider(MeasureRepository, EntityInstanceRepository);

                respondentRepositoryFactory = new MetricBasedRespondentRepositoryFactory(
                    AverageDescriptorRepository,
                    _dataPresenceGuarantor,
                    subsetWeightingMeasuresDictionary,
                    _loggerFactory.CreateLogger<MetricBasedRespondentRepositoryFactory>(),
                    ProductContext
                );

                (ProfileResponseAccessorFactory, RespondentRepositorySource, SampleSizeProvider, BaseExpressionGenerator) = CreateCommon(respondentRepositoryFactory, MetricConfigurationRepository);
                var referenceWeightingCalculator = new ReferenceWeightingCalculator(_logger);
                QuotaCellReferenceWeightingRepository = new WeightingStrategyReferenceWeightingFactory(subsetWeightingPlansLookup.ToDictionary(s=>s.Key, k=>k.Value.PlansForSubset), RespondentRepositorySource, referenceWeightingCalculator, ProfileResponseAccessorFactory);
                if (!ProductContext.IsAllVue)
                {
                    FilterRepository = Filters.FilterRepository.Construct(_loggerFactory.CreateLogger<FilterRepository>(), subsetWeightingMeasuresDictionary);
                }
            }

            Calculator = MetricCalculationOrchestratorFactory.Create(
                ProfileResponseAccessorFactory,
                QuotaCellReferenceWeightingRepository,
                new DefaultCalculationStageFactory(_loggerFactory),
                MeasureRepository,
                RespondentRepositorySource,
                _loggerFactory,
                _settings,
                _dataPresenceGuarantor,
                _calculationLogger,
                _writeableVariableConfigurationRepository,
                TextCountCalculatorFactory);

            ProfileResultsCalculator = new ProfileResultsCalculator(SubsetRepository, AverageDescriptorRepository, RespondentRepositorySource, QuotaCellReferenceWeightingRepository, ProductContext, _settings, EntityInstanceRepository, ProfileResponseAccessorFactory);
        }

        private Dictionary<Subset, WeightingMetrics> CreateSubsetWeightingMeasuresDictionary(Dictionary<Subset, PlansAndRespondentLoader>subsetWeightingPlansLookup)
        {
            return subsetWeightingPlansLookup.ToDictionary(w => w.Key,
                                w =>
                                {
                                    try
                                    {
                                        return new WeightingMetrics(MeasureRepository, EntityInstanceRepository, w.Key, w.Value.PlansForSubset, w.Value.Loader);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError($"{ProductContext.ShortCode} {ProductContext.SubProductId}: Weighting plan failed : {ex.Message}");
                                        return new WeightingMetrics(MeasureRepository, EntityInstanceRepository, w.Key, null);
                                    }
                                });
        }

        private record CommonProperties(ProfileResponseAccessorFactory ProfileResponseAccessorFactory, RespondentRepositorySource RespondentRepositorySource, SampleSizeProvider SampleSizeProvider, BaseExpressionGenerator BaseExpressionGenerator);
        private CommonProperties CreateCommon(IRespondentRepositoryFactory respondentRepositoryFactory, IReadableMetricConfigurationRepository metricConfigurationRepository)
        {
            var respondentRepositorySource = new RespondentRepositorySource(SubsetRepository,
                respondentRepositoryFactory, InstanceSettings.LastSignOffDate);
            var baseExpressionGenerator = new BaseExpressionGenerator(metricConfigurationRepository, ResponseFieldManager, _readableVariableConfigurationRepository, FieldExpressionParser);
            var sampleSizeProvider = new SampleSizeProvider(AverageDescriptorRepository,
                EntityInstanceRepository, respondentRepositorySource, FieldExpressionParser, _dataPresenceGuarantor,
                baseExpressionGenerator, ProductContext);

            var profileResponseAccessorFactory = new ProfileResponseAccessorFactory(respondentRepositorySource);
            return new(profileResponseAccessorFactory, respondentRepositorySource, sampleSizeProvider, baseExpressionGenerator);
        }

        public virtual void LoadBrandVueMetadata()
        {
            _allVueConfiguration = _allVueConfigurationRepository.GetOrCreateConfiguration();

            SubsetRepository = _subsetRepositoryLoader.LoadSubsetConfiguration(_allVueConfiguration);

            AverageDescriptorRepository = LoadAverageConfiguration();

            FilterRepository = LoadFilterConfiguration();
            int[] surveyIds = SubsetRepository.Where(s => s.SurveyIdToSegmentNames != null).SelectMany(s => s.SurveyIdToSegmentNames.Keys).Distinct()
                .ToArray();
            LazyDataLoader = _lazyDataLoaderFactory.Build(InstanceSettings.LastSignOffDate, _loggerFactory.CreateLogger<BrandVueDataLoader>(), ProductContext,
                surveyIds);

            EntityTypeRepository entityTypeRepository = new EntityTypeRepository();
            var entityTypeRepositorySql = new EntityTypeRepositorySql(ProductContext, _metaDataContextFactory, entityTypeRepository);
            EntityTypeRepository = LoadResponseEntityTypes(entityTypeRepository, entityTypeRepositorySql);
            EntityInstanceRepository = LoadEntityInstancesFromMapFile(SubsetRepository);
            EntitySetRepository = new EntitySetRepository(_loggerFactory, ProductContext);
            _responseFieldManager = LoadResponseFieldConfiguration(EntityTypeRepository);
            var mapFileFields = _responseFieldManager.GetAllFields();
            _answersTableMetadataLoader.AdjustForAnswersTable(SubsetRepository, EntityTypeRepository,
                EntityInstanceRepository, EntitySetRepository, _responseFieldManager);

            EntityInstanceRepositorySql entityInstanceRepositorySql = null;
            if (_settings.LoadConfigFromSql)
            {
                entityInstanceRepositorySql = new EntityInstanceRepositorySql(ProductContext, _metaDataContextFactory, EntityInstanceRepository);
                UpdateConfiguredEntityInstances(entityInstanceRepositorySql);
            }

            var fieldExpressionParser =
                new FieldExpressionParser(ResponseFieldManager, EntityInstanceRepository, EntityTypeRepository);
            fieldExpressionParser.DeclareDummyQuestionVariables(mapFileFields);

            FieldExpressionParser = fieldExpressionParser;

            var variableFactory = new VariableFactory(FieldExpressionParser, EntityTypeRepository);

            var baseExpressionGenerator = new BaseExpressionGenerator(null,
                ResponseFieldManager, _readableVariableConfigurationRepository, FieldExpressionParser);

            MetricFactory = new MetricFactory(ResponseFieldManager, FieldExpressionParser, SubsetRepository,
                _readableVariableConfigurationRepository, variableFactory, baseExpressionGenerator, _loggerFactory.CreateLogger<MetricFactory>());

            var writeableMetricConfigurationRepository = new MetricConfigurationRepositorySql(_metaDataContextFactory,
                ProductContext, MetricFactory, _loggerFactory.CreateLogger<IMetricConfigurationRepository>());
            var questionIdHasAnswersLookup = GenerateQuestionIdHasAnswersLookup();
            try
            {
                AutoGenerateVariablesAndMetrics(writeableMetricConfigurationRepository, _writeableVariableConfigurationRepository, questionIdHasAnswersLookup);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            _variableEntityLoader = new VariableEntityLoader(EntityTypeRepository, EntityInstanceRepository, EntitySetRepository);
            VariableConfigurationRepository = new InMemoryRepositoryUpdatingVariableConfigurationRepository(_writeableVariableConfigurationRepository, _variableEntityLoader, fieldExpressionParser);

            IReadableVariableConfigurationRepository variableConfigurationRepository = VariableConfigurationRepository;
            GenerateEntitiesFromVariables(variableConfigurationRepository.GetAll());

            _entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository,
                EntitySetRepository, SubsetRepository, EntityTypeRepository, EntityInstanceRepository, ProductContext,
                _loggerFactory);

            _entitySetConfigurationRepository = new InMemoryRepositoryUpdatingEntitySetConfigurationRepository(_entitySetConfigurationRepository, _entitySetLoader);
            _entitySetLoader.AddOrUpdateAll();

            var variableConfigurations = variableConfigurationRepository.GetAll().ToArray();
            var variableLoader = new VariableLoader(fieldExpressionParser, _loggerFactory.CreateLogger<VariableLoader>());
            
            variableLoader.ParsePythonExpressionVariablesInDependencyOrder(variableConfigurations);
            
            UpdateMissingCachedPythonExpressions(variableConfigurations, variableConfigurationRepository);
            
            var measureRepositoryFactory = new MetricRepositoryFactory(_settings,
                InstanceSettings,
                _metaDataContextFactory,
                _answersContextFactory,
                _loggerFactory,
                SubsetRepository,
                ResponseFieldManager,
                MetricFactory,
                writeableMetricConfigurationRepository,
                ProductContext,
                _userDataPermissionsOrchestrator,
                _commonMetadataFieldApplicator,
                VariableConfigurationRepository,
                _allVueConfigurationRepository,
                new MetricConfigurationFactory(baseExpressionGenerator), 
                questionIdHasAnswersLookup
                );
            var measureRepository = measureRepositoryFactory.CreateAndPopulateMeasureRepository();
            MeasureRepository = measureRepository;

            DeclareFilterValueMappingVariables(measureRepository, ProductContext, VariableConfigurationRepository, FieldExpressionParser);
            var questionTypeLookupRepository = new QuestionTypeLookupRepository(measureRepository, SubsetRepository);

            QuestionTypeLookupRepository = questionTypeLookupRepository;
            MetricConfigurationRepository = new InMemoryRepositoryUpdatingMetricConfigurationRepository(writeableMetricConfigurationRepository, measureRepository,
                MetricFactory, questionTypeLookupRepository, ProductContext, VariableConfigurationRepository, _variableEntityLoader, FieldExpressionParser, _loggerFactory);

            RespondentDataLoader = new RespondentDataLoader(LazyDataLoader, EntityInstanceRepository, _settings);
            _dataPresenceGuarantor = new DataPresenceGuarantor(LazyDataLoader, EntityInstanceRepository, RespondentDataLoader);
            SampleSizeProvider = new SampleSizeProvider(AverageDescriptorRepository,
                EntityInstanceRepository,
                RespondentRepositorySource,
                FieldExpressionParser,
                _dataPresenceGuarantor,
                BaseExpressionGenerator, ProductContext);

            if (entityInstanceRepositorySql is not null && _allVueConfiguration.AllowLoadFromMapFile && _settings.AppSettings.MigrateFields)
            {
                var writeableVariableConfigurationRepository = (VariableConfigurationRepository)_writeableVariableConfigurationRepository;
                var fieldMigrator = new FieldMigrator(_settings, _loggerFactory, _invalidatableLoaderCache,
                    _responseFieldManager, writeableVariableConfigurationRepository,
                    SubsetRepository, ProductContext, EntityInstanceRepository, EntityTypeRepository,
                    entityInstanceRepositorySql, entityTypeRepositorySql, _metaDataContextFactory, writeableMetricConfigurationRepository,
                    questionTypeLookupRepository);

                fieldMigrator.Migrate(fieldExpressionParser, measureRepository);
                writeableVariableConfigurationRepository.ClearCache();
                _allVueConfiguration.AllowLoadFromMapFile = false;
                _allVueConfigurationRepository.UpdateConfiguration(_allVueConfiguration);
            }
        }

        private Dictionary<int, bool> GenerateQuestionIdHasAnswersLookup()
        {
            if (!ProductContext.IsAllVue)
            {
                return new Dictionary<int, bool>{ };
            }

            using (var context = _answersContextFactory.CreateDbContext())
            {
                var surveyIds = ProductContext.NonMapFileSurveyIds;

                var questions = context.Questions
                                       .Where(q => surveyIds.Contains(q.SurveyId))
                                       .Select(q => q.QuestionId)
                                       .ToList();

                var answers = context.Answers
                                     .Where(a => questions.Contains(a.QuestionId))
                                     .Select(a => a.QuestionId)
                                     .Distinct()
                                     .ToList();

                var answerSet = new HashSet<int>(answers);

                return questions.ToDictionary(
                    questionId => questionId,
                    questionId => answerSet.Contains(questionId));
            }
        }

        private void AutoGenerateVariablesAndMetrics(IMetricConfigurationRepository metricConfigurationRepository,
            IVariableConfigurationRepository variableConfigurationRepository, Dictionary<int, bool> questionIdHasAnswersLookup)
        {
            if (!ProductContext.IsAllVue)
            {
                return;
            }

            var variableFactory = CreateVariableConfigurationFactory(metricConfigurationRepository, variableConfigurationRepository);
            var numericFieldCollector = new NumericFieldDefinitionCollector(metricConfigurationRepository, ResponseFieldManager, SubsetRepository, _answersContextFactory);
            var bucketedVariableCreator = new BucketedVariableConfigurationCreator(variableConfigurationRepository, variableFactory);
            var bucketedMetricCreator = new BucketedMetricConfigurationCreator(metricConfigurationRepository, ProductContext);
            var numericAutoGenerationManager = new NumericAutoGenerationManager(variableConfigurationRepository, bucketedVariableCreator, bucketedMetricCreator, _loggerFactory);

            var numericFields = numericFieldCollector.GetAllNewNumericFields(questionIdHasAnswersLookup);
            numericAutoGenerationManager.CreateAllAutoBucketedNumericMetrics(numericFields);
        }

        private VariableConfigurationFactory CreateVariableConfigurationFactory(IReadableMetricConfigurationRepository metricConfigurationRepository, IReadableVariableConfigurationRepository variableConfigurationRepository)
        {
            var variableNameValidator = new VariableValidator(FieldExpressionParser, variableConfigurationRepository, EntityInstanceRepository, EntityTypeRepository, metricConfigurationRepository, ResponseFieldManager);
            return new VariableConfigurationFactory(
                FieldExpressionParser,
                variableConfigurationRepository,
                EntityTypeRepository,
                ProductContext,
                metricConfigurationRepository,
                ResponseFieldManager,
                variableNameValidator
            );
        }

        private void UpdateConfiguredEntityInstances(EntityInstanceRepositorySql instanceRepositorySql)
        {
            var entityInstanceConfigurations = instanceRepositorySql.GetEntityInstances();
            var entityRepositoryConfigurator = new EntityRepositoryConfigurator(EntityInstanceRepository, EntityTypeRepository, SubsetRepository);
            entityRepositoryConfigurator.ApplyConfiguredEntityInstances(entityInstanceConfigurations);
        }

        private void DeclareFilterValueMappingVariables(IMeasureRepository measureRepository, IProductContext productContext, IVariableConfigurationRepository variableConfigurationRepository, IFieldExpressionParser fieldExpressionParser)
        {
            var filterValueMappingParser = new FilterValueMappingVariableParser(productContext, variableConfigurationRepository, _loggerFactory.CreateLogger<FilterValueMappingVariableParser>());
            foreach (var measure in measureRepository.GetAllForCurrentUser())
            {
                try
                {
                    var variableConfig = filterValueMappingParser.CreateVariableConfigurationOrNull(measure);
                    if (variableConfig != null)
                    {
                        _variableEntityLoader.CreateOrUpdateEntityForVariable(variableConfig);
                        var variable = fieldExpressionParser.DeclareOrUpdateVariable(variableConfig);
                        measure.FilterValueMappingVariable = variable;
                        measure.FilterValueMappingVariableConfiguration = variableConfig;
                    }
                }
                catch (Exception x)
                {
                    //Seen some weird overflow errors being generated from this code! hence the message.
                    _logger.LogWarning("{product} Failed to create FilterValueMapping variable for measure {measureName} ({FilterValueMapping}): {reason}", productContext, measure?.Name, measure?.FilterValueMapping, x.Message);
                }
            }
        }

        private void UpdateMissingCachedPythonExpressions(IReadOnlyCollection<VariableConfiguration> variableConfigurations, IReadableVariableConfigurationRepository readableVariableConfigurationRepository)
        {
            // If the repository is not writable, skip this update step
            if (readableVariableConfigurationRepository is not IVariableConfigurationRepository writableVariableConfigurationRepository)
            {
                _logger.LogDebug("Skipping cached Python expression updates - repository is read-only");
                return;
            }
            
            var variablesNeedingUpdate = new List<VariableConfiguration>();

            foreach (var variable in variableConfigurations)
            {
                if (variable.Definition is EvaluatableVariableDefinition evaluatableDefinition)
                {
                    if (string.IsNullOrEmpty(evaluatableDefinition.CachedPythonExpression))
                    {
                        try
                        {
                            evaluatableDefinition.CachedPythonExpression = variable.Definition.GetPythonExpression();
                            variablesNeedingUpdate.Add(variable);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Failed to generate cached Python expression for variable {variable.Identifier} ({variable.DisplayName})");
                        }
                    }
                }
            }

            writableVariableConfigurationRepository.UpdateMany(variablesNeedingUpdate);
        }

        private void GenerateEntitiesFromVariables(IReadOnlyCollection<VariableConfiguration> readOnlyCollection)
        {
            foreach (var variableConfig in readOnlyCollection.ToArray())
            {
                SanitiseInstances(variableConfig);
                _variableEntityLoader.CreateOrUpdateEntityForVariable(variableConfig);
            }
        }

        /// <summary>
        /// In the map file migration, we ended up generating instance ids covering a range of truevals like -99>999.
        /// Most aren't valid and this looks bad in the UI, so tidy up here.
        /// We can remove this code once all brandvues are migrated.
        /// </summary>
        private void SanitiseInstances(VariableConfiguration variableConfig)
        {
            var instanceListsToMutate = GetInstanceListsToMutate(variableConfig.Definition);
            var unvalidatedInstanceCount = instanceListsToMutate.Sum(c => c.InstanceIds.Count);
            foreach (var c in instanceListsToMutate)
            {
                var allowedInstances = EntityInstanceRepository.GetInstancesAnySubset(c.FromEntityTypeName).Select(e => e.Id).ToHashSet();
                var originalLength = c.InstanceIds.Count;
                c.InstanceIds = [.. c.InstanceIds.Where(allowedInstances.Contains)];
                unvalidatedInstanceCount -= c.InstanceIds.Count;
            }
            if (unvalidatedInstanceCount > 0)
            {
                _writeableVariableConfigurationRepository.Update(variableConfig);
                _logger.LogInformation($"{unvalidatedInstanceCount} instances removed for {variableConfig.Identifier}.");
            }
        }

        private static InstanceListVariableComponent[] GetInstanceListsToMutate(VariableDefinition vd)
        {
            var groups = vd is GroupedVariableDefinition gvd ? gvd.Groups : vd is BaseGroupedVariableDefinition bgvd ? bgvd.Groups : [];
            return [.. groups.SelectMany(g => g.Component.GetDescendantsIncludingSelf()).OfType<InstanceListVariableComponent>()];
        }

        private EntityTypeRepository LoadResponseEntityTypes(EntityTypeRepository entityTypeRepository, EntityTypeRepositorySql entityTypeRepositorySql)
        {
            if (_allVueConfiguration.AllowLoadFromMapFile && File.Exists(_settings.ResponseEntityTypesMetadataFilepath))
            {
                var entityLoader = new ResponseEntityTypeInformationLoader(entityTypeRepository, _loggerFactory.CreateLogger<ResponseEntityTypeInformationLoader>());
                entityLoader.Load(_settings.ResponseEntityTypesMetadataFilepath);
            }
            else
            {
                entityTypeRepository = Entity.EntityTypeRepository.GetDefaultEntityTypeRepository();
            }

            return _settings.LoadConfigFromSql
                ? ConfigureFromSql(entityTypeRepository, entityTypeRepositorySql)
                : entityTypeRepository;
        }

        private EntityTypeRepository ConfigureFromSql(EntityTypeRepository repository, EntityTypeRepositorySql entityTypeRepositorySql)
        {
            var orderedEntityTypes = entityTypeRepositorySql.GetEntityTypes().Select(ConvertToEntityType)
                .OrderBy(e => e.Identifier);
            foreach (var entityType in orderedEntityTypes)
            {
                repository.Remove(entityType.Identifier);
                repository.TryAdd(entityType.Identifier, entityType);
            }
            return repository;
        }

        private static EntityType ConvertToEntityType(EntityTypeConfiguration entityTypeConfiguration)
        {
            return new EntityType(entityTypeConfiguration.Identifier,
                entityTypeConfiguration.DisplayNameSingular,
                entityTypeConfiguration.DisplayNamePlural)
            {
                SurveyChoiceSetNames = Enumerable.ToHashSet(entityTypeConfiguration.SurveyChoiceSetNames, StringComparer.OrdinalIgnoreCase)
            };
        }

        private ILoadableEntityInstanceRepository LoadEntityInstancesFromMapFile(ISubsetRepository subsetRepository)
        {
            var entityRepository = new EntityInstanceRepository();

            if (_allVueConfiguration.AllowLoadFromMapFile && Directory.Exists(_settings.BaseMetadataPath) && !_settings.AutoCreateEntities)
            {
                var entityFiles = Directory.GetFiles(_settings.BaseMetadataPath).Where(f => f.EndsWith("Entity.csv"));
                foreach (string entityFilePath in entityFiles)
                {
                    var entityInstanceRepository = new MapFileEntityInstanceRepository();
                    var entityInstanceLoader = new EntityInstanceInformationLoader(entityInstanceRepository,
                        _commonMetadataFieldApplicator, subsetRepository, _loggerFactory.CreateLogger<EntityInstanceInformationLoader>());
                    entityInstanceLoader.Load(entityFilePath);

                    string name = Path.GetFileName(entityFilePath);
                    string trimmedName = name.Substring(0, name.IndexOf("Entity.csv", StringComparison.Ordinal));
                    entityRepository.AddForEntityType(trimmedName, entityInstanceRepository);
                }
            }

            return entityRepository;
        }

        private FilterRepository LoadFilterConfiguration()
        {
            var filterRepository = new FilterRepository();
            var filterDescriptorLoader = new FilterDescriptorLoader(SubsetRepository, filterRepository, _commonMetadataFieldApplicator, _loggerFactory.CreateLogger<FilterDescriptorLoader>());
            if (_allVueConfiguration.AllowLoadFromMapFile) filterDescriptorLoader.Load(_settings.FilterMetadataFilepath);
            return filterRepository;
        }

        private AverageDescriptorRepository LoadAverageConfiguration()
        {
            var averageDescriptorRepository = new AverageDescriptorRepository();
            var averageMapFileLoader = new AverageDescriptorMapFileLoader(
                ProductContext,
                SubsetRepository,
                averageDescriptorRepository,
                _commonMetadataFieldApplicator,
                _loggerFactory.CreateLogger<AverageDescriptorMapFileLoader>());
            if (_settings.LoadConfigFromSql)
            {
                var persistedAverageConfigurationRepository = AverageConfigurationRepository;
                var averageLoader = new AverageDescriptorSqlLoader(averageDescriptorRepository, ProductContext, persistedAverageConfigurationRepository, SubsetRepository, _settings.AppSettings);
                averageLoader.Load(averageMapFileLoader, _settings.AverageMetadataFilePath);
                AverageConfigurationRepository = new InMemoryRepositoryUpdatingAverageConfigurationRepository(persistedAverageConfigurationRepository, averageLoader);
            }
            else
            {
                averageMapFileLoader.Load(_settings.AverageMetadataFilePath, _allVueConfiguration);
            }
            return averageDescriptorRepository;
        }

        private ResponseFieldManager LoadResponseFieldConfiguration(IResponseEntityTypeRepository responseEntityTypeRepository)
        {
            var responseFieldManager = new ResponseFieldManager(_loggerFactory.CreateLogger<ResponseFieldManager>(), responseEntityTypeRepository);
            if (_allVueConfiguration.AllowLoadFromMapFile && File.Exists(_settings.FieldDefinitionsDataFilepath)) responseFieldManager.Load(_settings.FieldDefinitionsDataFilepath, _settings.BaseMetadataPath);
            return responseFieldManager;
        }

        public static bool IsSurveyVue(string productName)
        {
            return productName.Equals(SavantaConstants.AllVueShortCode, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsBarometer(string productName)
        {
            
            return productName.Equals("barometer", StringComparison.OrdinalIgnoreCase);
        }
    }
}

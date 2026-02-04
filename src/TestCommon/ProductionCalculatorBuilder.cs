using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestCommon.Mocks;
using BrandVue.EntityFramework.ResponseRepository;
using Vue.Common.FeatureFlags;
using BrandVue.SourceData.Snowflake;
using Vue.Common.Auth;

namespace TestCommon
{
    internal class ProductionCalculatorBuilder
    {
        private IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;
        private readonly List<(CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData)> _profileResponseEntities;
        private readonly HashSet<EntityInstance> _entityInstances = new HashSet<EntityInstance>();
        private TargetInstances[] _filterInstances = Array.Empty<TargetInstances>();
        private readonly ILoggerFactory _loggerFactory;
        private readonly TestResponseFactory _testResponseFactory = new(null);

        //private WeightingStrategy _weightingStrategy;
        private WeightingPlanConfiguration[] _weightingPlans = [];
        private IReadOnlyDictionary<string, TestAnswer[][]> _quotaCellToResponsesWithAnswers;
        private DataWaveVariable _dataWaveVariable;
        private readonly AverageDescriptorRepository _averageDescriptorRepository = Averages.CreateDefaultRepo(true);
        private readonly List<GroupedVariableDefinition> _variableDefinitionsUsed = new();
        private readonly HashSet<ResponseFieldDescriptor> _fieldsUsed = new();
        private readonly List<Question> _questions = new();

        public IProductContext ProductContext { get; }
        public IUserDataPermissionsOrchestrator UserDataPermissionsService { get; }
        public CalculationPeriod CalculationPeriod { get; private set; }
        public AverageDescriptor AverageDescriptor { get; private set; }
        public Subset Subset { get; private set; }
        public IGroupedQuotaCells AllQuotaCells { get; private set; }
        public readonly MetricRepository _metricRepository;
        public readonly EntityInstanceRepository _entityInstanceRepository;

        public ProductionCalculatorBuilder(ILoggerFactory loggerFactory,
            Subset subset, params (QuotaCell QuotaCell, WeightingValue Weight)[] quotaWeightings)
        {
            Subset = subset;
            var unweightedCell = QuotaCell.UnweightedQuotaCell(subset).Yield().Select(q => (QuotaCell: q, WeightingValue.StandardWeighting(1f)));
            var allCellWeightsIndexed = unweightedCell.Concat(quotaWeightings).Select((q, index) =>
            {
                q.QuotaCell.Index = index;
                return q;
            }).ToArray();
            _loggerFactory = loggerFactory;
            AllQuotaCells = GroupedQuotaCells.CreateUnfiltered(allCellWeightsIndexed.Select(w => w.QuotaCell));
            _quotaCellReferenceWeightingRepository = MockMetadata.CreateQuotaCellReferenceWeightingRepository(Subset, allCellWeightsIndexed);
            _profileResponseEntities = new List<(CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData)>();
            _entityInstanceRepository = new EntityInstanceRepository();
            ProductContext = Substitute.For<IProductContext>();
            ProductContext.ShortCode.Returns("survey");
            ProductContext.GenerateFromSurveyIds.Returns(true);
            ProductContext.IsAllVue.Returns(true);
            UserDataPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
            _metricRepository = new MetricRepository(UserDataPermissionsService);
            // Feel free to add fluent API methods to override these:
            AverageDescriptor = _averageDescriptorRepository.Get("Monthly");
            AverageDescriptor.IncludeResponseIds = true; //Make debugging easier
            CalculationPeriod = CalculationPeriod.Parse("2019/01/31", "2019/01/31");
        }

        public ProductionCalculatorBuilder(bool includeResponseIds = true) :
            this(Substitute.For<ILoggerFactory>(), TestResponseFactory.AllSubset, MockMetadata.CreateNonInterlockedQuotaCells(TestResponseFactory.AllSubset, 1).Cells.Select(q => (QuotaCell: q, Weight: WeightingValue.StandardWeighting(1f))).ToArray())
        {
            // Feel free to add fluent API methods to override these:
            AverageDescriptor.IncludeResponseIds = includeResponseIds; //Make debugging easier
        }

        public ProductionCalculatorBuilder WithCalculationPeriod(CalculationPeriod calculationPeriod)
        {
            CalculationPeriod = calculationPeriod;
            return this;
        }

        public ProductionCalculatorBuilder WithAverage(AverageDescriptor average)
        {
            AverageDescriptor = average;
            return this;
        }

        public ProductionCalculatorBuilder IncludeMeasures(Measure[] measures,
            IList<GroupedVariableDefinition> variableDefinitionsUsed = null)
        {
            if (variableDefinitionsUsed != null)
            {
                _variableDefinitionsUsed.AddRange(variableDefinitionsUsed);
            }

            _fieldsUsed.AddRange(measures.SelectMany(m => m.GetFieldDependencies()));

            foreach (var measure in measures)
            {
                _metricRepository.TryAdd(measure.Name, measure);
            }
            return this;
        }

        public ProductionCalculatorBuilder IncludeEntities(params EntityValue[] entityValues)
        {
            foreach (var entityValue in entityValues)
            {
                _entityInstanceRepository.Add(entityValue.EntityType, entityValue.AsInstance());
            }
            return this;
        }

        public ProductionCalculatorBuilder UseAllVueProductContext()
        {
            ProductContext.GenerateFromSurveyIds.Returns(true);
            return this;
        }

        public ProductionCalculatorBuilder WithFilterInstance(EntityValue filterInstance)
        {
            if (filterInstance != null)
            {
                _filterInstances = new[] {new TargetInstances(filterInstance.EntityType, new[] {filterInstance.AsInstance()})};
            }
            return this;
        }

        public ProductionCalculatorBuilder WithBrandResponses(Measure measure,
            (EntityInstance EntityInstance, QuotaCell QuotaCell, ValueResponseCount[] ReponseCounts)[] instanceResults, TestResponseMonthsPopulator testResponseMonthsPopulator)
        {
            foreach (var entityInstance in instanceResults.Select(x => x.EntityInstance)) _entityInstances.Add(entityInstance);
            var intendedResults = instanceResults.Select(r => (new [] { new EntityValue(TestEntityTypeRepository.Brand, r.EntityInstance.Id) }, r.QuotaCell, r.ReponseCounts)).ToArray();
            var profileResponseEntities = testResponseMonthsPopulator.CreateRespondentsForResults(measure, CalculationPeriod.Periods[0], intendedResults);
            _profileResponseEntities.AddRange(profileResponseEntities);
            return this;
        }

        public ProductionCalculatorBuilder WithAnswers(IReadOnlyCollection<TestAnswer> answers)
        {
            var responseAnswers = answers.Select(a => new ResponseAnswers(a.Yield().ToArray()));
            return WithResponses(responseAnswers.ToArray());
        }

        public ProductionCalculatorBuilder WithResponses(IReadOnlyCollection<ResponseAnswers> responses)
        {
            const int defaultSurveyId = -1;
            var defaultTimeStamp = CalculationPeriod.Periods.First().StartDate;
            var quotaCell = AllQuotaCells.Cells.First(q => !q.IsUnweightedCell);

            foreach ((var answers, DateTimeOffset? timeStamp, int? surveyId) in responses)
            {
                foreach (var entityValue in answers.SelectMany(a => a.EntityValues.AsReadOnlyCollection())) _entityInstances.Add(new EntityInstance {Id = entityValue.Value, Name = $"{entityValue.Value}"});
                var profileResponseEntity = AddQuotaCellToResponse(answers, quotaCell, timeStamp ?? defaultTimeStamp, surveyId ?? defaultSurveyId);
                _profileResponseEntities.Add(profileResponseEntity);
                _fieldsUsed.AddRange(answers.Select(a => a.Field));
            }

            CalculationPeriod = new CalculationPeriod(responses.Min(r => r.Timestamp ?? defaultTimeStamp), responses.Max(r => r.Timestamp ?? defaultTimeStamp));
            return this;
        }

        private (CellResponse ProfileResponse, List<EntityMetricData> EntityMeasureData) AddQuotaCellToResponse(IReadOnlyCollection<TestAnswer> answers, QuotaCell quotaCell, DateTimeOffset timeStamp, int surveyId)
        {
            var profileResponseWithMeasureData = _testResponseFactory.CreateResponse(timeStamp, surveyId, answers);
            return (new CellResponse(profileResponseWithMeasureData.ProfileResponse, quotaCell), profileResponseWithMeasureData.EntityMeasureData);
        }

        public ProductionCalculatorBuilder WithWeightingPlansAndResponses(WeightingPlanConfiguration[] weightingPlans,
            IReadOnlyDictionary<string, TestAnswer[][]> quotaCellToResponsesWithAnswers,
            DataWaveVariable dataWaveVariable = null)
        {
            _quotaCellToResponsesWithAnswers = quotaCellToResponsesWithAnswers;

            _fieldsUsed.AddRange(quotaCellToResponsesWithAnswers.Values.SelectMany(x => x).SelectMany(x => x).Select(x => x.Field));
            _weightingPlans = weightingPlans;
            _dataWaveVariable = dataWaveVariable;

            return this;
        }

        public ProductionCalculatorBuilder WithSubset(Subset subset)
        {
            Subset = subset;
            return this;
        }

        internal ProductionCalculatorPlusConvenienceOverloads BuildRealCalculatorWithInMemoryDb()
        {
            var loader = BuildLoaderWithInMemoryDb();

            return new ProductionCalculatorPlusConvenienceOverloads(loader.Calculator, this, loader);
        }

        public TestDataLoader BuildLoaderWithInMemoryDb()
        {
            if (_quotaCellToResponsesWithAnswers != null)
            {
                CreateProfiles();
            }

            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            weightingPlanRepository.GetLoaderWeightingPlansForSubset(Arg.Any<string>(), Arg.Any<string>(), Subset.Id)
                .Returns(_weightingPlans);

            var responseWeightingRepository = Substitute.For<IResponseWeightingRepository>();
            responseWeightingRepository.AreThereAnyRootResponseWeights(Subset.Id).Returns(false);

            ConfigurationSourcedLoaderSettings settings = TestLoaderSettings.WithProduct(ProductContext.ShortCode);
            var testMetadataContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            PopulateMetadataContext((TestMetadataContextFactoryInMemory)testMetadataContextFactory);

            var answerDbContextFactory = AnswersMetaDataHelper.CreateMockAnswersDbContext();

            Dictionary<string, ChoiceSet> choiceSetFromEntityName = _questions.SelectMany(q => q.GetAllChoiceSets())
                .Where(x => x.ChoiceSet != null)
                .ToDictionary(x => x.ChoiceSet.Name, x => x.ChoiceSet);
            var questions = _fieldsUsed.Select((f, i) =>
            {
                var dataAccessModel = f.GetDataAccessModel(Subset.Id);
                var choiceSets = dataAccessModel.OrderedEntityColumns
                    .Select(c => GetOrAdd(choiceSetFromEntityName, c))
                    .Reverse()
                    .ToList();
                return new Question()
                {
                    VarCode = f.Name,
                    QuestionText = f.Name,
                    ItemNumber = i,
                    AnswerChoiceSet = choiceSets.ElementAtOrDefault(0),
                    QuestionChoiceSet = choiceSets.ElementAtOrDefault(1),
                    PageChoiceSet = choiceSets.ElementAtOrDefault(2),
                    SectionChoiceSet = choiceSets.ElementAtOrDefault(3),
                    MasterType = dataAccessModel.QuestionModel.MasterType ?? (f.EntityCombination.Any() ? "RADIO" : "VALUE"),
                    NumberFormat = dataAccessModel.QuestionModel.NumberFormat
                };
            }).Concat(_questions).ToList();
            var choiceSetGroups = choiceSetFromEntityName.Values.Select(cs => new ChoiceSetGroup(cs, new[] { cs })).ToList();

            var choiceSetReader = Substitute.For<IChoiceSetReader>();
            choiceSetReader.GetSegmentIds(Arg.Any<Subset>()).Returns(args => args.Arg<Subset>().Index.Yield().ToArray());
            choiceSetReader.SurveyHasNonTestCompletes(Arg.Any<IEnumerable<int>>()).Returns(true);

            var main = new SurveySegment()
            {
                SegmentName = "Main",
                SurveyId = 1,
                SurveySegmentId = 1
            };
            choiceSetReader.GetSegments(null).ReturnsForAnyArgs(new[] { main });
            choiceSetReader.GetAnswerStats(null, null)
                .ReturnsForAnyArgs(questions.Select(q => new AnswerStat() { SegmentId = main.SurveySegmentId, ResponseCount = 3 }).ToList());
            choiceSetReader.GetChoiceSetTuple(Arg.Any<IReadOnlyCollection<int>>()).Returns(args => (questions, choiceSetGroups));

            var lazyDataLoader =
                new SingleSubsetManualDataLoader(_profileResponseEntities.SelectMany(p => p.EntityMeasureData).ToList());

            var answersDbContextFactory = AnswersMetaDataHelper.CreateMockAnswersDbContext();
            var loader = TestDataLoader.Create(settings, testMetadataContextFactory, answersDbContextFactory,
                ProductContext, UserDataPermissionsService, choiceSetReader, lazyDataLoader, Substitute.For<IAverageConfigurationRepository>(),
                Substitute.For<IEntitySetConfigurationRepository>(), weightingPlanRepository,
                responseWeightingRepository);
            loader.LoadBrandVueMetadata();
            loader.LoadBrandVueData();

            Subset = loader.SubsetRepository.Single();
            var respondentRepository = loader.RespondentRepositorySource.GetForSubset(Subset);
            AllQuotaCells = respondentRepository.GetGroupedQuotaCells(AverageDescriptor);
            return loader;
        }

        private ChoiceSet GetOrAdd(Dictionary<string, ChoiceSet> choiceSetFromEntityName,
            EntityFieldDefinitionModel c)
        {
            string name = c.EntityType.Identifier;
            if (choiceSetFromEntityName.TryGetValue(name, out var cs)) return cs;
            var choices = _entityInstanceRepository.GetInstancesOf(name, Subset).Select(i => new Choice(){SurveyChoiceId = i.Id, Name = i.Name, ImageURL= i.ImageURL}).ToList();
            if (!choices.Any()) throw new InvalidOperationException($"No entities included for {name}, call IncludeEntities with the entity instances");
            var choiceSet = new ChoiceSet(){Name = name, Choices = choices};
            choiceSetFromEntityName.Add(name, choiceSet);
            return choiceSet;
        }

        private void PopulateMetadataContext(TestMetadataContextFactoryInMemory testMetadataContextFactory)
        {
            using var ctx = testMetadataContextFactory.CreateDbContext();
            var variableConfigurationsFromName = _variableDefinitionsUsed.Select((v,i) => new VariableConfiguration()
            {
                Id = i,
                Definition = v,
                Identifier = v.ToEntityTypeName,
                ProductShortCode = ProductContext.ShortCode,
                SubProductId = ProductContext.SubProductId,
                DisplayName = v.ToEntityTypeName
            }).ToDictionary(v => v.Identifier);
            ctx.AddRange(variableConfigurationsFromName.Values);
            ctx.AddRange(_metricRepository.GetAllForCurrentUser().Select(measure => CreateMetricConfiguration(measure, variableConfigurationsFromName)));
            ctx.SaveChanges();
        }

        private MetricConfiguration CreateMetricConfiguration(Measure measure,
            Dictionary<string, VariableConfiguration> variableConfigurationsFromName)
        {
            var baseVals = measure.LegacyBaseValues.IsList ? string.Join("|", measure.LegacyBaseValues.Values) :
                measure.LegacyBaseValues.IsRange ? $"{measure.LegacyBaseValues.Minimum.Value}>{measure.LegacyBaseValues.Maximum.Value}" :
                null;
            string trueVals = measure.LegacyPrimaryTrueValues.IsList ? string.Join("|", measure.LegacyPrimaryTrueValues.Values) :
                measure.LegacyPrimaryTrueValues.IsRange ? $"{measure.LegacyPrimaryTrueValues.Minimum}>{measure.LegacyPrimaryTrueValues.Maximum}" :
                null;
            return new MetricConfiguration()
            {
                VariableConfigurationId = measure.PrimaryVariable != null ? variableConfigurationsFromName[measure.Name].Id : null,
                FieldExpression = null,
                BaseExpression = measure.BaseExpressionString,
                BaseField = measure.BaseField?.Name,
                BaseVals = baseVals,
                CalcType = CalculationTypeParser.AsString(measure.CalculationType),
                DefaultSplitByEntityType = measure.DefaultSplitByEntityTypeName,
                Field = measure.Field?.Name,
                TrueVals = trueVals,
                Name = measure.Name,
                Field2 = measure.Field2?.Name,
                ProductShortCode = ProductContext.ShortCode,
                SubProductId = ProductContext.SubProductId,
                Subset = measure.Subset != null ? string.Join("|",measure.Subset.Select(x => x.Id) ) : null,
                FilterValueMapping = measure.FilterValueMapping,
                IsAutoGenerated = measure.GenerationType,
            };
        }

        public ProductionCalculatorPlusConvenienceOverloads BuildRealCalculator()
        {
            var mockRespondentRespositorySource = MockRespondentRepositorySource(Subset,
                _profileResponseEntities.Select(p => p.ProfileResponse).ToList());
            var profileResponseAccessorFactory = new ProfileResponseAccessorFactory(mockRespondentRespositorySource);
            if (_weightingPlans.Any())
            {
                var subsetWeightingPlansLookup = CreateProfiles();

                var referenceWeightingCalculator = new ReferenceWeightingCalculator(NullLogger<ReferenceWeightingCalculator>.Instance);
                _quotaCellReferenceWeightingRepository = new WeightingStrategyReferenceWeightingFactory(subsetWeightingPlansLookup,
                    mockRespondentRespositorySource, referenceWeightingCalculator, profileResponseAccessorFactory);
            }

            return BuildRealCalculator(mockRespondentRespositorySource, profileResponseAccessorFactory);
        }
        private static IRespondentRepositorySource MockRespondentRepositorySource(Subset subset, IReadOnlyCollection<CellResponse> profileResponseEntities)
        {
            var respondentRepositorySource = Substitute.For<IRespondentRepositorySource>();
            var respondentRepository = new RespondentRepository(subset);
            foreach (var pre in profileResponseEntities) respondentRepository.Add(pre.ProfileResponseEntity, pre.QuotaCell);

            respondentRepositorySource.GetForSubset(subset).Returns(respondentRepository);
            return respondentRepositorySource;
        }

        private Dictionary<Subset, List<WeightingPlan>> CreateProfiles()
        {
            if (_quotaCellToResponsesWithAnswers != null)
            {
                var subsetWeightingPlansLookup = _weightingPlans.YieldNonNull().ToDictionary(_ => Subset, ws => ws.ToAppModel().ToList());

                var respondentRepository = new RespondentRepository(Subset);
                var calcPeriod = CalculationPeriod.Periods.Single();
                DateRangeVariableComponent dateRangeVariableComponent = null;
                foreach ((string quotaCell, var respondentsAndAnswers) in _quotaCellToResponsesWithAnswers)
                {
                    if ( (_weightingPlans.Length == 1) && !QuotaCell.UnweightedQuotaCell(Subset).ToString().Equals(quotaCell))
                    {
                        if (_dataWaveVariable != null)
                        {
                            string first = quotaCell.Split(QuotaCell.PartSeparator).First();
                            _dataWaveVariable.WaveIdToWaveConditions.TryGetValue(int.Parse(first), out dateRangeVariableComponent);
                        }
                    }
                    var calcPeriodStartDate = dateRangeVariableComponent?.MinDate ?? calcPeriod.StartDate;
                    var calcPeriodEndDate = dateRangeVariableComponent?.MaxDate ?? calcPeriod.EndDate;
                    var testResponses = _testResponseFactory
                        .CreateTestResponses(calcPeriodStartDate, calcPeriodEndDate, respondentsAndAnswers).ToList();
                    foreach (var (profileResponse, _) in testResponses)
                    {
                        respondentRepository.Add(profileResponse, QuotaCell.UnweightedQuotaCell(Subset));
                    }

                    var cellResponsesWithMeasureData = testResponses
                        .Select(r => (respondentRepository.Get(r.ProfileResponse.Id), r.EntityMeasureData));
                    _profileResponseEntities.AddRange(cellResponsesWithMeasureData);
                }

                return subsetWeightingPlansLookup;
            }

            return null;
        }

        private ProductionCalculatorPlusConvenienceOverloads BuildRealCalculator(IRespondentRepositorySource mockDailyQuotaCellRespondentsSource, ProfileResponseAccessorFactory profileResponseAccessorFactory)
        {
            var emptyBecauseDirectlyPopulatedResponses = new SingleSubsetManualDataLoader(Array.Empty<EntityMetricData>());
            var calculator = new ProductionCalculatorPlusConvenienceOverloads(profileResponseAccessorFactory,
                _quotaCellReferenceWeightingRepository,
                new DefaultCalculationStageFactory(_loggerFactory),
                _metricRepository,
                mockDailyQuotaCellRespondentsSource,
                _entityInstanceRepository,
                emptyBecauseDirectlyPopulatedResponses,
                this, _loggerFactory,
                new ConfigurationSourcedLoaderSettings(new AppSettings()),
                Substitute.For<ITextCountCalculatorFactory>());
            return calculator;
        }

        internal class ProductionCalculatorPlusConvenienceOverloads : IMetricCalculationOrchestrator
        {
            private readonly ProductionCalculatorBuilder _convenienceTestInfo;
            private readonly IMetricCalculationOrchestrator _productionCalculator;
            public TestDataLoader DataLoader { get; }
            public IProductContext ProductContext => _convenienceTestInfo.ProductContext;
            private IVariableConfigurationRepository _variableConfigurationRepository;

            public ProductionCalculatorPlusConvenienceOverloads(
                IProfileResponseAccessorFactory profileResponseAccessorFactory,
                IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
                ICalculationStageFactory calculationStageFactory,
                IMeasureRepository measureRepository,
                IRespondentRepositorySource respondentRepositorySource,
                IEntityRepository entityRepository,
                ILazyDataLoader lazyDataLoader,
                ProductionCalculatorBuilder convenienceTestInfo,
                ILoggerFactory loggerFactory,
                IBrandVueDataLoaderSettings settings,
                ITextCountCalculatorFactory textCountCalculatorFactory)
            {
                var dataPresenceGuarantor = new DataPresenceGuarantor(lazyDataLoader, entityRepository, new RespondentDataLoader(lazyDataLoader, entityRepository, settings));
                var orchestrator = new InMemoryTotalisationOrchestrator(respondentRepositorySource, dataPresenceGuarantor, profileResponseAccessorFactory, loggerFactory);
                _variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
                _productionCalculator = new MetricCalculationOrchestrator(
                    profileResponseAccessorFactory,
                    quotaCellReferenceWeightingRepository,
                    calculationStageFactory,
                    measureRepository,
                    respondentRepositorySource,
                    dataPresenceGuarantor,
                    orchestrator,
                    null,
                    NullLogger.Instance,
                    _variableConfigurationRepository,
                    null,
                    textCountCalculatorFactory);
                _convenienceTestInfo = convenienceTestInfo;
            }

            public ProductionCalculatorPlusConvenienceOverloads(
                IMetricCalculationOrchestrator productionCalculator,
                ProductionCalculatorBuilder convenienceTestInfo, TestDataLoader dataLoader)
            {
                _productionCalculator = productionCalculator;
                _convenienceTestInfo = convenienceTestInfo;
                DataLoader = dataLoader;
            }

            private FilteredMetric FilteredMeasureFromLoader(FilteredMetric filteredMetric) =>MeasureFromLoader(filteredMetric.Metric) != filteredMetric.Metric ?
                FilteredMetric.Create(MeasureFromLoader(filteredMetric.Metric), filteredMetric.FilterInstances, filteredMetric.Subset, filteredMetric.Filter) : filteredMetric;

            private Measure MeasureFromLoader(Measure measure) => DataLoader?.MeasureRepository.Get(measure.Name) ?? measure;
            private ResponseFieldDescriptor FieldFromLoader(ResponseFieldDescriptor field) => DataLoader?.ResponseFieldManager.Get(field.Name) ?? field;

            public async Task<EntityWeightedDailyResults[]> Calculate(Measure measure)
            {
                measure = MeasureFromLoader(measure);
                var entityInstances = _convenienceTestInfo._entityInstances.ToArray();
                var requestedInstances = new TargetInstances(measure.EntityCombination.SingleOrDefault() ?? EntityType.ProfileType, entityInstances.ToArray());
                Subset subset = _convenienceTestInfo.Subset;
                IFilter filter = new AlwaysIncludeFilter();
                return await _productionCalculator.Calculate(FilteredMetric.Create(measure, _convenienceTestInfo._filterInstances, subset, filter), _convenienceTestInfo.CalculationPeriod, _convenienceTestInfo.AverageDescriptor, requestedInstances, _convenienceTestInfo.AllQuotaCells, false, CancellationToken.None);
            }

            public async Task<UnweightedTotals> CalculateUnweighted(Measure measure)
            {
                measure = MeasureFromLoader(measure);
                var entityInstances = _convenienceTestInfo._entityInstances.ToArray();
                var requestedInstances = new TargetInstances(measure.EntityCombination.SingleOrDefault() ?? EntityType.ProfileType, entityInstances.ToArray());
                Subset subset = _convenienceTestInfo.Subset;
                IFilter filter = new AlwaysIncludeFilter();
                return await _productionCalculator.CalculateUnweightedTotals(FilteredMetric.Create(measure, _convenienceTestInfo._filterInstances, subset, filter),
                    _convenienceTestInfo.CalculationPeriod,
                    _convenienceTestInfo.AverageDescriptor,
                    requestedInstances,
                    _convenienceTestInfo.AllQuotaCells, CancellationToken.None);
            }

            public async Task<EntityWeightedDailyResults[]> CalculateFor(Measure measure, params EntityValue[] entityValues)
            {
                measure = MeasureFromLoader(measure);
                return await CalculateFor(measure, entityValues, new AlwaysIncludeFilter());
            }

            public async Task<EntityWeightedDailyResults[]> CalculateFor(Measure measure, EntityValue[] entityValues, IFilter filter)
            {
                measure = MeasureFromLoader(measure);
                var requestedInstances = GetRequestedInstances(entityValues);
                Subset subset = _convenienceTestInfo.Subset;
                return await _productionCalculator.Calculate(FilteredMetric.Create(measure, _convenienceTestInfo._filterInstances, subset, filter), _convenienceTestInfo.CalculationPeriod, _convenienceTestInfo.AverageDescriptor, requestedInstances, _convenienceTestInfo.AllQuotaCells, false, CancellationToken.None);
            }

            public Task<EntityWeightedDailyResults[]> Calculate(FilteredMetric filteredMetric,
                CalculationPeriod calculationPeriod,
                AverageDescriptor average,
                TargetInstances requestedInstances,
                IGroupedQuotaCells quotaCells, bool calculateSignificance,
                CancellationToken cancellationToken)
            {
                filteredMetric = FilteredMeasureFromLoader(filteredMetric);
                return _productionCalculator.Calculate(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, calculateSignificance, CancellationToken.None);
            }
            public Task<WeightedDailyResult> CalculateAverageMentions(FilteredMetric filteredMetric,
                CalculationPeriod calculationPeriod,
                AverageDescriptor average,
                TargetInstances requestedInstances,
                IGroupedQuotaCells quotaCells,
                CancellationToken cancellationToken)
            {
                var metricFromLoader = MeasureFromLoader(filteredMetric.Metric);
                var filteredMetricFromLoader = FilteredMetric.Create(metricFromLoader, filteredMetric.FilterInstances, filteredMetric.Subset, filteredMetric.Filter);
                return _productionCalculator.CalculateAverageMentions(filteredMetricFromLoader, calculationPeriod, average, requestedInstances, quotaCells, cancellationToken);
            }

            public async Task<WeightedDailyResult[]> CalculateForNumericResponseAverage(ResponseFieldDescriptor field,
                Measure measure,
                IFilter filter,
                TargetInstances requestedInstances,
                AverageType averageType)
            {
                field = FieldFromLoader(field);
                measure = MeasureFromLoader(measure);
                Subset subset = _convenienceTestInfo.Subset;
                var filteredMetric = FilteredMetric.Create(measure, _convenienceTestInfo._filterInstances, subset, filter);
                return await _productionCalculator.CalculateNumericResponseAverage(filteredMetric, _convenienceTestInfo.CalculationPeriod, _convenienceTestInfo.AverageDescriptor, requestedInstances, _convenienceTestInfo.AllQuotaCells, averageType, field, CancellationToken.None);
            }

            public Task<WeightedDailyResult[]> CalculateNumericResponseAverage(FilteredMetric filteredMetric,
                CalculationPeriod calculationPeriod,
                AverageDescriptor average,
                TargetInstances requestedInstances,
                IGroupedQuotaCells quotaCells,
                AverageType averageType,
                ResponseFieldDescriptor field,
                CancellationToken cancellationToken)
            {
                var metric = MeasureFromLoader(filteredMetric.Metric);
                var filteredMetricFromLoader = FilteredMetric.Create(metric, filteredMetric.FilterInstances, filteredMetric.Subset, filteredMetric.Filter);
                return _productionCalculator.CalculateNumericResponseAverage(filteredMetricFromLoader, calculationPeriod, average, requestedInstances, quotaCells, averageType, field, cancellationToken);
            }

            private static TargetInstances GetRequestedInstances(EntityValue[] entityValues)
            {
                if (entityValues == null) return null;
                var splitByType = entityValues.Select(ev => ev.EntityType).Distinct().SingleOrDefault() ??
                                  TestEntityTypeRepository.Profile;
                var entityInstances = entityValues.Select(ev => ev.AsInstance());
                var requestedInstances = new TargetInstances(splitByType, entityInstances.ToArray());
                return requestedInstances;
            }

            public Task<UnweightedTotals> CalculateUnweightedTotals(FilteredMetric filteredMetric,
                CalculationPeriod calculationPeriod,
                AverageDescriptor average, TargetInstances requestedInstances, IGroupedQuotaCells quotaCells,
                CancellationToken cancellationToken,
                EntityWeightedDailyResults[] weightedAverages = null)
            {
                filteredMetric = FilteredMeasureFromLoader(filteredMetric);
                return _productionCalculator.CalculateUnweightedTotals(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, CancellationToken.None, weightedAverages);
            }

            public Task<EntityWeightedDailyResults[]> CalculateWeightedFromUnweighted(
                UnweightedTotals unweighted,
                bool calculateSignificance, CancellationToken cancellationToken,
                IGroupedQuotaCells filteredCells = null)
            {
                return _productionCalculator.CalculateWeightedFromUnweighted(unweighted, calculateSignificance, cancellationToken, filteredCells);
            }

            public IList<WeightedDailyResult> CalculateMarketAverage(EntityWeightedDailyResults[] measureResults,
                Subset subset,
                ushort minimumSamplePerPoint,
                AverageType averageType,
                MainQuestionType questionType,
                EntityMeanMap entityMeanMaps,
                EntityWeightedDailyResults[] relativeSizes = null)
            {
                return _productionCalculator.CalculateMarketAverage(measureResults,
                    subset,
                    minimumSamplePerPoint,
                    averageType,
                    questionType,
                    entityMeanMaps,
                    relativeSizes);
            }
        }

        public ProductionCalculatorBuilder IncludeQuestions(params Question[] questions)
        {
            _questions.AddRange(questions);
            return this;
        }
    }
}

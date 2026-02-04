using BrandVue.EntityFramework;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Test.BrandVue.SourceData.Extensions;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using EntityType = BrandVue.SourceData.Entity.EntityType;
using Vue.Common.FeatureFlags;
using BrandVue.SourceData.Snowflake;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class PipelinesMeasureCalculationOrchestratorTests
    {
        private double _smallError = 1e-9;
        private static readonly Subset Subset = TestResponseFactory.AllSubset;

        private static readonly EntityType Type1 = new EntityType(nameof(Type1), nameof(Type1), nameof(Type1));
        private static readonly EntityInstance Type1Instance1 = new EntityValue(Type1, 1).AsInstance();
        private static readonly EntityInstance Type1Instance4 = new EntityValue(Type1, 4).AsInstance();
        private static readonly EntityType Type2 = new EntityType(nameof(Type2), nameof(Type2), nameof(Type2));
        private static readonly EntityInstance Type2Instance6 = new EntityValue(Type2, 6).AsInstance();
        private static readonly EntityInstance Type2Instance7 = new EntityValue(Type2, 7).AsInstance();
        private static readonly EntityType Brand = TestEntityTypeRepository.Brand;
        private static readonly EntityInstance BrandInstance12 = new EntityValue(Brand, 12).AsInstance();
        private static readonly EntityInstance BrandInstance15 = new EntityValue(Brand, 15).AsInstance();
        private readonly EntityInstanceRepository _entityInstanceRepo;
        private readonly ResponseFieldManager _responseFieldManager;
        private readonly ResponseFieldDescriptor _profileField;
        private readonly ResponseFieldDescriptor _type1Field;
        private readonly ResponseFieldDescriptor _type2Field;
        private readonly ResponseFieldDescriptor _type1And2Field;
        private readonly ResponseFieldDescriptor _brandField;
        private readonly TestEntityTypeRepository _entityTypeRepository;
        private EntityWeightedDailyResults[] _entityRequestedMeasureResults;
        private readonly List<int> _responseIds;

        public PipelinesMeasureCalculationOrchestratorTests()
        {
            _entityInstanceRepo = new EntityInstanceRepository();
            _entityInstanceRepo.AddInstances(Brand.Identifier, BrandInstance12, BrandInstance15);
            _entityInstanceRepo.AddInstances(Type1.Identifier, Type1Instance1, Type1Instance4);
            _entityInstanceRepo.AddInstances(Type2.Identifier, Type2Instance6, Type2Instance7);
            _entityTypeRepository = new TestEntityTypeRepository();
            var substituteLogger = Substitute.For<ILogger<ResponseFieldManager>>();
            _responseFieldManager = new ResponseFieldManager(substituteLogger, _entityTypeRepository);
            _profileField = _responseFieldManager.Add(nameof(_profileField));
            _type1Field = _responseFieldManager.Add(nameof(_type1Field), Type1);
            _type2Field = _responseFieldManager.Add(nameof(_type2Field), Type2);
            _type1And2Field = _responseFieldManager.Add(nameof(_type1And2Field), Type1, Type2);
            _brandField = _responseFieldManager.Add(nameof(_brandField), Brand);
            _responseIds = Enumerable.Range(1, 100).ToList();
            CreateMeasureResults();
        }

        [Test]
        public void ShouldReturnAverageMentions()
        {
            var measure = new Measure() { CalculationType = CalculationType.YesNo };
            var responseIdsWithWeights = GetResponseIdsWithWeights(_entityRequestedMeasureResults);
            var average = AverageMentionsCalculator.GetAverageMentions(measure, responseIdsWithWeights, _entityRequestedMeasureResults);
            Assert.That(average.WeightedResult, Is.EqualTo(0.38947368421052631).Within(_smallError));
        }

        [Test]
        public void AverageMentionsScoreShouldChangeWhenEntityInstanceDeselected()
        {
            var measure = new Measure() { CalculationType = CalculationType.YesNo };
            var updatedRequestedResults = _entityRequestedMeasureResults.Where(e => e.EntityInstance.Id != 1).ToArray();
            var responseIdsWithWeights = GetResponseIdsWithWeights(updatedRequestedResults);
            var average = AverageMentionsCalculator.GetAverageMentions(measure, responseIdsWithWeights, updatedRequestedResults);
            Assert.That(average.WeightedResult, Is.EqualTo(0.66666666666666).Within(_smallError));
        }

        [Test]
        public void ShouldReturnAverageMentionsForAvgCalcType()
        {
            var measure = new Measure() { CalculationType = CalculationType.Average };
            var responseIdsWithWeights = GetResponseIdsWithWeights(_entityRequestedMeasureResults);
            var average = AverageMentionsCalculator.GetAverageMentions(measure, responseIdsWithWeights, _entityRequestedMeasureResults);
            Assert.That(average.WeightedResult, Is.EqualTo(1.452631578947368).Within(_smallError));
        }

        [Test]
        public async Task ProfileMetricReferencingNonProfileFieldShouldLoadCorrectInstances()
        {
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepo, _entityTypeRepository);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();

            var orchestrator = CreateRealOrchestrator(lazyDataLoader, _entityInstanceRepo);

            var measure = new Measure
            {
                Name = "Test measure",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull("max(response._type1Field())"),
            };

            var profileInstances = new TargetInstances(TestEntityTypeRepository.Profile, Array.Empty<EntityInstance>());

            var calculationPeriod = new CalculationPeriod(DateTimeOffset.MinValue.AddDays(10), DateTimeOffset.MaxValue.AddDays(-10));

            var actualTargets = await GetTargetsFromCalculateUnweighted(orchestrator, lazyDataLoader, calculationPeriod, measure, profileInstances, CancellationToken.None);
            var actualTargetsValues = actualTargets.Select(t => (t.EntityType, t.SortedEntityInstanceIds));
            Assert.That(actualTargetsValues, Is.EquivalentTo(new[] { (Type1, new int[] { 1, 4 }) }));
        }

        [Test]
        public async Task NonProfileMetricReferencingOtherNonProfileFieldShouldLoadCorrectInstances()
        {
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepo, _entityTypeRepository);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();

            var orchestrator = CreateRealOrchestrator(lazyDataLoader, _entityInstanceRepo);

            var measure = new Measure
            {
                Name = "Test measure",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull("max(response._type1Field(Type1=[1])) and result.brand == 12 and result.brand"),
            };

            var instances = new TargetInstances(Brand, new[] { BrandInstance12 });

            var calculationPeriod = new CalculationPeriod(DateTimeOffset.MinValue.AddDays(10), DateTimeOffset.MaxValue.AddDays(-10));

            var actualTargets = await GetTargetsFromCalculateUnweighted(orchestrator, lazyDataLoader, calculationPeriod, measure, instances, CancellationToken.None);
            var actualTargetsValues = actualTargets.Select(t => (t.EntityType, t.SortedEntityInstanceIds));
            Assert.That(actualTargetsValues, Is.EquivalentTo(new[] { (Type1, new int[] { 1, 4 }) }));
        }

        [Test]
        public async Task AnyMetricFilteredByMetricReferencingNonProfileFieldShouldLoadCorrectInstances()
        {
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepo, _entityTypeRepository);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();

            var orchestrator = CreateRealOrchestrator(lazyDataLoader, _entityInstanceRepo);

            var anyMeasure = new Measure
            {
                Name = "Any measure",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull("1"),
            };

            var filterMeasure = new Measure
            {
                Name = "Filter measure",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull("max(response._type1Field())"),
            };

            var profileInstances = new TargetInstances(TestEntityTypeRepository.Profile, Array.Empty<EntityInstance>());

            var calculationPeriod = new CalculationPeriod(DateTimeOffset.MinValue.AddDays(10), DateTimeOffset.MaxValue.AddDays(-10));

            var filter = new MetricFilter(Subset, filterMeasure, default, new int[] { 1 });
            var actualTargets = await GetTargetsFromCalculateUnweighted(orchestrator, lazyDataLoader, calculationPeriod, anyMeasure, profileInstances, CancellationToken.None, filter);
            var actualTargetsValues = actualTargets.Select(t => (t.EntityType, t.SortedEntityInstanceIds));
            Assert.That(actualTargetsValues, Is.EquivalentTo(new[] { (Type1, new int[] { 1, 4 })}));
        }

        [Test]
        public async Task AnyMetricFilteredByMetricIntroducingUserEntityShouldLoadCorrectInstances()
        {
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityInstanceRepo, _entityTypeRepository);
            ILazyDataLoader lazyDataLoader = Substitute.For<ILazyDataLoader>();

            var orchestrator = CreateRealOrchestrator(lazyDataLoader, _entityInstanceRepo);

            var anyMeasure = new Measure
            {
                Name = "Any measure",
                CalculationType = CalculationType.YesNo,
                Field = _type2Field,
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                BaseField = _type2Field,
                LegacyBaseValues = { Values = new[] { 3, 4, 5 } },
            };

            var filterMeasure = new Measure
            {
                Name = "Filter measure",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull($"max(response._type1Field()) and result.brand == {BrandInstance12.Id}"),
            };

            var profileInstances = new TargetInstances(Type2, new []{ Type2Instance6, Type2Instance7 });

            var calculationPeriod = new CalculationPeriod(DateTimeOffset.MinValue.AddDays(10), DateTimeOffset.MaxValue.AddDays(-10));

            var filter = new MetricFilter(Subset, filterMeasure, new EntityValueCombination(new EntityValue(Brand, BrandInstance12.Id)), new int[] { 1 });
            var actualTargets = await GetTargetsFromCalculateUnweighted(orchestrator, lazyDataLoader, calculationPeriod, anyMeasure, profileInstances, CancellationToken.None, filter);
            var actualTargetsValues = actualTargets.Select(t => (t.EntityType, t.SortedEntityInstanceIds));
            Assert.That(actualTargetsValues, Is.EquivalentTo(new[] { (Type2, new int[] { 6, 7 }), (Type1, new int[] { 1, 4 })}));
        }

        private static IGrouping<int, ResponseWeight>[] GetResponseIdsWithWeights(EntityWeightedDailyResults[] updatedRequestedResults)
        {
            var responseIds = updatedRequestedResults.SelectMany(r => r.WeightedDailyResults.SelectMany(w => w.ResponseIdsForDay));
            var responseWeights = responseIds.Select(id => new ResponseWeight(id, 1));
            return responseWeights.GroupBy(r => r.ResponseId).ToArray();
        }

        private void CreateMeasureResults()
        {
            var entityRequestedMeasureResults = new List<EntityWeightedDailyResults>();
            var entityInstance1 = new EntityInstance()
            {
                Id = 1,
                Name = "1",
                Identifier = "1"
            };
            var weightedDailyResults1 = new List<WeightedDailyResult>()
            {

                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 95,
                    UnweightedValueTotal = 19,
                    WeightedSampleSize = 95,
                    WeightedValueTotal= 19,
                    WeightedResult=0.02,
                    ResponseIdsForDay = _responseIds.GetRange(0, 95)
                }
            };
            var entityInstance2 = new EntityInstance()
            {
                Id = 2,
                Name = "2",
                Identifier = "2"
            };
            var weightedDailyResults2 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize = 27,
                    UnweightedValueTotal = 11,
                    WeightedSampleSize = 27,
                    WeightedValueTotal= 11,
                    WeightedResult=0.407,
                    ResponseIdsForDay = _responseIds.GetRange(0, 27)
                }
            };
            var entityInstance3 = new EntityInstance()
            {
                Id = 3,
                Name = "3",
                Identifier = "3"
            };
            var weightedDailyResults3 = new List<WeightedDailyResult>()
            {
                new WeightedDailyResult(DateTimeOffset.Now)
                {
                    UnweightedSampleSize =  16,
                    UnweightedValueTotal = 7,
                    WeightedSampleSize = 16,
                    WeightedValueTotal= 7,
                    WeightedResult=0.4375,
                    ResponseIdsForDay = _responseIds.GetRange(0, 16)
                }
            };

            entityRequestedMeasureResults.Add(new EntityWeightedDailyResults(entityInstance1, weightedDailyResults1));
            entityRequestedMeasureResults.Add(new EntityWeightedDailyResults(entityInstance2, weightedDailyResults2));
            entityRequestedMeasureResults.Add(new EntityWeightedDailyResults(entityInstance3, weightedDailyResults3));
            _entityRequestedMeasureResults = entityRequestedMeasureResults.ToArray();
        }

        private static async Task<IDataTarget[]> GetTargetsFromCalculateUnweighted(
            MetricCalculationOrchestrator orchestrator, ILazyDataLoader lazyDataLoader,
            CalculationPeriod calculationPeriod, Measure measure, TargetInstances profileInstances,
            CancellationToken cancellationToken, IFilter filter = null)
        {
            List<IDataTarget> actualTargets = new List<IDataTarget>();
            var call = lazyDataLoader.GetDataForFields(
                Arg.Any<Subset>(),
                Arg.Any<IReadOnlyCollection<ResponseFieldDescriptor>>(),
                Arg.Any<(DateTime startDate, DateTime endDate)?>(),
                Arg.Do<IDataTarget[]>(x => actualTargets.AddRange(x)), cancellationToken);

            TargetInstances[] filterInstances = Array.Empty<TargetInstances>();
            IFilter filter1 = filter ?? new AlwaysIncludeFilter();
            await orchestrator.CalculateUnweightedTotals(FilteredMetric.Create(measure, filterInstances, Subset, filter1), calculationPeriod, new AverageDescriptor(), profileInstances, GroupedQuotaCells.CreateUnfiltered((IEnumerable<QuotaCell>)Array.Empty<QuotaCell>()), cancellationToken);
            return actualTargets.ToArray();
        }

        private static MetricCalculationOrchestrator CreateRealOrchestrator(ILazyDataLoader lazyDataLoader, EntityInstanceRepository entityInstanceRepo)
        {
            var respondentRepositorySource = Substitute.For<IRespondentRepositorySource>();

            IBrandVueDataLoaderSettings settings = new ConfigurationSourcedLoaderSettings(new AppSettings());
            var respondentMeasureDataLoader = new RespondentDataLoader(lazyDataLoader, entityInstanceRepo, settings);
            var dataPresenceGuarantor = new DataPresenceGuarantor(lazyDataLoader, entityInstanceRepo, respondentMeasureDataLoader);
            var profileResponseAccessorFactory = Substitute.For<IProfileResponseAccessorFactory>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var inMemoryOrchestrator = new InMemoryTotalisationOrchestrator(respondentRepositorySource, dataPresenceGuarantor, profileResponseAccessorFactory, loggerFactory);
            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            var orchestrator = new MetricCalculationOrchestrator(
                profileResponseAccessorFactory,
                Substitute.For<IQuotaCellReferenceWeightingRepository>(),
                Substitute.For<ICalculationStageFactory>(),
                Substitute.For<IMeasureRepository>(),
                respondentRepositorySource,
                dataPresenceGuarantor,
                inMemoryOrchestrator,
                null,
                NullLogger.Instance,
                variableConfigurationRepository,
                null,
                Substitute.For<ITextCountCalculatorFactory>());

            return orchestrator;
        }
    }
}

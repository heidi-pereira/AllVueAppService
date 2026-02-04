using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.QuotaCells;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class MetricFiltererTests
    {
        private const string Field_AdvertisingAwareness = "Advertising_awareness";
        private const string Field_PositiveBuzz = "Positive_buzz";
        private const string Field_ShopperSegment = "Shopper_segment";
        private const string Field_CarouselAsked = "CarouselAsked";
        private const string Field_Carousel = "Carousel";
        private const string Field_Age = "Age";

        private EntityInstance _brand1;
        private EntityInstance _brand2;
        private EntityInstance _brand3;
        private EntityInstance _brand4;
        private Measure _measure1;
        private Measure _measure2;
        private Measure _measure3;
        private Measure _carouselMeasure;
        private ProfileResponseEntity _profileResponse;
        private TestResponseMonthsPopulator _testResponseMonthsPopulator;
        private Subset _subset;
        private EntityTypeRepository _entityTypeRepository;
        private EntityType _ageBandEntityType;
        private EntityType _carouselContentEntityType;
        private EntityType _carouselOptionsEntityType;
        private EntityValue[] _carouselEntityValuesWithAnswer;

        [OneTimeSetUp]
        public async Task BuildTestRespondent()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            _subset = TestResponseFactory.AllSubset;
            var subsets = new SubsetRepository { _subset };
            _entityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            _ageBandEntityType = AddEntity("AgeBand");
            _carouselContentEntityType = AddEntity("CarouselContent");
            _carouselOptionsEntityType = AddEntity("CarouselOptions");
            var responseFieldManager = new ResponseFieldManager(_entityTypeRepository);
            responseFieldManager.Add(Field_AdvertisingAwareness, _subset.Id, TestEntityTypeRepository.Brand);
            responseFieldManager.Add(Field_PositiveBuzz, _subset.Id, TestEntityTypeRepository.Brand);
            responseFieldManager.Add(Field_ShopperSegment, _subset.Id, TestEntityTypeRepository.Brand);
            responseFieldManager.Add(Field_CarouselAsked, _subset.Id, _carouselContentEntityType);
            responseFieldManager.Add(Field_Carousel, _subset.Id, _carouselContentEntityType, _carouselOptionsEntityType);
            responseFieldManager.Add("Age", _subset.Id);
            responseFieldManager.Add("Region", _subset.Id);
            responseFieldManager.Add("SEG1", _subset.Id);
            responseFieldManager.Add("SEG2", _subset.Id);
            responseFieldManager.Add("Country", _subset.Id);
            responseFieldManager.Add("Gender", _subset.Id);

            _testResponseMonthsPopulator = new TestResponseMonthsPopulator(responseFieldManager);

            var brandVueDataLoaderSettings = TestLoaderSettings.Default;
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var categoryMappingFactory = CategoryMappingFactory.CreateFrom(brandVueDataLoaderSettings, loggerFactory);
            var substituteLogger = Substitute.For<ILogger<JsonReferenceWeightingFactory>>();
            var quotaCellTargetFactory = new JsonReferenceWeightingFactory(brandVueDataLoaderSettings, substituteLogger);
            var weightingChecker = subsets.ToDictionary(s => s, subset => quotaCellTargetFactory.LoadOrNullIfNotExists(subset));
            var categoryMapping = new MapFileQuotaCellDescriptionProvider(categoryMappingFactory, subsets);

            _brand1 = new EntityInstance {Id = 1, Name = "Inappropriate Secret Santa Gifts"};
            _brand2 = new EntityInstance {Id = 2, Name = "SFW Secret Santa Gifts"};
            _brand3 = new EntityInstance {Id = 3, Name = "Boring Secret Santa Gifts"};
            _brand4 = new EntityInstance {Id = 4, Name = "The worst Secret Santa Gifts?"};

            _measure1 = new Measure()
            {
                Name = "Positive Buzz",
                CalculationType = CalculationType.YesNo,
                Field = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_PositiveBuzz),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                BaseField = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_ShopperSegment),
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5 } },
            };

            _measure2 = new Measure()
            {
                Name = "Advertising Awareness",
                CalculationType = CalculationType.YesNo,
                Field = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_AdvertisingAwareness),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                BaseField = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_ShopperSegment),
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5 } },
            };

            _measure3 = new Measure()
            {
                Name = "Age",
                CalculationType = CalculationType.Average,
                Field = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_Age),
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 16, Maximum = 74 },
            };

            _carouselMeasure = new Measure()
            {
                Name = "Carousel",
                CalculationType = CalculationType.YesNo,
                Field = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_Carousel),
                BaseField = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_CarouselAsked),
                LegacyBaseValues =
                {
                    Minimum = 1,
                    Maximum = 999,
                },
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 1, Maximum = 999 },
            };

            const int responseId = 123;
            var responseDatetime = DateTimeOffset.Now;
            var quotaCellFieldValues = new Dictionary<ResponseFieldDescriptor, int>
            {
                { _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(RespondentFields.Age), 35 },
                { _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(RespondentFields.Region), 1 },
                { _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(RespondentFields.SocioEconomicGroup1), 1 },
                { _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(RespondentFields.Country), 1 },
                { _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(RespondentFields.Gender), 0 }
            };

            var lazyDataLoader = Substitute.For<ILazyDataLoader>();
            lazyDataLoader.GetResponses(_subset, Arg.Any<IReadOnlyCollection<ResponseFieldDescriptor>>())
                .Returns(c => new ResponseFieldData(responseId, responseDatetime, -1, quotaCellFieldValues).Yield());

            var fieldsOnlyRespondentRepositoryFactory = new FieldsOnlyRespondentRepositoryFactory(_testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager, lazyDataLoader, 
                new NullLogger<FieldsOnlyRespondentRepositoryFactory>(), brandVueDataLoaderSettings.AppSettings, Substitute.For<IProductContext>(), categoryMapping, weightingChecker);

            var respondentRepo = await fieldsOnlyRespondentRepositoryFactory.CreateRespondentRepository(_subset, null, cancellationToken);

            var entityValues1 = new[] {new EntityValue(TestEntityTypeRepository.Brand, _brand1.Id)};
            var entityValues2 = new[] {new EntityValue(TestEntityTypeRepository.Brand, _brand2.Id)};
            var entityValues3 = new[] {new EntityValue(TestEntityTypeRepository.Brand, _brand3.Id)};

            _profileResponse = respondentRepo.GetRespondentsForDay(responseDatetime.ToDateInstance().Ticks).Single().ProfileResponseEntity;
            _carouselEntityValuesWithAnswer = new[]{new EntityValue(_carouselContentEntityType, 3), new EntityValue(_carouselOptionsEntityType, 4)};
            _testResponseMonthsPopulator.TestResponseFactory.WithFieldValues(_profileResponse, new (string FieldName, int Value, IEnumerable<EntityValue>)[]
            {
                (Field_ShopperSegment, 1, entityValues1),
                (Field_PositiveBuzz, 1, entityValues1),
                (Field_AdvertisingAwareness, 0, entityValues1),
                (Field_ShopperSegment, 1, entityValues2),
                (Field_PositiveBuzz, 0, entityValues2),
                (Field_AdvertisingAwareness, 1, entityValues2),
                (Field_ShopperSegment, 1, entityValues3),
                (Field_CarouselAsked, 3, new[]{new EntityValue(_carouselContentEntityType, 3)}),
                (Field_Carousel, 4, _carouselEntityValuesWithAnswer),
            });
        }

        private EntityType AddEntity(string displayNameSingular)
        {
            var type = new EntityType(displayNameSingular, displayNameSingular, displayNameSingular + "s");
            _entityTypeRepository.TryAdd(type.Identifier, type);
            return type;
        }

        [Test]
        public void Should_include_measure_value_that_matches_filter()
        {
            //  What I want is advertising awareness for
            //  brand 2 amongst respondents who have heard
            //  positive buzz about brand 1.

            var filter = CreateBrandFilter(_measure1, _brand1.Id, new[] {1});

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand2));

            Assert.That(
                include,
                Is.True, "Should have included response.");
        }

        [Test]
        public void Should_include_measure_value_that_matches_multi_entity_filter()
        {
            //  What I want is advertising awareness for
            //  brand 2 amongst respondents who have a 
            //  carousel answer of 4 for content 3 option 4

            var filter = new MetricFilter(_subset, _carouselMeasure, new EntityValueCombination(_carouselEntityValuesWithAnswer), new[]{4});

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand2));

            Assert.That(
                include,
                Is.True, "Should have included response.");
        }

        [Test]
        public void Should_exclude_measure_value_that_does_not_match_multi_entity_filter()
        {
            int[] primaryValues = new[] { int.MinValue, int.MaxValue };
            var valuesResponseIsNotDefinedFor = new EntityValueCombination(_carouselEntityValuesWithAnswer.Select(x => new EntityValue(x.EntityType, x.Value + 1)));
            var filter = new MetricFilter(_subset, _carouselMeasure, valuesResponseIsNotDefinedFor, primaryValues, false, treatPrimaryValuesAsRange: true);
            var include = filter.CreateForEntityValues(new EntityValueCombination())(_profileResponse);

            Assert.That(include, Is.False, "Should not have included response.");
        }

        [Test]
        public void Should_exclude_measure_value_that_does_match_multi_entity_filter_true()
        {
            int[] primaryValues = new[] {int.MinValue, int.MaxValue};
            var valuesResponseIsDefinedFor = new EntityValueCombination(_carouselEntityValuesWithAnswer);
            var filter = new MetricFilter(_subset, _carouselMeasure, valuesResponseIsDefinedFor, primaryValues, false, treatPrimaryValuesAsRange: true);
            var include = filter.CreateForEntityValues(new EntityValueCombination())(_profileResponse);

            Assert.That(include, Is.True, "Should have included response.");
        }
        
        [Test]
        public void Should_include_measure_value_that_does_not_match_filter_with_not_operation_applied()
        {
            //  What I want is advertising awareness for
            //  brand 2 amongst respondents who are not
            //  aware of adverts for brand 1.

            var filter = CreateBrandFilter(_measure2, _brand1.Id, new[] {1}, invert: true);

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand2));

            Assert.That(
                include,
                Is.True, "Should have included response.");
        }

        [Test]
        public void Should_exclude_measure_value_that_does_not_match_filter()
        {
            //  What I want is advertising awareness for
            //  brand 2 amongst respondents who are
            //  also aware of adverts for brand 1.

            var filter = CreateBrandFilter(_measure2, _brand1.Id, new[] {1});

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand2));

            Assert.That(
                include,
                Is.False, "Should not have included response.");
        }

        [Test]
        public void Should_exclude_measure_value_that_matches_filter_with_not_operation_applied()
        {
            //  What I want is advertising awareness for
            //  brand 2 amongst respondents who have not heard
            //  positive buzz about brand 1.

            var filter = CreateBrandFilter(_measure1, _brand1.Id, new[] {1}, invert: true);

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand2));

            Assert.That(
                include,
                Is.False, "Should not have included response.");
        }

        [Test]
        public void Inverted_filter_should_include_response_without_answer()
        {
            //  What I want is positive buzz for
            //  brand 1 amongst respondents who have not heard
            //  positive buzz about brand 3.

            var filter = CreateBrandFilter(_measure1, _brand3.Id, new[] {1}, invert: true);

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand1));

            Assert.That(
                include,
                Is.True, "Should have included response.");
        }

        [Test]
        public void Inverted_filter_should_exclude_response_without_base()
        {
            //  What I want is positive buzz for
            //  brand 1 amongst respondents who have not heard
            //  positive buzz about brand 4.

            var filter = CreateBrandFilter(_measure1, _brand4.Id, new[] {1}, invert: true);

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand1));

            Assert.That(
                include,
                Is.False, "Should not have included response.");
        }

        [Test]
        public void Range_filter_should_exclude_response_out_of_range()
        {
            //  What I want is positive buzz for
            //  brand 1 amongst respondents between the age
            //  of 40 and 50.

            var filter = CreateProfileFilter(_measure3, new []{40, 60}, invert: false, isRange: true);

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand1));

            Assert.That(
                include,
                Is.False, "Should not have included response.");
        }

        [Test]
        public void Filter_with_value_list_should_work_with_measure_with_value_range()
        {
            //  What I want is positive buzz for
            //  brand 1 amongst respondents in Millennial generation

            var generationMeasure = new Measure
            {
                Name = "Generation",
                CalculationType = CalculationType.YesNo,
                Field = _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(Field_Age),
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 16, Maximum = 74 },
                LegacyBaseValues =
                {
                    Minimum = 16,
                    Maximum = 74,
                },
                FilterValueMapping = "16,17,18,19,20,21,22,23,24:Generation Z|25,26,27,28,29,30,31,32,33,34,35,36,37,38,39:Millennials|40,41,42,43,44,45,46,47,48,49,50,51,52,53,54:Generation X|55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73:Baby Boomers|74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100:Silent Generation"
            };

            var filterValueList = new []{25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39};
            var filter = CreateProfileFilter(generationMeasure, filterValueList, invert: false, isRange: false);

            var include = IsResponseIncluded(filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand1));

            Assert.That(
                include,
                Is.True, "Should have included response.");
        }

        [Test]
        public void Filter_using_field_expression_measure()
        {
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager, Substitute.For<IEntityRepository>(), _entityTypeRepository);

            const string ageBandExpression = "(((18 <= Age and Age <= 30) or Age in []) and result.AgeBand == 1 and 1) or (((31 <= Age and Age <= 60) or Age in []) and result.AgeBand == 2 and 2)";
            var measure = new Measure
            {
                Name = "Age Band",
                CalculationType = CalculationType.YesNo,
                PrimaryVariable = fieldExpressionParser.ParseUserNumericExpressionOrNull(ageBandExpression),
                BaseExpression = fieldExpressionParser.ParseUserBooleanExpression("Age != None")
            };

            const int under30InstanceId = 1;
            const int over30InstanceId = 2;

            var under30Filter = CreateEntityFilter(measure, _ageBandEntityType, under30InstanceId, new [] {under30InstanceId}, invert: false, isRange: false);
            var over30Filter = CreateEntityFilter(measure, _ageBandEntityType, over30InstanceId, new [] {over30InstanceId}, invert: false, isRange: false);

            bool includeIfUnder30 = IsResponseIncluded(under30Filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand1));
            bool includeIfOver30 = IsResponseIncluded(over30Filter, ResultEntityValue(TestEntityTypeRepository.Brand, _brand1));

            Assert.That(includeIfUnder30, Is.False, "Should not have included response.");
            Assert.That(includeIfOver30, Is.True, "Should have included response.");
        }

        private MetricFilter CreateBrandFilter(Measure measure, int brandId, int[] primaryValues, bool invert = false)
        {
            return new MetricFilter(_subset, measure, new EntityValueCombination(new EntityValue(TestEntityTypeRepository.Brand, brandId)), primaryValues, invert);
        }

        private MetricFilter CreateProfileFilter(Measure measure, int[] primaryValues, bool invert = false, bool isRange = false)
        {
            return new MetricFilter(_subset, measure, default, primaryValues, invert, isRange);
        }

        private MetricFilter CreateEntityFilter(Measure measure, EntityType entityType, int entityId, int[] primaryValues, bool invert = false, bool isRange = false)
        {
            return new MetricFilter(_subset, measure, new EntityValueCombination (new EntityValue(entityType, entityId)), primaryValues, invert, isRange);
        }

        private bool IsResponseIncluded(MetricFilter filter, EntityValue resultEntityValue)
        {
            return filter.CreateForEntityValues(new EntityValueCombination (resultEntityValue))(_profileResponse);
        }

        private static EntityValue ResultEntityValue(EntityType type, EntityInstance instance)
        {
            return new EntityValue(type, instance.Id);
        }
    }
}

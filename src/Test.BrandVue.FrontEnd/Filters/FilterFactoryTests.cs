using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.Filters
{
    [TestFixture]
    public class FilterFactoryTests
    {
        private const string Shopper_Segment_Field = nameof(Shopper_Segment_Field);
        private const string Image_Field = nameof(Image_Field);
        private const string PositiveBuzz_Field = nameof(PositiveBuzz_Field);
        private const string Image_Boring_Field = nameof(Image_Boring_Field);

        private Measure _resultBrandMeasure;
        private Measure _imageMeasure;
        private ProfileResponseEntity _profileResponse;
        private TestResponseMonthsPopulator _testResponseMonthsPopulator;
        private Subset _subset;
        private EntityType _imageEntityType;
        private EntityValue[] _imageBrandEntityValuesWithAnswer;
        private static readonly EntityType BrandEntityType = TestEntityTypeRepository.Brand;
        private Measure _imageBoringMeasure;
        private EntityValue _brandWithImageAnswers;
        private EntityValue _imageWithAnswers;
        private FilterFactory _filterFactory;
        private const int ResponseImageValue = 1;
        private IUserDataPermissionsOrchestrator _userDataPermissionsOrchestrator = Substitute.For<IUserDataPermissionsOrchestrator>();

        [OneTimeSetUp]
        public async Task BuildTestRespondent()
        {
            var subsets = new FallbackSubsetRepository();
            _subset = subsets.Get("UK");
            _imageEntityType = CreateEntityType(nameof(_imageEntityType));
            var responseFieldManager = CreateResponseFieldManager(BrandEntityType, _imageEntityType);
            responseFieldManager.AddAllWithTypes([BrandEntityType], Shopper_Segment_Field, PositiveBuzz_Field, Image_Boring_Field);
            responseFieldManager.AddAllWithTypes([BrandEntityType, _imageEntityType], Image_Field);
            _testResponseMonthsPopulator = new TestResponseMonthsPopulator(responseFieldManager);

            _resultBrandMeasure = new Measure()
            {
                Name = nameof(_resultBrandMeasure), CalculationType = CalculationType.YesNo,
                Field = GetField(PositiveBuzz_Field), LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                BaseField = GetField(Shopper_Segment_Field), LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5 } },
            };

            _imageMeasure = new Measure()
            {
                Name = nameof(_imageMeasure), CalculationType = CalculationType.YesNo,
                Field = GetField(Image_Field), LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 1, Maximum = 1 },
                BaseField = GetField(Shopper_Segment_Field), LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5 } },
            };

            _imageBoringMeasure = new Measure()
            {
                Name = nameof(_imageBoringMeasure), CalculationType = CalculationType.YesNo,
                Field = GetField(Image_Boring_Field), LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 1, Maximum = 1 },
                BaseField = GetField(Shopper_Segment_Field), LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5 } },
                
            };
            _filterFactory = CreateRealFilterFactory(_imageMeasure, _imageBoringMeasure, _resultBrandMeasure);

            _profileResponse = await BuildRespondent(123, DateTimeOffset.Parse("2024/02/02"));
            _brandWithImageAnswers = new EntityValue(BrandEntityType, 3);
            _imageWithAnswers = new EntityValue(_imageEntityType, 2); // Image "Boring"
            _imageBrandEntityValuesWithAnswer = [_brandWithImageAnswers, _imageWithAnswers];
            _testResponseMonthsPopulator.TestResponseFactory.WithFieldValues(_profileResponse,
                new (string FieldName, int Value, IEnumerable<EntityValue>)[]
                {
                    (Shopper_Segment_Field, 1, [_brandWithImageAnswers]),
                    (Image_Boring_Field, ResponseImageValue, [_brandWithImageAnswers]),
                    (Image_Field, ResponseImageValue, _imageBrandEntityValuesWithAnswer),
                });
        }

        [Test]
        [TestCase(new[] { ResponseImageValue }, true)]
        [TestCase(new[] { ResponseImageValue, ResponseImageValue - 100 }, true)]
        [TestCase(new[] { ResponseImageValue - 100 }, false)]
        public void Explicit_SingleEntityTypeFilter_SingleInstance(int[] filterByImageValues, bool expectedToInclude)
        {
            var entityInstances =
                new[] { _brandWithImageAnswers }.ToDictionary(x => x.EntityType.Identifier, x => new[] { x.Value });
            var filterToTest = CreateFilterWhereAnswersExist(filterByImageValues, entityInstances, _resultBrandMeasure, _imageBoringMeasure);

            var actuallyWasIncluded = IsResponseIncluded(filterToTest, _brandWithImageAnswers);

            Assert.That(actuallyWasIncluded, Is.EqualTo(expectedToInclude), "Should have included response.");
        }

        [Test]
        [TestCase(new[] { ResponseImageValue }, true)]
        [TestCase(new[] { ResponseImageValue, ResponseImageValue - 100 }, true)]
        [TestCase(new[] { ResponseImageValue - 100 }, false)]
        public void Implicit_SingleEntityTypeFilter_SingleInstance(int[] filterByImageValues, bool expectedToInclude)
        {
            var entityInstances =
                new[] { _brandWithImageAnswers }.ToDictionary(x => x.EntityType.Identifier, x => new[] { -1 });
            var filterToTest = CreateFilterWhereAnswersExist(filterByImageValues, entityInstances, _resultBrandMeasure, _imageBoringMeasure);

            var actuallyWasIncluded = IsResponseIncluded(filterToTest, _brandWithImageAnswers);

            Assert.That(actuallyWasIncluded, Is.EqualTo(expectedToInclude), "Should have included response.");
        }

        [Test]
        [TestCase(new[] { ResponseImageValue }, true)]
        [TestCase(new[] { ResponseImageValue, ResponseImageValue - 100 }, true)]
        [TestCase(new[] { ResponseImageValue - 100 }, false)]
        public void Explicit_MultiEntityTypeFilter_SingleInstance(int[] filterByImageValues, bool expectedToInclude)
        {
            var entityInstances =
                _imageBrandEntityValuesWithAnswer.ToDictionary(x => x.EntityType.Identifier, x => new[] { x.Value });
            var filterToTest = CreateFilterWhereAnswersExist(filterByImageValues, entityInstances, _resultBrandMeasure, _imageMeasure);

            var actuallyWasIncluded = IsResponseIncluded(filterToTest, _brandWithImageAnswers);

            Assert.That(actuallyWasIncluded, Is.EqualTo(expectedToInclude), "Should have included response.");
        }

        [Test]
        [TestCase(new[] { ResponseImageValue }, true)]
        [TestCase(new[] { ResponseImageValue, ResponseImageValue - 100 }, true)]
        [TestCase(new[] { ResponseImageValue - 100 }, false)]
        public void Explicit_MultiEntityTypeFilter_MultiInstance(int[] filterByImageValues, bool expectedToInclude)
        {
            int imageInstanceIdWithNoAnswers = _imageWithAnswers.Value + 100;
            var entityInstances =
                _imageBrandEntityValuesWithAnswer.ToDictionary(x => x.EntityType.Identifier, x =>
                {
                    return x.EntityType != _imageEntityType ? new[] { x.Value } : [x.Value, imageInstanceIdWithNoAnswers];
                });
            var filterToTest = CreateFilterWhereAnswersExist(filterByImageValues, entityInstances, _resultBrandMeasure, _imageMeasure);

            var actuallyWasIncluded = IsResponseIncluded(filterToTest, _brandWithImageAnswers);

            Assert.That(actuallyWasIncluded, Is.EqualTo(expectedToInclude), "Should have included response.");
        }

        private IFilter CreateFilterWhereAnswersExist(int[] filterByImageValues, Dictionary<string, int[]> entityInstances, Measure measureForCalculation, Measure imageMeasure)
        {
            var measureFilterRequestModel = new MeasureFilterRequestModel(
                imageMeasure.Name, entityInstances, invert: false, treatPrimaryValuesAsRange: false,
                filterByImageValues
            );
            var compositeFilterModel = new CompositeFilterModel(FilterOperator.And, [measureFilterRequestModel]);
            var filterToTest =
                _filterFactory.CreateFilterForMeasure(compositeFilterModel, measureForCalculation, _subset);
            return filterToTest;
        }

        //TODO Test that we don't override the datatarget (with implicit filter value) for a database only entity that's supposed to be fixed

        private FilterFactory CreateRealFilterFactory(params Measure[] measures)
        {
            var measureRepository = new MetricRepository(_userDataPermissionsOrchestrator);
            foreach (var measure in measures)
            {
                measureRepository.TryAdd(measure.Name, measure);
            }

            var expresionSub = Substitute.For<IBaseExpressionGenerator>();
            expresionSub.GetMeasureWithOverriddenBaseExpression(default, default).ReturnsForAnyArgs(args => args.Arg<Measure>());
            return new FilterFactory(measureRepository, expresionSub);
        }

        private static ResponseFieldManager CreateResponseFieldManager(params EntityType[] entityTypes)
        {
            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            foreach (var responseEntityType in entityTypes)
            {
                responseEntityTypeRepository.TryAdd(responseEntityType.Identifier, responseEntityType);
            }
            var responseFieldManager = new ResponseFieldManager(responseEntityTypeRepository);
            return responseFieldManager;
        }

        private ResponseFieldDescriptor GetField(string fieldAdvertisingAwareness) =>
            _testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(fieldAdvertisingAwareness);

        private EntityType CreateEntityType(string displayNameSingular) =>
            new(displayNameSingular, displayNameSingular, displayNameSingular + "s");

        private bool IsResponseIncluded(IFilter filter, EntityValue resultEntityValue) =>
            filter.CreateForEntityValues(new EntityValueCombination (resultEntityValue))(_profileResponse);

        private async Task<ProfileResponseEntity> BuildRespondent(int responseId, DateTimeOffset responseDatetime) =>
            new(responseId, responseDatetime.ToDateInstance(), 1234);
    }
}

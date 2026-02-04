using BenchmarkDotNet.Attributes;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Middleware;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.CalculationPipeline;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using VerifyNUnit;
using DiffEngine;
using BrandVue.SourceData.AnswersMetadata;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Configuration;
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.Extensions.Logging;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Variable;
using Vue.Common.Auth;
using Vue.Common.Auth.Permissions;

namespace Test.BrandVue.Benchmarks
{
    [SimpleJob(RuntimeMoniker.Net80)]
    public class CrosstabBenchmarks
    {
        private const int NumRespondents = 200;
        private const int BrandCount = 50;
        private ICrosstabResultsProvider _crosstabResultsProvider;

        private static EntityType Brand = TestEntityTypeRepository.Brand;
        private static EntityType Product = TestEntityTypeRepository.Product;
        private static EntityType Gender = new("Gender", "Gender", "Genders");
        private static EntityType Region = TestEntityTypeRepository.Region;
        private static EntityType Ethnicity = new("Ethnicity", "Ethnicity", "Ethnicities");
        private static EntityType MultipleChoice = new("MultipleChoice", "MultipleChoice", "MultipleChoices");
        private static EntityType EntityWithZeroTenScale = new("EntityWithZeroTenScale", "EntityWithZeroTenScale", "EntityWithZeroTenScale");

        private static readonly EntityValue[] BrandEntities = Enumerable.Range(0, BrandCount).Select(id => new EntityValue(Brand, id)).ToArray();
        private static readonly EntityValue[] ProductEntities = Enumerable.Range(0, 15).Select(id => new EntityValue(Product, id)).ToArray();
        private static readonly EntityValue[] GenderEntities = Enumerable.Range(0, 4).Select(id => new EntityValue(Gender, id)).ToArray();
        private static readonly EntityValue[] RegionEntities = Enumerable.Range(0, 5).Select(id => new EntityValue(Region, id)).ToArray();
        private static readonly EntityValue[] EthnicityEntities = Enumerable.Range(0, 4).Select(id => new EntityValue(Ethnicity, id)).ToArray();
        private static readonly EntityValue[] MultipleChoiceEntities = Enumerable.Range(0, 10).Select(id => new EntityValue(MultipleChoice, id)).ToArray();
        private static readonly EntityValue[] EntityWithScaleEntities = Enumerable.Range(0, 4).Select(id => new EntityValue(EntityWithZeroTenScale, id)).ToArray();

        private ResponseFieldDescriptor _brandField;
        private ResponseFieldDescriptor _productField;
        private ResponseFieldDescriptor _genderField;
        private ResponseFieldDescriptor _ageField;
        private ResponseFieldDescriptor _regionField;
        private ResponseFieldDescriptor _ethnicityField;
        private ResponseFieldDescriptor _baseFieldAlwaysTrue;
        private ResponseFieldDescriptor _multipleChoiceField;
        private ResponseFieldDescriptor _entityWithZeroTenScaleField;
        private ResponseFieldDescriptor _entityWithoutScaleField;

        private Measure _brandMeasure;
        private Measure _productMeasure;
        private Measure _genderMeasure;
        private Measure _regionMeasure;
        private Measure _ethnicityMeasure;
        private Measure _brandProductMeasure;
        private Measure _multipleChoiceMeasure;
        private Measure _limitedBaseMeasure;
        private Measure _filterValueMappingMeasure;
        private Measure _measureWithBaseFieldEntityType;
        private Measure _entityOnlyMeasure;
        private Measure _ageByEntityMeasure;

        private Subset _subset;
        private Period _period;
        private PipelineResultsProvider _pipelinedResultsProvider;

        [GlobalSetup, OneTimeSetUp]
        public void Setup()
        {
            // TODO Don't ship this line
            DiffRunner.Disabled = true;
            CreateFields();
            CreateMeasures();
            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(default, default).ReturnsForAnyArgs(args => args.Arg<Measure>());

            var averageRepository = Averages.CreateDefaultRepo(false);
            var unweightedSingleDayAverage = Averages.SingleDayAverage.ShallowCopy();
            unweightedSingleDayAverage.AverageId = unweightedSingleDayAverage.AverageId + "_unweighted";
            unweightedSingleDayAverage.WeightingMethod = WeightingMethod.None;
            averageRepository.Add(unweightedSingleDayAverage);
            var calculationPeriod = new CalculationPeriod(DateTimeOffset.Parse("2020/12/01"), DateTimeOffset.Parse("2020/12/31").EndOfDay());
            var calculator = new ProductionCalculatorBuilder()
                .IncludeMeasures(new[] { _brandMeasure, _productMeasure, _genderMeasure, _regionMeasure, _ethnicityMeasure, _brandProductMeasure, _multipleChoiceMeasure, _limitedBaseMeasure, _filterValueMappingMeasure, _measureWithBaseFieldEntityType, _entityOnlyMeasure, _ageByEntityMeasure })
                .IncludeEntities(BrandEntities.Concat(ProductEntities).Concat(GenderEntities).Concat(RegionEntities).Concat(EthnicityEntities).Concat(MultipleChoiceEntities).Concat(EntityWithScaleEntities).ToArray())
                .WithAverage(unweightedSingleDayAverage)
                .WithCalculationPeriod(calculationPeriod)
                .WithResponses(CreateResponses().ToArray())
                .BuildRealCalculatorWithInMemoryDb();
            Brand = UpdateSlightlyDifferentVersionFromLoadedRepository(calculator, Brand);
            Product = UpdateSlightlyDifferentVersionFromLoadedRepository(calculator, Product);
            Gender = UpdateSlightlyDifferentVersionFromLoadedRepository(calculator, Gender);
            Region = UpdateSlightlyDifferentVersionFromLoadedRepository(calculator, Region);
            Ethnicity = UpdateSlightlyDifferentVersionFromLoadedRepository(calculator, Ethnicity);
            MultipleChoice = UpdateSlightlyDifferentVersionFromLoadedRepository(calculator, MultipleChoice);
            _subset = calculator.DataLoader.SubsetRepository.Single();
            _period = new Period
            {
                Average = unweightedSingleDayAverage.AverageId,
                ComparisonDates = calculationPeriod.Periods
            };

            var questionTypeLookupRepository = Substitute.For<IQuestionTypeLookupRepository>();
            var getForSubset = calculator.DataLoader.MeasureRepository
                    .GetAllForCurrentUser()
                    .ToDictionary(m => m.Name, m => m.Name == "_ageByEntityMeasure" ? MainQuestionType.Value : MainQuestionType.SingleChoice);
            questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(getForSubset);
            var userDataPermissionsService = Substitute.For<IUserDataPermissionsService>();
            userDataPermissionsService.GetDataPermission().Returns(null as DataPermissionDto);

            var requestAdapter = new RequestAdapter(
                calculator.DataLoader.SubsetRepository,
                averageRepository,
                calculator.DataLoader.MeasureRepository,
                calculator.DataLoader.EntityInstanceRepository,
                new DemographicFilterToQuotaCellMapper(calculator.DataLoader.RespondentRepositorySource),
                calculator.DataLoader.EntityTypeRepository,
                Substitute.For<IWeightingPlanRepository>(),
                Substitute.For<IFilterRepository>(),
                Substitute.For<IProductContext>(),
                baseExpressionGenerator,
                new RequestScope(),
                questionTypeLookupRepository,
                userDataPermissionsService);

            var settings = new AppSettings();
            var brandVueSettings = Substitute.For<IBrandVueDataLoaderSettings>();
            var config = new InitialWebAppConfig(settings, Substitute.For<IConfiguration>());
            var filterFactory = new FilterFactory(calculator.DataLoader.MeasureRepository, baseExpressionGenerator);
            var convenientCalculator = new ConvenientCalculator(
                calculator,
                calculator.DataLoader.MeasureRepository,
                config,
                filterFactory,
                calculator.DataLoader.EntityInstanceRepository);
            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();

            _crosstabResultsProvider = new CrosstabResultsProvider(
                calculator.DataLoader.SubsetRepository,
                calculator.DataLoader.MeasureRepository,
                calculator.DataLoader.EntityInstanceRepository,
                requestAdapter: requestAdapter,
                convenientCalculator,
                calculator.DataLoader.EntityTypeRepository,
                baseExpressionGenerator: baseExpressionGenerator,
                Substitute.For<IResultsProvider>(),
                settings,
                questionTypeLookupRepository,
                brandVueSettings,
                variableConfigurationRepository,
                Substitute.For<IVariableManager>());
            var logger = Substitute.For<ILogger<PipelineResultsProvider>>();


            _pipelinedResultsProvider = new PipelineResultsProvider(
                calculator,
                calculator.DataLoader.SubsetRepository,
                calculator.DataLoader.ProfileResponseAccessorFactory,
                Substitute.For<IBreakdownCategoryFactory>(),
                convenientCalculator,
                requestAdapter,
                Substitute.For<IBreakdownResultsProvider>(),
                calculator.ProductContext,
                calculator.DataLoader.TextResponseRepository,
                null,
                calculator.DataLoader.EntityInstanceRepository,
                calculator.DataLoader.ProfileResultsCalculator,
                calculator.DataLoader.MeasureRepository,
                settings,
                new RequestScope(),
                calculator.DataLoader.FilterRepository,
                calculator.DataLoader.MetricConfigurationRepository,
                calculator.DataLoader.VariableConfigurationRepository,
                logger
            );
        }

        private static EntityType UpdateSlightlyDifferentVersionFromLoadedRepository(
            ProductionCalculatorBuilder.ProductionCalculatorPlusConvenienceOverloads calculator,
            EntityType entityType) =>
            calculator.DataLoader.EntityTypeRepository.Single(r => string.Equals(r.Identifier, entityType.Identifier, StringComparison.OrdinalIgnoreCase));

        private void CreateFields()
        {
            var testResponseEntityTypeRepository = new TestEntityTypeRepository(Brand, Product, Gender, Region, Ethnicity);
            var fieldManager = new ResponseFieldManager(testResponseEntityTypeRepository);
            _baseFieldAlwaysTrue = fieldManager.Add($"Base_Field_Always_True");
            _brandField = fieldManager.Add(Brand.Identifier, Brand);
            _productField = fieldManager.Add(Product.Identifier, Product);
            _genderField = fieldManager.Add(Gender.Identifier, Gender);
            _ageField = fieldManager.Add("Age");
            _regionField = fieldManager.Add(Region.Identifier, Region);
            _ethnicityField = fieldManager.Add(Ethnicity.Identifier, Ethnicity);
            _multipleChoiceField = fieldManager.Add(MultipleChoice.Identifier, MultipleChoice);
            _entityWithZeroTenScaleField = fieldManager.Add(EntityWithZeroTenScale.Identifier + "_Scale", EntityWithZeroTenScale);
            _entityWithoutScaleField = fieldManager.Add(EntityWithZeroTenScale.Identifier + "_EntityOnly", EntityWithZeroTenScale);
        }

        private void CreateMeasures()
        {
            _brandMeasure = new Measure
            {
                Name = nameof(Brand),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _brandField,
                LegacyPrimaryTrueValues = { Values = BrandEntities.Select(e => e.Value).ToArray() },
            };
            _productMeasure = new Measure
            {
                Name = nameof(Product),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _productField,
                LegacyPrimaryTrueValues = { Values = ProductEntities.Select(e => e.Value).ToArray() },
            };
            _genderMeasure = new Measure
            {
                Name = nameof(Gender),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _genderField,
                LegacyPrimaryTrueValues = { Values = GenderEntities.Select(e => e.Value).ToArray() },
            };
            _regionMeasure = new Measure
            {
                Name = nameof(Region),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _regionField,
                LegacyPrimaryTrueValues = { Values = RegionEntities.Select(e => e.Value).ToArray() },
            };
            _ethnicityMeasure = new Measure
            {
                Name = nameof(Ethnicity),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _ethnicityField,
                LegacyPrimaryTrueValues = { Values = EthnicityEntities.Select(e => e.Value).ToArray() },
            };
            _brandProductMeasure = new Measure
            {
                Name = nameof(_brandProductMeasure),
                CalculationType = CalculationType.YesNo,
                BaseField = _productField,
                LegacyBaseValues = { Values = ProductEntities.Select(e => e.Value).ToArray() },
                Field = _brandField,
                LegacyPrimaryTrueValues = { Values = BrandEntities.Select(e => e.Value).ToArray() },
            };
            _multipleChoiceMeasure = new Measure
            {
                Name = nameof(MultipleChoice),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _multipleChoiceField,
                LegacyPrimaryTrueValues = { Values = MultipleChoiceEntities.Select(e => e.Value).ToArray() },
            };
            _limitedBaseMeasure = new Measure
            {
                Name = nameof(_limitedBaseMeasure),
                CalculationType = CalculationType.YesNo,
                BaseField = _genderField,
                LegacyBaseValues = { Values = GenderEntities.Take(2).Select(e => e.Value).ToArray() },
                Field = _genderField,
                LegacyPrimaryTrueValues = { Values = GenderEntities.Select(e => e.Value).ToArray() },
            };
            _filterValueMappingMeasure = new Measure
            {
                Name = nameof(_filterValueMappingMeasure),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _genderField,
                LegacyPrimaryTrueValues = { Values = GenderEntities.Select(e => e.Value).ToArray() },
                FilterValueMapping = "0:M|1:F|2:O|3:N|0,1:MF|!0,1:invert|1-3:range|!1-3:invertrange"
            };
            _measureWithBaseFieldEntityType = new Measure
            {
                Name = nameof(_measureWithBaseFieldEntityType),
                CalculationType = CalculationType.Average,
                BaseField = _entityWithoutScaleField,
                LegacyBaseValues = { Values = new[] { 0, 1 } }, //skip entity ID 2,3 
                Field = _entityWithZeroTenScaleField,
                LegacyPrimaryTrueValues = { Values = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } }, //0-10 scale values
                FilterValueMapping = "0:Main|1,2:Competitor|3:Other"
            };
            _entityOnlyMeasure = new Measure
            {
                Name = nameof(_entityOnlyMeasure),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _entityWithoutScaleField,
                LegacyPrimaryTrueValues = { Values = EntityWithScaleEntities.Select(e => e.Value).ToArray() },
            };
            _ageByEntityMeasure = new Measure
            {
                Name = nameof(_ageByEntityMeasure),
                CalculationType = CalculationType.Average,
                BaseField = _entityWithoutScaleField,
                LegacyBaseValues = { Values = EntityWithScaleEntities.Select(e => e.Value).ToArray() },
                Field = _ageField,
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 0, Maximum = 100 },
                GenerationType = AutoGenerationType.CreatedFromField,
            };
        }

        private IEnumerable<ResponseAnswers> CreateResponses()
        {
            var brandIndex = 0;
            var productIndex = 0;
            var genderIndex = 0;
            var regionIndex = 0;
            var ethnicityIndex = 0;
            var multipleChoiceIndex = 0;
            var entityWithScaleFieldIndex = 0;
            var scaleValue = 1;
            var ageValue = 21;
            for (var i = 0; i < NumRespondents; i++)
            {
                var multipleChoiceAnswers = MultipleChoiceEntities.Skip(multipleChoiceIndex).Take(5)
                    .Select(entityValue => TestCommon.DataPopulation.TestAnswer.For(_multipleChoiceField, entityValue.Value, entityValue));
                var answers = new[] {
                    TestCommon.DataPopulation.TestAnswer.For(_brandField, brandIndex, BrandEntities[brandIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_productField, productIndex, ProductEntities[productIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_genderField, genderIndex, GenderEntities[genderIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_regionField, regionIndex, RegionEntities[regionIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_ethnicityField, ethnicityIndex, EthnicityEntities[ethnicityIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_entityWithZeroTenScaleField, scaleValue, EntityWithScaleEntities[entityWithScaleFieldIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_entityWithoutScaleField, entityWithScaleFieldIndex, EntityWithScaleEntities[entityWithScaleFieldIndex]),
                    TestCommon.DataPopulation.TestAnswer.For(_ageField, ageValue),
                    TestCommon.DataPopulation.TestAnswer.For(_baseFieldAlwaysTrue, 0)
                }.Concat(multipleChoiceAnswers).ToArray();
                brandIndex = (brandIndex + 1) % BrandEntities.Length;
                productIndex = (productIndex + 1) % ProductEntities.Length;
                genderIndex = (genderIndex + 1) % GenderEntities.Length;
                regionIndex = (regionIndex + 1) % RegionEntities.Length;
                ethnicityIndex = (ethnicityIndex + 1) % EthnicityEntities.Length;
                multipleChoiceIndex = (multipleChoiceIndex + 1) % MultipleChoiceEntities.Length - 3;
                entityWithScaleFieldIndex = (entityWithScaleFieldIndex + 1) % EntityWithScaleEntities.Length;
                scaleValue = (scaleValue + 3) % 11;
                ageValue = (ageValue + 17) % 101;
                yield return new ResponseAnswers(answers);
            }
        }

        [Test]
        public async Task SingleEntityCrossTabTest() => await Verify(Map(await SingleEntityCrosstab()));

        [Benchmark, MinInvokeCount(60)]
        public async Task<CrosstabResults> SingleEntityCrosstab()
        {
            var model = GetSingleEntityRequestModel(_brandMeasure, BrandEntities.Select(e => e.Value).ToArray(), Array.Empty<CrossMeasure>());
            return (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
        }

        [Test]
        public async Task SingleEntityCrosstabWithBreaksTest() => await Verify(Map(await SingleEntityCrosstabWithBreaks()));

        [Benchmark, MinInvokeCount(10)]
        public async Task<CrosstabResults> SingleEntityCrosstabWithBreaks()
        {
            var model = GetSingleEntityRequestModel(_brandMeasure, BrandEntities.Select(e => e.Value).ToArray(), GetBreaks());
            return (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
        }

        [Test]
        public async Task SingleEntityCrosstabWithNestedBreaksTest() => await Verify(Map(await SingleEntityCrosstabWithNestedBreaks()));

        [Benchmark]
        public async Task<CrosstabResults> SingleEntityCrosstabWithNestedBreaks()
        {
            var model = GetSingleEntityRequestModel(_brandMeasure, BrandEntities.Select(e => e.Value).ToArray(), GetNestedBreaks());
            return (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
        }


        [Test]
        public async Task SingleEntityCrosstabWithMultipleChoiceBreakTest() => await Verify(Map(await SingleEntityCrosstabWithMultipleChoiceBreak()));

        [Benchmark]
        public async Task<CrosstabResults> SingleEntityCrosstabWithMultipleChoiceBreak()
        {
            var model = GetSingleEntityRequestModel(_brandMeasure, BrandEntities.Select(e => e.Value).ToArray(), GetMultipleChoiceNestedBreaks());
            return (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
        }

        [Test]
        public async Task SingleEntityCrosstabWithLimitedBaseBreakTest() => await Verify(Map(await SingleEntityCrosstabWithLimitedBaseBreak()));

        [Benchmark]
        public async Task<CrosstabResults> SingleEntityCrosstabWithLimitedBaseBreak()
        {
            var model = GetSingleEntityRequestModel(_brandMeasure, BrandEntities.Select(e => e.Value).ToArray(), new[]
            {
                new CrossMeasure
                {
                    MeasureName = _limitedBaseMeasure.Name
                }
            });
            return (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
        }

        [Test]
        public async Task SingleEntityCrosstabWithFilterValueMappingBreakTest() => await Verify(Map(await SingleEntityCrosstabWithFilterValueMappingBreak()));

        [Benchmark]
        public async Task<CrosstabResults> SingleEntityCrosstabWithFilterValueMappingBreak()
        {
            var model = GetSingleEntityRequestModel(_brandMeasure, BrandEntities.Select(e => e.Value).ToArray(), new[]
            {
                new CrossMeasure
                {
                    MeasureName = _filterValueMappingMeasure.Name
                }
            });
            return (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
        }

        [Test]
        public async Task SingleEntityCrosstabWithEntityBaseFieldBreak()
        {
            /*  The behaviour here being tested here may actually be bugged - in CrosstabFilterModelFactory.GetMappedFilters the generated filters
                expect both the entity ID and response value to be the same, which for this example is not the case (except on some coincidental clashes).
                The entity IDs are 0-4 while the response values are scale 0-10. If this behaviour is fixed then the test results would be expected to change
            */
            var model = GetSingleEntityRequestModel(_entityOnlyMeasure, EntityWithScaleEntities.Select(e => e.Value).ToArray(), new[]
            {
                new CrossMeasure
                {
                    MeasureName = _measureWithBaseFieldEntityType.Name
                }
            });
            var results = await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None);
            await Verify(Map(results));
        }

        [Test]
        public async Task SingleEntityCrosstabWithProfileFieldEntityBaseFieldBreak()
        {
            var model = GetSingleEntityRequestModel(_ageByEntityMeasure, EntityWithScaleEntities.Select(e => e.Value).ToArray(), new[]
            {
                new CrossMeasure
                {
                    MeasureName = _ageByEntityMeasure.Name
                }
            });
            var results = await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None);
            await Verify(Map(results));
        }

        [Test]
        public async Task MultiEntityCrosstabTest() => await Verify(Map(await MultiEntityCrosstab()));

        [Benchmark, MinInvokeCount(7)]
        public async Task<CrosstabResults[]> MultiEntityCrosstab()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var filterInstances = new[] { new EntityInstanceRequest(nameof(Product), ProductEntities.Select(e => e.Value).ToArray()) };
            var model = GetMultiEntityCrosstabRequestModel(_brandProductMeasure, primaryInstances, filterInstances, Array.Empty<CrossMeasure>());
            return await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None);
        }

        [Test]
        public async Task MultiEntityCrosstabWithBreaksTest() => await Verify(Map(await MultiEntityCrosstabWithBreaks()));

        [Benchmark]
        public async Task<CrosstabResults[]> MultiEntityCrosstabWithBreaks()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var filterInstances = new[] { new EntityInstanceRequest(nameof(Product), ProductEntities.Select(e => e.Value).ToArray()) };
            var model = GetMultiEntityCrosstabRequestModel(_brandProductMeasure, primaryInstances, filterInstances, GetBreaks());
            return await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None);
        }

        [Test]
        public async Task MultiEntityCrosstabWithNestedBreaksTest() => await Verify(Map(await MultiEntityCrosstabWithNestedBreaks()));

        [Benchmark]
        public async Task<CrosstabResults[]> MultiEntityCrosstabWithNestedBreaks()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var filterInstances = new[] { new EntityInstanceRequest(nameof(Product), ProductEntities.Select(e => e.Value).ToArray()) };
            var model = GetMultiEntityCrosstabRequestModel(_brandProductMeasure, primaryInstances, filterInstances, GetNestedBreaks());
            return await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None);
        }

        [Test]
        public async Task GetGroupedCompetitionResultsWithCrossbreakFiltersTest() => await Verify(Map(await GetGroupedCompetitionResultsWithCrossbreakFilters()));


        [Benchmark]
        public async Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFilters()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var model = GetCuratedResultsModelWithCrossbreaks(_brandMeasure, primaryInstances, GetNestedBreaks());

            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            return await _pipelinedResultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(model.CuratedResultsModel, breaks, CancellationToken.None, model.Breaks);
        }

        [Test]
        public async Task GetGroupedCompetitionResultsWithCrossbreakFiltersLegacyTest() => await Verify(Map(await GetGroupedCompetitionResultsWithCrossbreakFiltersLegacy()));


        [Benchmark]
        public async Task<GroupedCrossbreakCompetitionResults> GetGroupedCompetitionResultsWithCrossbreakFiltersLegacy()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var model = GetCuratedResultsModelWithCrossbreaks(_brandMeasure, primaryInstances, GetNestedBreaks());

            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            return await _pipelinedResultsProvider.GetGroupedCompetitionResultsWithCrossbreakFiltersLegacy(model.CuratedResultsModel, breaks, CancellationToken.None, model.Breaks);
        }

        [Test]
        public async Task GetMultiEntityGroupedCompetitionResultsWithCrossbreakFiltersTest() => await Verify(Map(await GetGroupedCompetitionResultsWithCrossbreakFilters()));

        [Benchmark]
        public async Task<GroupedCrossbreakCompetitionResults>
            GetMultiEntityGroupedCompetitionResultsWithCrossbreakFilters()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var model = GetMultiEntityRequestModelWithCrossbreaks(_brandProductMeasure, primaryInstances, new[]
            {
                new EntityInstanceRequest(Product.Identifier, new []{ ProductEntities.First().Value})
            }, GetBreaks());

            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            return await _pipelinedResultsProvider.GetGroupedCompetitionResultsWithCrossbreakFilters(model.MultiEntityRequestModel, breaks, CancellationToken.None, model.Breaks);
        }

        [Test]
        public async Task GetMultiEntityGroupedCompetitionResultsWithCrossbreakFiltersLegacyTest() => await Verify(Map(await GetMultiEntityGroupedCompetitionResultsWithCrossbreakFiltersLegacy()));

        [Benchmark]
        public async Task<GroupedCrossbreakCompetitionResults>
            GetMultiEntityGroupedCompetitionResultsWithCrossbreakFiltersLegacy()
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, BrandEntities.Select(e => e.Value).ToArray());
            var model = GetMultiEntityRequestModelWithCrossbreaks(_brandProductMeasure, primaryInstances, new[]
            {
                new EntityInstanceRequest(Product.Identifier, new []{ProductEntities.First().Value})
            }, GetBreaks());

            var breaks = _crosstabResultsProvider.GetGroupedFlattenedBreaks(model.Breaks, model.SubsetId);
            return await _pipelinedResultsProvider.GroupedCompetitionResultsWithCrossbreakFiltersLegacy(model.MultiEntityRequestModel, breaks, model.Breaks, CancellationToken.None);
        }

        private MultiEntityRequestModelWithCrossbreaks GetMultiEntityRequestModelWithCrossbreaks(Measure measure,
            EntityInstanceRequest primaryInstances, EntityInstanceRequest[] singleInstanceRequests,
            CrossMeasure[] breaks)
        {
            var activeBrandId = primaryInstances.Type == Brand.Identifier ? primaryInstances.EntityInstanceIds[0] : -1;

            var measureFilterRequestModels = Array.Empty<MeasureFilterRequestModel>();
            var multiEntityRequestModel = new MultiEntityRequestModel(measure.Name, _subset.Id,
                _period, primaryInstances, singleInstanceRequests,
                new DemographicFilter(new FilterRepository()),
                new CompositeFilterModel(),
                measureFilterRequestModels, Array.Empty<BaseExpressionDefinition>(),
                true,
                SigConfidenceLevel.NinetyFive,
                activeBrandId);
            var result = new MultiEntityRequestModelWithCrossbreaks(multiEntityRequestModel, breaks);

            return result;
        }

        private CrossMeasure[] GetBreaks()
        {
            return new[]
            {
                new CrossMeasure { MeasureName = _genderMeasure.Name },
                new CrossMeasure { MeasureName = _regionMeasure.Name },
                new CrossMeasure { MeasureName = _ethnicityMeasure.Name }
            };
        }

        private CrossMeasure[] GetNestedBreaks()
        {
            return new[]
            {
                new CrossMeasure
                {
                    MeasureName = _genderMeasure.Name,
                    ChildMeasures = new[]
                    {
                        new CrossMeasure
                        {
                            MeasureName = _regionMeasure.Name,
                            ChildMeasures = new[]
                            {
                                new CrossMeasure
                                {
                                    MeasureName = _ethnicityMeasure.Name
                                }
                            }
                        }
                    }
                }
            };
        }

        private CrossMeasure[] GetMultipleChoiceNestedBreaks()
        {
            return new[]
            {
                new CrossMeasure
                {
                    MeasureName = _genderMeasure.Name,
                    ChildMeasures = new []
                    {
                        new CrossMeasure
                        {
                            MeasureName = _multipleChoiceMeasure.Name
                        }
                    }
                }
            };
        }

        private CrosstabRequestModel GetSingleEntityRequestModel(Measure measure, int[] entityInstanceIds, CrossMeasure[] breaks)
        {
            var primaryInstances = new EntityInstanceRequest(Brand.Identifier, entityInstanceIds);
            var activeBrandId = measure.EntityCombination.Any(t => t.Identifier == Brand.Identifier) ? entityInstanceIds[0] : -1;
            return new CrosstabRequestModel(
                measure.Name,
                _subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: _period,
                breaks,
                activeBrandId,
                new DemographicFilter(),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions {CalculateSignificance = true, SignificanceType = CrosstabSignificanceType.CompareToTotal, SigConfidenceLevel = SigConfidenceLevel.NinetyFive});
        }

        private CrosstabRequestModel GetMultiEntityCrosstabRequestModel(Measure measure, EntityInstanceRequest primaryInstances, EntityInstanceRequest[] filterInstances, CrossMeasure[] breaks)
        {
            var activeBrandId = primaryInstances.Type == Brand.Identifier ? primaryInstances.EntityInstanceIds[0] : -1;
            return new CrosstabRequestModel(
                measure.Name,
                _subset.Id,
                primaryInstances,
                filterInstances,
                _period,
                breaks,
                activeBrandId,
                new DemographicFilter(),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions { CalculateSignificance = true, SignificanceType = CrosstabSignificanceType.CompareWithinBreak, SigConfidenceLevel = SigConfidenceLevel.NinetyFive });
        }

        private CuratedResultsModelWithCrossbreaks GetCuratedResultsModelWithCrossbreaks(Measure measure, EntityInstanceRequest primaryInstances, CrossMeasure[] breaks)
        {
            var activeBrandId = primaryInstances.Type == Brand.Identifier ? primaryInstances.EntityInstanceIds[0] : -1;
            var sigOptions = new SigDiffOptions(true, SigConfidenceLevel.NinetyFive, DisplaySignificanceDifferences.ShowBoth, CrosstabSignificanceType.CompareToTotal);

            var curatedModel = new CuratedResultsModel(new DemographicFilter(),
                primaryInstances.EntityInstanceIds,
                _subset.Id,
                measure.Name.Yield().ToArray(),
                _period,
                activeBrandId,
                new CompositeFilterModel(),
                sigOptions);
            return new CuratedResultsModelWithCrossbreaks(curatedModel, breaks);
        }

        private async Task Verify(object o) => await Verifier.Verify(o);

        private string Map(GroupedCrossbreakCompetitionResults results)
        {
            var resultsLines = results.GroupedBreakResults
                .SelectMany(x => x.BreakResults.InstanceResults,
                    (g, b) => (GroupName: g.GroupName, BreakName: b.BreakName, Results: b.EntityResults)
                ).SelectMany(x => x.Results,
                    (g, r) => $"{g.GroupName}_{g.BreakName}\r\n{Map(r)}"
                );

            var lowSampleSummaryLines = results.GroupedBreakResults
                .SelectMany(x => x.BreakResults.LowSampleSummary.Where(x => x.DateTime.HasValue),
                    (g, b) => (GroupName: g.GroupName, BreakName: b.Name,
                        Results: $"{b.DateTime:yyyy-M-d}_{b.Metric}_{b.Name} ({b.EntityInstanceId})")
                );
            return string.Join("\r\n", resultsLines) + "\r\n" + string.Join("\r\n", lowSampleSummaryLines);
        }

        private string Map(EntityWeightedDailyResults dailyResults)
        {
            return $"{dailyResults.EntityInstance.Name} ({dailyResults.EntityInstance.Id})_{dailyResults.UnweightedResponseCount}" +
                   $"\r\n{string.Join("\r\n  ", dailyResults.WeightedDailyResults.Select(Map))}";
        }

        private string Map(WeightedDailyResult r)
        {
            if (r.ChildResults is not null) throw new NotSupportedException("Map doesn't handle child results");
            return $"{r.Date:yyyy-M-d}_{r.WeightedResult}_{r.UnweightedSampleSize}_{r.WeightedSampleSize}_{r.Significance}_{r.Tscore}";
        }

        private SimpleCrosstabResultsArray Map(CrosstabResults[] results)
        {
            return new SimpleCrosstabResultsArray
            {
                Results = results.Select(Map).ToArray()
            };
        }

        private SimpleCrosstabResults Map(CrosstabResults results)
        {
            return new SimpleCrosstabResults
            {
                Categories = results.Categories.Select(Map).ToArray(),
                InstanceResults = results.InstanceResults.Select(Map).ToArray(),
            };
        }

        private SimpleCategory Map(CrosstabCategory category)
        {
            return new SimpleCategory
            {
                Id = category.Id,
                Name = category.Name,
                SubCategories = category.SubCategories.Select(Map).ToArray()
            };
        }

        private string Map(InstanceResult result)
        {
            return $"{result.EntityInstance.Id}_{result.EntityInstance.Name} - {string.Join(" ", result.Values.Select(Map))}";
        }

        private string Map(KeyValuePair<string, CellResult> kvp)
        {
            return $"{kvp.Key}:{kvp.Value.Result}_{kvp.Value.Count ?? 0}_{kvp.Value.SampleForCount}_{kvp.Value.Significance}_{string.Join(",",kvp.Value.SignificantColumns)}";
        }

        private class SimpleCrosstabResultsArray
        {
            public SimpleCrosstabResults[] Results { get; set; }
        }

        private class SimpleCrosstabResults
        {
            public SimpleCategory[] Categories { get; set; }
            public IEnumerable<string> InstanceResults { get; set; }
        }

        private class SimpleCategory
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public IEnumerable<SimpleCategory> SubCategories { get; set; }
        }
    }
}

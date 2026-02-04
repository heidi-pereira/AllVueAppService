using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.Rim;
using CsvHelper;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestCommon.Weighting;
using ResponseWeight = BrandVue.EntityFramework.ResponseRepository.ResponseWeight;

namespace Test.BrandVue.Benchmarks
{
    public class AllVueEngineTests
    {
        private static readonly EntityType Gender = new("Gender", "Gender", "Genders");
        private static readonly EntityType Cell = new("Cell", "Cell", "Cells");
        private static readonly EntityType HouseholdComposition = new("HouseholdComposition", "HouseholdComposition", "HouseholdCompositions");
        private static readonly EntityType Ethnicity = new("Ethnicity", "Ethnicity", "Ethnicities");
        private static readonly EntityType Wave = new("DecemberWeeksWaveVariable", "DecemberWeeksWaveVariable", "DecemberWeeksWavesVariable");

        private static readonly EntityValue GenderFemale = new(Gender, 0);
        private static readonly EntityValue GenderMale = new(Gender, 1);
        private static readonly EntityValue GenderOther = new(Gender, 2);
        private static readonly EntityValue GenderPreferNotSay = new(Gender, 3);

        private static readonly EntityValue HouseholdNoChildren = new(HouseholdComposition, 1);
        private static readonly EntityValue HouseholdUnder10 = new(HouseholdComposition, 2);
        private static readonly EntityValue Household10To18 = new(HouseholdComposition, 3);
        private static readonly EntityValue HouseholdBoth = new(HouseholdComposition, 4);

        private static readonly EntityValue EthnicityWhiteBritish = new(Ethnicity, 1);
        private static readonly EntityValue EthnicityAllOthers = new(Ethnicity, 2);

        private static readonly Dictionary<int, EntityValue> Cells = Enumerable.Range(1, 5).ToDictionary(i => i, i => new EntityValue(Cell, i));

        private static readonly EntityValue Wave1 = new(Wave, 1);
        private static readonly EntityValue Wave2 = new(Wave, 2);
        private static readonly EntityValue Wave3 = new(Wave, 3);
        private static readonly EntityValue Wave4 = new(Wave, 4);
        private static readonly EntityValue Wave5 = new(Wave, 5);

        private TestAnswer _genderFemaleAnswer;
        private TestAnswer _genderMaleAnswer;
        private TestAnswer _genderOtherAnswer;
        private TestAnswer _genderPreferNotSayAnswer;

        private TestAnswer _householdNoChildrenAnswer;
        private TestAnswer _householdUnder10Answer;
        private TestAnswer _household10To18Answer;
        private TestAnswer _householdBothAnswer;

        private TestAnswer _ethnicityWhiteBritishAnswer;
        private TestAnswer _ethnicityAllOthersAnswer;

        private Dictionary<int, TestAnswer> _cellAnswer;

        private ResponseFieldDescriptor _baseFieldAlwaysTrue;
        private ResponseFieldDescriptor _genderField;
        private ResponseFieldDescriptor _householdCompositionField;
        private ResponseFieldDescriptor _ethnicityField;
        private ResponseFieldDescriptor _cellField;
        private ResponseFieldManager _responseFieldManager;

        private Measure _genderMeasure;
        private Measure _householdCompositionMeasure;
        private DataWaveVariable _dataWaveVariable;
        private Measure _ethnicityMeasure;
        private Measure _cellMeasure;
        private Measure[] _measuresToAddToRepo;
        private List<GroupedVariableDefinition> _variableDefinitions;
        private readonly Subset _subset = TestResponseFactory.AllSubset;

        private static TestAnswer[] GetAnswerArr(params TestAnswer[] answers) => answers;

        [SetUp]
        public void Setup()
        {
            var testResponseEntityTypeRepository = new TestEntityTypeRepository(Gender, HouseholdComposition, Wave, Cell);
            _responseFieldManager = new ResponseFieldManager(testResponseEntityTypeRepository);
            _baseFieldAlwaysTrue = _responseFieldManager.Add($"Base_Field_Always_True", _subset);
            _genderField = _responseFieldManager.Add(Gender.Identifier, _subset, Gender);
            _genderMeasure = new Measure
            {
                Name = nameof(Gender),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _genderField,
                LegacyPrimaryTrueValues = { Values = new[] { 0, 1, 2, 3 } },
            };
            _householdCompositionField = _responseFieldManager.Add(HouseholdComposition.Identifier, _subset, HouseholdComposition);
            _householdCompositionMeasure = new Measure { Name = "Household composition_2", Field = _householdCompositionField, LegacyPrimaryTrueValues = new AllowedValues(){Minimum= 1, Maximum= 100}};


            var groupedVariableDefinition = WeightingTestObjects.GetGroupedVariableDefinition();
            _variableDefinitions = new() { groupedVariableDefinition };
            _dataWaveVariable = new DataWaveVariable(groupedVariableDefinition);
            var filterMeasure = new Measure { Name = "DecemberWeeksWaveVariable", PrimaryVariable = _dataWaveVariable };

            _ethnicityField = _responseFieldManager.Add(Ethnicity.Identifier, _subset, Ethnicity);
            _ethnicityMeasure = new Measure
            {
                Name = nameof(Ethnicity),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                LegacyBaseValues = { Values = new[] { 0 } },
                Field = _ethnicityField,
                LegacyPrimaryTrueValues = { Values = new[] { EthnicityWhiteBritish.Value, EthnicityAllOthers.Value } },
            };


            _cellField = _responseFieldManager.Add(Cell.Identifier, _subset, Cell);
            _cellMeasure = new Measure
            {
                Name = nameof(Cell),
                CalculationType = CalculationType.YesNo,
                BaseField = _baseFieldAlwaysTrue,
                Field = _cellField,
                LegacyPrimaryTrueValues = { Values = new[] { 1, 2 } },
            };

            _measuresToAddToRepo = new[] { _genderMeasure, _householdCompositionMeasure, filterMeasure, _ethnicityMeasure, _cellMeasure };

            _genderFemaleAnswer = TestAnswer.For(_genderField, GenderFemale.Value, GenderFemale);
            _genderMaleAnswer = TestAnswer.For(_genderField, GenderMale.Value, GenderMale);
            _genderOtherAnswer = TestAnswer.For(_genderField, GenderOther.Value, GenderOther);
            _genderPreferNotSayAnswer = TestAnswer.For(_genderField, GenderPreferNotSay.Value, GenderPreferNotSay);

            _cellAnswer = Cells.ToDictionary(c => c.Key, c => TestAnswer.For(_cellField, c.Value.Value, c.Value));

            _householdNoChildrenAnswer = TestAnswer.For(_householdCompositionField, HouseholdNoChildren.Value, HouseholdNoChildren);
            _household10To18Answer = TestAnswer.For(_householdCompositionField, Household10To18.Value, Household10To18);
            _householdUnder10Answer = TestAnswer.For(_householdCompositionField, HouseholdUnder10.Value, HouseholdUnder10);
            _householdBothAnswer = TestAnswer.For(_householdCompositionField, HouseholdBoth.Value, HouseholdBoth);

            _ethnicityWhiteBritishAnswer = TestAnswer.For(_ethnicityField, EthnicityWhiteBritish.Value, EthnicityWhiteBritish);
            _ethnicityAllOthersAnswer = TestAnswer.For(_ethnicityField, EthnicityAllOthers.Value, EthnicityAllOthers);
        }

        [Test]
        public async Task SingleRimWeightingSchemeTest() => await AssertCorrectGenderResults(WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan(subsetId: _subset.Id).ToList(), new[]{_genderField, _householdCompositionField});

        [Test]
        public void SingleWeightingSchemeRoundTripResponseWeightTest()
        {
            var subsetId = _subset.Id;
            var plans = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan(subsetId: subsetId).ToList();
            
            var quotaCellFieldOrder = new[] { _genderField, _householdCompositionField };
            var quotaCellStringToAnswers = GetQuotaCellsWithResponsesForSingleWeightingScheme(quotaCellFieldOrder);
            var calculatorBuilder = CreateProductionCalculatorBuilder(DefaultAverageRepositoryData.CustomPeriodAverage, plans, quotaCellStringToAnswers, null);
            var loader = calculatorBuilder.BuildLoaderWithInMemoryDb();

            var productContext = Substitute.For<IProductContext>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            weightingPlanRepository.GetLoaderWeightingPlansForSubset(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(plans);

            var targetPlan = new TargetPlanWeightingGenerationService(loader.RespondentRepositorySource,loader.ProfileResponseAccessorFactory, loader.MeasureRepository);
            var targetWeightingGenerationService = new TargetWeightingGenerationService(loader.SubsetRepository, productContext, weightingPlanRepository, targetPlan);
            var responseWeightingGenerationService = new ResponseWeightingGenerationService(loggerFactory, loader.SubsetRepository, productContext, loader.RespondentRepositorySource, loader.ProfileResponseAccessorFactory, 
                loader.QuotaCellReferenceWeightingRepository);
            var expectedResponseWeightsFromRim = responseWeightingGenerationService.Export(null, null, CancellationToken.None).Select(w => new ResponseWeight(w.ResponseId, w.Weight.Value));
            using var responseIdWeightStream = MockIncomingResponseIdWeightStream(expectedResponseWeightsFromRim);
            var targetWeightingStrategy = targetWeightingGenerationService.ReverseScaleFactors(responseIdWeightStream, subsetId.Yield().ToArray()).SubsetToPlans.Single();
            var targetPlanConfiguration = targetWeightingStrategy.First().FromAppModel(plans.First().ProductShortCode, plans.First().SubProductId, plans.First().SubsetId).ToList();

            var recreatedCalculatorBuilder = CreateProductionCalculatorBuilder(DefaultAverageRepositoryData.CustomPeriodAverage, targetPlanConfiguration, quotaCellStringToAnswers, null);

            var recreatedLoader = recreatedCalculatorBuilder.BuildLoaderWithInMemoryDb();
            var responseWeightingGenerationServiceFromRecreatedLoader = new ResponseWeightingGenerationService(loggerFactory,recreatedLoader.SubsetRepository, productContext,recreatedLoader.RespondentRepositorySource, 
                recreatedLoader.ProfileResponseAccessorFactory, recreatedLoader.QuotaCellReferenceWeightingRepository);
            var actualResponseWeightsFromTarget = responseWeightingGenerationServiceFromRecreatedLoader.Export(null, null, CancellationToken.None).Select(w => new ResponseWeight(w.ResponseId, w.Weight.HasValue? w.Weight.Value: -1));
            Assert.That(actualResponseWeightsFromTarget, Is.EqualTo(expectedResponseWeightsFromRim).Using(new ResponseWeightComparer()));
        }

        private static Stream MockIncomingResponseIdWeightStream(IEnumerable<ResponseWeight> expectedResponseWeightsFromRim)
        {
            var memoryStream = new MemoryStream();
            var streamWriter = new StreamWriter(memoryStream);
            var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture, true);
            csvWriter.WriteRecords(expectedResponseWeightsFromRim);
            streamWriter.Flush();
            memoryStream.Position = 0;
            return memoryStream;
        }

        [Test]
        public async Task SingleInvertedRimWeightingSchemeTest() => await AssertCorrectGenderResults(WeightingPlanConfigurationsTestObjects.GetSingleInvertedWeightingSchemeStrategy(subsetId:_subset.Id).ToList(), new[]{_householdCompositionField, _genderField});

        [Test]
        public async Task NoWeightingStrategyTest()
        {
            var answers = CreateAnswers(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 2794)
                .Yield().ToDictionary(_ => QuotaCell.UnweightedQuotaCell(_subset).ToString());
            var measureResults = await CalculateGenderResults(new List<WeightingPlanConfiguration>(), answers, DefaultAverageRepositoryData.CustomPeriodAverageUnweighted);
            Assert.Multiple(() =>
            {
                var femaleResult = measureResults[0].WeightedDailyResults.Single();
                AssertWeightedDailyResult(femaleResult, 1.0, 2794);
                var maleResult = measureResults[1].WeightedDailyResults.Single();
                AssertWeightedDailyResult(maleResult, 0.0, 2794);
                var otherResult = measureResults[2].WeightedDailyResults.Single();
                AssertWeightedDailyResult(otherResult, 0.0, 2794);
                var preferNotToSayResult = measureResults[3].WeightedDailyResults.Single();
                AssertWeightedDailyResult(preferNotToSayResult, 0.0, 2794);
            });
        }

        [Test]
        public async Task SingleTargetWeightingSchemeWithPartsOfEntityTargetsMissingCharacterizationTest()
        {
            var plans = WeightingPlanConfigurationsTestObjects.GetSingleTargetWeightingSchemeStrategyMissingTargets(subsetId: _subset.Id).ToList();
            var quotaCellFieldOrder = new[] { _genderField, _householdCompositionField };
            var measureResults = await CalculateGenderResults(plans, GetQuotaCellsWithResponsesForSingleWeightingScheme(quotaCellFieldOrder), DefaultAverageRepositoryData.CustomPeriodAverage);
            Assert.Multiple(() =>
            {
                var femaleResult = measureResults[0].WeightedDailyResults.Single();
                AssertWeightedDailyResult(femaleResult, 0.4, 8543);
                var maleResult = measureResults[1].WeightedDailyResults.Single();
                AssertWeightedDailyResult(maleResult, 0.4,  8543);
                var otherResult = measureResults[2].WeightedDailyResults.Single();
                AssertWeightedDailyResult(otherResult, 0.15, 8543);
                var preferNotToSayResult = measureResults[3].WeightedDailyResults.Single();
                AssertWeightedDailyResult(preferNotToSayResult, 0.05, 8543);
            });
        }

        [Test]
        public async Task SingleRimWeightingSchemeWithPartsOfEntityTargetsMissingCharacterizationTest()
        {
            var plans = WeightingPlanConfigurationsTestObjects.GetSingleWeightingSchemeWithMissingTargetsStrategy(subsetId: _subset.Id).ToList();
            var quotaCellFieldOrder = new[] { _genderField, _householdCompositionField };
            var measureResults = await CalculateGenderResults(plans, GetQuotaCellsWithResponsesForSingleWeightingScheme(quotaCellFieldOrder), DefaultAverageRepositoryData.CustomPeriodAverage);
            Assert.Multiple(() =>
            {
                var femaleResult = measureResults[0].WeightedDailyResults.Single();
                AssertWeightedDailyResult(femaleResult, 0.65, 6846);
                var maleResult = measureResults[1].WeightedDailyResults.Single();
                AssertWeightedDailyResult(maleResult, 0.34, 6846);
                var otherResult = measureResults[2].WeightedDailyResults.Single();
                AssertWeightedDailyResult(otherResult, 0.01, 6846);
                var preferNotToSayResult = measureResults[3].WeightedDailyResults.Single();
                AssertWeightedDailyResult(preferNotToSayResult, 0.0, 6846);
            });
        }

        [Test]
        public async Task SingleTargetWeightingSchemeTest() => await AssertCorrectGenderResults(WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy(subsetId:_subset.Id).ToList(), new[] { _genderField, _householdCompositionField });

        [Test]
        public async Task SingleInvertedTargetWeightingSchemeTest() => await AssertCorrectGenderResults(WeightingPlanConfigurationsTestObjects.GetInvertedSingleTargetWeightingSchemeStrategy(subsetId: _subset.Id).ToList(), new[] { _householdCompositionField, _genderField });

        private async Task AssertCorrectGenderResults(List<WeightingPlanConfiguration> plans,
            ResponseFieldDescriptor[] quotaCellFieldOrder)
        {
            var measureResults = await CalculateGenderResults(plans, GetQuotaCellsWithResponsesForSingleWeightingScheme(quotaCellFieldOrder), DefaultAverageRepositoryData.CustomPeriodAverage);
            Assert.Multiple(() =>
            {
                var femaleResult = measureResults[0].WeightedDailyResults.Single();
                AssertWeightedDailyResult(femaleResult, 0.65, 8556);
                var maleResult = measureResults[1].WeightedDailyResults.Single();
                AssertWeightedDailyResult(maleResult, 0.34, 8556);
                var otherResult = measureResults[2].WeightedDailyResults.Single();
                AssertWeightedDailyResult(otherResult, 0.01, 8556);
                var preferNotToSayResult = measureResults[3].WeightedDailyResults.Single();
                AssertWeightedDailyResult(preferNotToSayResult, 0.0, 8556);
            });
        }


        private async Task<EntityWeightedDailyResults[]> CalculateGenderResults(List<WeightingPlanConfiguration> plans, Dictionary<string, TestAnswer[][]> quotaCellResponses, AverageDescriptor average)
        {
            var entityValues = new[] { GenderFemale, GenderMale, GenderOther, GenderPreferNotSay };
            var measureResults = await GetMeasureResults(average, plans,
                quotaCellResponses, _genderMeasure, entityValues, null);
            return measureResults;
        }

        private Dictionary<string, TestAnswer[][]> GetQuotaCellsWithResponsesForSingleWeightingScheme(ResponseFieldDescriptor[] quotaCellFieldOrder)
        {
            var pairs = new List<KeyValuePair<string, TestAnswer[][]>>
            {
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 2794),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 654),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 605),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 294),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 2812),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 569),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 584),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 224),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 11),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 2),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 2),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 1),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 4),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0),
                GetResponsesWithAnswersForQuotaCell(quotaCellFieldOrder, GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0)
            };

            return new Dictionary<string, TestAnswer[][]>(pairs);
        }

        private KeyValuePair<string, TestAnswer[][]> GetResponsesWithAnswersForQuotaCell(
            ResponseFieldDescriptor[] quotaCellFieldOrder, TestAnswer[] quotaCellAnswers, int count)
        {
            var responses = CreateAnswers(quotaCellAnswers, count);
            var quotaParts = quotaCellFieldOrder.Select(f => quotaCellAnswers.Single(a => a.Field == f).EntityValues.AsReadOnlyCollection().Single().Value.ToString());
            var quotaCellKey = QuotaCell.GenerateKey(quotaParts);
            return new(quotaCellKey, responses);
        }

        private TestAnswer[][] CreateAnswers(TestAnswer[] quotaCellAnswers, int count)
        {
            var baseAnswer = TestAnswer.For(_baseFieldAlwaysTrue, 0);
            var responses = Enumerable.Repeat(quotaCellAnswers.Prepend(baseAnswer).ToArray(), count).ToArray();
            return responses;
        }

        [Test]
        public async Task TwoWeightingSchemesTest() =>
            await AssertBritishResult(DefaultAverageRepositoryData.CustomPeriodAverage, 0.746842496, 3588,
                GetQuotaCellsWithResponsesForTwoWeightingSchemes(), WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy(subsetId: _subset.Id).ToList());

        [Test]
        public void TwoWeightingSchemesRoundTripResponseWeightTest()
        {
            var subsetId = _subset.Id;
            var plans = WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy(subsetId: subsetId).ToList();
            var quotaCellStringToAnswers = GetQuotaCellsWithResponsesForTwoWeightingSchemes();
            var calculatorBuilder = CreateProductionCalculatorBuilder(DefaultAverageRepositoryData.CustomPeriodAverage, plans, quotaCellStringToAnswers, _dataWaveVariable);
            var loader = calculatorBuilder.BuildLoaderWithInMemoryDb();
            var weightingPlanRepository = Substitute.For<IWeightingPlanRepository>();
            var productContext = Substitute.For<IProductContext>();
            var loggerFactory = Substitute.For<ILoggerFactory>();
            weightingPlanRepository.GetLoaderWeightingPlansForSubset(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(plans);

            var targetPlanService = new TargetPlanWeightingGenerationService(loader.RespondentRepositorySource,
                loader.ProfileResponseAccessorFactory, loader.MeasureRepository);
            var targetWeightingGenerationService = new TargetWeightingGenerationService(
                loader.SubsetRepository, 
                productContext, weightingPlanRepository, targetPlanService);

            var responseWeightingGenerationService = new ResponseWeightingGenerationService(loggerFactory,loader.SubsetRepository, productContext, loader.RespondentRepositorySource, loader.ProfileResponseAccessorFactory,
                loader.QuotaCellReferenceWeightingRepository);
            var expectedResponseWeightsFromRim = responseWeightingGenerationService.Export(null, null, CancellationToken.None).Select(w => new ResponseWeight(w.ResponseId, w.Weight.HasValue ? w.Weight.Value : -1));
            using var responseIdWeightStream = MockIncomingResponseIdWeightStream(expectedResponseWeightsFromRim);
            var targetWeightingStrategy = targetWeightingGenerationService.ReverseScaleFactors(responseIdWeightStream, subsetId.Yield().ToArray()).SubsetToPlans.Single();
            var targetPlanConfiguration = targetWeightingStrategy.First().FromAppModel(plans.First().ProductShortCode, plans.First().SubProductId, plans.First().SubsetId).ToList();
            var recreatedCalculatorBuilder = CreateProductionCalculatorBuilder(DefaultAverageRepositoryData.CustomPeriodAverage, targetPlanConfiguration
                , quotaCellStringToAnswers, _dataWaveVariable);

            var recreatedLoader = recreatedCalculatorBuilder.BuildLoaderWithInMemoryDb();
            var responseWeightingGenerationServiceFromRecreatedLoader = new ResponseWeightingGenerationService(loggerFactory, recreatedLoader.SubsetRepository, productContext, recreatedLoader.RespondentRepositorySource,
                recreatedLoader.ProfileResponseAccessorFactory, recreatedLoader.QuotaCellReferenceWeightingRepository);
            var actualResponseWeightsFromTarget = responseWeightingGenerationServiceFromRecreatedLoader.Export(null, null, CancellationToken.None).Select(w => new ResponseWeight(w.ResponseId, w.Weight.HasValue ? w.Weight.Value: -1));
            
            Assert.That(actualResponseWeightsFromTarget, Is.EqualTo(expectedResponseWeightsFromRim).Using(new ResponseWeightComparer()));
        }

        [Test]
        public async Task TwoParallelWeightingSchemes_NonWaveVariableTest() =>
            await AssertBritishResult(DefaultAverageRepositoryData.CustomPeriodAverage, 0.746842496, 3588,
                GetQuotaCellsWithResponsesForTwoWeightingSchemes(),
                WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy(subsetId: _subset.Id, filterMetricName: _cellMeasure.Name).ToList());

        [Test]
        public async Task TwoWeightingSchemesTestUnweighted() =>
            await AssertBritishResult(DefaultAverageRepositoryData.CustomPeriodAverageUnweighted, 0.6694703, 5588,
                GetQuotaCellsWithResponsesForTwoWeightingSchemes(), WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy(subsetId: _subset.Id).ToList());

        [Test]
        public async Task FiveWeightingSchemesTestCharacterization_All() =>
            await AssertBritishResult(DefaultAverageRepositoryData.CustomPeriodAverage, 0.7349875, 8824,
                GetQuotaCellsWithResponsesForFiveWeightingSchemes(), WeightingPlanConfigurationsTestObjects.FiveWeightingPlans(subsetId:_subset.Id).ToList());

        [Test]
        public async Task FiveWeightingSchemesTestCharacterization_Monthly() =>
            await AssertBritishResult(Averages.CreateDefaultRepo(true).Get("Monthly"), 0.7349875, 8824,
                GetQuotaCellsWithResponsesForFiveWeightingSchemes(), WeightingPlanConfigurationsTestObjects.FiveWeightingPlans(subsetId: _subset.Id).ToList());

        [Test]
        public async Task FiveWeightingSchemesTestUnweightedCharacterization() =>
            await AssertBritishResult(DefaultAverageRepositoryData.CustomPeriodAverageUnweighted, 0.7411441f, 9824,
                GetQuotaCellsWithResponsesForFiveWeightingSchemes(), WeightingPlanConfigurationsTestObjects.FiveWeightingPlans(subsetId: _subset.Id).ToList());

        private async Task AssertBritishResult(AverageDescriptor average, double expectedResult, uint expectedWeightedSampleSize,
            Dictionary<string, TestAnswer[][]> quotaCellStringToAnswers, List<WeightingPlanConfiguration> plans)
        {
            var entityValues = new[] { EthnicityWhiteBritish, EthnicityAllOthers };
            var measureResults = await GetMeasureResults(average, plans, quotaCellStringToAnswers, _ethnicityMeasure, entityValues,
                _dataWaveVariable);

            var whiteBritishResult = measureResults[0].WeightedDailyResults.Single();
            AssertWeightedDailyResult(whiteBritishResult, expectedResult, expectedWeightedSampleSize);
        }

        private async Task<EntityWeightedDailyResults[]> GetMeasureResults(AverageDescriptor average,List<WeightingPlanConfiguration> weightingPlans, Dictionary<string, TestAnswer[][]> quotaCellStringToAnswers, Measure measure, EntityValue[] entityValues, DataWaveVariable dataWaveVariable)
        {
            var calculatorBuilder = CreateProductionCalculatorBuilder(average, weightingPlans, quotaCellStringToAnswers, dataWaveVariable);
            var calculator = calculatorBuilder.BuildRealCalculatorWithInMemoryDb();
            return await calculator.CalculateFor(measure, entityValues, new AlwaysIncludeFilter());
        }

        private ProductionCalculatorBuilder CreateProductionCalculatorBuilder(AverageDescriptor average, List<WeightingPlanConfiguration> weightingPlans, Dictionary<string, TestAnswer[][]> quotaCellStringToAnswers, 
            DataWaveVariable dataWaveVariable)
        {

            var builder = new ProductionCalculatorBuilder(includeResponseIds: false);
            if (weightingPlans != null && weightingPlans.Any())  builder = builder.WithSubset(new Subset { Id = weightingPlans.First().SubsetId});

            var entityValues = new[] { GenderFemale, GenderMale, GenderOther, GenderPreferNotSay, HouseholdNoChildren, HouseholdUnder10, Household10To18, HouseholdBoth, Wave1, Wave2, Wave3, Wave4, Wave5, EthnicityAllOthers, EthnicityWhiteBritish }.Concat(Cells.Values).ToArray();
            return builder
                .WithCalculationPeriod(new CalculationPeriod(DateTimeOffset.Parse("2020/12/01"), DateTimeOffset.Parse("2020/12/31").EndOfDay()))
                .WithAverage(average)
                .IncludeMeasures(_measuresToAddToRepo, _variableDefinitions)
                .IncludeEntities(entityValues)
                .WithWeightingPlansAndResponses(weightingPlans.ToArray(), quotaCellStringToAnswers, dataWaveVariable)
                .UseAllVueProductContext();
        }

        private KeyValuePair<string, TestAnswer[][]> GetResponsesForWavesWithEthnicityAnswer(TestAnswer[] quotaCellAnswers, string quotaCellKey, int count, int ethnicityBritishCount)
        {
            var baseAnswer = TestAnswer.For(_baseFieldAlwaysTrue, 0);
            var responsesWithEthnicityBritish = Enumerable.Repeat(
                quotaCellAnswers.Prepend(baseAnswer).Append(_ethnicityWhiteBritishAnswer).ToArray(),
                ethnicityBritishCount);

            var responsesWithEthnicityOther = Enumerable.Repeat(
                quotaCellAnswers.Prepend(baseAnswer).Append(_ethnicityAllOthersAnswer).ToArray(),
                count - ethnicityBritishCount);

            return new(quotaCellKey, responsesWithEthnicityBritish.Concat(responsesWithEthnicityOther).ToArray());
        }

        private KeyValuePair<string, TestAnswer[][]> GetResponsesForWavesWithEthnicityAnswer(TestAnswer[] quotaCellAnswers, int count, int waveId, int ethnicityBritishCount)
        {
            quotaCellAnswers = _cellAnswer[waveId].Yield().Concat(quotaCellAnswers).ToArray();
            var quotaParts = quotaCellAnswers.Select(qca => qca.EntityValues.AsReadOnlyCollection().Single().Value.ToString());
            var quotaCellKey = QuotaCell.GenerateKey(quotaParts);
            return GetResponsesForWavesWithEthnicityAnswer(quotaCellAnswers, quotaCellKey, count, ethnicityBritishCount);
        }

        private Dictionary<string, TestAnswer[][]> GetQuotaCellsWithResponsesForTwoWeightingSchemes()
        {
            var pairs = new List<KeyValuePair<string, TestAnswer[][]>>
            {
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 713, 1, 558),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 164, 1, 111),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 144, 1, 107),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 50, 1, 37),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 448, 1, 389),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 36, 1, 24),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 75, 1, 62),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 16, 1, 10),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 1, 1, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 732, 2, 559),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 210, 2, 151),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 189, 2, 135),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 93, 2, 67),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 480, 2, 371),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 100, 2, 64),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 89, 2, 59),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 43, 2, 33),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 2, 2, 2),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 1, 2, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(), QuotaCell.UnweightedQuotaCell(_subset).ToString(), 2000, 1000),
            };

            return new Dictionary<string, TestAnswer[][]>(pairs);
        }

        private Dictionary<string, TestAnswer[][]> GetQuotaCellsWithResponsesForFiveWeightingSchemes()
        {
            var pairs = new List<KeyValuePair<string, TestAnswer[][]>>
            {
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 713, 1, 558),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 164, 1, 111),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 144, 1, 107),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 50, 1, 37),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 448, 1, 389),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 36, 1, 24),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 75, 1, 62),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 16, 1, 10),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 1, 1, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 1, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 1, 0),

                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 732, 2, 559),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 210, 2, 151),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 189, 2, 135),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 93, 2, 67),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 480, 2, 371),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 100, 2, 64),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 89, 2, 59),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 43, 2, 33),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 2, 2, 2),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 1, 2, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 2, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 2, 0),

                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 713, 3, 558),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 164, 3, 111),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 144, 3, 107),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 50, 3, 37),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 448, 3, 389),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 36, 3, 24),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 75, 3, 62),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 16, 3, 10),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 1, 3, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 3, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 0, 3, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 3, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 3, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 3, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 3, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 3, 0),

                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 732, 4, 559),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 210, 4, 151),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 189, 4, 135),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 93, 4, 67),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 480, 4, 371),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 100, 4, 64),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 89, 4, 59),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 43, 4, 33),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 2, 4, 2),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 4, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 1, 4, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 4, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 4, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 4, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 4, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 4, 0),

                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdNoChildrenAnswer), 713, 5, 558),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdUnder10Answer), 164, 5, 111),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _household10To18Answer), 144, 5, 107),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderFemaleAnswer, _householdBothAnswer), 50, 5, 37),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdNoChildrenAnswer), 448, 5, 389),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdUnder10Answer), 36, 5, 24),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _household10To18Answer), 75, 5, 62),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderMaleAnswer, _householdBothAnswer), 16, 5, 10),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdNoChildrenAnswer), 1, 5, 1),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdUnder10Answer), 0, 5, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _household10To18Answer), 0, 5, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderOtherAnswer, _householdBothAnswer), 0, 5, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdNoChildrenAnswer), 1, 5, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdUnder10Answer), 0, 5, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _household10To18Answer), 0, 5, 0),
                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(_genderPreferNotSayAnswer, _householdBothAnswer), 0, 5, 0),

                GetResponsesForWavesWithEthnicityAnswer(GetAnswerArr(), QuotaCell.UnweightedQuotaCell(_subset).ToString(), 1000, 500)
            };

            return new Dictionary<string, TestAnswer[][]>(pairs);
        }

        private static void AssertWeightedDailyResult(WeightedDailyResult result, double expectedResult, uint expectedSampleSize)
        {
            Assert.Multiple(() =>
            {
                CommonAssert.AssertDoubleEqualWithinErrorMargin(expectedResult, result.WeightedResult);
                CommonAssert.AssertDoubleEqualWithinErrorMargin(expectedSampleSize, result.WeightedSampleSize, 0.00001);
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize));
            });
        }
        private class ResponseWeightComparer : IEqualityComparer<ResponseWeight>
        {
            public bool Equals(ResponseWeight x, ResponseWeight y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.ResponseId == y.ResponseId && Math.Abs(x.Weight - y.Weight) <= RimWeightingCalculator.PointTolerance;
            }

            public int GetHashCode(ResponseWeight obj)
            {
                return obj.ResponseId.GetHashCode();
            }
        }

    }
}
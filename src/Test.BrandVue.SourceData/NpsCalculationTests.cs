using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    internal class NpsCalculationTests
    {
        [Test]
        public void NoDuplicateTestNames() => CommonAssert.NoDuplicatedTestNames(TestCases());

        [Test]
        public async Task NetPromoterScoreUsingValueRangesAsync()
        {
            var brand1 = new EntityValue(TestEntityTypeRepository.Brand, 1);
            var brand2 = new EntityValue(TestEntityTypeRepository.Brand, 2);

            var responseFieldManager = ResponseFieldManager();
            var recommendation = responseFieldManager.Add("Recommendation", TestEntityTypeRepository.Brand);

            var measure = new Measure
            {
                Name = "NPS",
                CalculationType = CalculationType.NetPromoterScore,
                Field = recommendation,
                LegacyPrimaryTrueValues = new AllowedValues { Minimum = 0, Maximum = 10 },
                LegacyBaseValues =
                {
                    Minimum = 0,
                    Maximum = 10,
                },
            };

            var answers = new[]
            {
                TestAnswer.For(recommendation, 10, brand1), // BaseTally: 1, PromoterTally: 1, DetractorTally: 0
                TestAnswer.For(recommendation, 9, brand1),  // BaseTally: 2, PromoterTally: 2, DetractorTally: 0
                TestAnswer.For(recommendation, 8, brand1),  // BaseTally: 3, PromoterTally: 2, DetractorTally: 0
                TestAnswer.For(recommendation, 7, brand1),  // BaseTally: 4, PromoterTally: 2, DetractorTally: 0
                TestAnswer.For(recommendation, 6, brand1),  // BaseTally: 5, PromoterTally: 2, DetractorTally: 1
                TestAnswer.For(recommendation, 3, brand1),  // BaseTally: 6, PromoterTally: 2, DetractorTally: 2
                TestAnswer.For(recommendation, 0, brand1),  // BaseTally: 7, PromoterTally: 2, DetractorTally: 3
                TestAnswer.For(recommendation, 10, brand2), // BaseTally: 7, PromoterTally: 2, DetractorTally: 3
            };

            var expectedSampleSize = 7;
            var expectedResult = (2 - 3) * 100.0 / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, brand1, brand2);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand1.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult).Within(TestConstants.ResultAccuracy), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        [Test, TestCaseSource(nameof(TestCases))]
        public async Task SingleResponseTestAsync(Measure measure,
            AverageDescriptor averageDescriptor,
            ResponseAnswers[] responses,
            Func<EntityWeightedDailyResults[], WeightedDailyResult> resultSelector,
            double expectedResult,
            int expectedBaseSize,
            EntityValue[] splitByInstances,
            EntityValue filterInstance)
        {
            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(averageDescriptor)
                .WithFilterInstance(filterInstance)
                .IncludeEntities(filterInstance.YieldNonNull().Concat(splitByInstances).ToArray())
                .WithResponses(responses);

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, splitByInstances);

            var result = resultSelector(measureResults);

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedBaseSize), "Incorrect sample size");
            });
        }

        public static IEnumerable<TestCaseData> TestCases()
        {
            var testMeasures = new TestMeasureProvider(CalculationType.NetPromoterScore)
                .GetAllTestMeasures()
                .Where(m => m.SecondaryField == null && m.FieldExpression is not DataWaveVariable && m.FieldExpression is not SurveyIdVariable);
            return testMeasures.SelectMany(GenerateSingleResponseTestCases);
        }

        /// <summary>
        /// This generates a test case for each possible response to the given measure
        /// </summary>
        private static IEnumerable<TestCaseData> GenerateSingleResponseTestCases(TestMetric testMetric)
        {
            var testPrefix = $"Metric: {testMetric.Name}";
            var entityValuesForCalculation = new List<EntityValue>();
            if (testMetric.InstanceToCheck != null) entityValuesForCalculation.Add(testMetric.InstanceToCheck);
            if (testMetric.FilterInstance != null) entityValuesForCalculation.Add(testMetric.FilterInstance);

            // To create answers for each field, we need to know which entity values are applicable
            var primaryEntityValues = entityValuesForCalculation.Where(i => FindPrimaryValues(testMetric, i)).ToArray();
            var baseEntityValues = testMetric.BaseField != null ? entityValuesForCalculation.Where(i => testMetric.BaseField.EntityCombination.Contains(i.EntityType)).ToArray() : Array.Empty<EntityValue>();

            bool hasBase = testMetric.BaseField != null;

            return hasBase switch
            {
                false => StandardNpsMeasure(testPrefix, testMetric, primaryEntityValues),
                true => HasBase(testPrefix, testMetric, primaryEntityValues, baseEntityValues)
            };
        }

        private static bool FindPrimaryValues(TestMetric testMetric, EntityValue i)
        {
            bool inFieldCombination = testMetric.PrimaryField?.EntityCombination.Contains(i.EntityType) ?? false;
            bool inExpressionCombination = testMetric.FieldExpression?.UserEntityCombination.Contains(i.EntityType) ?? false;
            return inFieldCombination || inExpressionCombination;
        }

        private static IEnumerable<TestCaseData> StandardNpsMeasure(string testPrefix, TestMetric testMetric, EntityValue[] primaryEntityValues)
        {
            var npsValues = new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            var npsMeasure = testMetric.WithTrueAndBaseValues(npsValues, npsValues);
            var entityValues = primaryEntityValues.ToArray();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 0").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 1").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 2").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 3").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 4").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 5").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 6").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues)).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix}, Answer: 7").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues)).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix}, Answer: 8").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues)).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix}, Answer: 9").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues)).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix}, Answer: 10").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues)).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 11").Build();

            var basePrimaryExpressionsEquivalent = npsMeasure
                .WithPrimaryExpression(testMetric.FieldExpressionParser.ParseUserNumericExpressionOrNull($"{testMetric.PrimaryField.Name}"))
                .WithPrimaryField(null)
                .WithBaseExpression(testMetric.FieldExpressionParser.ParseUserBooleanExpression($"{testMetric.PrimaryField.Name} in [0,1,2,3,4,5,6,7,8,9,10]"))
                .WithBaseField(null);
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 0").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 1").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 2").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 3").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 4").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 5").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues)).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 6").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues)).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 7").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues)).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 8").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues)).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Base/Variable expression, Answer: 9").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues)).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Base/Variable expression, Answer: 10").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues)).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 11").Build();
        }

        private static IEnumerable<TestCaseData> HasBase(string testPrefix, TestMetric testMetric, EntityValue[] primaryEntityValues, EntityValue[] baseEntityValues)
        {
            var npsValues = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var npsMeasure = testMetric.WithTrueAndBaseValues(npsValues, new[] { 3 });
            var entityValues = primaryEntityValues.ToArray();

            var baseTrue = TestAnswer.For(testMetric.BaseField, 3, baseEntityValues);
            var baseFalse = TestAnswer.For(testMetric.BaseField, -97, baseEntityValues);

            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 0 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 1 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 2 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 3 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 4 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 5 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix}, Answer: 6 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix}, Answer: 7 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix}, Answer: 8 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix}, Answer: 9 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix}, Answer: 10 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 11 - ResponseDetail: (InBase: True, Invalid)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 0 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 1 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 2 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 3 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 4 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 5 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 6 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 7 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 8 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 9 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 10 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return npsMeasure.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, Answer: 11 - ResponseDetail: (InBase: False, Invalid)").Build();

            var fieldExpressionWithBaseField = npsMeasure
                .WithPrimaryExpression(testMetric.FieldExpressionParser.ParseUserNumericExpressionOrNull($"{testMetric.PrimaryField.Name}"))
                .WithPrimaryField(null);
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only, Answer: 0 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only , Answer: 1 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only , Answer: 2 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only , Answer: 3 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only , Answer: 4 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only , Answer: 5 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Variable expression Only , Answer: 6 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 7 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 8 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Variable expression Only, Answer: 9 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Variable expression Only, Answer: 10 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 11 - ResponseDetail: (InBase: True, Invalid)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 0 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 1 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 2 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 3 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 4 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 5 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 6 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 7 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 8 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 9 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 10 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, Answer: 11 - ResponseDetail: (InBase: False, Invalid)").Build();


            var baseExpressionWithField = npsMeasure
                .WithBaseExpression(testMetric.FieldExpressionParser.ParseUserBooleanExpression($"{testMetric.BaseField.Name} in [3]"))
                .WithBaseField(null);
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 0 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 1 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 2 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 3 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 4 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 5 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base Expression Only, Answer: 6 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 7 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 8 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Base Expression Only, Answer: 9 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Base Expression Only, Answer: 10 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 11 - ResponseDetail: (InBase: True, Invalid)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 0 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 1 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 2 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 3 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 4 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 5 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 6 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 7 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 8 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 9 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 10 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return baseExpressionWithField.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, Answer: 11 - ResponseDetail: (InBase: False, Invalid)").Build();

            var baseAndFieldExpression = npsMeasure
                .WithBaseExpression(testMetric.FieldExpressionParser.ParseUserBooleanExpression($"{testMetric.BaseField.Name} in [3]"))
                .WithBaseField(null)
                .WithPrimaryExpression(testMetric.FieldExpressionParser.ParseUserNumericExpressionOrNull($"{testMetric.PrimaryField.Name}"))
                .WithPrimaryField(null);
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 0 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 1 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 2 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 3 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 4 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 5 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(-100).Named($"{testPrefix} With Base/Variable expression, Answer: 6 - ResponseDetail: (InBase: True, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 7 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 8 - ResponseDetail: (InBase: True, Neutral)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Base/Variable expression, Answer: 9 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseTrue).ExpectBaseSize(1).ExpectResult(100).Named($"{testPrefix} With Base/Variable expression, Answer: 10 - ResponseDetail: (InBase: True, Promoter)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 11 - ResponseDetail: (InBase: True, Invalid)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 0, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 0 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 1, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 1 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 2, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 2 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 3, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 3 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 4, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 4 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 5, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 5 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 6, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 6 - ResponseDetail: (InBase: False, Detractor)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 7, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 7 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 8, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 8 - ResponseDetail: (InBase: False, Neutral)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 9, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 9 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 10, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 10 - ResponseDetail: (InBase: False, Promoter)").Build();
            yield return baseAndFieldExpression.WithResponse(TestAnswer.For(npsMeasure.PrimaryField, 11, entityValues), baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, Answer: 11 - ResponseDetail: (InBase: False, Invalid)").Build();
        }

        private static ResponseFieldManager ResponseFieldManager(params EntityType[] entityTypes)
        {
            return new(new TestEntityTypeRepository(entityTypes));
        }
    }
}

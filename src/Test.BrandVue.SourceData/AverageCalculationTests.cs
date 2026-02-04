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
    internal class AverageCalculationTests
    {
        [Test]
        public void NoDuplicateTestNames() => CommonAssert.NoDuplicatedTestNames(TestCases());

        [Test]
        public async Task AverageAgeOfConsumerAsync()
        {
            var brand1 = new EntityValue(TestEntityTypeRepository.Brand, 1);
            var brand2 = new EntityValue(TestEntityTypeRepository.Brand, 2);

            var responseFieldManager = ResponseFieldManager();
            var age = responseFieldManager.Add("Age");
            var consumerSegment = responseFieldManager.Add("ConsumerSegment", TestEntityTypeRepository.Brand);

            var measure = new Measure
            {
                Name = "Age (average)",
                CalculationType = CalculationType.Average,
                Field = age,
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 16, Maximum = 74 },
                BaseField = consumerSegment,
                LegacyBaseValues = { Values = new[] {4, 5, 6} },
            };

            var responses = new[]
            {
                new[] {TestAnswer.For(consumerSegment, 4, brand1), TestAnswer.For(age, 17)}, // BaseTally: 1
                new[] {TestAnswer.For(consumerSegment, 5, brand1), TestAnswer.For(age, 19)}, // BaseTally: 2
                new[] {TestAnswer.For(consumerSegment, 6, brand1), TestAnswer.For(age, 23)}, // BaseTally: 3
                new[] {TestAnswer.For(consumerSegment, 1, brand1), TestAnswer.For(age, 29)}, // BaseTally: 3
                new[] {TestAnswer.For(consumerSegment, 2, brand1), TestAnswer.For(age, 31)}, // BaseTally: 3
                new[] {TestAnswer.For(consumerSegment, 4, brand2), TestAnswer.For(age, 37)}, // BaseTally: 3
            };

            var expectedSampleSize = 3;
            var expectedResult = (double) (17 + 19 + 23) / expectedSampleSize;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithResponses(responses.Select(answers => new ResponseAnswers(answers)).ToArray());

            var measureResults = await calculatorBuilder.IncludeEntities(brand1, brand2).BuildRealCalculator().CalculateFor(measure, brand1, brand2);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand1.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSampleSize), "Incorrect sample size");
            });
        }

        [Test]
        public async Task OutsideTrueValueShouldNotBeIncludedInBaseAsync()
        {
            // If the response were to be included in the base, it'd affects the average as if the value was 0

            var brand1 = new EntityValue(TestEntityTypeRepository.Brand, 1);

            var responseFieldManager = ResponseFieldManager();
            var age = responseFieldManager.Add("Age");
            var consumerSegment = responseFieldManager.Add("ConsumerSegment", TestEntityTypeRepository.Brand);

            var measure = new Measure
            {
                Name = "Age (average)",
                CalculationType = CalculationType.Average,
                Field = age,
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 16, Maximum = 74 },
                BaseField = consumerSegment,
                LegacyBaseValues = { Values = new[] {4, 5, 6} },
            };

            var answers = new[] {TestAnswer.For(consumerSegment, 4, brand1), TestAnswer.For(age, 83)};

            var expectedSampleSize = 0;
            var expectedResult = 0.0;

            var calculatorBuilder = new ProductionCalculatorBuilder().WithAverage(Averages.SingleDayAverage)
                .WithAnswers(answers);

            var measureResults = await calculatorBuilder.BuildRealCalculator().CalculateFor(measure, brand1);

            var result = measureResults.SingleOrDefault(r => r.EntityInstance.Id == brand1.Value)?.WeightedDailyResults.SingleOrDefault();

            Assert.That(result, Is.Not.Null, "No result found");
            Assert.Multiple(() =>
            {
                Assert.That(result.WeightedResult, Is.EqualTo(expectedResult), "Incorrect result");
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
            var testMeasures = new TestMeasureProvider(CalculationType.Average)
                .GetAllTestMeasures();
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
            var secondaryEntityValues = testMetric.SecondaryField != null ? entityValuesForCalculation.Where(i => testMetric.SecondaryField.EntityCombination.Contains(i.EntityType)).ToArray() : Array.Empty<EntityValue>();
            var baseEntityValues = testMetric.BaseField != null ? entityValuesForCalculation.Where(i => testMetric.BaseField.EntityCombination.Contains(i.EntityType)).ToArray() : Array.Empty<EntityValue>();

            (bool HasBase, bool HasSecondary, bool IsWaveMeasure, bool IsSurveyIdMeasure) measure =
                (testMetric.BaseField != null, testMetric.SecondaryField != null, testMetric.FieldExpression is DataWaveVariable, testMetric.FieldExpression is SurveyIdVariable);

            // The form of the possible responses for a measure depends on whether the measure has a base field and a secondary field.
            // e.g. A response for a metric that HasBaseHasSecondary will be composed of answers for the primary, secondary and base fields
            return measure switch
            {
                {IsSurveyIdMeasure: true} => SurveyMeasure(testPrefix, testMetric),
                {IsWaveMeasure: true} => WaveMeasure(testPrefix, testMetric),
                {HasBase: false, HasSecondary: false} => NoBaseNoSecondary(testPrefix, testMetric, primaryEntityValues),
                {HasBase: true, HasSecondary: false} => HasBaseNoSecondary(testPrefix, testMetric, primaryEntityValues, baseEntityValues),
                {HasBase: false, HasSecondary: true} => NoBaseHasSecondary(testPrefix, testMetric, primaryEntityValues, secondaryEntityValues),
                {HasBase: true, HasSecondary: true} => HasBaseHasSecondary(testPrefix, testMetric, primaryEntityValues, secondaryEntityValues, baseEntityValues)
            };
        }

        private static bool FindPrimaryValues(TestMetric testMetric, EntityValue i)
        {
            bool inFieldCombination = testMetric.PrimaryField?.EntityCombination.Contains(i.EntityType) ?? false;
            bool inExpressionCombination = testMetric.FieldExpression?.UserEntityCombination.Contains(i.EntityType) ?? false;
            return inFieldCombination || inExpressionCombination;
        }

        private static IEnumerable<TestCaseData> SurveyMeasure(string testPrefix, TestMetric testMetric)
        {
            yield return testMetric.WithResponses(new ResponseAnswers(Array.Empty<TestAnswer>(), SurveyId: 0)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: Not in any survey (InBase: True, InPrimary: False)").Build();
            yield return testMetric.WithResponses(new ResponseAnswers(Array.Empty<TestAnswer>(), SurveyId: 1)).ExpectBaseSize(1).ExpectResult(1.0).Named($"{testPrefix}, ResponseDetail: Survey 1 (InBase: True, InPrimary: True)").Build();
            yield return testMetric.WithResponses(new ResponseAnswers(Array.Empty<TestAnswer>(), SurveyId: 2)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: Survey 2 (InBase: True, InPrimary: False)").Build();
        }

        private static IEnumerable<TestCaseData> WaveMeasure(string testPrefix, TestMetric testMetric)
        {
            yield return testMetric.WithResponses(new ResponseAnswers(Array.Empty<TestAnswer>(), DateTimeOffset.Parse("2018-12-25"))).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: Not in any wave (InBase: True, InPrimary: False)").Build();
            yield return testMetric.WithResponses(new ResponseAnswers(Array.Empty<TestAnswer>(), DateTimeOffset.Parse("2019-01-25"))).ExpectBaseSize(1).ExpectResult(1.0).Named($"{testPrefix}, ResponseDetail: Wave 1 (InBase: True, InPrimary: True)").Build();
            yield return testMetric.WithResponses(new ResponseAnswers(Array.Empty<TestAnswer>(), DateTimeOffset.Parse("2019-03-15"))).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: Wave 2 (InBase: True, InPrimary: False)").Build();
        }

        private static IEnumerable<TestCaseData> NoBaseNoSecondary(string testPrefix, TestMetric testMetric, EntityValue[] primaryEntityValues)
        {
            var noBaseTest = testMetric.WithTrueAndBaseValues(new[] {1, 3}, new[] {1, 2});
            yield return noBaseTest.WithResponse(TestAnswer.For(testMetric.PrimaryField, 1, primaryEntityValues)).ExpectBaseSize(1).ExpectResult(1.0).Named($"{testPrefix}, ResponseDetail: (InBase: True, InPrimary: True)").Build();
            yield return noBaseTest.WithResponse(TestAnswer.For(testMetric.PrimaryField, 2, primaryEntityValues)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: (InBase: True, InPrimary: False)").Build(); //
            yield return noBaseTest.WithResponse(TestAnswer.For(testMetric.PrimaryField, 3, primaryEntityValues)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: (InBase: False, InPrimary: True)").Build();
            yield return noBaseTest.WithResponse(TestAnswer.For(testMetric.PrimaryField, 4, primaryEntityValues)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix}, ResponseDetail: (InBase: False, InPrimary: False)").Build();
            yield return noBaseTest.WithResponse().ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (No answer)").Build();

            var basePrimaryExpressionsEquivalent = noBaseTest
                .WithPrimaryExpression(testMetric.FieldExpressionParser.ParseUserNumericExpressionOrNull($"{testMetric.PrimaryField.Name} in [1, 3]"))
                .WithPrimaryField(null)
                .WithBaseExpression(testMetric.FieldExpressionParser.ParseUserBooleanExpression($"{testMetric.PrimaryField.Name} in [1, 2]"))
                .WithBaseField(null);
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(testMetric.PrimaryField, 1, primaryEntityValues)).ExpectBaseSize(1).ExpectResult(1.0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: True, InPrimary: True)").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(testMetric.PrimaryField, 2, primaryEntityValues)).ExpectBaseSize(1).ExpectResult(0.0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: True, InPrimary: False)").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(testMetric.PrimaryField, 3, primaryEntityValues)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: False, InPrimary: True)").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse(TestAnswer.For(testMetric.PrimaryField, 4, primaryEntityValues)).ExpectBaseSize(0).ExpectResult(0.0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: False, InPrimary: False)").Build();
            yield return basePrimaryExpressionsEquivalent.WithResponse().ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (No answer)").Build();
        }

        private static IEnumerable<TestCaseData> HasBaseNoSecondary(string testPrefix, TestMetric testMetric, EntityValue[] primaryEntityValues, EntityValue[] baseEntityValues)
        {
            var withBaseTest = testMetric.WithTrueAndBaseValues(new[] {1}, new[] {3});

            // These are answers for each field that can be reused to form different responses
            var primaryTrue = TestAnswer.For(testMetric.PrimaryField, 1, primaryEntityValues);
            var primaryFalse = TestAnswer.For(testMetric.PrimaryField, -91, primaryEntityValues);
            var baseTrue = TestAnswer.For(testMetric.BaseField, 3, baseEntityValues);
            var baseFalse = TestAnswer.For(testMetric.BaseField, -97, baseEntityValues);

            yield return withBaseTest.WithResponse(baseTrue, primaryTrue).ExpectBaseSize(1).ExpectResult(1).Named($"{testPrefix}, ResponseDetail: (InBase: True, InPrimary: True)").Build();
            yield return withBaseTest.WithResponse(baseTrue, primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (InBase: True, InPrimary: False)").Build(); //
            yield return withBaseTest.WithResponse(baseTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (InBase: True, NoPrimary)").Build();
            yield return withBaseTest.WithResponse(baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (InBase: False, NoPrimary)").Build();
            yield return withBaseTest.WithResponse(baseFalse, primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (InBase: False, InPrimary: True)").Build();
            yield return withBaseTest.WithResponse(baseFalse, primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (InBase: False, InPrimary: False)").Build();
            yield return withBaseTest.WithResponse(primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (NoBase, InPrimary: True)").Build();
            yield return withBaseTest.WithResponse(primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (NoBase, InPrimary: False)").Build();
            yield return withBaseTest.WithResponse().ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix}, ResponseDetail: (NoBase, NoPrimary)").Build();

            var fieldExpressionWithBaseField = withBaseTest
                .WithPrimaryExpression(testMetric.FieldExpressionParser.ParseUserNumericExpressionOrNull($"{testMetric.PrimaryField.Name} in [1]"))
                .WithPrimaryField(null);
            yield return fieldExpressionWithBaseField.WithResponse(baseTrue, primaryTrue).ExpectBaseSize(1).ExpectResult(1).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (InBase: True, InPrimary: True)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(baseTrue, primaryFalse).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (InBase: True, InPrimary: False)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (InBase: True, NoPrimary)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (InBase: False, NoPrimary)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(baseFalse, primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (InBase: False, InPrimary: True)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(baseFalse, primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (InBase: False, InPrimary: False)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (NoBase, InPrimary: True)").Build();
            yield return fieldExpressionWithBaseField.WithResponse(primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (NoBase, InPrimary: False)").Build();
            yield return fieldExpressionWithBaseField.WithResponse().ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Variable expression Only, ResponseDetail: (NoBase, NoPrimary)").Build();

            var baseExpressionWithField = withBaseTest
                .WithBaseExpression(testMetric.FieldExpressionParser.ParseUserBooleanExpression($"{testMetric.BaseField.Name} in [3]"))
                .WithBaseField(null);
            yield return baseExpressionWithField.WithResponse(baseTrue, primaryTrue).ExpectBaseSize(1).ExpectResult(1).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (InBase: True, InPrimary: True)").Build();
            yield return baseExpressionWithField.WithResponse(baseTrue, primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (InBase: True, InPrimary: False)").Build();
            yield return baseExpressionWithField.WithResponse(baseTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (InBase: True, NoPrimary)").Build();
            yield return baseExpressionWithField.WithResponse(baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (InBase: False, NoPrimary)").Build();
            yield return baseExpressionWithField.WithResponse(baseFalse, primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (InBase: False, InPrimary: True)").Build();
            yield return baseExpressionWithField.WithResponse(baseFalse, primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (InBase: False, InPrimary: False)").Build();
            yield return baseExpressionWithField.WithResponse(primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (NoBase, InPrimary: True)").Build();
            yield return baseExpressionWithField.WithResponse(primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (NoBase, InPrimary: False)").Build();
            yield return baseExpressionWithField.WithResponse().ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base Expression Only, ResponseDetail: (NoBase, NoPrimary)").Build();

            var baseAndFieldExpression = withBaseTest
                .WithBaseExpression(testMetric.FieldExpressionParser.ParseUserBooleanExpression($"{testMetric.BaseField.Name} in [3]"))
                .WithBaseField(null)
                .WithPrimaryExpression(testMetric.FieldExpressionParser.ParseUserNumericExpressionOrNull($"{testMetric.PrimaryField.Name} in [1]"))
                .WithPrimaryField(null);
            yield return baseAndFieldExpression.WithResponse(baseTrue, primaryTrue).ExpectBaseSize(1).ExpectResult(1).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: True, InPrimary: True)").Build();
            yield return baseAndFieldExpression.WithResponse(baseTrue, primaryFalse).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: True, InPrimary: False)").Build();
            yield return baseAndFieldExpression.WithResponse(baseTrue).ExpectBaseSize(1).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: True, NoPrimary)").Build();
            yield return baseAndFieldExpression.WithResponse(baseFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: False, NoPrimary)").Build();
            yield return baseAndFieldExpression.WithResponse(baseFalse, primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: False, InPrimary: True)").Build();
            yield return baseAndFieldExpression.WithResponse(baseFalse, primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (InBase: False, InPrimary: False)").Build();
            yield return baseAndFieldExpression.WithResponse(primaryTrue).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (NoBase, InPrimary: True)").Build();
            yield return baseAndFieldExpression.WithResponse(primaryFalse).ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (NoBase, InPrimary: False)").Build();
            yield return baseAndFieldExpression.WithResponse().ExpectBaseSize(0).ExpectResult(0).Named($"{testPrefix} With Base/Variable expression, ResponseDetail: (NoBase, NoPrimary)").Build();
        }

        private static IEnumerable<TestCaseData> NoBaseHasSecondary(string testPrefix, TestMetric testMetric, EntityValue[] primaryEntityValues, EntityValue[] secondaryEntityValues)
        {
            var withSecondary = testMetric
                .WithTrueAndBaseValues(new[] {1, 2}, new[] {1, 4, 8})
                .WithSecondaryTrueValues(new[] {4, 16});

            // These are answers for each field that can be reused to form different responses
            var primaryTrue = TestAnswer.For(withSecondary.PrimaryField, 1, primaryEntityValues);
            var primaryFalse = TestAnswer.For(withSecondary.PrimaryField, 8, primaryEntityValues);
            var primaryTrueOutsideBase = TestAnswer.For(withSecondary.PrimaryField, 2, primaryEntityValues);

            var secondaryTrue = TestAnswer.For(withSecondary.SecondaryField, 4, secondaryEntityValues);
            var secondaryFalse = TestAnswer.For(withSecondary.SecondaryField, 8, secondaryEntityValues);

            var testNamePrefix = $"{testPrefix}, FieldOperation: {testMetric.FieldOperation.ToString()}, ResponseDetail: ";

            // These are reusable responses in the for primarySecondary.
            // "Outside" means the answer matches the true values but is outside the range of base values.
            // "None" means there is no answer for that field in the response
            var trueTrue = withSecondary.WithResponse(primaryTrue, secondaryTrue).Named($"{testNamePrefix}(InBase: True, InPrimary: True, InSecondary: True)");
            var trueFalse = withSecondary.WithResponse(primaryTrue, secondaryFalse).Named($"{testNamePrefix}(InBase: True, InPrimary: True, InSecondary: False)");
            var falseTrue = withSecondary.WithResponse(primaryFalse, secondaryTrue).Named($"{testNamePrefix}(InBase: True, InPrimary: False, InSecondary: True)");
            var falseFalse = withSecondary.WithResponse(primaryFalse, secondaryFalse).Named($"{testNamePrefix}(InBase: True, InPrimary: False, InSecondary: False)");
            var outsideTrue = withSecondary.WithResponse(primaryTrueOutsideBase, secondaryTrue).Named($"{testNamePrefix}(InBase: Secondary, InPrimary: TrueOutsideBase, InSecondary: True)");
            var trueNone = withSecondary.WithResponse(primaryTrue).Named($"{testNamePrefix}(InBase: True, InPrimary: True, NoSecondary");
            var noneTrue = withSecondary.WithResponse(secondaryTrue).Named($"{testNamePrefix}(InBase: Secondary, NoPrimary, InSecondary: True)");

            switch (testMetric.FieldOperation)
            {
                case FieldOperation.Minus:
                    yield return trueTrue.ExpectBaseSize(1).ExpectResult(-3).Build();
                    yield return trueFalse.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return falseTrue.ExpectBaseSize(1).ExpectResult(-4).Build();
                    yield return falseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return outsideTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return trueNone.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return noneTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    break;
                case FieldOperation.Plus:
                    yield return trueTrue.ExpectBaseSize(1).ExpectResult(5).Build();
                    yield return trueFalse.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return falseTrue.ExpectBaseSize(1).ExpectResult(4).Build();
                    yield return falseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return outsideTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return trueNone.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return noneTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    break;
            }
        }

        private static IEnumerable<TestCaseData> HasBaseHasSecondary(string testPrefix, TestMetric testMetric, EntityValue[] primaryEntityValues, EntityValue[] secondaryEntityValues, EntityValue[] baseEntityValues)
        {
            var withBaseAndSecondary = testMetric.WithTrueAndBaseValues(new[] {1}, new[] {4}).WithSecondaryTrueValues(new[] {2});

            // These are answers for each field that can be reused to form different responses
            var primaryTrue = TestAnswer.For(testMetric.PrimaryField, 1, primaryEntityValues);
            var primaryFalse = TestAnswer.For(testMetric.PrimaryField, -91, primaryEntityValues);
            var secondaryTrue = TestAnswer.For(testMetric.SecondaryField, 2, secondaryEntityValues);
            var secondaryFalse = TestAnswer.For(testMetric.SecondaryField, -97, secondaryEntityValues);
            var baseTrue = TestAnswer.For(testMetric.BaseField, 4, baseEntityValues);
            var baseFalse = TestAnswer.For(testMetric.BaseField, -101, baseEntityValues);

            var testNamePrefix = $"{testPrefix}, FieldOperation: {testMetric.FieldOperation.ToString()}, ResponseDetail: ";

            // These are reusable responses in the for basePrimarySecondary.
            // "None" means there is no answer for that field in the response
            var trueTrueTrue = withBaseAndSecondary.WithResponse(baseTrue, primaryTrue, secondaryTrue).Named($"{testNamePrefix}(InBase: True, InPrimary: True, InSecondary: True)");
            var trueTrueFalse = withBaseAndSecondary.WithResponse(baseTrue, primaryTrue, secondaryFalse).Named($"{testNamePrefix}(InBase: True, InPrimary: True, InSecondary: False)");
            var trueFalseTrue = withBaseAndSecondary.WithResponse(baseTrue, primaryFalse, secondaryTrue).Named($"{testNamePrefix}(InBase: True, InPrimary: False, InSecondary: True)");
            var trueFalseFalse = withBaseAndSecondary.WithResponse(baseTrue, primaryFalse, secondaryFalse).Named($"{testNamePrefix}(InBase: True, InPrimary: False, InSecondary: False)");
            var falseTrueTrue = withBaseAndSecondary.WithResponse(baseFalse, primaryTrue, secondaryTrue).Named($"{testNamePrefix}(InBase: False, InPrimary: True, InSecondary: True)");
            var falseTrueFalse = withBaseAndSecondary.WithResponse(baseFalse, primaryTrue, secondaryFalse).Named($"{testNamePrefix}(InBase: False, InPrimary: True, InSecondary: False)");
            var falseFalseTrue = withBaseAndSecondary.WithResponse(baseFalse, primaryFalse, secondaryTrue).Named($"{testNamePrefix}(InBase: False, InPrimary: False, InSecondary: True)");
            var falseFalseFalse = withBaseAndSecondary.WithResponse(baseFalse, primaryFalse, secondaryFalse).Named($"{testNamePrefix}(InBase: False, InPrimary: False, InSecondary: False)");
            var trueTrueNone = withBaseAndSecondary.WithResponse(baseTrue, primaryTrue).Named($"{testNamePrefix}(InBase: True, InPrimary: True, NoSecondary)");
            var trueFalseNone = withBaseAndSecondary.WithResponse(baseTrue, primaryFalse).Named($"{testNamePrefix}(InBase: True, InPrimary: False, NoSecondary)");
            var trueNoneTrue = withBaseAndSecondary.WithResponse(baseTrue, secondaryTrue).Named($"{testNamePrefix}(InBase: True, NoPrimary, InSecondary: True)");
            var trueNoneFalse = withBaseAndSecondary.WithResponse(baseTrue, secondaryFalse).Named($"{testNamePrefix}(InBase: True, NoPrimary, InSecondary: False)");
            var falseTrueNone = withBaseAndSecondary.WithResponse(baseFalse, primaryTrue).Named($"{testNamePrefix}(InBase: False, InPrimary: True, NoSecondary)");
            var falseFalseNone = withBaseAndSecondary.WithResponse(baseFalse, primaryFalse).Named($"{testNamePrefix}(InBase: False, InPrimary: False, NoSecondary)");
            var falseNoneTrue = withBaseAndSecondary.WithResponse(baseFalse, secondaryTrue).Named($"{testNamePrefix}(InBase: False, NoPrimary, InSecondary: True)");
            var falseNoneFalse = withBaseAndSecondary.WithResponse(baseFalse, secondaryFalse).Named($"{testNamePrefix}(InBase: False, NoPrimary, InSecondary: False)");
            var noneTrueTrue = withBaseAndSecondary.WithResponse(primaryTrue, secondaryTrue).Named($"{testNamePrefix}(NoBase, InPrimary: True, InSecondary: True)");
            var noneTrueFalse = withBaseAndSecondary.WithResponse(primaryTrue, secondaryFalse).Named($"{testNamePrefix}(NoBase, InPrimary: True, InSecondary: False)");
            var noneFalseTrue = withBaseAndSecondary.WithResponse(primaryFalse, secondaryTrue).Named($"{testNamePrefix}(NoBase, InPrimary: False, InSecondary: True)");
            var noneFalseFalse = withBaseAndSecondary.WithResponse(primaryFalse, secondaryFalse).Named($"{testNamePrefix}(NoBase, InPrimary: False, InSecondary: False)");
            var noneNoneTrue = withBaseAndSecondary.WithResponse(secondaryTrue).Named($"{testNamePrefix}(NoBase, NoPrimary, InSecondary: True)");
            var noneNoneFalse = withBaseAndSecondary.WithResponse(secondaryFalse).Named($"{testNamePrefix}(NoBase, NoPrimary, InSecondary: False)");
            var noneTrueNone = withBaseAndSecondary.WithResponse(primaryTrue).Named($"{testNamePrefix}(NoBase, InPrimary: True, NoSecondary)");
            var noneFalseNone = withBaseAndSecondary.WithResponse(primaryFalse).Named($"{testNamePrefix}(NoBase, InPrimary: False, NoSecondary)");
            var noneNoneNone = withBaseAndSecondary.WithResponse(primaryFalse).Named($"{testNamePrefix}(NoBase, NoPrimary, NoSecondary)");

            switch (testMetric.FieldOperation)
            {
                case FieldOperation.Minus:
                    yield return trueTrueTrue.ExpectBaseSize(1).ExpectResult(-1).Build();
                    yield return trueTrueFalse.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return trueFalseTrue.ExpectBaseSize(1).ExpectResult(-2).Build();
                    yield return trueFalseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseTrueTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseTrueFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseFalseTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseFalseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return trueTrueNone.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return trueFalseNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return trueNoneTrue.ExpectBaseSize(1).ExpectResult(-2).Build();
                    yield return trueNoneFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseTrueNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseFalseNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseNoneTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseNoneFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneTrueTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneTrueFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneFalseTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneFalseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneNoneTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneNoneFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneTrueNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneFalseNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneNoneNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    break;
                case FieldOperation.Plus:
                    yield return trueTrueTrue.ExpectBaseSize(1).ExpectResult(3).Build();
                    yield return trueTrueFalse.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return trueFalseTrue.ExpectBaseSize(1).ExpectResult(2).Build();
                    yield return trueFalseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseTrueTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseTrueFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseFalseTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseFalseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return trueTrueNone.ExpectBaseSize(1).ExpectResult(1).Build();
                    yield return trueFalseNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return trueNoneTrue.ExpectBaseSize(1).ExpectResult(2).Build();
                    yield return trueNoneFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseTrueNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseFalseNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseNoneTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return falseNoneFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneTrueTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneTrueFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneFalseTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneFalseFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneNoneTrue.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneNoneFalse.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneTrueNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneFalseNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    yield return noneNoneNone.ExpectBaseSize(0).ExpectResult(0).Build();
                    break;
            }
        }

        private static ResponseFieldManager ResponseFieldManager(params EntityType[] entityTypes)
        {
            return new(new TestEntityTypeRepository(entityTypes));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using TestCommon.Mocks;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class MultiEntityAverageMonthlyTests
    {
        // These are all common test data that shouldn't be mutated
        private const int TrueVal = 1;
        private const int FalseVal = 0;
        private static readonly Subset Subset = TestResponseFactory.AllSubset;
        private static readonly EntityInstance[] TwoBrands = MockMetadata.CreateEntityInstances(2);
        private readonly QuotaCell[] _twoQuotaCells = MockMetadata.CreateNonInterlockedQuotaCells(Subset, 2).Cells.ToArray();
        private readonly int[] _baseVals = { FalseVal, TrueVal };
        private readonly int[] _trueVals = { TrueVal };

        private CalculationPeriod _calculatorPeriod = CalculationPeriod.Parse("2019/05/01", "2019/08/31");

        private static uint BrandQuotaCellSampleSize = 120; // As long as we use whole percentages (2dp fractions), this avoids fractional respondent numbers
        [TestCase(0.0, 0.0)]
        [TestCase(0.0, 0.5)]
        [TestCase(0.5, 1.0)]
        public async Task SimpleBrandAverageAsync(double firstBrandPercentTrue, double secondBrandPercentTrue)
        {
            var expectedResult = new[] { firstBrandPercentTrue, secondBrandPercentTrue }.Average();

            var responseFieldManager = CreateResponseFieldManager();
            responseFieldManager.Add("PositiveBuzz", Subset.Id, new[] {TestEntityTypeRepository.Brand});
            var testResponseFactory = new TestResponseMonthsPopulator(responseFieldManager);

            var positiveBuzz = responseFieldManager.CreateYesNoMeasure("PositiveBuzz", _baseVals, _trueVals);

            var positiveBuzzResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(secondBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(secondBrandPercentTrue))
            };
            int expectedPositiveBuzzSampleSize = SumCounts(positiveBuzzResults);

            var substituteLoggerFactory = MockedSubstituteLoggerFactory();

            var calculatorBuilder = new ProductionCalculatorBuilder(substituteLoggerFactory, Subset, 
                    (_twoQuotaCells[0], Weight: WeightingValue.StandardWeighting(0.5f)), 
                    (_twoQuotaCells[1], Weight: WeightingValue.StandardWeighting(0.5f)))
                .WithBrandResponses(positiveBuzz, positiveBuzzResults, testResponseFactory);
            var measureResults = await calculatorBuilder.BuildRealCalculator().Calculate(positiveBuzz);

            var actualAverageResult = calculatorBuilder.BuildRealCalculator()
                .CalculateMarketAverage(measureResults, Subset, 1, AverageType.Mean, MainQuestionType.Unknown, null)
                .Single();

            Assert.That(actualAverageResult.UnweightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize));
            Assert.That(actualAverageResult.WeightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize));
            Assert.That(actualAverageResult.WeightedResult, Is.EqualTo(expectedResult).Within(TestConstants.ResultAccuracy));
        }

        [TestCase(1.0, 0.5, 1.0)]
        [TestCase(2000.0, 0.5, 1.0)]
        [TestCase(40000.0, 0.5, 1.0)]
        public async Task SimpleBrandAverageChangesByAbsoluteAsync(double weightingFactor, double firstBrandPercentTrue, double secondBrandPercentTrue)
        {
            var expectedResult = new[] { firstBrandPercentTrue, secondBrandPercentTrue }.Average();

            var responseFieldManager = CreateResponseFieldManager();
            responseFieldManager.Add("PositiveBuzz", Subset.Id, new[] { TestEntityTypeRepository.Brand });
            var testResponseFactory = new TestResponseMonthsPopulator(responseFieldManager);

            var positiveBuzz = responseFieldManager.CreateYesNoMeasure("PositiveBuzz", _baseVals, _trueVals);

            var positiveBuzzResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(secondBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(secondBrandPercentTrue))
            };
            int expectedPositiveBuzzSampleSize = SumCounts(positiveBuzzResults);

            var substituteLoggerFactory = MockedSubstituteLoggerFactory();

            var calculatorBuilder = new ProductionCalculatorBuilder(substituteLoggerFactory, Subset,
                    (_twoQuotaCells[0], Weight: WeightingValue.ResponseLevelWeighting(weightingFactor)),
                    (_twoQuotaCells[1], Weight: WeightingValue.ResponseLevelWeighting(weightingFactor)))
                .WithBrandResponses(positiveBuzz, positiveBuzzResults, testResponseFactory);
            var measureResults = await calculatorBuilder.BuildRealCalculator().Calculate(positiveBuzz);

            var actualAverageResult = calculatorBuilder.BuildRealCalculator()
                .CalculateMarketAverage(measureResults, Subset, 1, AverageType.Mean, MainQuestionType.Unknown, null)
                .Single();

            Assert.That(actualAverageResult.UnweightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize));
            Assert.That(actualAverageResult.WeightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize*weightingFactor));
            Assert.That(actualAverageResult.WeightedResult, Is.EqualTo(expectedResult).Within(TestConstants.ResultAccuracy));
        }
        [TestCase(0.5, 0.5, 1.0)]
        [TestCase(2000.0, 0.5, 1.0)]
        [TestCase(40000.0, 0.5, 1.0)]
        public async Task SimpleBrandAverageChangesByRelativeAsync(double weightingFactor, double firstBrandPercentTrue, double secondBrandPercentTrue)
        {
            var expectedResult = new[] { firstBrandPercentTrue, secondBrandPercentTrue }.Average();

            var responseFieldManager = CreateResponseFieldManager();
            responseFieldManager.Add("PositiveBuzz", Subset.Id, new[] { TestEntityTypeRepository.Brand });
            var testResponseFactory = new TestResponseMonthsPopulator(responseFieldManager);

            var positiveBuzz = responseFieldManager.CreateYesNoMeasure("PositiveBuzz", _baseVals, _trueVals);

            var positiveBuzzResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(secondBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(secondBrandPercentTrue))
            };
            int expectedPositiveBuzzSampleSize = SumCounts(positiveBuzzResults);

            var substituteLoggerFactory = MockedSubstituteLoggerFactory();

            var calculatorBuilder = new ProductionCalculatorBuilder(substituteLoggerFactory, Subset,
                    (_twoQuotaCells[0], Weight: WeightingValue.StandardWeighting(weightingFactor)),
                    (_twoQuotaCells[1], Weight: WeightingValue.StandardWeighting(weightingFactor)))
                .WithBrandResponses(positiveBuzz, positiveBuzzResults, testResponseFactory);
            var measureResults = await calculatorBuilder.BuildRealCalculator().Calculate(positiveBuzz);

            var actualAverageResult = calculatorBuilder.BuildRealCalculator()
                .CalculateMarketAverage(measureResults, Subset, 1, AverageType.Mean, MainQuestionType.Unknown, null)
                .Single();

            Assert.That(actualAverageResult.UnweightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize));
            Assert.That(actualAverageResult.WeightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize*2*weightingFactor));
            Assert.That(actualAverageResult.WeightedResult, Is.EqualTo(expectedResult).Within(TestConstants.ResultAccuracy));
        }

        private static ILoggerFactory MockedSubstituteLoggerFactory() => Substitute.For<ILoggerFactory>();

        [TestCase(0.0, 0.0, 1, 0.0)]
        [TestCase(0.0, 1.0, 1, 0.5)]
        [TestCase(0.0, 1.0, 4, 0.8)]
        [TestCase(0.5, 1.0, 4, 0.9)]
        public async Task EqualDemogWeightedMarketAverageAsync(double firstBrandPercentTrue, double secondBrandPercentTrue, int secondBrandSizeMultiplier, double expectedAverageResult)
        {
            var responseFieldManager = CreateResponseFieldManager();
            responseFieldManager.Add("UnbiasedRelativeBrandSizes", Subset.Id, new[] {TestEntityTypeRepository.Brand});
            responseFieldManager.Add("PositiveBuzz", Subset.Id, new[] {TestEntityTypeRepository.Brand});

            var testResponseFactory = new TestResponseMonthsPopulator(responseFieldManager);
            var relativeBrandSize = responseFieldManager.CreateYesNoMeasure("UnbiasedRelativeBrandSizes", _baseVals, _trueVals);
            var positiveBuzz = responseFieldManager.CreateYesNoMeasure("PositiveBuzz", _baseVals, _trueVals);

            var positiveBuzzResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(secondBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(secondBrandPercentTrue))
            };
            int expectedPositiveBuzzSampleSize = SumCounts(positiveBuzzResults);

            var relativeBrandSizeResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(0.2, 1)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(0.2, 1)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(0.2 * secondBrandSizeMultiplier, secondBrandSizeMultiplier)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(0.2 * secondBrandSizeMultiplier, secondBrandSizeMultiplier))
            };

            var substituteLoggerFactory = MockedSubstituteLoggerFactory();

            var calculatorBuilder = new ProductionCalculatorBuilder(substituteLoggerFactory, Subset, 
                    (_twoQuotaCells[0], Weight: WeightingValue.StandardWeighting(0.5f)), 
                    (_twoQuotaCells[1], Weight: WeightingValue.StandardWeighting(0.5f)))
                .WithBrandResponses(positiveBuzz, positiveBuzzResults, testResponseFactory)
                .WithBrandResponses(relativeBrandSize, relativeBrandSizeResults, testResponseFactory);

            var measureResults = await calculatorBuilder.BuildRealCalculator().Calculate(positiveBuzz);
            var relativeBrandSizes = await calculatorBuilder.BuildRealCalculator().Calculate(relativeBrandSize);

            var actualAverageResult = calculatorBuilder.BuildRealCalculator()
                .CalculateMarketAverage(measureResults, Subset, 1, AverageType.Mean, MainQuestionType.Unknown, null, relativeBrandSizes)
                .Single();

            Assert.That(actualAverageResult.UnweightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize));
            Assert.That(actualAverageResult.WeightedResult, Is.EqualTo(expectedAverageResult).Within(TestConstants.ResultAccuracy));
        }

        [TestCase(0, 0, 1, 0.0)]
        [TestCase(0, 1, 1, 0.5)]
        [TestCase(0, 1, 4, 0.8)]
        [TestCase(0.5, 1, 4, 0.9)]
        public async Task EqualDemogWeightedMarketAverageMisalignedWeightingsAsync(double firstBrandPercentTrue, double secondBrandPercentTrue, int secondBrandSizeMultiplier, double expectedAverageResult)
        {
            // arrange
            var responseFieldManager = CreateResponseFieldManager();
            responseFieldManager.Add("UnbiasedRelativeBrandSizes", Subset.Id, new[] {TestEntityTypeRepository.Brand});
            responseFieldManager.Add("PositiveBuzz", Subset.Id, new[] {TestEntityTypeRepository.Brand});

            var testResponseFactory = new TestResponseMonthsPopulator(responseFieldManager);
            var relativeBrandSize = responseFieldManager.CreateYesNoMeasure("UnbiasedRelativeBrandSizes", _baseVals, _trueVals);
            var positiveBuzz = responseFieldManager.CreateYesNoMeasure("PositiveBuzz", _baseVals, _trueVals, _calculatorPeriod.Periods[0].StartDate.AddMonths(2).AddTicks(-1));

            var positiveBuzzRespondents = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(firstBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(secondBrandPercentTrue)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(secondBrandPercentTrue))
            };
            int expectedPositiveBuzzSampleSize = SumCounts(positiveBuzzRespondents);

            var relativeBrandSizeRespondents = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(0.2, 1)),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(0.2, 1)),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(0.2 * secondBrandSizeMultiplier, secondBrandSizeMultiplier)),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(0.2 * secondBrandSizeMultiplier, secondBrandSizeMultiplier))
            };

            var substituteLoggerFactory = MockedSubstituteLoggerFactory();

            var calculatorBuilder = new ProductionCalculatorBuilder(substituteLoggerFactory, Subset, 
                    (_twoQuotaCells[0], Weight: WeightingValue.StandardWeighting(0.5f)), 
                    (_twoQuotaCells[1], Weight: WeightingValue.StandardWeighting(0.5f)))
                .WithCalculationPeriod(_calculatorPeriod)
                .WithBrandResponses(positiveBuzz, positiveBuzzRespondents, testResponseFactory)
                .WithBrandResponses(relativeBrandSize, relativeBrandSizeRespondents, testResponseFactory);

            var measureResults = await calculatorBuilder.BuildRealCalculator().Calculate(positiveBuzz);
            var relativeBrandSizes = await calculatorBuilder.BuildRealCalculator().Calculate(relativeBrandSize);

            // act
            var actualAverageResults = calculatorBuilder.BuildRealCalculator().CalculateMarketAverage(measureResults, Subset, 1, AverageType.Mean,
                MainQuestionType.Unknown, null, relativeBrandSizes);

            // assert
            int numberOfMonthsForCalculationPeriod = 2;

            Assert.That(actualAverageResults, Has.Count.EqualTo(numberOfMonthsForCalculationPeriod));

            Assert.That(actualAverageResults.Sum(x => x.UnweightedSampleSize), Is.EqualTo(expectedPositiveBuzzSampleSize));

            Assert.That(actualAverageResults.Select(r => r.WeightedResult), Is.EqualTo(Enumerable.Repeat(expectedAverageResult, numberOfMonthsForCalculationPeriod)).Within(TestConstants.ResultAccuracy));
        }


        /// <summary>
        /// * The algorithm being tested uses the weightings in the brand relative size calculation to account for sampling bias just like in other results.
        /// </summary>
        /// <remarks>
        /// Make sure you understand the test above before trying to comprehend the expected results.
        /// The main thing is, the values used to come out as these and from my calculations I believe they're correct.
        /// I've picked the numbers carefully to exercise the system but make mental calculation plausible for the enthusiastic.
        /// </remarks>
        [TestCase(new[] { 0.0, 0.0, 0.0, 0.0 })]
        [TestCase(new[] { 0.0, 0.0, 1.0, 1.0 })]
        [TestCase(new[] { 0.25, 0.25, 0.75, 0.75 })]
        [TestCase(new[] { 0.25, 0, 0.25, 0 })]
        [TestCase(new[] { 0.1, 0.3, 0.2, 0.4 })]
        public async Task SkewedDemogWeightedMarketAverageAsync(double[] brandCellPercentTrue)
        {
            var relativeBrandSizeSampleSizeMultipliers = new[] { 1, 2, 3, 5 };
            var relativeBrandSizeMultipliers = new[] { 1, 5, 9, 7 };
            var positiveBuzzSampleSizeMultipliers = new[] { 1, 15, 2, 6 };
            var cell1WeightMultiplier = 7;

            var expectedCellTotalWeightings = GetExpectedCellTotalWeightings(positiveBuzzSampleSizeMultipliers, relativeBrandSizeSampleSizeMultipliers, cell1WeightMultiplier, relativeBrandSizeMultipliers);
            var expectedMarketAverageResult = brandCellPercentTrue.Zip(expectedCellTotalWeightings, (r, w) => r * w).Sum();

            var responseFieldManager = CreateResponseFieldManager();
            responseFieldManager.Add("UnbiasedRelativeBrandSizes", Subset.Id, new[] {TestEntityTypeRepository.Brand});
            responseFieldManager.Add("PositiveBuzz", Subset.Id, new[] {TestEntityTypeRepository.Brand});

            var testResponseFactory = new TestResponseMonthsPopulator(responseFieldManager);
            var relativeBrandSize = responseFieldManager.CreateYesNoMeasure("UnbiasedRelativeBrandSizes", _baseVals, _trueVals);
            var positiveBuzz = responseFieldManager.CreateYesNoMeasure("PositiveBuzz", _baseVals, _trueVals);

            var positiveBuzzResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(brandCellPercentTrue[0], positiveBuzzSampleSizeMultipliers[0])),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(brandCellPercentTrue[1], positiveBuzzSampleSizeMultipliers[1])),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(brandCellPercentTrue[2], positiveBuzzSampleSizeMultipliers[2])),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(brandCellPercentTrue[3], positiveBuzzSampleSizeMultipliers[3]))
            };
            int expectedPositiveBuzzSampleSize = SumCounts(positiveBuzzResults);

            var relativeBrandSizeResults = new (EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[]
            {
                (TwoBrands[0], _twoQuotaCells[0], PercentageTrueVals(0.1 * relativeBrandSizeMultipliers[0], relativeBrandSizeSampleSizeMultipliers[0])),
                (TwoBrands[0], _twoQuotaCells[1], PercentageTrueVals(0.1 * relativeBrandSizeMultipliers[1], relativeBrandSizeSampleSizeMultipliers[1])),
                (TwoBrands[1], _twoQuotaCells[0], PercentageTrueVals(0.1 * relativeBrandSizeMultipliers[2], relativeBrandSizeSampleSizeMultipliers[2])),
                (TwoBrands[1], _twoQuotaCells[1], PercentageTrueVals(0.1 * relativeBrandSizeMultipliers[3], relativeBrandSizeSampleSizeMultipliers[3]))
            };

            double firstCellWeight = 0.1;
            var skewedWeightings = new[]
            {
                (_twoQuotaCells[0], Weight: WeightingValue.StandardWeighting(firstCellWeight)), 
                (_twoQuotaCells[1], Weight: WeightingValue.StandardWeighting(firstCellWeight * cell1WeightMultiplier)),
            };

            var substituteLoggerFactory = MockedSubstituteLoggerFactory();
            var calculatorBuilder = new ProductionCalculatorBuilder(substituteLoggerFactory, Subset, skewedWeightings)
                .WithBrandResponses(positiveBuzz, positiveBuzzResults, testResponseFactory)
                .WithBrandResponses(relativeBrandSize, relativeBrandSizeResults, testResponseFactory);

            var measureResults = await calculatorBuilder.BuildRealCalculator().Calculate(positiveBuzz);
            var relativeBrandSizes = await calculatorBuilder.BuildRealCalculator().Calculate(relativeBrandSize);

            var actualAverageResult = calculatorBuilder.BuildRealCalculator()
                .CalculateMarketAverage(measureResults, Subset, 1, AverageType.Mean, MainQuestionType.Unknown, null, relativeBrandSizes)
                .Single();

            Assert.That(actualAverageResult.UnweightedSampleSize, Is.EqualTo(expectedPositiveBuzzSampleSize));
            Assert.That(actualAverageResult.WeightedResult, Is.EqualTo(expectedMarketAverageResult).Within(TestConstants.ResultAccuracy));
        }

        /// <summary>
        /// Expected per-brand per-quota cell weightings so you can see how they fit together totally independent of any other factors
        /// </summary>
        private static IEnumerable<double> GetExpectedCellTotalWeightings(int[] positiveBuzzSampleSizeMultipliers,
            int[] relativeBrandSizeSampleSizeMultipliers, int requiredCell1WeightMultiplier,
            int[] relativeBrandSizeTrueValPercentage)
        {
            // Assumes no respondents for positiveBuzz are also included in the relativeBrandSize measure. The test code ensures this, but in reality, the first would be a subset of the second and thus wouldn't need to be added
            int cell0Sample = positiveBuzzSampleSizeMultipliers[0] + positiveBuzzSampleSizeMultipliers[2] + relativeBrandSizeSampleSizeMultipliers[0] + relativeBrandSizeSampleSizeMultipliers[2];
            int cell1Sample = positiveBuzzSampleSizeMultipliers[1] + positiveBuzzSampleSizeMultipliers[3] + relativeBrandSizeSampleSizeMultipliers[1] + relativeBrandSizeSampleSizeMultipliers[3];
            double expectedCell1WeightMultiplier = cell1Sample / (double)cell0Sample;
            var cell1DemogMultiplier = requiredCell1WeightMultiplier / expectedCell1WeightMultiplier;

            var positiveBuzzCellDemogWeightings = GetDemogCellWeightings(positiveBuzzSampleSizeMultipliers, cell1DemogMultiplier);
            var relativeSizeCellResults =
                relativeBrandSizeTrueValPercentage.Zip(GetDemogCellWeightings(relativeBrandSizeSampleSizeMultipliers, cell1DemogMultiplier),
                (r, w) => r * w).ToArray();

            var brandSizeCellWeightings = GetBrandCellWeightings(relativeSizeCellResults);

            var totalBrandCellWeightings = positiveBuzzCellDemogWeightings.Zip(brandSizeCellWeightings, (w1, w2) => w1 * w2).ToArray();
            Assert.That(totalBrandCellWeightings.Sum(), Is.EqualTo(1), "Test code issue, weightings must equal 1");
            return totalBrandCellWeightings;
        }

        private static IEnumerable<double> GetBrandCellWeightings(double[] relativeSizeCellResults)
        {
            var perBrandResults = Normalize(new[]
            {
                relativeSizeCellResults[0] + relativeSizeCellResults[1],
                relativeSizeCellResults[2] + relativeSizeCellResults[3]
            }).ToArray();

            var brandResultsSplitBackIntoCells = new[]
            {
                perBrandResults[0],
                perBrandResults[0],
                perBrandResults[1],
                perBrandResults[1]
            };
            return brandResultsSplitBackIntoCells;
        }

        /// <summary>
        /// The individual cell weightings that can later be multiplied by the unweighted percentage of truevals
        /// </summary>
        private static IEnumerable<double> GetDemogCellWeightings(int[] brandCellSampleSizes, double cell1DemogMultiplier)
        {
            var brandCellWeightedSampleSizes = brandCellSampleSizes
                .Select((v, i) => i % 2 != 0 ? v * cell1DemogMultiplier : v).ToArray();
            var perCellDenominators = new[]
            {
                brandCellWeightedSampleSizes[0] + brandCellWeightedSampleSizes[1],
                brandCellWeightedSampleSizes[0] + brandCellWeightedSampleSizes[1],
                brandCellWeightedSampleSizes[2] + brandCellWeightedSampleSizes[3],
                brandCellWeightedSampleSizes[2] + brandCellWeightedSampleSizes[3]
            };
            var positiveBuzzCellDemogWeightings = brandCellWeightedSampleSizes.Zip(perCellDenominators, SafeDivide);
            return positiveBuzzCellDemogWeightings.ToArray();
        }

        private static IEnumerable<double> Normalize(IReadOnlyCollection<double> results)
        {
            var total = results.Sum();
            return results.Select(r => r / total);
        }

        private static double SafeDivide(double numerator, double denominator)
        {
            return denominator > 0 ? numerator / denominator : 0;
        }

        private static ValueResponseCount[] PercentageTrueVals(double firstBrandPercentTrue, int brandQuotaCellSampleSize = 1)
        {
            return ValueResponseCount.PercentageTrueVals(firstBrandPercentTrue, (uint)(BrandQuotaCellSampleSize * brandQuotaCellSampleSize), FalseVal, TrueVal);
        }

        private static int SumCounts((EntityInstance Brand, QuotaCell QuotaCell, ValueResponseCount[] ResponseCounts)[] results)
        {
            return results.SelectMany(r => r.ResponseCounts.Select(rc => (int)rc.Count)).Sum();
        }

        private static ResponseFieldManager CreateResponseFieldManager()
            => new(EntityTypeRepository.GetDefaultEntityTypeRepository());
    }
}

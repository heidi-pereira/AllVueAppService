using System;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using NUnit.Framework;
using static BrandVue.SourceData.CalculationPipeline.SignificanceCalculator;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    class SignificanceCalculatorTests
    {
        [Test]
        [TestCase(0.09283301, 1667U, 0.11379499, 1591U, Significance.Down)]
        [TestCase(0.08254201, 1666U, 0.0459505469, 1590U, Significance.Up)]
        [TestCase(0.08096294, 1667U, 0.0822374448, 1594U, Significance.None)]
        public void Test_percentage_significance_calculation_with_ninety_five_confidence(double currentResult,
            uint currentSample,
            double previousResult,
            uint previousSample,
            Significance significance)
        {
            var currentWeightedResult = new WeightedDailyResult(DateTimeOffset.Now)
            {
                WeightedResult = currentResult,
                UnweightedSampleSize = currentSample
            };

            var previousWeightedResult = new WeightedDailyResult(DateTimeOffset.Now)
            {
                WeightedResult = previousResult,
                UnweightedSampleSize = previousSample
            };

            var calculateSignificance = CalculateSignificance(CalculationType.YesNo,
                currentWeightedResult,
                previousWeightedResult,
                SigConfidenceLevel.NinetyFive);

            Assert.That(calculateSignificance, Is.EqualTo(significance));
        }

        [Test]
        [TestCase(27.112606, 169U, 9.88483402602296D, 27.0074329, 168U, 9.277245113690093D, Significance.None)]
        [TestCase(18.3654881, 257U, 7.940356602450915D, 16.9787464, 272U, 8.103969142332838D, Significance.Up)]
        [TestCase(12.4752941, 269U, 6.265020907291957D, 13.7138643, 319U, 6.474392224978404D, Significance.Down)]
        public void Test_average_significance_calculation_with_ninety_five_confidence(double currentResult,
            uint currentSample,
            double currentStandardDeviation,
            double previousResult,
            uint previousSample,
            double previousStandardDeviation,
            Significance significance)
        {
            var currentWeightedResult = new WeightedDailyResult(DateTimeOffset.Now)
            {
                WeightedResult = currentResult,
                StandardDeviation = currentStandardDeviation,
                UnweightedSampleSize = currentSample
            };

            var previousWeightedResult = new WeightedDailyResult(DateTimeOffset.Now)
            {
                WeightedResult = previousResult,
                StandardDeviation = previousStandardDeviation,
                UnweightedSampleSize = previousSample
            };

            var calculateSignificance = CalculateSignificance(CalculationType.Average,
                currentWeightedResult,
                previousWeightedResult,
                SigConfidenceLevel.NinetyFive);

            Assert.That(calculateSignificance, Is.EqualTo(significance));
        }

        [Test]
        [TestCase(44.7352142, 1888U, 45.14203, 1758U, Significance.None)]
        [TestCase(44.9437943, 773U, 39.6149063, 683U, Significance.Up)]
        [TestCase(32.3598442, 744U, 35.2549248, 700U, Significance.Down)]
        public void Test_NPS_significance_calculation_with_ninety_five_confidence(double currentResult,
            uint currentSample,
            double previousResult, 
            uint previousSample,
            Significance significance)
        {
            var currentWeightedResult = new WeightedDailyResult(DateTimeOffset.Now)
            {
                WeightedResult = currentResult,
                UnweightedSampleSize = currentSample
            };

            var previousWeightedResult = new WeightedDailyResult(DateTimeOffset.Now)
            {
                WeightedResult = previousResult,
                UnweightedSampleSize = previousSample
            };

            var calculateSignificance = CalculateSignificance(CalculationType.NetPromoterScore,
                currentWeightedResult,
                previousWeightedResult,
                SigConfidenceLevel.NinetyFive);

            Assert.That(calculateSignificance, Is.EqualTo(significance));
        }

        [TestCase(SigConfidenceLevel.NinetyNine)]
        [TestCase(SigConfidenceLevel.NinetyFive)]
        [TestCase(SigConfidenceLevel.Ninety)]
        public void Test_significance_always_zero_when_sample_zero(SigConfidenceLevel confidenceLevel)
        {
            foreach (var calculationType in new [] {CalculationType.Average, CalculationType.YesNo, CalculationType.NetPromoterScore})
            {
                var sigWithZeroSampleForCurrent = CalculateSignificance(calculationType,
                    new WeightedDailyResult(DateTimeOffset.Now) {WeightedResult = double.MaxValue, UnweightedSampleSize = uint.MaxValue},
                    new WeightedDailyResult(DateTimeOffset.Now) {WeightedResult = double.MinValue, UnweightedSampleSize = 0},
                    confidenceLevel);

                Assert.That(sigWithZeroSampleForCurrent, Is.EqualTo(Significance.None));

                var sigWithZeroSampleForPrevious = CalculateSignificance(calculationType,
                    new WeightedDailyResult(DateTimeOffset.Now) {WeightedResult = double.MaxValue, UnweightedSampleSize = 0},
                    new WeightedDailyResult(DateTimeOffset.Now) {WeightedResult = double.MinValue, UnweightedSampleSize = uint.MaxValue},
                    confidenceLevel);

                Assert.That(sigWithZeroSampleForPrevious, Is.EqualTo(Significance.None));
            }
        }

        private static Significance CalculateSignificance(
            CalculationType calculationType,
            WeightedDailyResult currentWeightedResult,
            WeightedDailyResult previousWeightedResult,
            SigConfidenceLevel confidenceLevel)
        {
            var measure = new Measure
            {
                CalculationType = calculationType
            };

            var tScore = CalculateTScore(measure, currentWeightedResult, previousWeightedResult);
            var calculateSignificance = SignificanceCalculator.CalculateSignificance(tScore, confidenceLevel);
            return calculateSignificance;
        }

        [Test]
        public void Test_significance_varies_with_confidence_level()
        {
            // These values are chosen so that the tScore is between the z-scores for 90, 95, 98, and 99% confidence
            // For example, tScore = 2.2 is significant at 90% and 95%, but not at 98% or 99%
            double tScoreHigh = 2.7;   // Should be significant at all levels (Up)
            double tScoreMid = 2.2;    // Up at 90/95, None at 98/99
            double tScoreLow = 1.7;    // Up at 90, None at 95/98/99
            double tScoreNone = 0.5;   // None at all levels
            double tScoreDown = -2.7;  // Down at all levels

            // High significance (Up at all)
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreHigh, SigConfidenceLevel.Ninety), Is.EqualTo(Significance.Up));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreHigh, SigConfidenceLevel.NinetyFive), Is.EqualTo(Significance.Up));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreHigh, SigConfidenceLevel.NinetyEight), Is.EqualTo(Significance.Up));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreHigh, SigConfidenceLevel.NinetyNine), Is.EqualTo(Significance.Up));

            // Mid significance (Up at 90/95, None at 98/99)
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreMid, SigConfidenceLevel.Ninety), Is.EqualTo(Significance.Up));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreMid, SigConfidenceLevel.NinetyFive), Is.EqualTo(Significance.Up));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreMid, SigConfidenceLevel.NinetyEight), Is.EqualTo(Significance.None));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreMid, SigConfidenceLevel.NinetyNine), Is.EqualTo(Significance.None));

            // Low significance (Up at 90, None at others)
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreLow, SigConfidenceLevel.Ninety), Is.EqualTo(Significance.Up));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreLow, SigConfidenceLevel.NinetyFive), Is.EqualTo(Significance.None));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreLow, SigConfidenceLevel.NinetyEight), Is.EqualTo(Significance.None));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreLow, SigConfidenceLevel.NinetyNine), Is.EqualTo(Significance.None));

            // No significance (None at all)
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreNone, SigConfidenceLevel.Ninety), Is.EqualTo(Significance.None));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreNone, SigConfidenceLevel.NinetyFive), Is.EqualTo(Significance.None));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreNone, SigConfidenceLevel.NinetyEight), Is.EqualTo(Significance.None));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreNone, SigConfidenceLevel.NinetyNine), Is.EqualTo(Significance.None));

            // Down significance (Down at all)
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreDown, SigConfidenceLevel.Ninety), Is.EqualTo(Significance.Down));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreDown, SigConfidenceLevel.NinetyFive), Is.EqualTo(Significance.Down));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreDown, SigConfidenceLevel.NinetyEight), Is.EqualTo(Significance.Down));
            Assert.That(SignificanceCalculator.CalculateSignificance(tScoreDown, SigConfidenceLevel.NinetyNine), Is.EqualTo(Significance.Down));
        }
    }
}

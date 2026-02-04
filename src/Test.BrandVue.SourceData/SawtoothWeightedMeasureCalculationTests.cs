using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.QuotaCells;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Test.BrandVue.SourceData.Utils;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    /// <summary>
    /// These tests use simple data which has a respondent each day alternating between a "yes" and a "no" for each measure.
    /// It should be easy to see what the result should be in each case.
    /// They use a large part of the core calculation code, but especially target the totaliser functionality.
    /// Also see <see cref="FlatDataWeightedMeasureCalculationTests"/>
    /// </summary>
    [TestFixture]
    public class SawtoothWeightedMeasureCalculationTests
    {
        private BrandVueDataLoader _loader;

        [OneTimeSetUp]
        public void LoadAllMahFlatDataBaby()
        {
            var settings = TestLoaderSettings.WithProduct("Test.Sawtooth");
            _loader = TestDataLoader.Create(settings);
            _loader.LoadBrandVueMetadataAndData();
        }

        [TestCase("Positive Buzz", 1, 0, "Daily")]
        [TestCase("Positive Buzz", 4.0 / 7, 3.0 / 7, "7Days")]
        [TestCase("Positive Buzz", 0.5, 0.5, "14Days")]
        [TestCase("Positive Buzz", 0.5, 0.5, "28Days")]
        [TestCase("Positive Buzz", 0.5, 0.5, "12Weeks")]
        [TestCase("Positive Buzz", 0.5, 0.5, "26Weeks")]
        [TestCase("Negative Buzz", 1, 0, "Daily")]
        [TestCase("Negative Buzz", 4.0 / 7, 3.0 / 7, "7Days")]
        [TestCase("Negative Buzz", 0.5, 0.5, "14Days")]
        [TestCase("Negative Buzz", 0.5, 0.5, "28Days")]
        [TestCase("Negative Buzz", 0.5, 0.5, "12Weeks")]
        [TestCase("Negative Buzz", 0.5, 0.5, "26Weeks")]
        [TestCase("Net Buzz", 0, 0, "Daily")]
        [TestCase("Net Buzz", 0, 0, "7Days")]
        [TestCase("Net Buzz", 0, 0, "14Days")]
        [TestCase("Net Buzz", 0, 0, "28Days")]
        [TestCase("Net Buzz", 0, 0, "12Weeks")]
        [TestCase("Net Buzz", 0, 0, "26Weeks")]
        [TestCase("Buzz Noise", 1, 0, "Daily")]
        [TestCase("Buzz Noise", 4.0 / 7, 3.0 / 7, "7Days")]
        [TestCase("Buzz Noise", 0.5, 0.5, "14Days")]
        [TestCase("Buzz Noise", 0.5, 0.5, "28Days")]
        [TestCase("Buzz Noise", 0.5, 0.5, "12Weeks")]
        [TestCase("Buzz Noise", 0.5, 0.5, "26Weeks")]
        [TestCase("Purchase Frequency In-Store (L12M)", 1, 0, "Daily")]
        [TestCase("Purchase Frequency In-Store (L12M)", 4.0 / 7, 3.0 / 7, "7Days")]
        [TestCase("Purchase Frequency In-Store (L12M)", 0.5, 0.5, "14Days")]
        [TestCase("Purchase Frequency In-Store (L12M)", 0.5, 0.5, "28Days")]
        [TestCase("Purchase Frequency In-Store (L12M)", 0.5, 0.5, "12Weeks")]
        [TestCase("Purchase Frequency In-Store (L12M)", 0.5, 0.5, "26Weeks")]
        [TestCase("NPS", 100, -100, "Daily")]
        [TestCase("NPS", (400.0 - 300) / 7, (300.0 - 400) / 7, "7Days")]
        [TestCase("NPS", 0, 0, "14Days")]
        [TestCase("NPS", 0, 0, "28Days")]
        [TestCase("NPS", 0, 0, "12Weeks")]
        [TestCase("NPS", 0, 0, "26Weeks")]
        [TestCase("Promoters", 1, 0, "Daily")]
        [TestCase("Promoters", 4.0 / 7, 3.0 / 7, "7Days")]
        [TestCase("Promoters", 0.5, 0.5, "14Days")]
        [TestCase("Promoters", 0.5, 0.5, "28Days")]
        [TestCase("Promoters", 0.5, 0.5, "12Weeks")]
        [TestCase("Promoters", 0.5, 0.5, "26Weeks")]
        [TestCase("Detractors", 1, 0, "Daily")]
        [TestCase("Detractors", 4.0 / 7, 3.0 / 7, "7Days")]
        [TestCase("Detractors", 0.5, 0.5, "14Days")]
        [TestCase("Detractors", 0.5, 0.5, "28Days")]
        [TestCase("Detractors", 0.5, 0.5, "12Weeks")]
        [TestCase("Detractors", 0.5, 0.5, "26Weeks")]
        [TestCase("Passives", 0, 0, "Daily")]
        [TestCase("Passives", 0, 0, "7Days")]
        [TestCase("Passives", 0, 0, "14Days")]
        [TestCase("Passives", 0, 0, "28Days")]
        [TestCase("Passives", 0, 0, "12Weeks")]
        [TestCase("Passives", 0, 0, "26Weeks")]
        public async Task CheckUkWeightedMeasureValuesAsync(
            string measureId,
            double expectedResult1,
            double expectedResult2,
            string averageType)
        {
            var period = CalculationPeriod.Parse("2017/02/01", "2017/08/19");

            var measure = _loader.MeasureRepository.Get(measureId);

            var subset = _loader.SubsetRepository.Get("UK");

            var brand = CommonAssert.AssertEntityFoundInRepository(_loader.EntityInstanceRepository, subset, TestEntityTypeRepository.Brand, "Aberstarsky & Fudge");

            var averageDescriptor = _loader.AverageDescriptorRepository.Get(averageType, "test").ShallowCopy();
            averageDescriptor.IncludeResponseIds = true;

            var weighted = await _loader.Calculate(subset, period, averageDescriptor, measure, brand);

            Assert.That(weighted.Length, Is.GreaterThan(0), "Got no results back");
            for (int index = 0, size = weighted.Length; index < size; ++index)
            {
                var result = weighted[index].WeightedDailyResults[0];

                if (index >= averageDescriptor.NumberOfPeriodsInAverage)
                {

                    Assert.That(result.WeightedResult, Is.EqualTo(expectedResult1).Within(TestConstants.ResultAccuracy).Or.EqualTo(expectedResult2).Within(TestConstants.ResultAccuracy),
                        $"Calculated result mismatch for average type {averageType} with measure {measureId}. Expected {expectedResult1} or {expectedResult2} but was {result.WeightedResult}.");
                }

                Assert.That(
                    result.UnweightedSampleSize,
                    Is.EqualTo(averageDescriptor.NumberOfPeriodsInAverage),
                    $"Calculated sample size mismatch for average type {averageType} with measure {measureId}.");
            }
            int expectedSampleOfOnePerDay = averageDescriptor.NumberOfPeriodsInAverage;
            foreach (var weightedResult in weighted.Skip(1))
            {
                Assert.That(weightedResult.EntityInstance.Id, Is.EqualTo(brand.OrderedInstances.Single().Id), $"Mismatch of brands {brand.OrderedInstances.Single().Name} is not equal to {weightedResult.EntityInstance.Name}");
                CommonAssert.AssertResult(weightedResult.WeightedDailyResults,
                    Is.EqualTo(expectedResult1).Within(TestConstants.ResultAccuracy)
                        .Or.EqualTo(expectedResult2).Within(TestConstants.ResultAccuracy),
                    expectedSampleOfOnePerDay, $"for average type {averageType} with measure {measureId}."
                );
            }
        }
    }
}

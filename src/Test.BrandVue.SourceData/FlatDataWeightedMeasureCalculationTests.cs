using System;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.QuotaCells;
using NUnit.Framework;
using Test.BrandVue.SourceData.Utils;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    /// <summary>
    /// These tests use simple data which has a respondent each day with a "yes" for each measure.
    /// It should be easy to see what the result should be in each case.
    /// They use a large part of the core calculation code, but especially target the totaliser functionality.
    /// Also see <see cref="SawtoothWeightedMeasureCalculationTests"/>
    /// </summary>
    [TestFixture]
    public class FlatDataWeightedMeasureCalculationTests
    {
        private BrandVueDataLoader _loader;

        [OneTimeSetUp]
        public void LoadAllMahFlatDataBaby()
        {
            var settings = TestLoaderSettings.WithProduct("Test.FlatData");;
            _loader = TestDataLoader.Create(settings);
            _loader.LoadBrandVueMetadataAndData();
        }

        [Test, Ignore("Only for debugging")]
        public void OutputAverages()
        {
            foreach (var averageDescriptor in _loader.AverageDescriptorRepository)
            {
                Console.WriteLine($"{averageDescriptor.AverageId}");
            }
        }

        [TestCase("Positive Buzz", 1, "Daily")]
        [TestCase("Positive Buzz", 1, "7Days")]
        [TestCase("Positive Buzz", 1, "14Days")]
        [TestCase("Positive Buzz", 1, "28Days")]
        [TestCase("Positive Buzz", 1, "12Weeks")]
        [TestCase("Positive Buzz", 1, "26Weeks")]

        [TestCase("Negative Buzz", 1, "Daily")]
        [TestCase("Negative Buzz", 1, "7Days")]
        [TestCase("Negative Buzz", 1, "14Days")]
        [TestCase("Negative Buzz", 1, "28Days")]
        [TestCase("Negative Buzz", 1, "12Weeks")]
        [TestCase("Negative Buzz", 1, "26Weeks")]

        [TestCase("Net Buzz", 0, "Daily")]
        [TestCase("Net Buzz", 0, "7Days")]
        [TestCase("Net Buzz", 0, "14Days")]
        [TestCase("Net Buzz", 0, "28Days")]
        [TestCase("Net Buzz", 0, "12Weeks")]
        [TestCase("Net Buzz", 0, "26Weeks")]


        [TestCase("Buzz Noise", 1, "Daily")]
        [TestCase("Buzz Noise", 1, "7Days")]
        [TestCase("Buzz Noise", 1, "14Days")]
        [TestCase("Buzz Noise", 1, "28Days")]
        [TestCase("Buzz Noise", 1, "12Weeks")]
        [TestCase("Buzz Noise", 1, "26Weeks")]

        [TestCase("Purchase Frequency In-Store (L12M)", 1, "Daily")]
        [TestCase("Purchase Frequency In-Store (L12M)", 1, "7Days")]
        [TestCase("Purchase Frequency In-Store (L12M)", 1, "14Days")]
        [TestCase("Purchase Frequency In-Store (L12M)", 1, "28Days")]
        [TestCase("Purchase Frequency In-Store (L12M)", 1, "12Weeks")]
        [TestCase("Purchase Frequency In-Store (L12M)", 1, "26Weeks")]

        [TestCase("NPS", 100, "Daily")]
        [TestCase("NPS", 100, "7Days")]
        [TestCase("NPS", 100, "14Days")]
        [TestCase("NPS", 100, "28Days")]
        [TestCase("NPS", 100, "12Weeks")]
        [TestCase("NPS", 100, "26Weeks")]


        [TestCase("Promoters", 1, "Daily")]
        [TestCase("Promoters", 1, "7Days")]
        [TestCase("Promoters", 1, "14Days")]
        [TestCase("Promoters", 1, "28Days")]
        [TestCase("Promoters", 1, "12Weeks")]
        [TestCase("Promoters", 1, "26Weeks")]

        [TestCase("Detractors", 0, "Daily")]
        [TestCase("Detractors", 0, "7Days")]
        [TestCase("Detractors", 0, "14Days")]
        [TestCase("Detractors", 0, "28Days")]
        [TestCase("Detractors", 0, "12Weeks")]
        [TestCase("Detractors", 0, "26Weeks")]

        [TestCase("Passives", 0, "Daily")]
        [TestCase("Passives", 0, "7Days")]
        [TestCase("Passives", 0, "14Days")]
        [TestCase("Passives", 0, "28Days")]
        [TestCase("Passives", 0, "12Weeks")]
        [TestCase("Passives", 0, "26Weeks")]

        public async Task CheckUKWeightedMeasureValuesAsync(string measureId, double expectedResult, string averageType)
        {
            var period = CalculationPeriod.Parse("2017/02/01", "2017/08/19");

            var measure = _loader.MeasureRepository.Get(measureId);

            var subset = _loader.SubsetRepository.Get("UK");

            var brand = CommonAssert.AssertEntityFoundInRepository(_loader.EntityInstanceRepository, subset, TestEntityTypeRepository.Brand, "Aberstarsky & Fudge");

            var averageDescriptor = _loader.AverageDescriptorRepository.Get(averageType, "test").ShallowCopy();
            averageDescriptor.IncludeResponseIds = true;

            var weighted = await _loader.Calculate(subset, period, averageDescriptor, measure, brand);
            Assert.That(weighted.Length, Is.GreaterThan(0), "Got no results back");

            int expectedSampleOfOnePerDay = averageDescriptor.NumberOfPeriodsInAverage;
            foreach (var weightedResult in weighted)
            {
                Assert.That(weightedResult.EntityInstance.Id, Is.EqualTo(brand.OrderedInstances.Single().Id), $"Mismatch of brands {brand.OrderedInstances.Single().Name} is not equal to {weightedResult.EntityInstance.Name}");
                CommonAssert.AssertResult(weightedResult.WeightedDailyResults, Is.EqualTo(expectedResult), expectedSampleOfOnePerDay, $"for average type {averageType} with measure {measureId}.");
            }
        }

        [TestCase("Positive Buzz", "Aberstarsky & Fudge", 1, 0, "Daily")]
        [TestCase("Positive Buzz", "Aberstarsky & Fudge", 0.6, 0.6, "7Days")]
        [TestCase("Positive Buzz", "Aberstarsky & Fudge", 0.6, 0.6, "14Days")]
        [TestCase("Positive Buzz", "Aberstarsky & Fudge", 0.6, 0.6, "28Days")]
        [TestCase("Positive Buzz", "Aberstarsky & Fudge", 0.6, 0.6, "12Weeks")]
        [TestCase("Positive Buzz", "Aberstarsky & Fudge", 0.6, 0.6, "26Weeks")]

        [TestCase("Positive Buzz", "Brown Clothes Only", 1, 0, "Daily")]
        [TestCase("Positive Buzz", "Brown Clothes Only", 0.4, 0.4, "7Days")]
        [TestCase("Positive Buzz", "Brown Clothes Only", 0.4, 0.4, "14Days")]
        [TestCase("Positive Buzz", "Brown Clothes Only", 0.4, 0.4, "28Days")]
        [TestCase("Positive Buzz", "Brown Clothes Only", 0.4, 0.4, "12Weeks")]
        [TestCase("Positive Buzz", "Brown Clothes Only", 0.4, 0.4, "26Weeks")]
        public async Task CheckUSWeightedMeasureValuesAsync(
            string measureId,
            string brandName,
            double expectedResult1,
            double expectedResult2,
            string averageType)
        {
            var period = CalculationPeriod.Parse("2017/02/01", "2017/08/19");

            var measure = _loader.MeasureRepository.Get(measureId);

            var subset = _loader.SubsetRepository.Get("US");

            var brand = CommonAssert.AssertEntityFoundInRepository(_loader.EntityInstanceRepository, subset, TestEntityTypeRepository.Brand, brandName);

            var averageDescriptor = _loader.AverageDescriptorRepository.Get(averageType, "test");

            var weighted = await _loader.Calculate(subset, period, averageDescriptor, measure, brand);

            var numberOfDaysInAverage = averageDescriptor.NumberOfPeriodsInAverage;
            Assert.That(weighted.Length, Is.GreaterThan(0), "Got no results back");

            for (int index = 0, size = weighted.Length; index < size; ++index)
            {
                var result = weighted[index].WeightedDailyResults[0];

                if (index > 0)
                {
                    Assert.That(result.WeightedResult, Is.EqualTo(expectedResult1).Within(TestConstants.ResultAccuracy).Or.EqualTo(expectedResult2).Within(TestConstants.ResultAccuracy),
                        $"Calculated result mismatch for average type {averageType} with measure {measureId}. Expected {expectedResult1} or {expectedResult2} but was {result.WeightedResult}.");
                }

                Assert.That(
                    result.UnweightedSampleSize,
                    Is.EqualTo(numberOfDaysInAverage),
                    $"Calculated sample size mismatch for average type {averageType} with measure {measureId}.");
            }
        }
    }
}

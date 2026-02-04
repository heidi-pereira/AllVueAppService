using BrandVue.Models;
using BrandVue.Services.ReportVue;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.QuotaCells;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Test.BrandVue.SourceData.Utils;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{

#if DEBUG // Too long for local debug loop
    [Explicit]
#endif
    [TestFixture]
    public class MonthlyAndQuarterlyMeasureValuePresenceTests
    {
        private BrandVueDataLoader _loader;

        [OneTimeSetUp]
        public void LoadUkData()
        {
            var settings = TestLoaderSettings.Default;
            _loader = TestDataLoader.Create(settings);
            _loader.LoadBrandVueMetadataAndData();
        }

        [Test]
        public async Task Should_Have_Same_Values_For_Same_Quarters_Within_Different_Periods()
        {
            var measure = _loader.MeasureRepository.Get("Net Buzz");

            var subset = _loader.SubsetRepository.Get("UK");

            var requestedInstances = CommonAssert.AssertEntityFoundInRepository(_loader.EntityInstanceRepository, subset, TestEntityTypeRepository.Brand, "Next");

            var endOfQ2 = DateTimeOffsetExtensions.ParseDate("2017/06/30");
            var endOfQ3 = DateTimeOffsetExtensions.ParseDate("2017/09/30");
            var endOfQ4 = DateTimeOffsetExtensions.ParseDate("2017/12/31");

            var averageDescriptor = _loader.AverageDescriptorRepository.Get("Quarterly", "test");

            var periodQ2Q3 = new CalculationPeriod(endOfQ2,endOfQ3);

            var resultsQ2Q3 = await _loader.Calculate(subset, periodQ2Q3, averageDescriptor, measure, requestedInstances);
            var periodQ3Q4 = new CalculationPeriod(
                endOfQ3,
                endOfQ4);

            var resultsQ3Q4 = await _loader.Calculate(subset, periodQ3Q4, averageDescriptor, measure, requestedInstances);

            Console.WriteLine($"resultsQ3Q4 for {requestedInstances.OrderedInstances.Single().Name} - {measure.Name}");
            Console.WriteLine(JsonConvert.SerializeObject(resultsQ3Q4, Formatting.Indented));

            Console.WriteLine($"resultsQ2Q3 for {requestedInstances.OrderedInstances.Single().Name} - {measure.Name}");
            Console.WriteLine(JsonConvert.SerializeObject(resultsQ2Q3, Formatting.Indented));

            Assert.That(resultsQ2Q3[0].WeightedDailyResults[1].Date, Is.EqualTo(resultsQ3Q4[0].WeightedDailyResults[0].Date), "Q3 dates not same");
            Assert.That(resultsQ2Q3[0].WeightedDailyResults[1].UnweightedSampleSize, Is.EqualTo(resultsQ3Q4[0].WeightedDailyResults[0].UnweightedSampleSize), "Sample sizes not same for Q3");
            Assert.That(resultsQ2Q3[0].WeightedDailyResults[1].WeightedResult, Is.EqualTo(resultsQ3Q4[0].WeightedDailyResults[0].WeightedResult), "Results not same for Q3");

        }

        [Test]
        public async Task Should_Have_Monthly_Value_For_Next_Net_Buzz_In_April_And_May()
        {
            var period = new CalculationPeriod(
                DateTimeOffsetExtensions.ParseDate("2017/03/01"),
                DateTimeOffsetExtensions.ParseDate("2017/05/31"));
            var averageDescriptor = _loader.AverageDescriptorRepository.Get("Monthly", "test");

            var measure = _loader.MeasureRepository.Get("Net Buzz");

            var subset = _loader.SubsetRepository.Get("UK");

            var requestedInstances = CommonAssert.AssertEntityFoundInRepository(_loader.EntityInstanceRepository, subset, TestEntityTypeRepository.Brand, "Next");

            var results = await _loader.Calculate(
                subset,
                period,
                averageDescriptor,
                measure,
                requestedInstances);

            Assert.That(
                results[0].WeightedDailyResults.Count,
                Is.EqualTo(2),
                $"Should have results for every month in period, except March, because the beginning of dataset is 18/03/2017 and we don't have enough data for March.");


            var result = results[0].WeightedDailyResults[0];
            Assert.That(result.Date, Is.EqualTo(DateTimeOffsetExtensions.ParseDate("2017/04/30")), "Should have a result for April.");

            result = results[0].WeightedDailyResults[1];
            Assert.That(result.Date,Is.EqualTo(DateTimeOffsetExtensions.ParseDate("2017/05/31")),"Should have a result for May.");
        }

        [TestCase(2)]
        public async Task Should_Have_Quarterly_Value_For_Next_Net_Buzz_In_June_And_September(
            int expectedNumberOfQuarterlyResults)
        {
            var period = new CalculationPeriod(
                DateTimeOffsetExtensions.ParseDate("2017/03/01"),
                DateTimeOffsetExtensions.ParseDate("2017/09/30"));
            var averageDescriptor = _loader.AverageDescriptorRepository.Get("Quarterly", "test");

            var measure = _loader.MeasureRepository.Get("Net Buzz");

            var subset = _loader.SubsetRepository.Get("UK");

            var requestedInstances = CommonAssert.AssertEntityFoundInRepository(_loader.EntityInstanceRepository, subset, TestEntityTypeRepository.Brand, "Next");

            var results = await _loader.Calculate(
                subset,
                period,
                averageDescriptor,
                measure,
                requestedInstances);

            Assert.That(
                results[0].WeightedDailyResults.Count,
                Is.EqualTo(expectedNumberOfQuarterlyResults),
                $"Should have results for Q2 and Q3 only; for relative and absolute changes there should only be results for Q3. " +
                $"Because the begging of data is 18/03/2017 and we don't have enough data for Q1");

            if (expectedNumberOfQuarterlyResults == 2)
            {
                AssertOnQuarterlyResult("2017/06/30", results[0].WeightedDailyResults[0]);
            }

            AssertOnQuarterlyResult("2017/09/30", results[0].WeightedDailyResults[expectedNumberOfQuarterlyResults - 1]);
        }

        private static void AssertOnQuarterlyResult(
            string expectedDateString,
            WeightedDailyResult result)
        {
            var expectedDate = DateTimeOffsetExtensions
                .ParseDate(expectedDateString);
            var monthString = CultureInfo.CurrentCulture
                .DateTimeFormat.GetMonthName(expectedDate.Month);

            Assert.That(
                result.Date,
                Is.EqualTo(expectedDate),
                $"Should have a result for {monthString}.");

            Assert.That(
                result.WeightedResult != 0,
                Is.True, $"{monthString} should have a non-zero result value.");

            Assert.That(
                result.UnweightedSampleSize != 0,
                Is.True, $"{monthString} should have a non-zero base size (i.e., number of samples).");
        }
    }
}

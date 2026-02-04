using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using NUnit.Framework;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
#if DEBUG // Too long for local debug loop
    [Explicit]
#endif
    [TestFixture]
    public class DataLoadAndMeasureCalculationTests
    {
        private BrandVueDataLoader _loader;

        private double _valueTotal;
        private double _baseSizeTotal;

        [SetUp]
        public void LoadData()
        {
            var settings = TestLoaderSettings.Default;
            _loader = TestDataLoader.Create(settings);
            var sw = new Stopwatch();
            sw.Start();
            _loader.LoadBrandVueMetadataAndData();
            sw.Stop();
            Console.WriteLine($"##teamcity[buildStatisticValue key='{nameof(DataLoadAndMeasureCalculationTests)}_DataLoad_All' value='{sw.ElapsedMilliseconds}']");

            _valueTotal = 0;
            _baseSizeTotal = 0;
        }

        /// <summary>
        /// These are some of the only tests which hit CalculateUnweightedInParallel
        /// </summary>
        [Test]
        public async Task Should_load_UK_test_data_and_calculate_all_measures_for_all_brands_without_errorAsync()
        {
            await Total_measures_for_all_brands_without_error_timedAsync("UK",
                new ResultSampleSizePair {Result = 1010.6147814949711, SampleSize = 16_208_860 }, CancellationToken.None);
        }

        /// <summary>
        /// These are some of the only tests which hit CalculateUnweightedInParallel
        /// </summary>
        [Test]
        public async Task Should_load_US_test_data_and_calculate_all_measures_for_all_brands_without_errorAsync()
        {
            await Total_measures_for_all_brands_without_error_timedAsync("US",
                new ResultSampleSizePair {Result = 1590.9672968837535, SampleSize = 17_724_775 }, CancellationToken.None);
        }

        [TestCase("US", 33_813, 32_056)]
        [TestCase("UK", 32_794, 32_785)]
        public async Task Should_load_test_data_and_verify_respondentCounts(string subsetIdentifier, int totalNumberOfRespondents, int totalNumberOfWeightedRespondents)
        {
            var subset = _loader.SubsetRepository.Get(subsetIdentifier);
            var respondentRepository = _loader.RespondentRepositorySource.GetForSubset(subset);
            var profileResponseAccessor = _loader.ProfileResponseAccessorFactory.GetOrCreate(subset);
            var weightedCellsGroup = _loader.RespondentRepositorySource.GetForSubset(subset).WeightedCellsGroup;
            var unWeightedCellsGroup = _loader.RespondentRepositorySource.GetForSubset(subset).UnWeightedCellsGroup;


            Assert.That(respondentRepository.Count, Is.EqualTo(totalNumberOfRespondents), "Wrong number of Total respondents");
            Assert.That(profileResponseAccessor.GetResponses(weightedCellsGroup).Sum(x => x.Profiles.Length),
                Is.EqualTo(totalNumberOfWeightedRespondents), "Wrong number of weighted respondents");
            Assert.That(profileResponseAccessor.GetResponses(unWeightedCellsGroup).Sum(x => x.Profiles.Length),
                Is.EqualTo(totalNumberOfRespondents-totalNumberOfWeightedRespondents), $"Wrong number of unweighted respondents");
        }

        private async Task Total_measures_for_all_brands_without_error_timedAsync(string subsetIdentifier,
            ResultSampleSizePair resultSampleSizePair, CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            sw.Start();
            await Total_single_entity_measures_all_entityies_without_errorAsync(subsetIdentifier, resultSampleSizePair, cancellationToken);
            sw.Stop();
            Console.WriteLine($"##teamcity[buildStatisticValue key='{nameof(DataLoadAndMeasureCalculationTests)}_Calculation_{subsetIdentifier}' value='{sw.ElapsedMilliseconds}']");
        }

        private async Task Total_single_entity_measures_all_entityies_without_errorAsync(string subsetIdentifier,
            ResultSampleSizePair expected, CancellationToken cancellationToken)
        {
            var subset = _loader.SubsetRepository.Get(subsetIdentifier);
            var endDate = _loader.ProfileResponseAccessorFactory.GetOrCreate(subset).EndDate;
            var startDate = endDate.Subtract(TimeSpan.FromDays(30));

            var period = new CalculationPeriod(startDate, endDate);
            var averageDescriptor = _loader.AverageDescriptorRepository.Get("28Days", "test");

            var desiredQuotaCells = _loader.RespondentRepositorySource.GetForSubset(subset).WeightedCellsGroup;
            var measures = _loader.MeasureRepository;

            var allResults = (await Task.WhenAll(
                    measures.GetAllMeasuresWithDisabledPropertyFalseForSubset(subset)
                        .Where(m => m.EntityCombination.Count() == 1)
                        .Take(5)
                        .Select(async m => 
                        {
                            var responseEntityType = m.EntityCombination.Single();
                            var allEntities = _loader.EntityInstanceRepository.GetInstancesOf(responseEntityType.Identifier, subset);
                            IFilter filter = new AlwaysIncludeFilter();
                            TargetInstances[] filterInstances = Array.Empty<TargetInstances>();
                            return await _loader.Calculator.Calculate(FilteredMetric.Create(m, filterInstances, subset, filter), period, averageDescriptor, new TargetInstances(responseEntityType, allEntities.ToArray()), desiredQuotaCells, false, cancellationToken);
                        }))
                )
                .SelectMany(results => results.SelectMany(r => r.WeightedDailyResults));

            foreach (var result in allResults)
            {
                _valueTotal += result.WeightedResult;
                _baseSizeTotal += result.UnweightedSampleSize;
            }

            Assert.That(_valueTotal, Is.EqualTo(expected.Result).Within(0.000001),//Doubles have 15 significant figures
                $"Weighted value total should be non-zero and positive but was {_valueTotal}.");

            Assert.That(_baseSizeTotal, Is.EqualTo(expected.SampleSize),
                $"Very small sample size detected for weighted values: {_baseSizeTotal}");
            
        }
    }
}

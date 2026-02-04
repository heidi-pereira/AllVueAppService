using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;
using TestCommon.DataPopulation;
using TestCommon.Weighting;

namespace Test.BrandVue.SourceData.Weightings
{
    public class WeightGeneratorForRequestedPeriodTests
    {
        private static readonly Subset UKSubset = FallbackSubsetRepository.UkSubset;
        private const string MeasureName = "part";
        
        //Intentionally have ids and indices out of sync to ensure there are no weird dependencies on the wrong one
        private static readonly QuotaCell QuotaCell1 = new(1, UKSubset, new Dictionary<string, string> { { MeasureName, "0" } }, 1) {Index = 1};
        private static readonly QuotaCell QuotaCell2 = new(0, UKSubset, new Dictionary<string, string> { { MeasureName, "1" } }, 1) {Index = 2};
        private static readonly IGroupedQuotaCells UnfilteredQuotaCells = GroupedQuotaCells.CreateUnfiltered(new[]{QuotaCell1, QuotaCell2});
        private int _id;
        
        private static readonly QuotaCellReferenceWeightings ReferenceWeightings = new(new Dictionary<string, WeightingValue>()
        {
            {QuotaCell1.ToString(), WeightingValue.StandardWeighting(0.4f)}, 
            {QuotaCell2.ToString(), WeightingValue.StandardWeighting(0.6f)},
        });
        
        private static readonly AverageDescriptor WeightedAverage = new() { AverageId = "AllWeighted", WeightingMethod = WeightingMethod.QuotaCell, TotalisationPeriodUnit = TotalisationPeriodUnit.All };
        private readonly DateTimeOffset _currentTime = new(2000, 1, 1, 4, 0, 0, TimeSpan.Zero);

        private readonly Measure _testMeasure = new() { Name = MeasureName, Field = new ResponseFieldDescriptor(MeasureName) };

        [SetUp]
        public void SetUp() => _id = 1;
        
        [Test]
        public void ShouldGenerateSameWeightsForFilteredAndUnfilteredQuotaCells()
        {
            var accessor = CreateProfileResponseAccessor();
            var mockWeightingRepository = Substitute.For<IQuotaCellReferenceWeightingRepository>();
            mockWeightingRepository.Get(UKSubset).Returns(ReferenceWeightings);

            var unfilteredWeights = WeightGeneratorForRequestedPeriod.Generate(UKSubset, accessor, mockWeightingRepository, WeightedAverage,
                UnfilteredQuotaCells, _currentTime);

            var filter = CreateMeasureFilter(_testMeasure, new[] {1});
            var sut = EnforcedFilteredGroupedQuotaCells.Create(UnfilteredQuotaCells, new[] { MeasureName });

            var filteredQuotaCells = sut.FilterUnnecessary(filter);
            var filteredWeights = WeightGeneratorForRequestedPeriod.Generate(UKSubset, accessor, mockWeightingRepository, WeightedAverage,
                filteredQuotaCells, _currentTime);

            Assert.Multiple(() =>
            {
                Assert.That(unfilteredWeights, Is.Not.Empty, "Should have unfiltered weights");
                Assert.That(filteredWeights, Is.Not.Empty, "Should have filtered weights");
                Assert.That(filteredWeights, Is.SubsetOf(unfilteredWeights), "Filtered cell optimization should not change absolute weights since it's used for weighted sample count");
            });
        }

        private ProfileResponseAccessor CreateProfileResponseAccessor()
        {
            var quotaCell1Profiles = CreateProfiles(QuotaCell1, _currentTime);
            var quotaCell2Profiles = CreateProfiles(QuotaCell2, _currentTime);
            var accessor = new ProfileResponseAccessor(quotaCell1Profiles.Concat(quotaCell2Profiles), UKSubset);
            return accessor;
        }

        private CellResponse[] CreateProfiles(QuotaCell quotaCell, params DateTimeOffset[] profileDates)
        {
            return profileDates.Select(profileDate =>
            {
                var profileResponseEntity = new ProfileResponseEntity(_id++, profileDate.ToDateInstance(), 0);
                return new CellResponse(profileResponseEntity, quotaCell);
            }).ToArray();
        }
        
        private MetricFilter CreateMeasureFilter(Measure measure, int[] primaryValues, bool treatPrimaryValuesAsRange = false, bool invert = false)
        {
            return new MetricFilter(UKSubset, measure, new EntityValueCombination(), primaryValues, invert, treatPrimaryValuesAsRange);
        }
    }
}

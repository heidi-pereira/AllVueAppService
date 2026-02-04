using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class MapQuotaCellCreationStrategyTests
    {
        private readonly IReadOnlyCollection<Dictionary<string, string>> _profilingFieldsDictionaries = ProfileQuotaCsvTestData.ProfilingFieldsDictionaries;
        
        private readonly IReadOnlyCollection<Dictionary<string, string>> _filterFieldsDictionaries = ProfileQuotaCsvTestData.FilterFieldsDictionaries;

        private MapFileQuotaCellDescriptionProvider CreateProfileToQuotaCellMapperFactory(ISubsetRepository subsets)
        {
            var categoryMappingFactory = new CategoryMappingFactory(_profilingFieldsDictionaries, _filterFieldsDictionaries);
            var quotaCellReferenceWeightingRepository = Substitute.For<IQuotaCellReferenceWeightingRepository>();
            quotaCellReferenceWeightingRepository.Get(Arg.Any<Subset>())
                .Returns(c =>
                {
                    var quotaCellReferenceWeightings = new QuotaCellReferenceWeightings(new Dictionary<string, WeightingValue>());
                    return quotaCellReferenceWeightings;
                });
            var weightingChecker = subsets.ToDictionary(s => s, s => quotaCellReferenceWeightingRepository.Get(s));
            return new MapFileQuotaCellDescriptionProvider(categoryMappingFactory, subsets);
        }

        [Test]
        public void ParsesBarometerAgeGroupCategories()
        {
            string ageGroupCategories = "16-24:16-24|25-39:25-39|40-54:40-54|55-74:55-74";

            var ageGroupMappings = new CategoryMappingFactory(_profilingFieldsDictionaries, _filterFieldsDictionaries).GetKeyValuePairs(ageGroupCategories);

            Assert.That(ageGroupMappings, Is.EquivalentTo(new[]
            {
                new KeyValuePair<string, string>("16-24", "16-24"),
                new KeyValuePair<string, string>("25-39", "25-39"),
                new KeyValuePair<string, string>("40-54", "40-54"),
                new KeyValuePair<string, string>("55-74", "55-74"),
            }));
        }

        [Test]
        [TestCase(17, ExpectedResult = "16-24")]
        [TestCase(42, ExpectedResult = "40-54")]
        [TestCase(61, ExpectedResult = "55-74")]
        public string ParsesProfilingFieldsForAgeGroupQuotaCellKeys(int ageValue)
        {
            var subsets = new FallbackSubsetRepository();

            var loader = CreateProfileToQuotaCellMapperFactory(subsets);

            var mapper = loader.GetOrCreateMapperFor(subsets.Get("UK"), DefaultQuotaFieldGroups.Age);
            return mapper.GetCellKeyForProfile(new Dictionary<string, int> {{RespondentFields.Age, ageValue}});
        }

        [Test]
        [TestCase("16-24", ExpectedResult = "16-24")]
        [TestCase("40-54", ExpectedResult = "40-54")]
        [TestCase("55-74", ExpectedResult = "55-74")]
        [TestCase(null, ExpectedResult = "All Ages")]
        public string ParsesFiltersForAgeGroupDescriptions(string ageQuotaCellKey)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);
            var mapper = loader.GetOrCreateMapperFor(subsets.Get("UK"), DefaultQuotaFieldGroups.Age);
            return mapper.GetDescriptionForQuotaCellKey(ageQuotaCellKey);
        }

        [Test]
        [TestCase(11, ExpectedResult = "L")]
        public string ParsesProfilingFieldsForUkRegionGroupQuotaCellKeys(int regionFieldValue)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);

            var mapper = loader.GetOrCreateMapperFor(subsets.Get("UK"), DefaultQuotaFieldGroups.Region);
            return mapper.GetCellKeyForProfile(new Dictionary<string, int> {{RespondentFields.Region, regionFieldValue}});
        }

        [Test]
        [TestCase(42, ExpectedResult = "S")]
        [TestCase(7, ExpectedResult = "N")]
        public string ParsesProfilingFieldsForUsRegionGroupQuotaCellKeys(int regionFieldValue)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);

            var mapper = loader.GetOrCreateMapperFor(subsets.Get("US"), DefaultQuotaFieldGroups.Region);
            return mapper.GetCellKeyForProfile(new Dictionary<string, int> {{RespondentFields.Region, regionFieldValue}});
        }

        [Test]
        [TestCase("L", ExpectedResult = "London")]
        [TestCase("M", ExpectedResult = "Midlands")]
        [TestCase(null, ExpectedResult = "All Regions")]
        public string ParsesFiltersForUkRegionGroupDescriptions(string regionQuotaCellKey)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);
            var mapper = loader.GetOrCreateMapperFor(subsets.Get("UK"), DefaultQuotaFieldGroups.Region);
            return mapper.GetDescriptionForQuotaCellKey(regionQuotaCellKey);
        }

        [Test]
        [TestCase("S", ExpectedResult = "South")]
        [TestCase("M", ExpectedResult = "Midwest")]
        [TestCase("N", ExpectedResult = "Northeast")]
        [TestCase(null, ExpectedResult = "All Regions")]
        public string ParsesFiltersForUsRegionGroupDescriptions(string regionQuotaCellKey)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);
            var mapper = loader.GetOrCreateMapperFor(subsets.Get("US"), DefaultQuotaFieldGroups.Region);
            return mapper.GetDescriptionForQuotaCellKey(regionQuotaCellKey);
        }

        [Test]
        [TestCase(0, ExpectedResult = "F")]
        [TestCase(1, ExpectedResult = "M")]
        public string ParsesProfilingFieldsForGenderQuotaCellKeys(int genderFieldValue)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);

            var profileResponse = Substitute.For<IProfileResponseEntity>();
            profileResponse.GetIntegerFieldValue(Arg.Any<ResponseFieldDescriptor>(), Arg.Any<EntityValueCombination>()).Returns(genderFieldValue);

            var mapper = loader.GetOrCreateMapperFor(subsets.First(), DefaultQuotaFieldGroups.Gender);
            return mapper.GetCellKeyForProfile(new Dictionary<string, int> {{RespondentFields.Gender, genderFieldValue}});
        }

        [Test]
        [TestCase("F", ExpectedResult = "Female")]
        [TestCase("M", ExpectedResult = "Male")]
        [TestCase(null, ExpectedResult = "Male & Female")]
        public string ParsesFiltersForGenderDescriptions(string genderQuotaCellKey)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);
            var mapper = loader.GetOrCreateMapperFor(subsets.First(), DefaultQuotaFieldGroups.Gender);
            return mapper.GetDescriptionForQuotaCellKey(genderQuotaCellKey);
        }

        [Test]
        [TestCase(1, 0, ExpectedResult = "1")]
        [TestCase(3, 0, ExpectedResult = "1")]
        [TestCase(4, 0, ExpectedResult = "2")]
        [TestCase(8, 0, ExpectedResult = "2")]
        [TestCase(9, 1, ExpectedResult = "1")]
        [TestCase(9, 4, ExpectedResult = "2")]
        [TestCase(8, -99, ExpectedResult = "2")]
        [TestCase(8, 0, ExpectedResult = "2")]
        [TestCase(8, 0, ExpectedResult = "2")]
        public string ParsesProfilingFieldsForUkSocialGroupQuotaCellKeys(int socialGroupFieldValue1, int socialGroupFieldValue2)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);

            var mapper = loader.GetOrCreateMapperFor(subsets.Get("UK"), DefaultQuotaFieldGroups.Seg);
            var fieldValues = new Dictionary<string, int>
            {
                {RespondentFields.SocioEconomicGroup1, socialGroupFieldValue1},
                {RespondentFields.SocioEconomicGroup2, socialGroupFieldValue2}
            };

            return mapper.GetCellKeyForProfile(fieldValues);
        }

        [Test]
        [TestCase("1", ExpectedResult = "ABC1")]
        [TestCase("2", ExpectedResult = "C2DE")]
        [TestCase(null, ExpectedResult = "All SEGs")]
        public string ParsesFiltersForUkSocialGroupDescriptions(string socialGroupQuotaCellKey)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);
            var mapper = loader.GetOrCreateMapperFor(subsets.Get("UK"), DefaultQuotaFieldGroups.Seg);
            return mapper.GetDescriptionForQuotaCellKey(socialGroupQuotaCellKey);
        }

        [Test]
        [TestCase(0, ExpectedResult = "L")]
        [TestCase(749, ExpectedResult = "L")]
        [TestCase(1000, ExpectedResult = "H")]
        [TestCase(4999, ExpectedResult = "H")]
        public string ParsesProfilingFieldsForUsSocialGroupQuotaCellKeys(int socialGroupFieldValue)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);

            var profileResponse = Substitute.For<IProfileResponseEntity>();
            profileResponse.GetIntegerFieldValue(Arg.Any<ResponseFieldDescriptor>(), Arg.Any<EntityValueCombination>()).Returns(socialGroupFieldValue);

            var mapper = loader.GetOrCreateMapperFor(subsets.Get("US"), DefaultQuotaFieldGroups.Seg);
            return mapper.GetCellKeyForProfile(new Dictionary<string, int> {{RespondentFields.HouseholdIncome, socialGroupFieldValue}});
        }

        [Test]
        [TestCase("L", ExpectedResult = "Low")]
        [TestCase("H", ExpectedResult = "High")]
        [TestCase(null, ExpectedResult = "All SEGs")]
        public string ParsesFiltersForUsSocialGroupDescriptions(string socialGroupQuotaCellKey)
        {
            var subsets = new FallbackSubsetRepository();
            var loader = CreateProfileToQuotaCellMapperFactory(subsets);
            var mapper = loader.GetOrCreateMapperFor(subsets.Get("US"), DefaultQuotaFieldGroups.Seg);
            return mapper.GetDescriptionForQuotaCellKey(socialGroupQuotaCellKey);
        }

    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData.Respondents
{
    public class ProfileResponseAccessorTests
    {
        private static readonly Subset UKSubset = FallbackSubsetRepository.UkSubset;

        //Intentionally have ids and indices out of sync to ensure there are no weird dependencies on the wrong one
        private static readonly QuotaCell QuotaCell1 = new(1, UKSubset, new Dictionary<string, string> { { "part", "0" } }) {Index = 1};
        private static readonly QuotaCell QuotaCell2 = new(0, UKSubset, new Dictionary<string, string> { { "part", "1" } }) {Index = 2};
        private static readonly IGroupedQuotaCells QuotaCells = GroupedQuotaCells.CreateUnfiltered((IEnumerable<QuotaCell>)new[]{QuotaCell1, QuotaCell2});
        private int _id;

        [SetUp]

        public void SetUp() => _id = 1;

        [Test]
        public void EmptyDoesNotThrow()
        {
            var inputProfiles = CreateProfiles();
            var actualReturnedProfiles = GetFromRepository(inputProfiles, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, QuotaCells);
            Assert.That(actualReturnedProfiles, Is.EquivalentTo(inputProfiles.Select(p => p.ProfileResponseEntity)));
        }

        [Test]
        public void GetsSingleProfile()
        {
            var inputProfiles = CreateProfiles(DateTimeOffset.MinValue);
            var actualReturnedProfiles = GetFromRepository(inputProfiles, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, QuotaCells);
            Assert.That(actualReturnedProfiles, Is.EquivalentTo(inputProfiles.Select(p => p.ProfileResponseEntity)));
        }

        [Test]
        public void GetsAllProfilesWithDuplicateTimestamps()
        {
            var inputProfiles = CreateProfiles(DateTimeOffset.MinValue, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, DateTimeOffset.MaxValue);
            var actualReturnedProfiles = GetFromRepository(inputProfiles, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, QuotaCells);
            Assert.That(actualReturnedProfiles, Is.EquivalentTo(inputProfiles.Select(p => p.ProfileResponseEntity)));
        }

        [Test]
        public void GetsOnlyMatchingForSingleDay()
        {
            var dayToGet = DateTimeOffset.Parse("2020-01-02");
            var before = CreateProfiles(dayToGet.AddDays(-1));
            var expected = CreateProfiles(dayToGet, dayToGet, dayToGet);
            var after = CreateProfiles(dayToGet.AddDays(1));
            var actualReturnedProfiles = GetFromRepository(before.Concat(expected).Concat(after), dayToGet, dayToGet, QuotaCells);
            Assert.That(actualReturnedProfiles, Is.EquivalentTo(expected.Select(p => p.ProfileResponseEntity)));
        }

        [Test]
        public void GivenReversedInputOrder_GetsOnlyMatchingForSingleDay()
        {
            var dayToGet = DateTimeOffset.Parse("2020-01-02");
            var before = CreateProfiles(dayToGet.AddDays(-1));
            var expected = CreateProfiles(dayToGet, dayToGet, dayToGet);
            var after = CreateProfiles(dayToGet.AddDays(1));
            var actualReturnedProfiles = GetFromRepository(after.Concat(expected).Concat(before), dayToGet, dayToGet, QuotaCells);
            Assert.That(actualReturnedProfiles, Is.EquivalentTo(expected.Select(p => p.ProfileResponseEntity)));
        }

        [Test]
        public void GetsOnlyMatchingForSingleDayForBothQuotaCells()
        {
            var dayToGet = DateTimeOffset.Parse("2020-01-02");
            var before = CreateProfiles(QuotaCell2, dayToGet.AddDays(-1), dayToGet.AddDays(-1)).Concat(CreateProfiles(QuotaCell1, dayToGet.AddDays(-1)));
            var expectedProfiles = CreateProfiles(QuotaCell2, dayToGet, dayToGet).Concat(CreateProfiles(QuotaCell1, dayToGet, dayToGet, dayToGet)).ToArray();
            var after = CreateProfiles(QuotaCell2, dayToGet.AddDays(-1)).Concat(CreateProfiles(QuotaCell1, dayToGet.AddDays(-1), dayToGet.AddDays(-1)));
            var actualReturnedProfiles = GetFromRepository(before.Concat(expectedProfiles).Concat(after), dayToGet, dayToGet, QuotaCells);

            Assert.That(actualReturnedProfiles, Is.EquivalentTo(expectedProfiles.Select(p => p.ProfileResponseEntity)));
        }

        private IProfileResponseEntity[] GetFromRepository(IEnumerable<CellResponse> inputProfile, DateTimeOffset minValue, DateTimeOffset maxValue, IGroupedQuotaCells desiredQuotaCells)
        {
            var r = new RespondentRepository(UKSubset);
            foreach (var p in inputProfile) r.Add(p.ProfileResponseEntity, p.QuotaCell);
            DateTimeOffset startDate = minValue.ToDateInstance();
            DateTimeOffset endDate = maxValue.ToDateInstance();
            var accessor = new ProfileResponseAccessor(r, UKSubset);
            return accessor.GetResponses(desiredQuotaCells).WithinTimesInclusive(startDate, endDate).SelectMany(x => x.Profiles.ToArray()).ToArray();
        }

        private CellResponse[] CreateProfiles(params DateTimeOffset[] profileDates) => CreateProfiles(QuotaCell1, profileDates);

        private CellResponse[] CreateProfiles(QuotaCell quotaCell, params DateTimeOffset[] profileDates)
        {
            return profileDates.Select(profileDate =>
            {
                var profileResponseEntity = new ProfileResponseEntity(_id++, profileDate.ToDateInstance(), 0);
                return new CellResponse(profileResponseEntity, quotaCell);
            }).ToArray();
        }
    }
}

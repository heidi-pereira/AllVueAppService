using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class RespondentRepositoryQuotaCellTests
    {
        private static readonly Subset UKSubset = new() { Id = "UK" };
        private static readonly CustomQuotaCellComparer QuotaCellComparer = new();

        [Test]
        public void AddingQuotaCellAlsoAddsUnweightedCell()
        {
            var respondentRepository = new RespondentRepository(UKSubset);
            var quotaCell = new QuotaCell(0, UKSubset, new Dictionary<string, string>());
            var profile = new ProfileResponseEntity(0, DateTimeOffset.Now, 0);
            respondentRepository.Add(profile, quotaCell);
            Assert.That(respondentRepository.WeightedCellsGroup.Cells.Count, Is.EqualTo(1));
            Assert.That(respondentRepository.AllCellsGroup.Cells.Count, Is.EqualTo(2));
            var expectedQuotaCell = new QuotaCell(0, UKSubset, default(IReadOnlyDictionary<string, int>));
            Assert.That(respondentRepository.WeightedCellsGroup.Cells.Single(), Is.EqualTo(expectedQuotaCell));
            Assert.That(respondentRepository.AllCellsGroup.Cells.First(), Is.EqualTo(QuotaCell.UnweightedQuotaCell(UKSubset)));
        }

        [Test]
        public void GetsUnweightedCellCorrectlyWhenGetOrAdd()
        {
            var respondentRepository = new RespondentRepository(UKSubset);
            var unweightedCell = QuotaCell.UnweightedQuotaCell(UKSubset);
            var quotaCell = new QuotaCell(0, UKSubset, new Dictionary<string, string>());
            var profile = new ProfileResponseEntity(0, DateTimeOffset.Now, 0);
            var unweightedProfile = new ProfileResponseEntity(1, DateTimeOffset.Now, 0);
            respondentRepository.Add(profile, quotaCell);
            respondentRepository.Add(unweightedProfile, unweightedCell);
            Assert.That(respondentRepository.WeightedCellsGroup.Cells.Count, Is.EqualTo(1));
            Assert.That(respondentRepository.AllCellsGroup.Cells.First(), Is.EqualTo(unweightedCell));
        }

        [Test]
        public void IfUnweightedCellIsPassedWithOtherCellsItIsNotDuplicated()
        {
            var respondentRepository = new RespondentRepository(UKSubset);
            var unweightedCell = QuotaCell.UnweightedQuotaCell(UKSubset);
            var cell1Parts = new Dictionary<string, string> { { "Field1", "1" } };
            var cell2Parts = new Dictionary<string, string> { { "Field1", "2" } };
            var quotaCell1 = new QuotaCell(0, UKSubset, cell1Parts){Index = 1};
            var profile1 = new ProfileResponseEntity(0, DateTimeOffset.Now, 0);
            var unweightedProfile = new ProfileResponseEntity(1, DateTimeOffset.Now, 0);
            var quotaCell2 = new QuotaCell(1, UKSubset, cell2Parts) { Index =2 };
            var profile2 = new ProfileResponseEntity(2, DateTimeOffset.Now, 0);
            respondentRepository.Add(profile1, quotaCell1);
            respondentRepository.Add(unweightedProfile, unweightedCell);
            respondentRepository.Add(profile2, quotaCell2);
            var expectedWeightedCells = new[] { quotaCell1, quotaCell2 };
            var expectedAllCells = unweightedCell.Yield().Concat(expectedWeightedCells);
            Assert.That(respondentRepository.WeightedCellsGroup.Cells, Is.EquivalentTo(expectedWeightedCells).Using(QuotaCellComparer));
            Assert.That(respondentRepository.AllCellsGroup.Cells, Is.EquivalentTo(expectedAllCells).Using(QuotaCellComparer));
        }

        private class CustomQuotaCellComparer : IEqualityComparer<QuotaCell>
        {
            public bool Equals(QuotaCell x, QuotaCell y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id && x.Index == y.Index;
            }

            public int GetHashCode(QuotaCell obj)
            {
                return HashCode.Combine(obj.Id, obj.Index);
            }
        }
    }
}

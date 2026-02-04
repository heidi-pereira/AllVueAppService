using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class OrFilterTests
    {
        [TestCase(new[] {true, true}, ExpectedResult = true)]
        [TestCase(new[] {true, false}, ExpectedResult = true)]
        [TestCase(new[] {false, true}, ExpectedResult = true)]
        [TestCase(new[] {false, false}, ExpectedResult = false)]
        public bool ApplyShouldCombineFiltersCorrectly(bool[] filterResults)
        {
            var filters = filterResults.Select(r =>
            {
                var mockFilter = Substitute.For<IFilter>();
                mockFilter.CreateForEntityValues(Arg.Any<EntityValueCombination>()).Returns(_ => r);
                return mockFilter;
            }).ToArray();

            var orFilter = new OrFilter(filters);

            return orFilter.CreateForEntityValues(default)(null);
        }

        [Test]
        public void ShouldCombineFieldDataTargets()
        {
            AndFilterTests.AssertCombinesFieldsAndDataTargets((firstFilter, secondFilter) => new OrFilter(new[] { firstFilter, secondFilter }));
        }
    }
}
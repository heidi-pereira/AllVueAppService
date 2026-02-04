using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class AndFilterTests
    {
        [TestCase(new[] {true, true}, ExpectedResult = true)]
        [TestCase(new[] {true, false}, ExpectedResult = false)]
        [TestCase(new[] {false, true}, ExpectedResult = false)]
        [TestCase(new[] {false, false}, ExpectedResult = false)]
        public bool ApplyShouldCombineFiltersCorrectly(bool[] filterResults)
        {
            var filters = filterResults.Select(r =>
            {
                var mockFilter = Substitute.For<IFilter>();
                mockFilter.CreateForEntityValues(Arg.Any<EntityValueCombination>()).Returns(_ => r);
                return mockFilter;
            }).ToArray();

            var andFilter = new AndFilter(filters);

            return andFilter.CreateForEntityValues(default)(null);
        }

        [Test]
        public void ShouldCombineFieldDataTargets()
        {
            AssertCombinesFieldsAndDataTargets((firstFilter, secondFilter) => new AndFilter(new[] {firstFilter, secondFilter}));
        }

        public static void AssertCombinesFieldsAndDataTargets(Func<IFilter, IFilter, IFilter> createCompositeFilter)
        {
            var firstFilter = Substitute.For<IFilter>();
            firstFilter.GetFieldDependenciesAndDataTargets(Arg.Any<IReadOnlyCollection<IDataTarget>>()).Returns(new FieldsAndDataTargets(
            [
                new ResponseFieldDescriptor("Age"), new ResponseFieldDescriptor("Positive_Buzz"),
                new ResponseFieldDescriptor("Consider_Product")
            ], [
                new DataTarget(new EntityType("Brand", "Brand", "Brands"), new int[] { 1, 2 }),
                new DataTarget(new EntityType("Brand", "Brand", "Brands"), new int[] { 3, 4 }),
                new DataTarget(new EntityType("Product", "Product", "Product"), new int[] { 5, 6 }),
            ]));

            var secondFilter = Substitute.For<IFilter>();
            secondFilter.GetFieldDependenciesAndDataTargets(Arg.Any<IReadOnlyCollection<IDataTarget>>()).Returns(
                new FieldsAndDataTargets(
                [
                    new ResponseFieldDescriptor("Gender"),
                    new ResponseFieldDescriptor("Negative_Buzz"),
                    new ResponseFieldDescriptor("Consider_Product")
                ], [

                    new DataTarget(new EntityType("Brand", "Brand", "Brands"), new int[] { 7, 8 }),
                    new DataTarget(new EntityType("Brand", "Brand", "Brands"), new int[] { 3, 9 }),
                    new DataTarget(new EntityType("Product", "Product", "Product"), new int[] { 5, 10 }),
                ]));

            var andFilter = createCompositeFilter(firstFilter, secondFilter);

            var fieldDataTargets = andFilter.GetFieldDependenciesAndDataTargets(null);

            Assert.Multiple(() =>
                {
                    Assert.That(fieldDataTargets.Fields.Count, Is.EqualTo(5), "There should be 5 unique fields");
                    Assert.That(fieldDataTargets.Fields.Select(f => f.Name), Is.EquivalentTo(new[] {"Age", "Positive_Buzz", "Consider_Product", "Gender", "Negative_Buzz"}));
                    var dataTargets = fieldDataTargets.DataTargets;
                    Assert.That(dataTargets.Count, Is.GreaterThanOrEqualTo(2), "Consider_Product should have 2 data targets");
                    Assert.That(dataTargets.Single(t => t.EntityType.Identifier == "Product").SortedEntityInstanceIds, Is.EquivalentTo(new[] {5, 6, 10}));
                }
            );
        }
    }
}
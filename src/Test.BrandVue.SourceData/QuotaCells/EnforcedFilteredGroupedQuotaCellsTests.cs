using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData.QuotaCells
{
    internal class EnforcedFilteredGroupedQuotaCellsTests
    {
        private const string Gender = "CustomGender";
        private const string Region = "CustomRegion";
        private readonly Measure _genderMeasure = new() { Name = Gender, Field = new ResponseFieldDescriptor(Gender) };
        private readonly Measure _regionMeasure = new() { Name = Region, Field = new ResponseFieldDescriptor(Region) };
        private readonly Measure _nonWeightingMeasure = new() { Name = "NonWeightingMeasure", Field = new ResponseFieldDescriptor("NonWeightingField") };
        private static readonly FallbackSubsetRepository SubsetRepo = new();
        private static readonly Subset Subset = SubsetRepo.First();
        private static readonly AverageDescriptor WeightedAverage = new() { AverageId = "Weighted", WeightingMethod = WeightingMethod.QuotaCell };
        private static readonly AverageDescriptor UnweightedAverage = new() { AverageId = "Unweighted", WeightingMethod = WeightingMethod.None };

        [Test]
        public void GivenUnrelatedFilter_ThenFiltersOutNothing()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_nonWeightingMeasure, new[] { 0 }), WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void GivenInvertedFilter_ThenFiltersOutNothing()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_genderMeasure, new[] { 0 }, invert: true), WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void GivenSingleRangeMeasureFilter_ThenFiltersOutUnnecessaryHalf()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_genderMeasure, new[] { 0, 0 }, true), WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenSingleDiscreteMeasureFilter_ThenFiltersOutUnnecessaryHalf()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_genderMeasure, new[] { 0 }), WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenSingleAndedDiscreteMeasureFilter_ThenFiltersOutUnnecessaryHalf()
        {
            var filter = new AndFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenAndedDiscreteMeasureFilters_ThenFiltersOutUnnecessaryHalf()
        {
            var filter = new AndFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }), CreateMeasureFilter(_nonWeightingMeasure, new[] { 0 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenOredDiscreteMeasureFilter_WhenOneMeasureFiltered_ThenFiltersOutUnnecessary()
        {
            var filter = new OrFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenNestedOredDiscreteMeasureFilter_WhenOneMeasureFiltered_ThenFiltersOutUnnecessary()
        {
            var filter = new OrFilter(new[] { new OrFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }) }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenNestedOredDiscreteMeasureFilters_WhenOneMeasureFiltered_ThenFiltersOutNothing()
        {
            var filter = new OrFilter(new IFilter[] { new OrFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }) }), CreateMeasureFilter(_regionMeasure, new[] { 0 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void GivenAndedDiscreteMeasureFilters_WhenBothMeasuresFiltered_ThenFiltersOutUnnecessary()
        {
            var filter = new AndFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }), CreateMeasureFilter(_regionMeasure, new[] { 0 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0 }));
        }

        [Test]
        public void GivenDuplicateAndedMeasureFilters_ThenFiltersOutSingleValue()
        {
            var filter = new AndFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }), CreateMeasureFilter(_genderMeasure, new[] { 1, 2 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0 }));
        }

        [Test]
        public void GivenDuplicateOredMeasureFilters_ThenNothingIsFiltered()
        {
            var filter = new OrFilter(new[] { CreateMeasureFilter(_genderMeasure, new[] { 0 }), CreateMeasureFilter(_genderMeasure, new[] { 1, 2 }) });
            var filteredCells = FilterUnnecessary(filter, WeightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { 0, 0, 0, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void WithUnweighted_GivenUnrelatedFilter_ThenFiltersOutNothing()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_nonWeightingMeasure, new[] { 0 }), UnweightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { default(int?), 0, 0, 0, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void WithUnweighted_GivenInvertedFilter_ThenFiltersOutNothing()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_genderMeasure, new[] { 0 }, invert: true), UnweightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { default(int?), 0, 0, 0, 0, 1, 1, 1, 1 }));
        }

        [Test]
        public void WithUnweighted_GivenSingleRangeMeasureFilter_ThenFiltersOutHalf()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_genderMeasure, new[] { 0, 0 }, true), UnweightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { default(int?), 0, 0, 0, 0 }));
        }

        [Test]
        public void WithUnweighted_GivenSingleDiscreteMeasureFilter_ThenFiltersOutHalf()
        {
            var filteredCells = FilterUnnecessary(CreateMeasureFilter(_genderMeasure, new[] { 0 }), UnweightedAverage);
            Assert.That(GetKeyParts(filteredCells, Gender), Is.EqualTo(new[] { default(int?), 0, 0, 0, 0 }));
        }

        private IGroupedQuotaCells FilterUnnecessary(IFilter filter, AverageDescriptor averageDescriptor)
        {
            var respondentRepository = new RespondentRepository(Subset);
            foreach (var cellDef in EnumerableExtensions.CartesianProduct(new[] { 0, 1 }, new[] { 0, 1, 2, 3 }))
            {
                var cell = new Dictionary<string, string>
                {
                    { Gender, cellDef[0].ToString() },
                    { Region, cellDef[1].ToString() }
                };
                var quotaCell = new QuotaCell(respondentRepository.Count, Subset, cell);
                var dateTimeOffset = DateTimeOffset.Parse("2020-12-31");
                var existingRespondentsCount = respondentRepository.Count;
                var profileResponse = new ProfileResponseEntity(existingRespondentsCount, dateTimeOffset, -1);
                respondentRepository.Add(profileResponse, quotaCell);
            }

            var groupedQuotaCells = respondentRepository.GetGroupedQuotaCells(averageDescriptor);
            var sut = EnforcedFilteredGroupedQuotaCells.Create(groupedQuotaCells, new[] { Region, Gender });
            return sut.FilterUnnecessary(filter);
        }

        private static IEnumerable<int?> GetKeyParts(IGroupedQuotaCells filteredCells, string fieldGroupName)
        {
            return filteredCells.Cells.Select(c => c.FieldGroupToKeyPart.TryGetValue(fieldGroupName, out var v) ? int.Parse(v) : default(int?));
        }

        private MetricFilter CreateMeasureFilter(Measure genderMeasure, int[] primaryValues, bool treatPrimaryValuesAsRange = false, bool invert = false)
        {
            return new MetricFilter(Subset, genderMeasure, new EntityValueCombination(), primaryValues, invert, treatPrimaryValuesAsRange);
        }

    }
}

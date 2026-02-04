using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd
{
    public class FilterModelFactoryTests
    {
        private IEntityRepository _entityRepository;
        private IQuestionTypeLookupRepository _questionTypeLookupRepository;
        private CrosstabFilterModelFactory _filterModelFactory;

        [SetUp]
        public void SetUp()
        {
            _entityRepository = Substitute.For<IEntityRepository>();
            _questionTypeLookupRepository = Substitute.For<IQuestionTypeLookupRepository>();
            _filterModelFactory = new CrosstabFilterModelFactory(_entityRepository, _questionTypeLookupRepository);
        }

        [TestCase("1:Yes", "Yes", new[] {1}, false, false)]
        [TestCase("!1:No", "No", new[] {1}, false, true)]
        [TestCase("1,2,3:Trio", "Trio", new[] {1, 2, 3}, false, false)]
        [TestCase("!1,2,3:Not trio", "Not trio", new[] {1, 2, 3}, false, true)]
        [TestCase("1-5:UpToFive", "UpToFive", new[] {1, 5}, true, false)]
        [TestCase("!1-5:Over five", "Over five", new[] {1, 5}, true, true)]
        public void ShouldParseZeroEntityMeasureFilterMappingCorrectly(string mapping, string expectedName, int[] expectedValues, bool expectTreatAsRange, bool expectInvert)
        {
            var measure = new Measure
            {
                Name = "TestMeasure",
                FilterValueMapping = mapping,
                Field = new ResponseFieldDescriptor("TestField")
            };
            
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "TestMeasure", MainQuestionType.SingleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);

            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToArray();

            Assert.Multiple(() =>
                {
                    var filterModel = models.Single();
                    Assert.That(filterModel.Name, Is.EqualTo(expectedName), "Incorrect name");

                    var measureFilter = filterModel.Filters.Single();
                    Assert.That(expectedValues, Is.EqualTo(measureFilter.Values), "Incorrect values");
                    Assert.That(measureFilter.TreatPrimaryValuesAsRange, Is.EqualTo(expectTreatAsRange), $"Incorrect {nameof(measureFilter.TreatPrimaryValuesAsRange)}");
                    Assert.That(measureFilter.Invert, Is.EqualTo(expectInvert), "Incorrect invert");
                }
            );
        }

        [Test]
        public void FilterModelsShouldBeCreatedForEachMapping()
        {
            var measure = new Measure
            {
                Name = "Age",
                FilterValueMapping = "20-29:Twenties|30-39:Thirties",
                Field = new ResponseFieldDescriptor("Age")
            };

            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "Age", MainQuestionType.SingleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);

            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToArray();

            Assert.Multiple(() =>
                {
                    Assert.That(models.Length, Is.EqualTo(2), "Should have one filter per mapping");
                    Assert.That(models[0].Name, Is.EqualTo("Twenties"), "Incorrect name");
                    var thirtiesFilter = models[1];
                    Assert.That(thirtiesFilter.Name, Is.EqualTo("Thirties"), "Incorrect name");
                    Assert.That(thirtiesFilter.Filters.Single().TreatPrimaryValuesAsRange, Is.True, "Expected filter to treat primary values as range");
                    Assert.That(thirtiesFilter.Filters.Single().Values, Is.EquivalentTo(new[] {30, 39}), "Incorrect values");
                }
            );
        }

        [Test]
        public void WhenSingleEntityMeasureWithNoMapping_ShouldCreateFilterForEachInstance()
        {
            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType("Type", "Type", "Types")) {ValueEntityIdentifier = "Type"},
            };
            
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.SingleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);
            
            var entityInstances = new[]
            {
                new EntityInstance {Id = 1, Name = "A"},
                new EntityInstance {Id = 2, Name = "B"},
                new EntityInstance {Id = 3, Name = "C"},
            };
            _entityRepository.GetInstancesOf(Arg.Is("Type"), Arg.Any<Subset>()).Returns(entityInstances);

            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToArray();

            Assert.Multiple(() =>
                {
                    Assert.That(models.Length, Is.EqualTo(3), "Should have one filter per instance");
                    Assert.That(new[] {1, 2, 3}, Is.EqualTo(models.Select(m => m.Filters.Single().EntityInstances["Type"].Single())), "Incorrect instance id");
                    Assert.That(new[] {1, 2, 3}, Is.EqualTo(models.Select(m => m.Filters.Single().Values.Single())), "Incorrect filter values");
                    Assert.That(new[] {"A", "B", "C"}, Is.EqualTo(models.Select(m => m.Name)), "Incorrect filter names");
                }
            );
        }

        [Test]
        public void WhenSingleEntityMeasureWithMapping_ShouldCreateOrFilter()
        {
            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType("Type", "Type", "Types")) {ValueEntityIdentifier = "Type"},
                FilterValueMapping = "1,2:Grouped|3:Other"
            };

            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.SingleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);

            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToArray();

            Assert.Multiple(() =>
                {
                    Assert.That(models.Length, Is.EqualTo(2), "Should have one filter per mapping");
                    Assert.That(new[] {"Grouped", "Other"}, Is.EqualTo(models.Select(m => m.Name)), "Incorrect filter names");
                    var grouped = models[0];
                    Assert.That(grouped.FilterOperator, Is.EqualTo(FilterOperator.Or));
                    Assert.That(new[] {1, 2}, Is.EqualTo(grouped.Filters.Select(m => m.EntityInstances["Type"].Single())), "Incorrect instance id");
                    Assert.That(new[] {1, 2}, Is.EqualTo(grouped.Filters.Select(m => m.Values.Single())), "Incorrect filter values");
                }
            );
        }

        [Test]
        public void WhenSingleEntityMeasureWithMapping_AndIsInverted_ShouldCreateAndFilter()
        {
            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType("Type", "Type", "Types")) {ValueEntityIdentifier = "Type"},
                FilterValueMapping = "!1-3:Grouped"
            };

            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.SingleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);

            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToArray();

            Assert.Multiple(() =>
                {
                    var grouped = models.Single();
                    Assert.That(grouped.Filters.All(f => f.Invert), Is.True, "All measure filters should be inverted");
                    Assert.That(grouped.FilterOperator, Is.EqualTo(FilterOperator.And), "Should be AND when inverted");
                    Assert.That(new[] {1, 2, 3}, Is.EqualTo(grouped.Filters.Select(m => m.EntityInstances["Type"].Single())), "Incorrect instance id");
                    Assert.That(new[] {1, 2, 3}, Is.EqualTo(grouped.Filters.Select(m => m.Values.Single())), "Incorrect filter values");
                }
            );
        }

        [Test]
        public void MultipleChoiceSingleEntityMetricsShouldCreateFilterModelForEachEntityInstance()
        {
            const string testEntityTypeName = "Type";

            var testEntityTypeInstances = new List<EntityInstance>()
            {
                new EntityInstance() {Id = 1, Name = "First choice"},
                new EntityInstance() {Id = 2, Name = "Second choice"},
                new EntityInstance() {Id = 3, Name = "Third choice"}
            };

            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType(testEntityTypeName, testEntityTypeName, "Types")) { ValueEntityIdentifier = "Something not type" },
                LegacyPrimaryTrueValues = { Values = new [] { 1 } },
                FilterValueMapping = "1:Yes|!1:No",
            };
            
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.MultipleChoice },
            };

            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);
            _entityRepository.GetInstancesOf(testEntityTypeName, Arg.Any<Subset>()).Returns(testEntityTypeInstances);
            
            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(new[] { 1, 2, 3 }, Is.EqualTo(models.Select(m => m.Filters.First().EntityInstances["Type"].Single())), "Incorrect instance id");
                Assert.That(new[] { 1, 1, 1 }, Is.EqualTo(models.SelectMany(m => m.Filters.First().Values)), "Incorrect filter values");
            });
        }

        [Test]
        public void MultipleChoiceMetricsShouldTakeFilterValuesFromPrimaryTrueValues()
        {
            const string testEntityTypeName = "Type";

            var testEntityTypeInstances = new List<EntityInstance>()
            {
                new EntityInstance() {Id = 1, Name = "First choice"},
                new EntityInstance() {Id = 2, Name = "Second choice"},
                new EntityInstance() {Id = 3, Name = "Third choice"}
            };

            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType(testEntityTypeName, testEntityTypeName, "Types")) { ValueEntityIdentifier = "Something not type" },
                LegacyPrimaryTrueValues = { Values = new [] { 2,3,4,5 } },
                FilterValueMapping = "2,3,4,5:Yes|1:No",
            };
            
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.MultipleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);
            _entityRepository.GetInstancesOf(testEntityTypeName, Arg.Any<Subset>()).Returns(testEntityTypeInstances);
            
            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToList();

            Assert.That(new[] { 2, 3, 4, 5 }, Is.EqualTo(models.First().Filters.First().Values), "Incorrect filter values");
        }

        [Test]
        public void MultipleChoiceMetricsShouldTakeFilterValuesFromPrimaryRange()
        {
            const string testEntityTypeName = "Type";

            var testEntityTypeInstances = new List<EntityInstance>()
            {
                new EntityInstance() {Id = 1, Name = "First choice"},
                new EntityInstance() {Id = 2, Name = "Second choice"},
                new EntityInstance() {Id = 3, Name = "Third choice"}
            };

            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType(testEntityTypeName, testEntityTypeName, "Types")) { ValueEntityIdentifier = "Something not type" },
                LegacyPrimaryTrueValues = new AllowedValues() { Minimum = 1, Maximum = 5 },
                FilterValueMapping = "1,2,3,4,5:Yes|!1:No",
            };
            
            _entityRepository.GetInstancesOf(testEntityTypeName, Arg.Any<Subset>()).Returns(testEntityTypeInstances);
            
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.MultipleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);
            
            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), false).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(new[] { 1, 5 }, Is.EqualTo(models.First().Filters.First().Values), "Incorrect filter values");
                Assert.That(models.First().Filters.First().TreatPrimaryValuesAsRange, Is.True, message: "Not identifying primary values as range");
            });
        }

        [Test]
        public void MultipleChoiceShouldSupportFilterValueMapping()
        {
            const string testEntityTypeName = "Type";
            var measure = new Measure
            {
                Name = "SingleEntity",
                Field = new ResponseFieldDescriptor("SingleEntity", new EntityType(testEntityTypeName, testEntityTypeName, "Types")) { ValueEntityIdentifier = "Something not type" },
                FilterValueMapping = "1:Yes|-99:No",
            };
            
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { "SingleEntity", MainQuestionType.MultipleChoice },
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);
            
            var models = _filterModelFactory.GetAllFiltersForMeasure(measure, new Subset(), Array.Empty<CrossMeasureFilterInstance>(), true).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(models.Length, Is.EqualTo(2), "Should have one filter per mapping");
                Assert.That(models[0].Name, Is.EqualTo("Yes"), "Incorrect name");
                Assert.That(models[1].Name, Is.EqualTo("No"), "Incorrect name");
                Assert.That(new[] { -1, -1 }, Is.EqualTo(models.Select(m => m.Filters.Single().EntityInstances["Type"].Single())), "Incorrect instance id");
                Assert.That(new[] { 1, -99 }, Is.EqualTo(models.Select(m => m.Filters.Single().Values.Single())), "Incorrect filter values");
            });
        }
    }
}
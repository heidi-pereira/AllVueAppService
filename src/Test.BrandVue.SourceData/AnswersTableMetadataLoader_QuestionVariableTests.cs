using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class AnswersTableMetadataLoader_QuestionVariableTests
    {
        private static EntityTypeRepository _responseEntityTypeRepository;
        private const int FirstSurveyId = 1;
        private const int SecondSurveyId = 2;

        [SetUp]
        public void Setup()
        {
            _responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
        }

        [Test]
        public void FieldNamesAreStable_WhenSurveyRepublished()
        {
            var contextFactory = new TestChoiceSetReaderFactory();
            var testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            using var context = contextFactory.CreateDbContext();
            var subsetRepository = CreateSubsetRepository(TestResponseFactory.AllSubset.Id);
            var choiceSetOne = CreateChoiceSetOne(FirstSurveyId);
            context.ChoiceSets.Add(choiceSetOne);

            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Q1",
                MasterType = "RADIO",
                ItemNumber = 1,
                QuestionText = "How much do you like football?",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Q2",
                MasterType = "RADIO",
                ItemNumber = 2,
                QuestionText = "How much do you like cheese?",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

            context.SaveChanges();

            string[] originalFieldNames =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory)
                    .Select(f => f.Name).ToArray();
            foreach (var field in originalFieldNames)
            {
                Assert.That(field, Does.Contain("football").Or.Contain("cheese"), "Sanity check that rarest words picked");
            }

            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Q3",
                MasterType = "RADIO",
                ItemNumber = 3,
                QuestionText = "Football football cheese cheese",
                AnswerChoiceSet = choiceSetOne,
            });
            context.SaveChanges();

            string[] newFieldNames =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory)
                    .Select(f => f.Name).ToArray();
            Assert.That(newFieldNames, Is.SupersetOf(originalFieldNames), "GenerateMetricConfigsForUnusedFields (or a person manually adding metrics) will have saved metrics referencing the old names, but they've changed");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CharacterizeCurrentNaming(int subsets)
        {
            string[] extraSubsetNames = Enumerable.Range(1, subsets).Select(i => i.ToString()).ToArray();

            var contextFactory = CreateContextFactoryForSurveys(FirstSurveyId);
            var testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            using var context = contextFactory.CreateDbContext();
            var subsetRepository = CreateSubsetRepository(extraSubsetNames);
            var choiceSetOne = CreateChoiceSetOne(FirstSurveyId);
            context.ChoiceSets.Add(choiceSetOne);

            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Q1",
                MasterType = "RADIO",
                ItemNumber = 1,
                QuestionText = "How much do you like football?",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Q2",
                MasterType = "RADIO",
                ItemNumber = 2,
                QuestionText = "How much do you like cheese?",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));
            context.SaveChanges();

            string[] actualFieldNames =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory)
                    .Select(f => f.Name).ToArray();
            Assert.That(actualFieldNames, Is.EquivalentTo(new[]{ "how_much_cheese_Q2_asked", "how_much_football_Q1_asked", "how_much_cheese_Q2", "how_much_football_Q1" }),
                "GenerateMetricConfigsForUnusedFields will have saved metrics referencing these names. If this test fails, we need to have saved the old names somewhere in the database.");
        }

        [Test]
        public void CharacterizeClashingGeneratedNamesAreSuffixed()
        {
            var contextFactory = CreateContextFactoryForSurveys(FirstSurveyId);
            var testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            using var context = contextFactory.CreateDbContext();
            var subsetRepository = CreateSubsetRepository(TestResponseFactory.AllSubset.Id);
            var choiceSetOne = CreateChoiceSetOne(FirstSurveyId);
            context.ChoiceSets.Add(choiceSetOne);

            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Question1",
                MasterType = "RADIO",
                ItemNumber = 1,
                QuestionText = "Irrelevant",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Question-1",
                MasterType = "RADIO",
                ItemNumber = 2,
                QuestionText = "Irrelevant",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Question 1",
                MasterType = "RADIO",
                ItemNumber = 3,
                QuestionText = "Irrelevant",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

            context.SaveChanges();

            string[] originalFieldNames =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory)
                    .Select(f => f.Name).ToArray();
            Assert.That(originalFieldNames, Is.EquivalentTo(new[]{ "Question1", "Question1_asked", "Question1_2", "Question1_2_asked", "Question1_3", "Question1_3_asked" }));
        }

        [Theory]
        public void CharacterizeQuestionWithDifferentDbLayoutGetsSubsetSuffix(bool hasExistingField)
        {
            var responseFieldManager = new ResponseFieldManager(_responseEntityTypeRepository);
            var contextFactory = CreateContextFactoryForSurveys(FirstSurveyId, SecondSurveyId);
            var testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            using var context = contextFactory.CreateDbContext();
            string ukSubsetId = "UK";
            string usSubsetId = "US";
            var subsetRepository = new SubsetRepository
            {
                new()
                {
                    Id = ukSubsetId,
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { FirstSurveyId, new[] { "Main" } },
                    }
                },
                new()
                {
                    Id = usSubsetId,
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { SecondSurveyId, new[] { "Main" } }
                    }
                }
            };

            if (hasExistingField)
            {
                var existingField = new FieldDefinitionModel("Age", "", "", "", "", null, "Age",
                    EntityInstanceColumnLocation.optValue, "",
                    false, null, [], SqlRoundingType.Round);
                _ = responseFieldManager.LazyLoad([(ukSubsetId, existingField)]).ToArray();
            }

            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = "Age",
                MasterType = "Slider",
                ItemNumber = 1,
                QuestionText = "How old are you?",
            });

            var choiceSetOne = CreateChoiceSetOne(SecondSurveyId);
            context.ChoiceSets.Add(choiceSetOne);
            context.Questions.Add(new Question()
            {
                SurveyId = SecondSurveyId,
                VarCode = "Age",
                MasterType = "RADIO",
                ItemNumber = 1,
                QuestionText = "How old are you?",
                AnswerChoiceSet = choiceSetOne,
            });
            context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

            context.SaveChanges();

            string[] actualFieldNames =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory, responseFieldManager)
                    .Select(f => f.Name).ToArray();
            Assert.That(actualFieldNames, Is.EquivalentTo(new[] { "Age", "Age_US", "Age_US_asked" }));
        }


        [TestCase("Age", "Age", false)]
        [TestCase("SEG1", "SEG1", false, Description = "Triggers name generation")]
        [TestCase("AgeField", "AgeVarCode", false)]
        [TestCase("Age", "Age", true)]
        [TestCase("SEG1", "SEG1", true, Description = "Triggers name generation")]
        [TestCase("AgeField", "AgeVarCode", true)]
        public void CharacterizeQuestionWithSameDbLayoutGetsUnified(string fieldName, string varcode, bool hasChoiceSet)
        {
            var responseFieldManager = new ResponseFieldManager(_responseEntityTypeRepository);
            var contextFactory = CreateContextFactoryForSurveys(FirstSurveyId, SecondSurveyId);
            var testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            using var context = contextFactory.CreateDbContext();
            string ukSubsetId = "UK";
            string usSubsetId = "US";
            var subsetRepository = new SubsetRepository
            {
                new()
                {
                    Id = ukSubsetId,
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { FirstSurveyId, new[] { "Main" } },
                    }
                },
                new()
                {
                    Id = usSubsetId,
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { SecondSurveyId, new[] { "Main" } }
                    }
                }
            };

            var entityType = TestEntityTypeRepository.GenericQuestion;
            IEnumerable<EntityFieldDefinitionModel> entityFieldDefinitionModels = hasChoiceSet
                ? [new EntityFieldDefinitionModel(DbLocation.AnswerShort, entityType, entityType.Identifier)]
                : [];
            var existingField = new FieldDefinitionModel(fieldName, "", "", "", "", null, varcode,
                    EntityInstanceColumnLocation.optValue, "",
                    false, null, entityFieldDefinitionModels, SqlRoundingType.Round);
            _ = responseFieldManager.LazyLoad([(ukSubsetId, existingField), (usSubsetId, existingField)]).ToArray();


            var choiceSetOneUk = CreateChoiceSetOne(FirstSurveyId);
            var choiceSetOneUs = CreateChoiceSetOne(SecondSurveyId);
            context.ChoiceSets.AddRange([choiceSetOneUk, choiceSetOneUs]);
            context.Questions.Add(new Question()
            {
                SurveyId = FirstSurveyId,
                VarCode = varcode,
                MasterType = hasChoiceSet ? "RADIO" : "Slider",
                ItemNumber = 1,
                QuestionText = "Some question words" + varcode,
                AnswerChoiceSet = hasChoiceSet ? choiceSetOneUk : null,
            });

            context.Questions.Add(new Question()
            {
                SurveyId = SecondSurveyId,
                VarCode = varcode,
                MasterType = hasChoiceSet ? "RADIO" : "Slider",
                ItemNumber = 1,
                QuestionText = "Chief income earner" + varcode,
                AnswerChoiceSet = hasChoiceSet ? choiceSetOneUs : null,
            });
            context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

            context.SaveChanges();

            string[] actualFieldNames =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory, responseFieldManager)
                    .Select(f => f.Name).ToArray();

            using var metaContext = testMetadataContextFactory.CreateDbContext();
            Assert.Multiple(() =>
            {
                Assert.That(actualFieldNames, Is.EquivalentTo(new[] { fieldName }));
                Assert.That(metaContext.VariableConfigurations.Select(v => v.Identifier), Is.Empty);
            });
        }

        [TestCase(0, 0, null)]
        [TestCase(null, int.MaxValue, null)]
        [TestCase(int.MinValue, null, null)]
        [TestCase(int.MinValue, int.MaxValue, null)]
        [TestCase(200, 500000, null)]
        public void FieldScaleFactors_AreCalculatedCorrectly(int? minValue, int? maxValue, double? expectedScaleFactor)
        {
            var subsetId = TestResponseFactory.AllSubset.Id;
            var contextFactory = CreateContextFactoryForSurveys(FirstSurveyId);
            var testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            using var context = contextFactory.CreateDbContext();
            var subsetRepository = CreateSubsetRepository(subsetId);

            context.Questions.Add(new Question
            {
                SurveyId = FirstSurveyId,
                VarCode = "Q1",
                MasterType = "SLIDER",
                ItemNumber = 1,
                QuestionText = "How much do you earn?",
                MaximumValue = maxValue,
                MinimumValue = minValue,
            });
            context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

            context.SaveChanges();

            var fields =
                AdjustForAnswersTableAndGetFields(choiceSetReader, subsetRepository, testMetadataContextFactory);

            double? scaleFactor = fields.Single().GetDataAccessModel(subsetId).ScaleFactor;
            Assert.Multiple(() =>
            {
                Assert.That(scaleFactor, Is.EqualTo(expectedScaleFactor));
                Assert.That(scaleFactor * maxValue, Is.Null.Or.LessThanOrEqualTo(int.MaxValue), "Should fit the max within the range");
                Assert.That(scaleFactor * maxValue, Is.Null.Or.GreaterThanOrEqualTo(2f * int.MaxValue / 5f), "Should use at least 2/5 of the available range of numbers");
            });
        }

        [TestCase("#.#", ExpectedResult = 1)]
        [TestCase("#.##", ExpectedResult = 2)]
        [TestCase("#.## hours", ExpectedResult = 2)]
        [TestCase("#Q5_EVERY# ##.# #Q5_YEARS#", ExpectedResult = 1)]
        [TestCase("£ #,###.##", ExpectedResult = 2)]
        [TestCase("####.##", ExpectedResult = 2)]
        [TestCase("###.## #KGPIPE#", ExpectedResult = 2)]
        [TestCase("###.##%", ExpectedResult = 2)]
        [TestCase("###.#% ABV", ExpectedResult = 1)]
        [TestCase("##.##", ExpectedResult = 2)]
        [TestCase("##.##%", ExpectedResult = 2)]
        [TestCase("##.#%", ExpectedResult = 1)]
        [TestCase("##0.0 Million", ExpectedResult = 1)]
        [TestCase("#,###.#", ExpectedResult = 1)]
        [TestCase("#,###.##", ExpectedResult = 2)]
        [TestCase("#,###.## €", ExpectedResult = 2)]
        [TestCase("#,###.####", ExpectedResult = 4)]
        [TestCase("#,###.####%", ExpectedResult = 4)]
        [TestCase("#.# %", ExpectedResult = 1)]
        [TestCase("#.# days per week", ExpectedResult = 1)]
        [TestCase("#.# hours", ExpectedResult = 1)]
        [TestCase("#.##", ExpectedResult = 2)]
        [TestCase("#.## hours", ExpectedResult = 2)]
        [TestCase("#.## PM", ExpectedResult = 2)]
        [TestCase("#.###", ExpectedResult = 3)]
        [TestCase("#.##RMB", ExpectedResult = 2)]
        [TestCase("#.00", ExpectedResult = 2)]
        [TestCase("#.OO", ExpectedResult = 0)]
        [TestCase("#SYM# #,###.##", ExpectedResult = 2)]
        [TestCase("$ ####.##", ExpectedResult = 2)]
        [TestCase("$ #,###.##", ExpectedResult = 2)]
        [TestCase("$####.##", ExpectedResult = 2)]
        [TestCase("$#,###.##", ExpectedResult = 2)]
        [TestCase("$#,##0.00", ExpectedResult = 2)]
        [TestCase("$#.##", ExpectedResult = 2)]
        [TestCase("£ #,###.##", ExpectedResult = 2)]
        [TestCase("£ #.##", ExpectedResult = 2)]
        [TestCase("£##.##", ExpectedResult = 2)]
        [TestCase("£#,###.##", ExpectedResult = 2)]
        [TestCase("£#,###.##m", ExpectedResult = 2)]
        [TestCase("£#,##0.00", ExpectedResult = 2)]
        [TestCase("£#.##", ExpectedResult = 2)]
        [TestCase("£#.###", ExpectedResult = 3)]
        [TestCase("€#.##", ExpectedResult = 2)]
        [TestCase("0.0", ExpectedResult = 1)]
        [TestCase("GBP#,##0.00", ExpectedResult = 2)]
        [TestCase("Hours #.##", ExpectedResult = 2)]
        [TestCase("####", ExpectedResult = 0)]
        [TestCase("0000", ExpectedResult = 0)]
        [TestCase("#0#0", ExpectedResult = 0)]
        [TestCase("", ExpectedResult = 0)]
        public int CountHashesOrZerosAfterLastDotTests(string input)
        {
            return AnswersTableMetadataLoader.GetDecimalPlaces(input);
        }

        private static ResponseFieldDescriptor[] AdjustForAnswersTableAndGetFields(ChoiceSetReader choiceSetReader, SubsetRepository subsetRepository, TestMetadataContextFactoryInMemory testMetadataContextFactory, ResponseFieldManager responseFieldManager = null)
        {
            var loadableResponseFieldManager = responseFieldManager ?? new ResponseFieldManager(_responseEntityTypeRepository);
            var instanceSettings = Substitute.For<IInstanceSettings>();
            instanceSettings.GenerateFromAnswersTable.Returns(true);

            var productContext = new ProductContext("survey", "1234", true, "Survey1234");
            var variableConfigurationRepository =
                new VariableConfigurationRepository(testMetadataContextFactory, productContext);
            var loader = new AnswersTableMetadataLoader(
                Substitute.For<ILogger>(),
                instanceSettings,
                choiceSetReader,
                variableConfigurationRepository,
                productContext,
                new ConfigurationSourcedLoaderSettings(new AppSettings()));

            loader.AdjustForAnswersTable(subsetRepository,
                _responseEntityTypeRepository,
                new EntityInstanceRepository(),
                new EntitySetRepository(Substitute.For<ILoggerFactory>(), Substitute.For<IProductContext>()),
                loadableResponseFieldManager);

            return loadableResponseFieldManager.GetAllFields().ToArray();
        }

        private static ChoiceSet CreateChoiceSetOne(int firstSurveyId)
        {
            return new ChoiceSet()
            {
                Name = "ChoiceSetOne",
                SurveyId = firstSurveyId,
                ParentChoiceSet1 = null,
                ParentChoiceSet2 = null,
                Choices = new List<Choice>()
                {
                    new()
                    {
                        ChoiceSetId = 1,
                        Name = "Yes",
                        SurveyChoiceId = 1,
                        SurveyId = firstSurveyId,
                    },
                    new()
                    {
                        ChoiceSetId = 1,
                        Name = "No",
                        SurveyChoiceId = 2,
                        SurveyId = firstSurveyId,
                    },
                }
            };
        }

        private static SubsetRepository CreateSubsetRepository(params string[] subsetIds)
        {
            var subsetRepository = new SubsetRepository();
            foreach(var subset in subsetIds)
            {
                subsetRepository.Add(new()
                {
                    Id = subset,
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        { FirstSurveyId, new[] { "Main" } },
                        { SecondSurveyId, new[] { "Main" } }
                    }
                });
            }
            return subsetRepository;
        }

        private static TestChoiceSetReaderFactory CreateContextFactoryForSurveys(params int[] surveyAndSegmentIds)
        {
            var contextFactory = new TestChoiceSetReaderFactory();
            using var context = contextFactory.CreateDbContext();
            SetupSurvey(context, surveyAndSegmentIds);
            context.SaveChanges();
            return contextFactory;
        }

        private static void SetupSurvey(AnswersDbContext context, params int[] surveyIds)
        {
            foreach(var surveyId in surveyIds)
            {
                int surveySegmentId = 1000 + surveyId;
                context.SurveyResponses.Add(new SurveyResponse()
                {
                    ResponseId = 100 + surveyId,
                    SurveyId = surveyId,
                    SegmentId = surveySegmentId,
                    Timestamp = DateTime.Now
                });

                context.SurveySegments.Add(new SurveySegment()
                {
                    SegmentName = "Main",
                    SurveyId = surveyId,
                    SurveySegmentId = surveySegmentId
                });
            }
        }

    }
}
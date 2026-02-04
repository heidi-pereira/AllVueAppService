using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    [TestFixture]
    public class AnswersTableMetadataLoader_ChoiceSetTests
    {
        [Test]
        public void TwoQuestionsWithTheSameChoicesShouldOnlyGenerateOneEntityType()
        {
            const int firstSurveyId = 1;
            const int secondSurveyId = 2;

            var contextFactory = new TestChoiceSetReaderFactory();
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            var subsetRepository = new SubsetRepository();

            using (var context = contextFactory.CreateDbContext())
            {
                subsetRepository.Add(new Subset
                {
                    Id = "All",
                    SegmentIds = new List<int> { 1, 2 },
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        {firstSurveyId, new [] { "Main" }},
                        {secondSurveyId, new [] { "Main" }}
                    }
                });

                subsetRepository.Add(new Subset
                {
                    Id = "Main",
                    SegmentIds = new List<int> { 3, 4 },
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        {firstSurveyId, new [] { "Main" }},
                        {secondSurveyId, new [] { "Main" }}
                    }
                });

                var choiceSetOne = new ChoiceSet()
                {
                    Name = "ChoiceSetOne",
                    ChoiceSetId = 1,
                    SurveyId = firstSurveyId,
                    ParentChoiceSet1 = null,
                    ParentChoiceSet2 = null,
                    Choices = new List<Choice>()
                    {
                        new Choice()
                        {
                            ChoiceId = 1,
                            ChoiceSetId = 1,
                            Name = "Yes",
                            SurveyChoiceId = 1,
                            SurveyId = firstSurveyId,
                        },
                        new Choice()
                        {
                            ChoiceId = 2,
                            ChoiceSetId = 1,
                            Name = "No",
                            SurveyChoiceId = 2,
                            SurveyId = firstSurveyId,
                        },
                    }
                };

                var choiceSetTwo = new ChoiceSet()
                {
                    Name = "ChoiceSetTwo",
                    ChoiceSetId = 2,
                    SurveyId = secondSurveyId,
                    ParentChoiceSet1 = null,
                    ParentChoiceSet2 = null,
                    Choices = new List<Choice>()
                    {
                        new Choice()
                        {
                            ChoiceId = 3,
                            ChoiceSetId = 2,
                            Name = "Yes",
                            SurveyChoiceId = 1,
                            SurveyId = secondSurveyId,
                        },
                        new Choice()
                        {
                            ChoiceId = 4,
                            ChoiceSetId = 2,
                            Name = "No",
                            SurveyChoiceId = 2,
                            SurveyId = secondSurveyId,
                        },
                    }
                };

                context.ChoiceSets.Add(choiceSetOne);
                context.ChoiceSets.Add(choiceSetTwo);

                context.Questions.Add(new Question()
                {
                    SurveyId = firstSurveyId,
                    VarCode = "QuestionOne",
                    QuestionId = 1,
                    MasterType = "RADIO",
                    ItemNumber = 1,
                    QuestionText = "How are you doing?",
                    AnswerChoiceSet = choiceSetOne,
                });

                context.Questions.Add(new Question()
                {
                    SurveyId = secondSurveyId,
                    VarCode = "QuestionTwo",
                    QuestionId = 2,
                    MasterType = "RADIO",
                    ItemNumber = 1,
                    QuestionText = "Blah blah blah",
                    AnswerChoiceSet = choiceSetTwo
                });

                context.SurveySegments.Add(new SurveySegment()
                {
                    SegmentName = "testSegment",
                    SurveyId = firstSurveyId,
                    SurveySegmentId = 1
                });

                context.SurveyResponses.Add(new SurveyResponse()
                {
                    ResponseId = 1,
                    SurveyId = firstSurveyId,
                    SegmentId = 1,
                    Timestamp = DateTime.Now
                });

                context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

                context.SaveChanges();
            }

            var instanceSettings = Substitute.For<IInstanceSettings>();
            instanceSettings.GenerateFromAnswersTable.Returns(true);
            instanceSettings.ForceBrandTypeAsDefault.Returns(true);

            var loader = new AnswersTableMetadataLoader(
                Substitute.For<ILogger>(),
                instanceSettings,
                choiceSetReader, 
                new InMemoryVariableConfigurationRepository(),
                new ProductContext("survey", "1234", true, "Survey1234"),
                new ConfigurationSourcedLoaderSettings(new AppSettings()));

            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();

            loader.AdjustForAnswersTable(subsetRepository,
                responseEntityTypeRepository,
                new EntityInstanceRepository(),
                new EntitySetRepository(Substitute.For<ILoggerFactory>(), Substitute.For<IProductContext>()),
                new ResponseFieldManager(responseEntityTypeRepository));

            var profileEntityType = responseEntityTypeRepository.Get("profile");
            var generatedEntityType = responseEntityTypeRepository.Get("brand");
            Assert.That(responseEntityTypeRepository, Is.EquivalentTo(new [] {profileEntityType, generatedEntityType}));

            Assert.That(generatedEntityType.SurveyChoiceSetNames, Does.Contain("ChoiceSetOne"));
        }

        [Test]
        public void AQuestionWithEmptyChoicesShouldOnlyGenerateNamedItems()
        {
            const int firstSurveyId = 1;
            const int secondSurveyId = 2;

            var contextFactory = new TestChoiceSetReaderFactory();
            var choiceSetReader = new ChoiceSetReader(contextFactory, NullLogger.Instance);
            var subsetRepository = new SubsetRepository();

            using (var context = contextFactory.CreateDbContext())
            {
                subsetRepository.Add(new Subset
                {
                    Id = "All",
                    SegmentIds = new List<int> { 1, 2 },
                    SurveyIdToSegmentNames = new Dictionary<int, IReadOnlyCollection<string>>
                    {
                        {firstSurveyId, new [] { "Main" }},
                        {secondSurveyId, new [] { "Main" }}
                    }
                });

                var choiceSetOne = new ChoiceSet()
                {
                    Name = "ChoiceSetOne",
                    ChoiceSetId = 1,
                    SurveyId = firstSurveyId,
                    ParentChoiceSet1 = null,
                    ParentChoiceSet2 = null,
                    Choices = new List<Choice>()
                    {
                        new Choice()
                        {
                            ChoiceId = 1,
                            ChoiceSetId = 1,
                            Name = "Yes",
                            SurveyChoiceId = 1,
                            SurveyId = firstSurveyId,
                        },
                        new Choice()
                        {
                            ChoiceId = 2,
                            ChoiceSetId = 1,
                            Name = "No",
                            SurveyChoiceId = 2,
                            SurveyId = firstSurveyId,
                        },
                        new Choice()
                        {
                            ChoiceId = 3,
                            ChoiceSetId = 1,
                            Name = "",
                            SurveyChoiceId = 3,
                            SurveyId = firstSurveyId,
                        },
                        new Choice()
                        {
                            ChoiceId = 4,
                            ChoiceSetId = 1,
                            Name="",
                            ImageURL = "ImageNamed",
                            SurveyChoiceId = 4,
                            SurveyId = firstSurveyId,
                        },
                    }
                };

                context.ChoiceSets.Add(choiceSetOne);

                context.Questions.Add(new Question()
                {
                    SurveyId = firstSurveyId,
                    VarCode = "QuestionOne",
                    QuestionId = 1,
                    MasterType = "RADIO",
                    ItemNumber = 1,
                    QuestionText = "How are you doing?",
                    AnswerChoiceSet = choiceSetOne,
                });

                context.SurveySegments.Add(new SurveySegment()
                {
                    SegmentName = "testSegment",
                    SurveyId = firstSurveyId,
                    SurveySegmentId = 1
                });

                context.SurveyResponses.Add(new SurveyResponse()
                {
                    ResponseId = 1,
                    SurveyId = firstSurveyId,
                    SegmentId = 1,
                    Timestamp = DateTime.Now
                });

                context.Choices.AddRange(context.ChoiceSets.SelectMany(c => c.Choices));

                context.SaveChanges();
            }

            var instanceSettings = Substitute.For<IInstanceSettings>();
            instanceSettings.GenerateFromAnswersTable.Returns(true);
            instanceSettings.ForceBrandTypeAsDefault.Returns(true);

            var loader = new AnswersTableMetadataLoader(
                Substitute.For<ILogger>(),
                instanceSettings,
                choiceSetReader,
                new InMemoryVariableConfigurationRepository(),
                new ProductContext("survey", "1234", true, "Survey1234"),
                new ConfigurationSourcedLoaderSettings(new AppSettings()));

            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            var entityInstanceRepository = new EntityInstanceRepository();
            var entitySetRepository = new EntitySetRepository(Substitute.For<ILoggerFactory>(), Substitute.For<IProductContext>());
            var responseFieldManager = new ResponseFieldManager(responseEntityTypeRepository);
            loader.AdjustForAnswersTable(subsetRepository,
                responseEntityTypeRepository,
                entityInstanceRepository,
                entitySetRepository,
                responseFieldManager);

            var profileEntityType = responseEntityTypeRepository.Get("profile");
            var generatedEntityType = responseEntityTypeRepository.Get("brand");
            Assert.That(responseEntityTypeRepository, Is.EquivalentTo(new[] { profileEntityType, generatedEntityType }));

            Assert.That(generatedEntityType.SurveyChoiceSetNames, Does.Contain("ChoiceSetOne"));
            var instances = entityInstanceRepository.GetInstancesAnySubset("brand").ToArray();
            Assert.That(instances.Count, Is.EqualTo(3));
            Assert.That(instances[0].Equals(new EntityInstance() { Id=1, Name="Yes"}), Is.True);
            Assert.That(instances[1].Equals(new EntityInstance() { Id = 2, Name = "No" }), Is.True);
            Assert.That(instances[2].Equals(new EntityInstance() { Id = 4, Name = "ImageNamed" }), Is.True);
        }
    }
}

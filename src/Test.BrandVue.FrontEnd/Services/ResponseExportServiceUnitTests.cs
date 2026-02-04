using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Services;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using TestCommon;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class ResponseExportServiceUnitTests
    {
        private TestChoiceSetReaderFactory _answerDbContextFactory;
        private IProductContext _productContext;
        public ResponseExportService _responseExportService;
        const int surveyId = 13290;

        [OneTimeSetUp]
        public void Setup()
        {
            _answerDbContextFactory = new TestChoiceSetReaderFactory();
            using var dbContext = _answerDbContextFactory.CreateDbContext();
            _productContext = Substitute.For<IProductContext>();
            _productContext.NonMapFileSurveyIds.Returns(new int[] { 13290 });
            _responseExportService = new ResponseExportService(_answerDbContextFactory, _productContext);
        }

        [Test]
        public void ShouldCreateSingleEntityRadioFieldModel()
        {
            const int questionId = 1;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("GENDER")
                .WithQuestionText("What is your gender?")
                .WithMasterType("RADIO")
                .WithAnswerChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 1,
                    Name = "Gender choices",
                    Choices = new List<Choice>
                    {
                        new() { ChoiceId = 101, ChoiceSetId = 1, Name = "M", SurveyChoiceId = 1 },
                        new() { ChoiceId = 102, ChoiceSetId = 1, Name = "F", SurveyChoiceId = 2 },
                        new() { ChoiceId = 103, ChoiceSetId = 1, Name = "O", SurveyChoiceId = 3 },
                        new() { ChoiceId = 104, ChoiceSetId = 1, Name = "", ImageURL="Image", SurveyChoiceId = 4 },
                    },
                })
                .Build();

            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).SingleOrDefault();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.VarCode, Is.EqualTo("GENDER"), "Incorrect varcode");
                Assert.That(fieldModel.Label, Is.EqualTo(string.Empty), "Incorrect label");
                Assert.That(fieldModel.ValueLabels, Is.EqualTo("1:M|2:F|3:O|4:Image"), "Incorrect value label");
                Assert.That(fieldModel.MasterType, Is.EqualTo("RADIO"), "Incorrect master type");
                Assert.That(fieldModel.SplitByType, Is.Null, "Incorrect split by");
                Assert.That(fieldModel.FilterByType, Is.Null, "Incorrect filter by");
            });
        }

        [Test]
        public void ShouldCreateTwoEntityRadioFieldModelWithAnswerAndSectionChoiceSets()
        {
            const int questionId = 2;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("WHISKEY")
                .WithQuestionText("Do you like these whiskeys?")
                .WithMasterType("RADIO")
                .WithAnswerChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 22,
                    Choices = new List<Choice> {
                        new() { ChoiceId = 25, ChoiceSetId = 22, Name = "Love it", SurveyChoiceId = 1 },
                        new() { ChoiceId = 26, ChoiceSetId = 22, Name = "Its ok", SurveyChoiceId = 2 },
                        new() { ChoiceId = 27, ChoiceSetId = 22, Name = "Hate it", SurveyChoiceId = 3 }
                    },
                    Name = "Feelings"
                })
                .WithSectionChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 21,
                    Choices = new List<Choice>
                    {
                        new() { ChoiceId = 21, ChoiceSetId = 21, Name = "Singleton", SurveyChoiceId = 1 },
                        new() { ChoiceId = 22, ChoiceSetId = 21, Name = "Bowmore", SurveyChoiceId = 2 },
                        new() { ChoiceId = 23, ChoiceSetId = 21, Name = "Jura", SurveyChoiceId = 3 },
                        new() { ChoiceId = 24, ChoiceSetId = 21, Name = "Moonshine", SurveyChoiceId = 4 }
                    },
                    Name = "Brands"
                })
                .Build();

            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.Count, Is.EqualTo(4), "Incorrect number of field models");

                Assert.That(fieldModel[0].VarCode, Is.EqualTo("WHISKEY_1"), "Incorrect varcode");
                Assert.That(fieldModel[0].Label, Is.EqualTo("WHISKEY_Singleton"), "Incorrect label");
                Assert.That(fieldModel[0].ValueLabels, Is.EqualTo("1:Love it|2:Its ok|3:Hate it"), "Incorrect value label");
                Assert.That(fieldModel[0].MasterType, Is.EqualTo("RADIO"), "Incorrect master type");
                Assert.That(fieldModel[0].SplitByType, Is.EqualTo(ChoiceSetType.AnswerChoiceSet), "Incorrect split by");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.SectionChoiceSet), "Incorrect filter by");

                Assert.That(fieldModel[3].VarCode, Is.EqualTo("WHISKEY_4"), "Incorrect varcode");
                Assert.That(fieldModel[3].Label, Is.EqualTo("WHISKEY_Moonshine"), "Incorrect label");
                Assert.That(fieldModel[3].ValueLabels, Is.EqualTo("1:Love it|2:Its ok|3:Hate it"), "Incorrect value label");
                Assert.That(fieldModel[3].MasterType, Is.EqualTo("RADIO"), "Incorrect master type");
                Assert.That(fieldModel[0].SplitByType, Is.EqualTo(ChoiceSetType.AnswerChoiceSet), "Incorrect split by");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.SectionChoiceSet), "Incorrect filter by");
            });
        }

        [Test]
        public void ShouldCreateTwoEntityRadioFieldModelWithAnswerAndPageChoiceSets()
        {
            const int questionId = 3;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("WHISKEY")
                .WithQuestionText("Do you like these whiskeys?")
                .WithMasterType("RADIO")
                .WithAnswerChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 32,
                    Choices = new List<Choice> {
                                    new() { ChoiceId = 35, ChoiceSetId = 32, Name = "Love it", SurveyChoiceId = 1 },
                                    new() { ChoiceId = 36, ChoiceSetId = 32, Name = "Its ok", SurveyChoiceId = 2 },
                                    new() { ChoiceId = 37, ChoiceSetId = 32, Name = "Hate it", SurveyChoiceId = 3 }
                    },
                    Name = "Feelings"
                })
                .WithPageChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 31,
                    Choices = new List<Choice>
                    {
                                    new() { ChoiceId = 31, ChoiceSetId = 31, Name = "Singleton", SurveyChoiceId = 1 },
                                    new() { ChoiceId = 32, ChoiceSetId = 31, Name = "Bowmore", SurveyChoiceId = 2 },
                                    new() { ChoiceId = 33, ChoiceSetId = 31, Name = "Jura", SurveyChoiceId = 3 },
                                    new() { ChoiceId = 34, ChoiceSetId = 31, Name = "Moonshine", SurveyChoiceId = 4 }
                    },
                    Name = "Brands"
                })
                .Build();

            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.Count, Is.EqualTo(4), "Incorrect number of field models");

                Assert.That(fieldModel[0].VarCode, Is.EqualTo("WHISKEY_1"), "Incorrect varcode");
                Assert.That(fieldModel[0].Label, Is.EqualTo("WHISKEY_Singleton"), "Incorrect label");
                Assert.That(fieldModel[0].ValueLabels, Is.EqualTo("1:Love it|2:Its ok|3:Hate it"), "Incorrect value label");
                Assert.That(fieldModel[0].MasterType, Is.EqualTo("RADIO"), "Incorrect master type");
                Assert.That(fieldModel[0].SplitByType, Is.EqualTo(ChoiceSetType.AnswerChoiceSet), "Incorrect split by");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.PageChoiceSet), "Incorrect filter by");

                Assert.That(fieldModel[3].VarCode, Is.EqualTo("WHISKEY_4"), "Incorrect varcode");
                Assert.That(fieldModel[3].Label, Is.EqualTo("WHISKEY_Moonshine"), "Incorrect label");
                Assert.That(fieldModel[3].ValueLabels, Is.EqualTo("1:Love it|2:Its ok|3:Hate it"), "Incorrect value label");
                Assert.That(fieldModel[3].MasterType, Is.EqualTo("RADIO"), "Incorrect master type");
                Assert.That(fieldModel[0].SplitByType, Is.EqualTo(ChoiceSetType.AnswerChoiceSet), "Incorrect split by");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.PageChoiceSet), "Incorrect filter by");
            });
        }

        [Test]
        public void ShouldCreateFieldModelForZeroEntitySlider()
        {
            const int questionId = 4;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("TESTS")
                .WithQuestionText("On a scale of 1-10, how tedious is writing tests?")
                .WithMasterType("SLIDER")
                .WithMinimumValue(1)
                .WithMaximumValue(10)
                .Build();

            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.Count, Is.EqualTo(1), "Incorrect number of field models");

                Assert.That(fieldModel[0].VarCode, Is.EqualTo("TESTS"), "Incorrect varcode");
                Assert.That(fieldModel[0].Label, Is.EqualTo(string.Empty), "Incorrect label");
                Assert.That(fieldModel[0].ValueLabels, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldModel[0].MasterType, Is.EqualTo("SLIDER"), "Incorrect master type");
                Assert.That(fieldModel[0].SplitByType, Is.Null, "Incorrect split by");
                Assert.That(fieldModel[0].FilterByType, Is.Null, "Incorrect filter by");
            });
        }

        [Test]
        public void ShouldCreateFieldModelForSingleEntitySliderWithQuestionChoiceSet()
        {
            const int questionId = 5;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("TESTS")
                .WithQuestionText("On a scale of 1-10, how tedious is #ANSWERCHIOCE# test?")
                .WithMasterType("SLIDER")
                .WithMaximumValue(10)
                .WithMinimumValue(1)
                .WithQuestionChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 51,
                    Choices = new List<Choice>
                    {
                        new() { ChoiceId = 51, ChoiceSetId = 51, Name = "Slider", SurveyChoiceId = 1 },
                        new() { ChoiceId = 52, ChoiceSetId = 51, Name = "Radio", SurveyChoiceId = 2 },
                        new() { ChoiceId = 53, ChoiceSetId = 51, Name = "Dropzone", SurveyChoiceId = 3 }
                    },
                    Name = "QuestionTypes"
                })
                .Build();

            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.Count, Is.EqualTo(3), "Incorrect number of field models");

                Assert.That(fieldModel[0].VarCode, Is.EqualTo("TESTS_1"), "Incorrect varcode");
                Assert.That(fieldModel[0].Label, Is.EqualTo("TESTS_Slider"), "Incorrect label");
                Assert.That(fieldModel[0].ValueLabels, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldModel[0].MasterType, Is.EqualTo("SLIDER"), "Incorrect master type");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.QuestionChoiceSet), "Incorrect master type");
                Assert.That(fieldModel[0].FilterByChoiceId, Is.EqualTo(1), "Incorrect filter by Id");
                Assert.That(fieldModel[0].SplitByType, Is.Null, "Incorrect split by");
            });
        }

        [Test]
        public void ShouldCreateFieldModelForSingleEntitySliderWithPageChoiceSet()
        {
            const int questionId = 6;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("TESTS")
                .WithQuestionText("On a scale of 1-10, how tedious is #ANSWERCHIOCE# test?")
                .WithMasterType("SLIDER")
                .WithMaximumValue(10)
                .WithMinimumValue(1)
                .WithPageChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 61,
                    Choices = new List<Choice>
                    {
                        new() { ChoiceId = 61, ChoiceSetId = 61, Name = "Slider", SurveyChoiceId = 1 },
                        new() { ChoiceId = 62, ChoiceSetId = 61, Name = "Radio", SurveyChoiceId = 2 },
                        new() { ChoiceId = 63, ChoiceSetId = 61, Name = "Dropzone", SurveyChoiceId = 3 }
                    },
                    Name = "QuestionTypes"
                })
                .Build();

            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.Count, Is.EqualTo(3), "Incorrect number of field models");

                Assert.That(fieldModel[0].VarCode, Is.EqualTo("TESTS_1"), "Incorrect varcode");
                Assert.That(fieldModel[0].Label, Is.EqualTo("TESTS_Slider"), "Incorrect label");
                Assert.That(fieldModel[0].ValueLabels, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldModel[0].MasterType, Is.EqualTo("SLIDER"), "Incorrect master type");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.PageChoiceSet), "Incorrect filter by");
                Assert.That(fieldModel[0].FilterByChoiceId, Is.EqualTo(1), "Incorrect filter by Id");
                Assert.That(fieldModel[0].SplitByType, Is.Null, "Incorrect split by");
            });
        }

        [Test]
        public void ShouldCreateFieldModelForSingleEntitySliderWithSectionChoiceSet()
        {
            const int questionId = 7;
            var question = new QuestionBuilder(questionId, surveyId)
                .WithVarCode("TESTS")
                .WithQuestionText("On a scale of 1-10, how tedious is #ANSWERCHIOCE# test?")
                .WithMasterType("SLIDER")
                .WithMaximumValue(10)
                .WithMinimumValue(1)
                .WithSectionChoiceSet(new ChoiceSet
                {
                    ChoiceSetId = 71,
                    Choices = new List<Choice>
                    {
                        new() { ChoiceId = 71, ChoiceSetId = 71, Name = "Slider", SurveyChoiceId = 1 },
                        new() { ChoiceId = 72, ChoiceSetId = 71, Name = "Radio", SurveyChoiceId = 2 },
                        new() { ChoiceId = 73, ChoiceSetId = 71, Name = "Dropzone", SurveyChoiceId = 3 }
                    },
                    Name = "QuestionTypes"
                })
                .Build();
            using var dbContext = _answerDbContextFactory.CreateDbContext();
            dbContext.Questions.Add(question);
            dbContext.SaveChanges();

            _responseExportService.CreateFieldModels();
            var fieldModel = _responseExportService._fieldModels.Where(f => f.QuestionId == questionId).ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(fieldModel.Count, Is.EqualTo(3), "Incorrect number of field models");

                Assert.That(fieldModel[0].VarCode, Is.EqualTo("TESTS_1"), "Incorrect varcode");
                Assert.That(fieldModel[0].Label, Is.EqualTo("TESTS_Slider"), "Incorrect label");
                Assert.That(fieldModel[0].ValueLabels, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldModel[0].MasterType, Is.EqualTo("SLIDER"), "Incorrect master type");
                Assert.That(fieldModel[0].FilterByType, Is.EqualTo(ChoiceSetType.SectionChoiceSet), "Incorrect filter by");
                Assert.That(fieldModel[0].FilterByChoiceId, Is.EqualTo(1), "Incorrect filter by Id");
                Assert.That(fieldModel[0].SplitByType, Is.Null, "Incorrect split by");
            });
        }
    }
}

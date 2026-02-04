using BrandVue.EntityFramework.Answers.Model;
using BrandVue.Services;
using BrandVue.Services.ServiceModels;
using NUnit.Framework;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class ExportResponseToExcelTests
    {
        private int _varNameColumn = 1;
        private int _labelColumn = 2;
        private int _valueLabelColumn = 3;
        private int _questionTypeColumn = 4;
        private int _fullQuestionColumn = 5;

        [Test]
        public void ExportShouldPopulateQuestionsInExcelSheet()
        {
            //these models are set up based on what has been tested in the ResponseExportServiceUnitTests
            var singleEntityRadioResponseFieldModel = GetSingleEntityRadioModel();
            var twoEntityRadioResponseFieldModelWithSectionChoiceSet = GetTwoEntityRadioResponseFieldModelWithSectionChoiceSet();
            var twoEntityRadioResponseFieldModelWithPageChoiceSet = GetTwoEntityRadioResponseFieldModelWithPageChoiceSet();
            var zeroEntitySliderModel = GetZeroEntitySliderFieldModel();
            var sliderWithAnswerChoiceSetModel = GetModelForSingleEntitySliderWithQuestionChoiceSet();
            var sliderWithSectionChoiceSetModel = GetModelForSingleEntitySliderWithSectionChoiceSet();
            var sliderWithPageChoiceSetModel = GetModelForSingleEntitySliderWithPageChoiceSet();
            var radioModelWithNullAnswer = GetRadioModelWithNullAnswer();

            var fieldModels = new List<ResponseFieldModel>
            {
                singleEntityRadioResponseFieldModel,
                twoEntityRadioResponseFieldModelWithSectionChoiceSet,
                twoEntityRadioResponseFieldModelWithPageChoiceSet,
                zeroEntitySliderModel,
                sliderWithAnswerChoiceSetModel,
                sliderWithSectionChoiceSetModel,
                sliderWithPageChoiceSetModel,
                radioModelWithNullAnswer
            };

            var responses = new List<SurveyResponse>
            {
                new SurveyResponse
                {
                    ResponseId = 1,
                    SurveyId = 13290,
                    Archived = false,
                    LastChangeTime = DateTime.Now,
                    SegmentId = 1,
                    Timestamp = DateTime.Now.AddMinutes(-5),
                }
            }.ToArray();

            var warnings = new List<ResponseExportWarningModel>();
            var responseExporter = new ResponseExporter(fieldModels, responses, warnings);
            responseExporter.CreateAndPopulateSheets();

            var excelPackage = new ExcelPackage(responseExporter.ToStream());
            var fieldsWorksheet = excelPackage.Workbook.Worksheets[0];
            var dataWorksheet = excelPackage.Workbook.Worksheets[1];

            Assert.Multiple(() =>
            {
                //single entity radio
                Assert.That(fieldsWorksheet.Cells[2, _varNameColumn].Value, Is.EqualTo("GENDER"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[2, _labelColumn].Value, Is.EqualTo(string.Empty), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[2, _valueLabelColumn].Value, Is.EqualTo("1:M|2:F|3:O"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[2, _questionTypeColumn].Value, Is.EqualTo("RADIO"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[2, _fullQuestionColumn].Value, Is.EqualTo("What is your gender?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 1].Value, Is.EqualTo(1), "Incorrect response id");
                Assert.That(dataWorksheet.Cells[2, 2].Value, Is.EqualTo(4), "Incorrect data");

                //two entity radio with SECTION choice set - we only need to test a single iteration of the loop
                Assert.That(fieldsWorksheet.Cells[3, _varNameColumn].Value, Is.EqualTo("WHISKEY_1"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[3, _labelColumn].Value, Is.EqualTo("WHISKEY_Singleton"), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[3, _valueLabelColumn].Value, Is.EqualTo("1:Love it|2:Its ok|3:Hate it"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[3, _questionTypeColumn].Value, Is.EqualTo("RADIO"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[3, _fullQuestionColumn].Value, Is.EqualTo("Do you like these whiskeys?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 3].Value, Is.EqualTo(3), "Incorrect data");

                //two entity radio with PAGE choice set
                Assert.That(fieldsWorksheet.Cells[4, _varNameColumn].Value, Is.EqualTo("WHISKEY_2"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[4, _labelColumn].Value, Is.EqualTo("WHISKEY_Bowmore"), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[4, _valueLabelColumn].Value, Is.EqualTo("1:Love it|2:Its ok|3:Hate it"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[4, _questionTypeColumn].Value, Is.EqualTo("RADIO"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[4, _fullQuestionColumn].Value, Is.EqualTo("Do you REALLY like these whiskeys?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 4].Value, Is.EqualTo(2), "Incorrect data");

                //zero entity slider
                Assert.That(fieldsWorksheet.Cells[5, _varNameColumn].Value, Is.EqualTo("TESTS"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[5, _labelColumn].Value, Is.EqualTo(string.Empty), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[5, _valueLabelColumn].Value, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[5, _questionTypeColumn].Value, Is.EqualTo("SLIDER"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[5, _fullQuestionColumn].Value, Is.EqualTo("On a scale of 1-10, how tedious is writing tests?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 5].Value, Is.EqualTo(7), "Incorrect data");

                //question entity slider
                Assert.That(fieldsWorksheet.Cells[6, _varNameColumn].Value, Is.EqualTo("TESTS_1"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[6, _labelColumn].Value, Is.EqualTo("TESTS_Slider"), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[6, _valueLabelColumn].Value, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[6, _questionTypeColumn].Value, Is.EqualTo("SLIDER"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[6, _fullQuestionColumn].Value, Is.EqualTo("On a scale of 1-10, how tedious is writing tests?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 6].Value, Is.EqualTo(8), "Incorrect data");

                //section entity slider
                Assert.That(fieldsWorksheet.Cells[7, _varNameColumn].Value, Is.EqualTo("TESTS_2"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[7, _labelColumn].Value, Is.EqualTo("TESTS_Radio"), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[7, _valueLabelColumn].Value, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[7, _questionTypeColumn].Value, Is.EqualTo("SLIDER"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[7, _fullQuestionColumn].Value, Is.EqualTo("On a scale of 1-10, how tedious is writing tests?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 7].Value, Is.EqualTo(4), "Incorrect data");

                //Page entity slider
                Assert.That(fieldsWorksheet.Cells[8, _varNameColumn].Value, Is.EqualTo("TESTS_3"), "Incorrect variable name");
                Assert.That(fieldsWorksheet.Cells[8, _labelColumn].Value, Is.EqualTo("TESTS_Dropzone"), "Incorrect label");
                Assert.That(fieldsWorksheet.Cells[8, _valueLabelColumn].Value, Is.EqualTo("1-10"), "Incorrect value label");
                Assert.That(fieldsWorksheet.Cells[8, _questionTypeColumn].Value, Is.EqualTo("SLIDER"), "Incorrect question type");
                Assert.That(fieldsWorksheet.Cells[8, _fullQuestionColumn].Value, Is.EqualTo("On a scale of 1-10, how tedious is writing tests?"), "Incorrect  full question");

                Assert.That(dataWorksheet.Cells[2, 8].Value, Is.EqualTo(10), "Incorrect data");

                // single entity radio with null answer
                Assert.That(dataWorksheet.Cells[2, 9].Value, Is.EqualTo(""), "Incorrect data");
            });
        }

        private ResponseFieldModel GetTwoEntityRadioResponseFieldModelWithPageChoiceSet()
        {
            var questionId = 3;
            var responseFieldModel = new ResponseFieldModel("WHISKEY_2",
                "WHISKEY_Bowmore",
                "1:Love it|2:Its ok|3:Hate it",
                "Do you REALLY like these whiskeys?",
                questionId,
                "RADIO",
                ChoiceSetType.AnswerChoiceSet,
                null,
                ChoiceSetType.PageChoiceSet,
                2);

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(2)
                    .WithPageChoiceId(2)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(2)
                    .WithPageChoiceId(3)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(3)
                    .WithPageChoiceId(1)
                    .Build()
            };

            responseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return responseFieldModel;
        }

        private ResponseFieldModel GetTwoEntityRadioResponseFieldModelWithSectionChoiceSet()
        {
            var questionId = 2;
            var responseFieldModel = new ResponseFieldModel("WHISKEY_1",
                "WHISKEY_Singleton",
                "1:Love it|2:Its ok|3:Hate it",
                "Do you like these whiskeys?",
                questionId,
                "RADIO",
                ChoiceSetType.AnswerChoiceSet,
                null,
                ChoiceSetType.SectionChoiceSet,
                1);

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(1)
                    .WithSectionChoiceId(2)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(2)
                    .WithSectionChoiceId(3)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(3)
                    .WithSectionChoiceId(1)
                    .Build()
            };

            responseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return responseFieldModel;
        }

        private static ResponseFieldModel GetSingleEntityRadioModel()
        {
            var questionId = 1;
            var singleEntityRadioResponseFieldModel = new ResponseFieldModel("GENDER",
                string.Empty,
                "1:M|2:F|3:O",
                "What is your gender?",
                1,
                "RADIO");

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerChoiceId(4)
                    .Build()
            };

            singleEntityRadioResponseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return singleEntityRadioResponseFieldModel;
        }

        private ResponseFieldModel GetZeroEntitySliderFieldModel()
        {
            var questionId = 5;
            var zeroEntitySliderResponseFieldModel = new ResponseFieldModel("TESTS",
                string.Empty,
                "1-10",
                "On a scale of 1-10, how tedious is writing tests?",
                questionId,
                "SLIDER");

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerValue(7)
                    .Build()
            };

            zeroEntitySliderResponseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return zeroEntitySliderResponseFieldModel;
        }

        private ResponseFieldModel GetModelForSingleEntitySliderWithQuestionChoiceSet()
        {
            var questionId = 6;
            var singleEntitySliderResponseFieldModel = new ResponseFieldModel("TESTS_1",
                "TESTS_Slider",
                "1-10",
                "On a scale of 1-10, how tedious is writing tests?",
                questionId,
                "SLIDER",
                null,
                null,
                ChoiceSetType.QuestionChoiceSet,
                1);

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerValue(8)
                    .WithQuestionChoiceId(1)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerValue(4)
                    .WithQuestionChoiceId(2)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerValue(10)
                    .WithQuestionChoiceId(3)
                    .Build()
            };

            singleEntitySliderResponseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return singleEntitySliderResponseFieldModel;
        }

        private ResponseFieldModel GetModelForSingleEntitySliderWithSectionChoiceSet()
        {
            var questionId = 6;
            var singleEntitySliderResponseFieldModel = new ResponseFieldModel("TESTS_2",
                "TESTS_Radio",
                "1-10",
                "On a scale of 1-10, how tedious is writing tests?",
                questionId,
                "SLIDER",
                null,
                null,
                ChoiceSetType.SectionChoiceSet,
                2);

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerValue(8)
                    .WithSectionChoiceId(1)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerValue(4)
                    .WithSectionChoiceId(2)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerValue(10)
                    .WithSectionChoiceId(3)
                    .Build()
            };

            singleEntitySliderResponseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return singleEntitySliderResponseFieldModel;
        }

        private ResponseFieldModel GetModelForSingleEntitySliderWithPageChoiceSet()
        {
            var questionId = 6;
            var singleEntitySliderResponseFieldModel = new ResponseFieldModel("TESTS_3",
                "TESTS_Dropzone",
                "1-10",
                "On a scale of 1-10, how tedious is writing tests?",
                questionId,
                "SLIDER",
                null,
                null,
                ChoiceSetType.SectionChoiceSet,
                3);

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId)
                    .WithAnswerValue(8)
                    .WithSectionChoiceId(1)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerValue(4)
                    .WithSectionChoiceId(2)
                    .Build(),
                new AnswerBuilder(questionId)
                    .WithAnswerValue(10)
                    .WithSectionChoiceId(3)
                    .Build()
            };

            singleEntitySliderResponseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return singleEntitySliderResponseFieldModel;
        }

        private static ResponseFieldModel GetRadioModelWithNullAnswer()
        {
            var questionId = 1;
            var singleEntityRadioResponseFieldModel = new ResponseFieldModel("GENDER",
                string.Empty,
                "1:M|2:F|3:O",
                "What is your gender?",
                1,
                "RADIO");

            var singleEntityRadioResponse = new List<Answer>
            {
                new AnswerBuilder(questionId).Build()
            };

            singleEntityRadioResponseFieldModel.GroupedData = new List<IGrouping<int, Answer>> {
                singleEntityRadioResponse.GroupBy(x => x.ResponseId).Single()
            };
            return singleEntityRadioResponseFieldModel;
        }
    }
}

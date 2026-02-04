using Microsoft.EntityFrameworkCore;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.EntityFramework;
using BrandVue.Services.ServiceModels;
using System.IO;

namespace BrandVue.Services
{
    public interface IResponseExportService
    {
        Stream ExportAllRespondents();
    }

    public class ResponseExportService : IResponseExportService
    {
        private readonly IAnswerDbContextFactory _dbContextFactory;
        internal List<ResponseFieldModel> _fieldModels { get; set; }
        private IProductContext _productContext;
        private List<ResponseExportWarningModel> _errors { get; set; }
        private IReadOnlyList<int> _subProductIds => _productContext.NonMapFileSurveyIds;

        public ResponseExportService(IAnswerDbContextFactory dbContextFactory, IProductContext productContext)
        {
            _dbContextFactory = dbContextFactory;
            _fieldModels = new List<ResponseFieldModel>();
            _errors = new List<ResponseExportWarningModel>();
            _productContext = productContext;
        }

        public Stream ExportAllRespondents()
        {
            CreateFieldModels();
            GetResponses();
            using var context = _dbContextFactory.CreateDbContext();
            var responseIds = context.SurveyResponses.Where(s => _subProductIds.Contains(s.SurveyId)
                && !s.Archived
                && s.Status == SurveyCompletionStatus.Completed)
                .Distinct()
                .OrderBy(a => a)
                .ToArray();
            var exporter = new ResponseExporter(_fieldModels, responseIds, _errors);
            exporter.CreateAndPopulateSheets();
            return exporter.ToStream();
        }

        private void GetResponses()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var answersContext = context.Answers;
            var answers = new List<IQueryable<Answer>>();

            foreach (var model in _fieldModels)
            {
                var questionAnswers = 
                    answersContext
                        .Where(answers => answers.QuestionId == model.QuestionId)
                        .AsEnumerable()
                        .GroupBy(a => a.ResponseId)
                        .ToList();
                model.GroupedData = questionAnswers;
            }
        }

        internal void CreateFieldModels()
        {
            using var context = _dbContextFactory.CreateDbContext();
            var questions = context.Questions
                .Where(q => _subProductIds.Contains(q.SurveyId))
                    .Include(q => q.AnswerChoiceSet)
                        .Include(q => q.AnswerChoiceSet.Choices)
                    .Include(q => q.PageChoiceSet)
                        .Include(q => q.PageChoiceSet.Choices)
                    .Include(q => q.SectionChoiceSet)
                        .Include(q => q.SectionChoiceSet.Choices)
                    .Include(q => q.QuestionChoiceSet)
                        .Include(q => q.QuestionChoiceSet.Choices);

            foreach (var question in questions)
            {
                switch (question.MasterType)
                {
                    case "RADIO":
                        if (question.EntityCount == 1)
                        {
                            ConvertSingleEntityRadioToResponseFieldModel(question, question.AnswerChoiceSet);
                        }
                        else if (question.EntityCount == 2)
                        {
                            var choiceSetAndType = question.ValuedChoiceSetsWithType;
                            var splitBy = choiceSetAndType.First(c => c.type == ChoiceSetType.AnswerChoiceSet);
                            var filterBy = choiceSetAndType.FirstOrDefault(c => c.type != splitBy.type);

                            ConvertTwoEntityRadioToResponseFieldModel(question, splitBy, filterBy);
                        }
                        else
                        {
                            _errors.Add(new ResponseExportWarningModel(question, $"Radio question has {question.EntityCount} choice sets where 1 or 2 was expected"));
                        }
                        break;

                    case "CHECKBOX":
                        if (question.EntityCount == 1)
                        {
                            var choiceSetAndType = question.ValuedChoiceSetsWithType.Single();
                            ConvertCheckBoxSingleEntityToResponseFieldModel(question, choiceSetAndType);
                        }
                        else if (question.EntityCount == 2)
                        {
                            var choiceSetAndType = question.ValuedChoiceSetsWithType;

                            (ChoiceSet choiceSet, ChoiceSetType type) splitBy;
                            //checkbox's never use the answerChoiceSet on the question
                            if (choiceSetAndType.Any(c => c.type == ChoiceSetType.SectionChoiceSet))
                            {
                                splitBy = choiceSetAndType.First(c => c.type == ChoiceSetType.SectionChoiceSet);
                            }
                            else if (choiceSetAndType.Any(c => c.type == ChoiceSetType.PageChoiceSet))
                            {
                                splitBy = choiceSetAndType.First(c => c.type == ChoiceSetType.PageChoiceSet);
                            }
                            else if (choiceSetAndType.Any(c => c.type == ChoiceSetType.QuestionChoiceSet))
                            {
                                splitBy = choiceSetAndType.First(c => c.type == ChoiceSetType.QuestionChoiceSet);
                            }
                            else
                            {
                                _errors.Add(new ResponseExportWarningModel(question, $"Unable to establish choiceset for question"));
                                break;
                            }

                            var filterBy = choiceSetAndType.FirstOrDefault(c => c.type != splitBy.type);
                            ConvertCheckBoxTwoEntityToResponseFieldModel(question, splitBy, filterBy);
                        }
                        else
                        {
                            _errors.Add(new ResponseExportWarningModel(question, $"Checkbox question has {question.EntityCount} choice sets where only 1 or 2 was expected"));
                        }
                        break;

                    case "DROPZONE":
                    case "COMBO":
                        {
                            if (question.EntityCount == 2)
                            {
                                var choiceSetAndType = question.ValuedChoiceSetsWithType;
                                var splitBy = choiceSetAndType.First(c => c.type == ChoiceSetType.AnswerChoiceSet);
                                var filterBy = choiceSetAndType.FirstOrDefault(c => c.type != splitBy.type);
                                ConvertDropZoneOrComboToResponseFieldModel(question, splitBy, filterBy);
                            }
                            else
                            {
                                _errors.Add(new ResponseExportWarningModel(question, $"{question.MasterType} question has {question.EntityCount} choice sets where 2 was expected"));
                            }

                            break;
                        }

                    case "SLIDER":
                        {
                            var valueLabels = $"{question.MinimumValue}-{question.MaximumValue}";
                            if (question.EntityCount == 0)
                            {
                                _fieldModels.Add(new ResponseFieldModel(question.VarCode,
                                    string.Empty,
                                    valueLabels,
                                    question.QuestionText,
                                    question.QuestionId,
                                    question.MasterType));
                            }

                            else if (question.EntityCount == 1)
                            {
                                var choiceSetAndType = question.ValuedChoiceSetsWithType.Single();
                                ConvertSingleEntitySliderToResponseFieldModel(question, choiceSetAndType.choiceSet, choiceSetAndType.type);
                            }
                            else
                            {
                                _errors.Add(new ResponseExportWarningModel(question, $"{question.MasterType} question has {question.EntityCount} choice sets where less than 2 was expected"));
                            }

                            break;
                        }

                    case "TEXT":
                    case "TEXTENTRY":
                        if (question.EntityCount == 0)
                        {
                            _fieldModels.Add(new ResponseFieldModel(question.VarCode,
                                string.Empty,
                                string.Empty,
                                question.QuestionText,
                                question.QuestionId,
                                question.MasterType));
                        }
                        else if (question.EntityCount == 1)
                        {
                            ConvertTextBoxWithEntityToResponseFieldModel(question);
                        }
                        else
                        {
                            _errors.Add(new ResponseExportWarningModel(question, $"{question.MasterType} question has {question.EntityCount} choice sets where 0 or 1 was expected"));
                        }
                        break;
                }
            }
        }

        private void ConvertTextBoxWithEntityToResponseFieldModel(Question question)
        {
            var choiceSetAndType = question.ValuedChoiceSetsWithType.Single();
            var valueLabelPairs = choiceSetAndType.choiceSet.Choices.Select(c => {
                return $"{c.SurveyChoiceId}:{c.GetDisplayName()}";
            });
            var valueLabels = string.Join("|", valueLabelPairs);

            foreach (var choice in choiceSetAndType.choiceSet.Choices)
            {
                var varCode = $"{question.VarCode}_{choice.SurveyChoiceId}";
                var label = $"{question.VarCode}_{choice.GetDisplayName()}";
                _fieldModels.Add(new ResponseFieldModel(varCode, label, valueLabels, question.QuestionText, question.QuestionId, question.MasterType, choiceSetAndType.type, null, null, null));
            }
        }

        private void ConvertCheckBoxTwoEntityToResponseFieldModel(Question question, (ChoiceSet choiceSet, ChoiceSetType type) splitBy, (ChoiceSet choiceSet, ChoiceSetType type) filterBy)
        {
            var splitByChoices = splitBy.choiceSet.Choices.OrderBy(c => c.ChoiceId);
            var filterByChoices = filterBy.choiceSet.Choices.OrderBy(c => c.ChoiceId);
            foreach(var sChoice in splitByChoices)
            {
                foreach (var fChoice in filterByChoices)
                {
                    var varCode = $"{question.VarCode}_{sChoice.SurveyChoiceId}_{fChoice.SurveyChoiceId}";
                    var label = $"{question.VarCode}_{sChoice.GetDisplayName()}_{fChoice.GetDisplayName()}";
                    var valueLabels = $"{sChoice.SurveyChoiceId}:{sChoice.GetDisplayName()}|{fChoice.SurveyChoiceId}:{fChoice.GetDisplayName()}";
                    var model = new ResponseFieldModel(varCode,
                        label,
                        valueLabels,
                        question.QuestionText,
                        question.QuestionId,
                        question.MasterType,
                        splitBy.type,
                        sChoice.SurveyChoiceId,
                        filterBy.type,
                        fChoice.SurveyChoiceId);
                    _fieldModels.Add(model);
                }
            }
        }

        private void ConvertCheckBoxSingleEntityToResponseFieldModel(Question question, (ChoiceSet choiceSet, ChoiceSetType type) choiceSet)
        {
            foreach(var choice in choiceSet.choiceSet.Choices)
            {
                var varCode = $"{question.VarCode}_{choice.SurveyChoiceId}";
                var label = $"{question.VarCode}_{choice.GetDisplayName()}";
                var valueLabelPairs = choiceSet.choiceSet.Choices.Select(c => {
                    return $"{c.SurveyChoiceId}:{c.GetDisplayName()}";
                });
                var valueLabels = string.Join("|", valueLabelPairs);
                var model = new ResponseFieldModel(varCode,
                    label,
                    valueLabels,
                    question.QuestionText,
                    question.QuestionId,
                    question.MasterType,
                    choiceSet.type,
                    choice.SurveyChoiceId,
                    null,
                    null);
                _fieldModels.Add(model);
            }
        }

        private void ConvertDropZoneOrComboToResponseFieldModel(Question question, (ChoiceSet choiceSet, ChoiceSetType type) splitBy, (ChoiceSet choiceSet, ChoiceSetType type) filterBy)
        {
            var valueLabelPairs = splitBy.choiceSet.Choices.Select(c => {
                return $"{c.SurveyChoiceId}:{c.GetDisplayName()}";
                    });
            var valueLabels = string.Join("|", valueLabelPairs);
            foreach (var choice in filterBy.choiceSet?.Choices)
            {
                var varCode = $"{question.VarCode}_{choice.SurveyChoiceId}";
                var label = $"{question.VarCode}_{choice.GetDisplayName()}";
                _fieldModels.Add(new ResponseFieldModel(varCode,
                    label,
                    valueLabels,
                    question.QuestionText,
                    question.QuestionId,
                    question.MasterType,
                    splitBy.type,
                    null,
                    filterBy.type,
                    choice.SurveyChoiceId));
            }
        }

        private void ConvertSingleEntitySliderToResponseFieldModel(Question question, ChoiceSet choiceSet, ChoiceSetType choiceSetType)
        {
            var valueLabels = $"{question.MinimumValue}-{question.MaximumValue}";
            if (choiceSet != null && choiceSet.Choices.Count > 0)
            {
                foreach (var choice in choiceSet?.Choices)
                {
                    var varCode = $"{question.VarCode}_{choice.SurveyChoiceId}";
                    var label = $"{question.VarCode}_{choice.GetDisplayName()}";
                    _fieldModels.Add(new ResponseFieldModel(varCode,
                        label,
                        valueLabels,
                        question.QuestionText,
                        question.QuestionId,
                        question.MasterType,
                        null,
                        null,
                        choiceSetType,
                        choice.SurveyChoiceId));
                }
            }
        }

        private void ConvertSingleEntityRadioToResponseFieldModel(Question question, ChoiceSet choiceSet = null)
        {
            var label = string.Empty;
            var valueLabelsList = new List<(int id, string label)>();
            if (choiceSet != null && choiceSet.Choices.Count > 0)
            {
                foreach (var choice in choiceSet?.Choices)
                {
                    valueLabelsList.Add((choice.SurveyChoiceId, choice.GetDisplayName()));
                }
            }
            var valueLabels = string.Join("|", valueLabelsList.Select(x => $"{x.id}:{x.label}").ToArray());
            _fieldModels.Add(new ResponseFieldModel(question.VarCode,
                label,
                valueLabels,
                question.QuestionText,
                question.QuestionId,
                question.MasterType));
        }

        private void ConvertTwoEntityRadioToResponseFieldModel(Question question, (ChoiceSet choiceSet, ChoiceSetType type) splitBy, (ChoiceSet choiceSet, ChoiceSetType type) filterBy)
        {
            var valueLabelPairs = splitBy.choiceSet.Choices.Select(c => {
                return $"{c.SurveyChoiceId}:{c.GetDisplayName()}";
            });
            var valueLabels = string.Join("|", valueLabelPairs);
            foreach (var choice in filterBy.choiceSet?.Choices)
            {
                var varCode = $"{question.VarCode}_{choice.SurveyChoiceId}";
                var label = $"{question.VarCode}_{choice.GetDisplayName()}";
                _fieldModels.Add(new ResponseFieldModel(varCode,
                    label,
                    valueLabels,
                    question.QuestionText,
                    question.QuestionId,
                    question.MasterType,
                    splitBy.type,
                    null,
                    filterBy.type,
                    choice.SurveyChoiceId));
            }
        }
    }
}

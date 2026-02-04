using BrandVue.EntityFramework.Answers.Model;
using System;

namespace Test.BrandVue.FrontEnd.Services
{
    public class QuestionBuilder
    {
        public Question _question;
        public QuestionBuilder(int questionId, int surveyId)
        {
            _question = new Question();
            _question.QuestionId = questionId;
            _question.SurveyId = surveyId;
        }

        public QuestionBuilder WithQuestionText(string questionText)
        {
            _question.QuestionText = questionText;
            return this;
        }

        public Question Build()
        {
            return _question;
        }

        public QuestionBuilder WithVarCode(string varCode)
        {
            _question.VarCode = varCode;
            return this;
        }

        public QuestionBuilder WithMasterType(string masterType)
        {
            _question.MasterType = masterType;
            return this;
        }

        public QuestionBuilder WithAnswerChoiceSet(ChoiceSet answerChoiceSet)
        {
            _question.AnswerChoiceSet = answerChoiceSet;
            return this;
        }

        public QuestionBuilder WithQuestionChoiceSet(ChoiceSet choiceSet)
        {
            _question.QuestionChoiceSet = choiceSet;
            return this;
        }

        public QuestionBuilder WithSectionChoiceSet(ChoiceSet choiceSet)
        {
            _question.SectionChoiceSet = choiceSet;
            return this;
        }

        public QuestionBuilder WithPageChoiceSet(ChoiceSet choiceSet)
        {
            _question.PageChoiceSet = choiceSet;
            return this;
        }

        public QuestionBuilder WithMinimumValue(int value)
        {
            _question.MinimumValue = value;
            return this;
        }

        public QuestionBuilder WithMaximumValue(int value)
        {
            _question.MaximumValue = value;
            return this;
        }

    }
}

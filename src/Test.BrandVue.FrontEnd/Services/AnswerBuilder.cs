using BrandVue.EntityFramework.Answers.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.BrandVue.FrontEnd.Services
{
    public class AnswerBuilder
    {
        public Answer _answer;
        public AnswerBuilder(int questionId)
        {
            _answer = new Answer();
            _answer.ResponseId = 1;
            _answer.QuestionId = questionId;
        }

        public AnswerBuilder WithAnswerChoiceId(int answerChoiceId)
        {
            _answer.AnswerChoiceId = answerChoiceId;
            return this;
        }

        public Answer Build()
        {
            return _answer;
        }

        public AnswerBuilder WithSectionChoiceId(int sectionChoiceId)
        {
            _answer.SectionChoiceId = sectionChoiceId;
            return this;
        }

        public AnswerBuilder WithQuestionChoiceId(int choiceId)
        {
            _answer.QuestionChoiceId = choiceId;
            return this;
        }

        public AnswerBuilder WithPageChoiceId(int pageChoiceId)
        {
            _answer.PageChoiceId = pageChoiceId;
            return this;
        }


        public AnswerBuilder WithAnswerValue(int value)
        {
            _answer.AnswerValue = value;
            return this;
        }
    }
}

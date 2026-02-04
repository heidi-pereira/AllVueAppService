using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.Answers.Model
{
    [Keyless]
    public class Answer
    {
        [Required]
        public int ResponseId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public Question Question { get; set; }

        public int? SectionChoiceId { get; set; }

        public int? PageChoiceId { get; set; }

        public int? QuestionChoiceId { get; set; }

        public int? AnswerChoiceId { get; set; }

        public int? AnswerValue { get; set; }

        [MaxLength(4000)] 
        public string AnswerText { get; set; }

        public SurveyResponse Response { get; set; }
    }
}
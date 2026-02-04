using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.ResponseRepository
{

    [Keyless]
    public class AnswerTextResponse
    {
        public int ResponseId { get; set; }
        public int? SectionChoiceId { get; set; }
        public int? PageChoiceId { get; set; }
        public int? QuestionChoiceId { get; set; }
        public string AnswerText { get; set; }
        public string VarCode { get; set; }
    }
}
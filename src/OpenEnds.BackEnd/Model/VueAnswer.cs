using System.ComponentModel.DataAnnotations.Schema;

namespace OpenEnds.BackEnd.Model;

public class VueAnswer
{

    public int QuestionId { get; set; }
    public int ResponseId { get; set; }
    public string AnswerText { get; set; }

    public VueQuestion Question { get; set; }
    public int? SectionChoiceId { get; set; }
    public int? PageChoiceId { get; set; }
    public int? QuestionChoiceId { get; set; }
}
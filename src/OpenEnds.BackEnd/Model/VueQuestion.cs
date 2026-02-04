using System.ComponentModel.DataAnnotations;

namespace OpenEnds.BackEnd.Model;

public class VueQuestion
{
    [Key]
    public int QuestionId { get; set; }
    public int SurveyId { get; set; }
    public string VarCode { get; set; }
    public string QuestionText { get; set; }
    public string MasterType { get; set; }
    public int? QuestionChoiceSetId { get; set; }
    public bool QuestionShownInSurvey { get; set; }
}
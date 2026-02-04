namespace OpenEnds.BackEnd.Model
{
    public class OpenEndResponse
    {
        public int ResponseId { get; set; }
        public int? QuestionChoiceId { get; set; }
        public string AnswerText { get; set; }
        public int? SectionChoiceId { get; set; }
    }
}

using OpenEnds.BackEnd.Controllers;

namespace OpenEnds.BackEnd.Model
{
    public class OpenEndQuestionSummaryResult
    {
        public Question Question { get; set; }
        public string Summary { get; set; }
        public List<OpenEndTheme> Themes { get; set; }
        public IEnumerable<TextTheme> TextThemes { get; set; }
        public int OpenTextAnswerCount { get; set; }
        public int TotalCount { get; set; }
        public string AdditionalInstructions { get; set; }
    }
}
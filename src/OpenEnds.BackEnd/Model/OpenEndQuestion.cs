using OpenEnds.BackEnd.Library;

namespace OpenEnds.BackEnd.Model
{
    public class OpenEndQuestion
    {
        public Question Question { get; set; }
        public int QuestionCount { get; set; }
        public StatusResult Status { get; set; }
        public int ThemeCount { get; set; }
        public string AdditionalInstructions { get; set; }
    }
}
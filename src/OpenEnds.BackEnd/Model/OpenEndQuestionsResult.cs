namespace OpenEnds.BackEnd.Model
{
    public class OpenEndQuestionsResult
    {
        public int RespondentCount { get; set; }
        public IList<OpenEndQuestion> OpenTextQuestions { get; set; }
    }
}
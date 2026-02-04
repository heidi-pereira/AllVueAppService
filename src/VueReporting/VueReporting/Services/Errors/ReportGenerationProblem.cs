using System;

namespace VueReporting.Services.Errors
{
    public enum ProblemType
    {
        ChartProblem,
        DynamicTextProblem
    }

    public class ReportGenerationProblem
    {
        public int SlideNumber { get; set; }

        public ProblemType ProblemType { get; set; }

        public string ProblemMessage { get; set; }

        /// <summary>
        /// Empty constructor required by CsvHelper code
        /// </summary>
        public ReportGenerationProblem()
        {
        }

        public ReportGenerationProblem(int slideNumber, ProblemType problemType, Exception exception) : this(slideNumber, problemType, $"Exception msg: {exception.Message} Stack trace: {exception.StackTrace}")
        {
        }

        public ReportGenerationProblem(int slideNumber, ProblemType problemType, string problemMessage)
        {
            SlideNumber = slideNumber;
            ProblemType = problemType;
            ProblemMessage = problemMessage;
        }
    }
}
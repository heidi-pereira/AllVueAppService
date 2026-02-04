using System;
using System.Collections.Generic;

namespace VueReporting.Services.Errors
{
    public class ReportGenerationException : Exception
    {
        public List<ReportGenerationProblem> ReportGenerationProblems { get; set; }

        public ReportGenerationException(List<ReportGenerationProblem> reportProblems) : base(reportProblems?.Count > 0 ? reportProblems[0].ProblemMessage : "No problem list was specified")
        {
            this.ReportGenerationProblems = reportProblems;
        }
    }
}

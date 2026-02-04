using BrandVue.EntityFramework.MetaData.Reports;

namespace BrandVue.Models
{
    public class CrosstabRequestOptions
    {
        public bool CalculateSignificance { get; set; }
        public DisplaySignificanceDifferences DisplaySignificanceDifferences { get; set; }
        public CrosstabSignificanceType SignificanceType { get; set; }
        public bool IsDataWeighted { get; set; }
        public bool HideEmptyColumns { get; set; }
        public bool ShowMultipleTablesAsSingle { get; set; }
        public SigConfidenceLevel SigConfidenceLevel { get; set; }
        public bool CalculateIndexScores { get; set; }
    }
}

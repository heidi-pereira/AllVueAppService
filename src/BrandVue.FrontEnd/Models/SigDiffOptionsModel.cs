using BrandVue.EntityFramework.MetaData.Reports;

namespace BrandVue.Models
{
    public class SigDiffOptions
    {
        public bool HighlightSignificance { get; set; }
        public SigConfidenceLevel SigConfidenceLevel { get; set; }
        public DisplaySignificanceDifferences DisplaySignificanceDifferences { get; set; }
        public CrosstabSignificanceType SignificanceType { get; set; }

        public SigDiffOptions(bool highlightSignificance,
            SigConfidenceLevel sigConfidenceLevel,
            DisplaySignificanceDifferences displaySignificanceDifferences,
            CrosstabSignificanceType significanceType)
        {
            HighlightSignificance = highlightSignificance;
            SigConfidenceLevel = sigConfidenceLevel;
            DisplaySignificanceDifferences = displaySignificanceDifferences;
            SignificanceType = significanceType;
        }
    }
}

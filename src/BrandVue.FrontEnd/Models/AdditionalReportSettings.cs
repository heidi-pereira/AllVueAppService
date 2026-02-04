using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;

namespace BrandVue.Models
{
    public class AdditionalReportSettings
    {
        public bool IncludeCounts { get; set; }
        public bool WeightingEnabled { get; set; }
        public bool HighlightLowSample { get; set; }
        public bool HighlightSignificance { get; set; }
        public DisplaySignificanceDifferences DisplaySignificanceDifferences { get; set; }
        public bool DisplayMeanValues { get; set; }
        public CrosstabSignificanceType SignificanceType { get; set; }
        public ReportOrder ResultSortingOrder { get; set; }
        public int DecimalPlaces { get; set; }
        public AverageType[] SelectedAverages { get; set; }
        public CrossMeasure[] Categories { get; set; }
        public SigConfidenceLevel SigConfidenceLevel { get; set; }
        public bool HideTotalColumn { get; set; }
        public bool ShowMultipleTablesAsSingle { get; set; }
        public bool CalculateIndexScores { get; set; }
        public BaseDefinitionType? BaseTypeOverride { get; set; }
        public int? BaseVariableId { get; set; }
    }
}

using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;

namespace BrandVue.EntityFramework.MetaData.Reports
{
    public abstract class AllVueReport
    {
        public bool IsShared { get; set; }
        public string CreatedByUserId { get; set; }
        public ReportOrder Order { get; set; }
        public int DecimalPlaces { get; set; }
        public ReportType ReportType { get; set; }
        public ReportWaveConfiguration Waves { get; set; }
        public List<CrossMeasure> Breaks { get; set; }
        public bool SinglePageExport { get; set; }
        public bool HighlightSignificance { get; set; }
        public CrosstabSignificanceType SignificanceType { get; set; }
        public DisplaySignificanceDifferences DisplaySignificanceDifferences { get; set; }
        public SigConfidenceLevel SigConfidenceLevel { get; set; }
        public bool IncludeCounts { get; set; }
        public bool CalculateIndexScores { get; set; }
        public bool HighlightLowSample { get; set; }
        public bool IsDataWeighted { get; set; }
        public bool HideEmptyRows { get; set; }
        public bool HideEmptyColumns { get; set; }
        public bool HideTotalColumn { get; set; }
        public bool HideDataLabels { get; set; }
        public bool ShowMultipleTablesAsSingle { get; set; }
        public BaseDefinitionType? BaseTypeOverride { get; set; }
        public List<DefaultReportFilter> DefaultFilters { get; set; }
        public ReportOverTimeConfiguration OverTimeConfig { get; set; }
        public string SubsetId { get; set; }
        public int? LowSampleThreshold { get; set; }
    }
}
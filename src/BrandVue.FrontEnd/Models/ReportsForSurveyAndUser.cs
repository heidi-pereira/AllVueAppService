using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Reports;
using NJsonSchema.Annotations;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Averages;

namespace BrandVue.Models
{
    public class ReportsForSurveyAndUser
    {
        public int? DefaultReportId { get; set; }
        public IEnumerable<Report> Reports { get; set; }
    }

    public class Report
    {
        public int SavedReportId { get; set; }
        public bool IsShared { get; set; }
        public int PageId { get; set; }
        public ReportOrder ReportOrder { get; set; }
        public DateTimeOffset ModifiedDate { get; set; }
        public string ModifiedGuid { get; set; }
        public string LastModifiedByUser { get; set; }
        public int DecimalPlaces { get; set; }
        public ReportType ReportType { get; set; }
        [CanBeNull]
        public ReportWaveConfiguration Waves { get; set; }
        public List<CrossMeasure> Breaks { get; set; }
        public bool IncludeCounts { get; set; }
        public bool CalculateIndexScores { get; set; }
        public bool HighlightLowSample { get; set; }
        public bool HighlightSignificance { get; set; }
        public bool ShowMultipleTablesAsSingle { get; set; }
        public CrosstabSignificanceType SignificanceType {get;set;}
        public DisplaySignificanceDifferences DisplaySignificanceDifferences { get; set; }
        public SigConfidenceLevel SigConfidenceLevel { get;set;}
        public bool SinglePageExport { get; set; }
        public bool IsDataWeighted { get; set; }
        public bool HideEmptyRows { get; set; }
        public bool HideEmptyColumns { get; set; }
        public bool HideTotalColumn { get; set; }
        public bool HideDataLabels { get; set; }
        public BaseDefinitionType BaseTypeOverride { get; set; }
        public int? BaseVariableId { get; set; }
        public List<DefaultReportFilter> DefaultFilters { get; set; }
        [CanBeNull]
        public ReportOverTimeConfiguration OverTimeConfig { get; set; }
        [CanBeNull]
        public string SubsetId { get; set; }
        public bool UserHasAccess { get; set; }
        public int? LowSampleThreshold { get; set; }
    }

    public record ParsedReport(
        Measure Measure,
        AverageDescriptor Average,
        TargetInstances SplitByInstances,
        IEnumerable<TargetInstances> FilterByInstances,
        Break[] Breaks)
    {
        public IEnumerable<TargetInstances> TargetInstances => SplitByInstances != null
                ? SplitByInstances.EntityType.IsProfile
                    ? Enumerable.Empty<TargetInstances>()
                    : new List<TargetInstances>(FilterByInstances)
                        { SplitByInstances
                        }
                : [];
    }
}

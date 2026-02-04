using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;
using BrandVue.SourceData.QuotaCells;
using BrandVue.EntityFramework.MetaData.Reports;
using Newtonsoft.Json;
using BrandVue.EntityFramework.MetaData.Averages;

namespace BrandVue.Models
{
    public abstract class CrosstabExportRequestOptions
    {
        public ReportOrder ResultSortingOrder { get; set; }
        public bool IncludeCounts { get; set; }
        public bool CalculateIndexScores { get; set; }
        public bool HighlightLowSample { get; set; }
        public int DecimalPlaces { get; set; }
        public bool HideEmptyRows { get; set; }
        public bool HideEmptyColumns { get; set; }
        public bool HideTotalColumn { get; set; }
        public bool ShowMultipleTablesAsSingle { get; set; }
        public AverageType[] Averages { get; set; }
        public bool DisplayMeanValues { get; set; }
        public bool DisplayStandardDeviation { get; set; }
        public int LowSampleThreshold { get; set; }
    }

    public class CrosstabExportRequest : CrosstabExportRequestOptions, ISubsetIdProvider
    {
        public CrosstabRequestModel RequestModel { get; set; }
        [JsonIgnore]
        public string SubsetId => RequestModel.SubsetId;
    }

    public class ReportExportRequest : ISubsetIdProvider
    {
        [Required]
        public string SubsetId { get; set; }
        [Required]
        public Period Period { get; set; }
        [Required]
        public Period OverTimePeriod { get; set; }
        [Required]
        public DemographicFilter DemographicFilter { get; set; }
        [Required]
        public CompositeFilterModel FilterModel { get; set; }
        [Required]
        public int SavedReportId { get; set; }
        [Required]
        public bool UseGenerativeAi { get; set; }
    }

    public class ReportPartExportRequest : ReportExportRequest
    {
        [Required]
        public int PartId { get; set; }
    }

}

using BrandVue.EntityFramework.MetaData.Breaks;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Reports;
using NJsonSchema.Annotations;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public class UpdateReportSettingsRequest : ISubsetIdProvider
    {
        [Required]
        public string PageDisplayName { get; set; }

        [Required]
        public string PageName { get; set; }

        [Required]
        public int SavedReportId { get; set; }

        [Required]
        public bool IsShared { get; set; }

        [Required]
        public bool IsDefault { get; set; }

        [Required]
        public ReportOrder Order { get; set; }

        [Required]
        public int DecimalPlaces { get; set; }

        [CanBeNull]
        public ReportWaveConfiguration Waves { get; set; }

        [Required]
        public CrossMeasure[] Breaks { get; set; }

        [Required]
        public bool IncludeCounts { get; set; }
        [Required]
        public bool CalculateIndexScores { get; set; }

        [Required]
        public bool HighlightLowSample { get; set; }
        [Required]
        public bool HighlightSignificance { get; set; }
        [Required]
        public DisplaySignificanceDifferences DisplaySignificanceDifferences { get; set; }
        [Required]
        public CrosstabSignificanceType SignificanceType { get; set; }
        [Required]
        public SigConfidenceLevel SigConfidenceLevel { get; set; }
        [Required]
        public bool SinglePageExport { get; set; }
        [Required]
        public bool IsDataWeighted { get; set; }
        [Required]
        public bool HideEmptyRows { get; set; }
        [Required]
        public bool HideEmptyColumns { get; set; }
        [Required]
        public bool HideTotalColumn { get; set; }
        [Required]
        public bool HideDataLabels { get; set; }
        [Required]
        public bool ShowMultipleTablesAsSingle { get; set; }
        public BaseDefinitionType? BaseTypeOverride { get; set; }
        public int? BaseVariableId { get; set; }
        [Required]
        public DefaultReportFilter[] DefaultFilters { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string ModifiedGuid { get; set; }
        [CanBeNull]
        public string SubsetId { get; set; }
        [CanBeNull]
        public ReportOverTimeConfiguration OverTimeConfig { get; set; }
        public int? LowSampleThreshold { get; set; }
    }
}

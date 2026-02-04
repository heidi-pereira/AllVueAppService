using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.SourceData.Dashboard;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public class CreateNewReportRequest : ISubsetIdProvider
    {
        [Required]
        public ReportType ReportType { get; set; }
        [Required]
        public bool IsShared { get; set; }
        [Required]
        public bool IsDefault { get; set; }
        [Required]
        public string SubsetId { get; set; }
        [Required]
        public PageDescriptor Page { get; set; }
        [Required]
        public ReportOrder Order { get; set; }
        [CanBeNull]
        public ReportWaveConfiguration Waves { get; set; }
        [CanBeNull]
        public ReportOverTimeConfiguration OverTimeConfig { get; set; }
        [CanBeNull]
        public AdditionalReportSettings AdditionalReportSettings {get;set;}
    }
}

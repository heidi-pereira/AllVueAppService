using BrandVue.SourceData.Dashboard;
using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;
using System.Collections.Generic;
using BrandVue.EntityFramework.MetaData.Page;

namespace BrandVue.Models
{
    public class CopySavedReportRequest : ISubsetIdsProvider<IEnumerable<string>>
    {
        [Required]
        public int ReportId { get; set; }
        [Required]
        public PageDescriptor ExistingPage { get; set; }
        [Required]
        public string NewName { get; set; }
        [Required]
        public string NewDisplayName { get; set; }
        [Required]
        public bool IsShared { get; set; }
        [Required]
        public bool IsDefault { get; set; }
        IEnumerable<string> ISubsetIdsProvider<IEnumerable<string>>.SubsetIds => ((ISubsetIdsProvider<IEnumerable<string>>)ExistingPage).SubsetIds;
    }
}

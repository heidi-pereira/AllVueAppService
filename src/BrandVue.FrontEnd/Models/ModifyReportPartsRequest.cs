using BrandVue.SourceData.Dashboard;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.Models
{
    public class ModifyReportPartsRequest
    {
        [Required]
        public int SavedReportId { get; set; }
        [Required]
        public PartDescriptor[] Parts { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string ExpectedGuid { get; set; }
    }
}

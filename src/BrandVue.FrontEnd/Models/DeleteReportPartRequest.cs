using BrandVue.SourceData.Dashboard;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.Models
{
    public class DeleteReportPartRequest
    {
        [Required]
        public int SavedReportId { get; set; }
        [Required]
        public int PartIdToDelete { get; set; }
        [Required]
        public PartDescriptor[] PartsToUpdate { get; set; }
        [Required(AllowEmptyStrings = true)]
        public string ExpectedGuid { get; set; }
    }
}

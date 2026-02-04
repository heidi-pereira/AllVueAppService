using System.ComponentModel.DataAnnotations;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public class AsyncExportTaskModel : ISubsetIdProvider
    {
        [Required]
        public string ExportKey { get; set; }
        [Required]
        public string SubsetId { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData.Page
{
    [PrimaryKey(nameof(SubsetId), nameof(PageId))]
    public class PageSubsetConfiguration
    {
        public int SubsetId { get; set; }
        public int PageId { get; set; }
        [MaxLength(400)]
        public string HelpText { get; set; }
        public bool Enabled { get; set; }
        
        [ForeignKey(nameof(SubsetId))]
        public SubsetConfiguration Subset { get; set; }
        
        [ForeignKey(nameof(PageId))]
        public DbPage Page { get; set; }
    }
}
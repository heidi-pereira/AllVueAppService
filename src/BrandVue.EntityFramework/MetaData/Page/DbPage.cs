using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.Page
{
    [Table("Pages")]
    public class DbPage
    {
        public int Id { get; set; }
        public string ProductShortCode { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public int? PageDisplayIndex { get; set; }
        public int? ParentId { get; set; }
        public string MenuIcon { get; set; }
        public string PageType { get; set; }
        public string HelpText { get; set; }
        public int MinUserLevel { get; set; }
        public bool StartPage { get; set; }
        public string Layout { get; set; }
        public string PageTitle { get; set; }
        public string AverageGroup { get; set; }
        public string Subset { get; set; }
        public string Roles { get; set; }
        public string DefaultBase { get; set; }
        public int? DefaultPaneViewType { get; set; }
        
        public ICollection<PageSubsetConfiguration> PageSubsetConfiguration { get; set; }
    }
}

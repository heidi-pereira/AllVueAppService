using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BrandVue.EntityFramework.MetaData.Page;

namespace BrandVue.EntityFramework.MetaData
{
    public class SubsetConfiguration
    {
        [Key]
        public int Id { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Identifier { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string DisplayName { get; set; }
        [StringLength(50)]
        public string DisplayNameShort { get; set; }
        [StringLength(256)]
        public string Alias { get; set; }
        [StringLength(2)]
        public string Iso2LetterCountryCode { get; set; }
        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Description { get; set; }
        public int Order { get; set; }
        public bool Disabled { get; set; }

        /// <summary>
        /// If there are no specified segment names, use all
        /// </summary>
        public IReadOnlyDictionary<int, IReadOnlyCollection<string>> SurveyIdToAllowedSegmentNames { get; set; }
        public bool EnableRawDataApiAccess { get; set; }
        [MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        public DateTimeOffset? OverriddenStartDate { get; set; }
        public bool AlwaysShowDataUpToCurrentDate { get; set; }
        public string? ParentGroupName { get; set; }

        public ICollection<PageSubsetConfiguration> PageSubsetConfigurations { get; set; }
    }
}

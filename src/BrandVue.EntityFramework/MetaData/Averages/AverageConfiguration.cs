using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData.Averages
{
    [Table("Averages")]
    public class AverageConfiguration : ISubsetIdsProvider<string[]>
    {
        [Key]
        public int Id { get; set; }
        [Required(AllowEmptyStrings = true), MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [Required, MaxLength(100)]
        public string AverageId { get; set; }
        [Required, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string DisplayName { get; set; }
        [Required]
        public int Order { get; set; }
        public string[] Group { get; set; }
        [Required]
        public TotalisationPeriodUnit TotalisationPeriodUnit { get; set; }
        [Required]
        public int NumberOfPeriodsInAverage { get; set; }
        [Required]
        public WeightingMethod WeightingMethod { get; set; }
        [Required]
        public WeightAcross WeightAcross { get; set; }
        [Required]
        public AverageStrategy AverageStrategy { get; set; }
        [Required]
        public MakeUpTo MakeUpTo { get; set; }
        [Required]
        public WeightingPeriodUnit WeightingPeriodUnit { get; set; }
        [Required]
        public bool IncludeResponseIds { get; set; }
        [Required]
        public bool IsDefault { get; set; }
        [Required]
        public bool AllowPartial { get; set; }
        [Required]
        public bool Disabled { get; set; }
        [Required]
        public string[] SubsetIds { get; set; } //SubsetIds = empty means any subset, should convert to AverageDescriptor.Subset = null

        [CanBeNull]
        public string AuthCompanyShortCode { get; set; }
    }

    public class AverageConfigurationConfiguration : IEntityTypeConfiguration<AverageConfiguration>
    {
        public void Configure(EntityTypeBuilder<AverageConfiguration> builder)
        {
            builder.HasIndex(r => new { r.ProductShortCode, r.SubProductId, r.AverageId }).IsUnique();
            builder.Property(r => r.SubsetIds).HasJsonConversion().HasDefaultValue(Array.Empty<string>());
            builder.Property(r => r.Group).HasJsonConversion();
        }
    }
}

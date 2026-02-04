using BrandVue.EntityFramework.MetaData.Breaks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NJsonSchema.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BrandVue.EntityFramework.MetaData
{
    [Table("SavedBreakCombinations", Schema = "SavedBreaks")]
    public class SavedBreakCombination
    {
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [CanBeNull, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [CanBeNull, MaxLength(50)]
        public string AuthCompanyShortCode { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; }
        [CanBeNull, MaxLength(50)]
        public string Category { get; set; }
        [Required]
        public bool IsShared { get; set; }
        [Required, MaxLength(450)]
        public string CreatedByUserId { get; set; }
        [Required]
        public List<CrossMeasure> Breaks { get; set; }
        [CanBeNull, MaxLength(750)]
        public string Description { get; set; }
    }

    [Table("DefaultBreaksForSubProducts", Schema = "SavedBreaks")]
    public class DefaultSavedBreaks
    {
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [CanBeNull, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [Required]
        public SavedBreakCombination SavedBreakCombination { get; set; }
    }

    public class SavedBreakCombinationConfiguration : IEntityTypeConfiguration<SavedBreakCombination>
    {
        public void Configure(EntityTypeBuilder<SavedBreakCombination> builder)
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Breaks).HasJsonConversion();
            builder.HasIndex(b => new { b.ProductShortCode, b.SubProductId, b.AuthCompanyShortCode });
        }
    }

    public class DefaultSavedBreaksConfiguration : IEntityTypeConfiguration<DefaultSavedBreaks>
    {
        public void Configure(EntityTypeBuilder<DefaultSavedBreaks> builder)
        {
            builder.HasKey(b => new { b.ProductShortCode, b.SubProductId });
        }
    }
}

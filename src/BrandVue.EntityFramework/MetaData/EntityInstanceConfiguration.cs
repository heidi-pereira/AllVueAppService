using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData
{
    public class EntityInstanceConfiguration
    {
        [Key]
        public int Id { get; set; }

        public int SurveyChoiceId { get; set; }

        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string EntityTypeIdentifier { get; set; }

        public Dictionary<string, string> DisplayNameOverrideBySubset { get; set; }

        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string ProductShortCode { get; set; }

        [StringLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }

        [StringLength(SqlTypeConstants.DefaultJsonVarcharLength)]
        [Required]
        public Dictionary<string, bool> EnabledBySubset { get; set; }

        [StringLength(SqlTypeConstants.DefaultJsonVarcharLength)]
        public Dictionary<string, DateTimeOffset> StartDateBySubset { get; set; } = new Dictionary<string, DateTimeOffset>();

        [MaxLength(1024)]
        public string ImageURL { get; set; }
    }
    
    public class EntityInstanceConfigurationConfiguration : IEntityTypeConfiguration<EntityInstanceConfiguration>
    {
        public void Configure(EntityTypeBuilder<EntityInstanceConfiguration> builder)
        {
            builder.HasIndex(r => r.Id).IsUnique();
            builder.Property(r => r.EnabledBySubset).HasJsonConversion(SqlTypeConstants.DefaultJsonVarcharLength).HasDefaultValue(new Dictionary<string, bool>());
            builder.Property(r => r.StartDateBySubset).HasJsonConversion(SqlTypeConstants.DefaultJsonVarcharLength);
            builder.Property(r => r.DisplayNameOverrideBySubset).HasJsonConversion();
        }
    }
}

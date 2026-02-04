using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenEnds.BackEnd.Model
{
    [Table("VariableConfigurations")]
    public class VariableConfiguration
    {
        [Key]
        public int Id { get; set; }
        public required string ProductShortCode { get; set; }
        public string? SubProductId { get; set; }
        public required string DisplayName { get; set; }
        public required VariableDefinition Definition { get; set; }
        public required string Identifier { get; set; }
    }

    public class VariableConfigurationConfiguration : IEntityTypeConfiguration<VariableConfiguration>
    {
        public void Configure(EntityTypeBuilder<VariableConfiguration> builder)
        {
            builder.HasKey(c => c.Id);
            builder.HasIndex(v => new { v.ProductShortCode, v.SubProductId, v.DisplayName }).IsUnique();
            builder.Property(b => b.Definition).HasJsonConversion();
        }
    }
}

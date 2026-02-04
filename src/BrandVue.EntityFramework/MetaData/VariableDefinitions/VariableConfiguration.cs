using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.VariableDefinitions
{
    [Table("VariableConfigurations")]
    public record VariableConfiguration
    {
        public int Id { get; init; }

        [Required, MaxLength(20)]
        public string ProductShortCode { get; init; }

        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; init; }

        /// <summary>
        /// Python-safe identifier
        /// </summary>
        [Required, MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string Identifier { get; init; }

        /// <summary>
        /// Name of the new variable
        /// </summary>
        [Required]
        public string DisplayName { get; init; }

        /// <summary>
        /// Specific implementation of the variable. Either
        /// 1. FieldExpressionVariableDefinition, a single FieldExpression which returns a single value
        /// 2. GroupedVariableDefinition, mapping from multiple fields into one output entity type
        /// 3. QuestionVariableDefinition, autocreated from questions and fields
        /// </summary>
        public VariableDefinition Definition { get; init; }

        public ICollection<VariableDependency> VariableDependencies { get; init; } = new List<VariableDependency>();
        public ICollection<VariableDependency> VariablesDependingOnThis { get; init; } = new List<VariableDependency>();
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

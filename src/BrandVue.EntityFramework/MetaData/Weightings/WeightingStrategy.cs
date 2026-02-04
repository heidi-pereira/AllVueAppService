using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Weightings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NJsonSchema.Annotations;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    [Table("WeightingStrategies")]
    public class WeightingStrategy
    {
        public int Id { get; set; }

        public string ProductShortCode { get; set; }

        public string SubProductId { get; set; }

        /// <summary>
        /// If null, the strategy is valid for all Subsets.
        /// </summary>
        [MaxLength(50)]
        public string SubsetId { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Defines a single entity filter metric which splits the subset data into data batches.
        /// Each batch has a corresponding weighting scheme, otherwise it will be unweighted.
        /// If the value is null, the whole subset is treated as a single data batch.
        /// In this case there must be at most one weighting scheme in this strategy - used for the whole subset.
        /// </summary>
        [MaxLength(50), CanBeNull]
        public string FilterMetricName { get; set; }

        public List<WeightingScheme> WeightingSchemes { get; set; } = new();
    }
}

public class WeightingStrategyConfiguration : IEntityTypeConfiguration<WeightingStrategy>
{
    public void Configure(EntityTypeBuilder<WeightingStrategy> builder)
    {
        builder.HasKey(ws => ws.Id);
        builder.Property(w => w.ProductShortCode).IsRequired().HasMaxLength(20);
        builder.Property(w => w.SubProductId).HasMaxLength(SqlTypeConstants.DefaultVarcharLength);
        builder.HasIndex(ws => new {ws.ProductShortCode, ws.SubProductId, ws.SubsetId}).HasFilter(null).IsUnique();
        builder.HasIndex(ws => new {ws.ProductShortCode, ws.SubProductId, ws.Name}).HasFilter(null).IsUnique();
    }
}

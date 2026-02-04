using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;

namespace BrandVue.EntityFramework.MetaData
{
    public class LinkedMetric
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string ProductShortCode { get; set; }

        [MaxLength(256)]
        public string SubProductId { get; set; }
        
        [MaxLength(450)]
        public string MetricName { get; set; }

        public string[] LinkedMetricNames { get; set; }
    }

    public class LinkedMetricConfiguration : IEntityTypeConfiguration<LinkedMetric>
    {
        public void Configure(EntityTypeBuilder<LinkedMetric> builder)
        {
            builder.ToTable("LinkedMetric");
            builder.HasKey(lm => lm.Id);
            builder.Property(lm => lm.ProductShortCode).IsRequired();
            builder.Property(lm => lm.MetricName).IsRequired();
            builder.HasIndex(lm => new { lm.ProductShortCode, lm.MetricName });
            builder.Property(lm => lm.LinkedMetricNames).HasJsonConversion();
        }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData
{
    [Table("MetricAbout")]
    public class MetricAbout
    {
        public int Id { get; set; }
        [MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(450)]
        public string MetricName { get; set; }
        [MaxLength(200)]
        public string AboutTitle { get; set; }
        [MaxLength(1000)]
        public string AboutContent { get; set; }
        [MaxLength(200)]
        [CanBeNull]
        public string User { get; set; }
        public bool Editable { get; set; }
    }

    public class MetricAboutConfiguration : IEntityTypeConfiguration<MetricAbout>
    {
        public void Configure(EntityTypeBuilder<MetricAbout> builder)
        {
            builder.Property(m => m.AboutTitle).IsRequired();
            builder.Property(m => m.AboutContent).IsRequired();
            builder.Property(m => m.ProductShortCode).IsRequired();
            builder.Property(m => m.MetricName).IsRequired();
            builder.HasKey(m => m.Id);
            builder.HasIndex(m => new { m.ProductShortCode, m.MetricName });
        }
    }
}

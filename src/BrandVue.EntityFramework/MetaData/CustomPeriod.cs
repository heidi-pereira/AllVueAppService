using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData
{
    public class CustomPeriod
    {
        public int Id { get; set; }
        [Required, MaxLength(20)]
        public string ProductShortCode { get; set; }
        [MaxLength(SqlTypeConstants.DefaultVarcharLength)]
        public string SubProductId { get; set; }
        [MaxLength(50)]
        public string Organisation { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        [Required]
        public DateTimeOffset StartDate { get; set; }
        [Required]
        public DateTimeOffset EndDate { get; set; }
    }

    public class CustomPeriodConfiguration : IEntityTypeConfiguration<CustomPeriod>
    {
        public void Configure(EntityTypeBuilder<CustomPeriod> builder)
        {
            builder.HasKey(m => m.Id);
            builder.HasIndex(m => new { m.ProductShortCode, m.SubProductId, m.Organisation, m.Name }).IsUnique().HasFilter(null);

            builder.Property(e => e.StartDate)
                .HasConversion(v => v.UtcDateTime, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            builder.Property(e => e.EndDate)
                .HasConversion(v => v.UtcDateTime, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        }
    }
}

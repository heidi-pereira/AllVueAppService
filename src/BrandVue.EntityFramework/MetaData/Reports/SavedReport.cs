using BrandVue.EntityFramework.MetaData.Page;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Reports
{
    public class SavedReport : AllVueReport
    {
        public int Id { get; set; }
        public string ProductShortCode { get; set; }
        public string SubProductId { get; set; }
        public int ReportPageId { get; set; }
        public DbPage ReportPage { get; set; }
        public DateTimeOffset ModifiedDate { get; set; }
        public string ModifiedGuid { get; set; }
        public string LastModifiedByUser { get; set; }
        public int? BaseVariableId { get; set; }
        public TemplateImportLog TemplateImportLog { get; set; }
    }

    public class DefaultSavedReport
    {
        public string ProductShortCode { get; set; }
        public string SubProductId { get; set; }
        public SavedReport Report { get; set; }
    }

    public class SavedReportConfiguration : IEntityTypeConfiguration<SavedReport>
    {
        public void Configure(EntityTypeBuilder<SavedReport> builder)
        {
            builder.ToTable("SavedReports", "Reports");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.ProductShortCode).IsRequired().HasMaxLength(20);
            builder.Property(r => r.SubProductId).IsRequired().HasMaxLength(SqlTypeConstants.DefaultVarcharLength);
            builder.Property(r => r.ReportPageId).IsRequired();
            builder.Property(r => r.ModifiedDate).IsRequired()
                .HasConversion(v => v.UtcDateTime, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            builder.Property(r => r.ModifiedGuid).IsRequired().HasMaxLength(32);
            builder.Property(r => r.LastModifiedByUser);
            builder.Property(r => r.BaseVariableId);

            builder.HasIndex(r => new { r.ProductShortCode, r.SubProductId });

            builder.HasOne(r => r.ReportPage)
                .WithMany()
                .HasForeignKey(r => r.ReportPageId)
                .IsRequired();

            builder.Property(b => b.Waves).HasJsonConversion();
            builder.Property(b => b.Breaks).HasJsonConversion();
            builder.Property(b => b.DefaultFilters).HasJsonConversion().HasDefaultValue(new List<DefaultReportFilter>());
            builder.Property(b => b.OverTimeConfig).HasJsonConversion();
            builder.Property(b => b.SubsetId).HasMaxLength(450);
            builder.Property(b => b.CreatedByUserId).IsRequired().HasMaxLength(450);
            builder.Property(b => b.DefaultFilters).IsRequired();
            builder.Property(b => b.TemplateImportLog).HasJsonConversion();
        }
    }

    public class DefaultSavedReportConfiguration : IEntityTypeConfiguration<DefaultSavedReport>
    {
        public void Configure(EntityTypeBuilder<DefaultSavedReport> builder)
        {
            builder.ToTable("DefaultSavedReports", "Reports");
            builder.HasKey(r => new { r.ProductShortCode, r.SubProductId });
            builder.Property(r => r.ProductShortCode).IsRequired().HasMaxLength(20);
            builder.Property(r => r.SubProductId).IsRequired().HasMaxLength(SqlTypeConstants.DefaultVarcharLength);
            builder.HasOne(r => r.Report)
                .WithMany()
                .IsRequired();
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Reports;

public class ReportColourSettingsConfiguration : IEntityTypeConfiguration<ReportColourSettings>
{
    public void Configure(EntityTypeBuilder<ReportColourSettings> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.OrganisationId });
    }
}
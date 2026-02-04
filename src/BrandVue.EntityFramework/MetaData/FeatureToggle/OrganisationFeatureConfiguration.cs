using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.FeatureToggle;

public class OrganisationFeaturesConfiguration : IEntityTypeConfiguration<OrganisationFeature>
{
    public void Configure(EntityTypeBuilder<OrganisationFeature> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.FeatureId, m.OrganisationId });
    }
}
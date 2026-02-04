using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.FeatureToggle;

public class UserFeaturesConfiguration : IEntityTypeConfiguration<UserFeature>
{
    public void Configure(EntityTypeBuilder<UserFeature> builder)
    {
        builder.HasKey(m => m.Id);
        builder.HasIndex(m => new { m.FeatureId, m.UserId });
    }
}
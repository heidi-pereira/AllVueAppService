using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions
{
    public class PermissionFeature
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SystemKey SystemKey { get; set; }

        public ICollection<PermissionOption> Options { get; set; } = new List<PermissionOption>();
    }

    public class PermissionFeatureConfiguration : IEntityTypeConfiguration<PermissionFeature>
    {
        public void Configure(EntityTypeBuilder<PermissionFeature> builder)
        {
            builder.HasKey(f => f.Id);

            builder.ToTable("PermissionFeatures", nameof(SqlPermissionsSchema.UserFeaturePermissions));

            builder
                .Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.MaxPermissionNameLength);

            builder
                .Property(f => f.SystemKey)
                .IsRequired()
                .HasConversion<int>(); // Ensure SystemKey is stored as an integer

            builder
                .HasIndex(p => new { p.Name, p.SystemKey })
                .IsUnique();
        }
    }
}
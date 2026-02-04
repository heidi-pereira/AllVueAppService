using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions
{
    public class PermissionOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int FeatureId { get; set; }

        public PermissionFeature Feature { get; set; } = null!;
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }

    public class PermissionOptionConfiguration : IEntityTypeConfiguration<PermissionOption>
    {
        public void Configure(EntityTypeBuilder<PermissionOption> builder)
        {
            builder.HasKey(p => p.Id);

            builder.ToTable("PermissionOptions", nameof(SqlPermissionsSchema.UserFeaturePermissions));

            builder
                .Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.MaxPermissionNameLength);

            builder
                .HasOne(p => p.Feature)
                .WithMany(f => f.Options)
                .HasForeignKey(p => p.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
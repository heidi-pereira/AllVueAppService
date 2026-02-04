using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions
{
    public class Role
    {
        public int Id { get; set; }

        public string RoleName { get; set; }

        public ICollection<PermissionOption> Options { get; set; } = new List<PermissionOption>();

        public string OrganisationId { get; set; }

        public string UpdatedByUserId { get; set; }

        public DateTime UpdatedDate { get; set; }
    }

    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);

            builder.ToTable("Roles", nameof(SqlPermissionsSchema.UserFeaturePermissions));

            builder.ToTable(tb =>
                tb.IsTemporal(x =>
                    {
                        x.HasPeriodStart("SysStartTime");
                        x.HasPeriodEnd("SysEndTime");
                    }
                )
            ).Property("UpdatedDate").HasComputedColumnSql("SysStartTime");

            builder
                .Property(r => r.RoleName)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.MaxPermissionNameLength);

            builder
                .Property(r => r.OrganisationId)
                .HasMaxLength(SqlLengthConstants.AuthOrganisationLength)
                .IsRequired();

            builder
                .Property(r => r.UpdatedByUserId)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthUserIdLength);

            builder
                .Property(r => r.UpdatedDate)
                .IsRequired();

            builder
                .HasMany(r => r.Options)
                .WithMany(p => p.Roles)
                .UsingEntity(j => j.ToTable("RolePermissionOption", nameof(SqlPermissionsSchema.UserFeaturePermissions)));
        }
    }
}

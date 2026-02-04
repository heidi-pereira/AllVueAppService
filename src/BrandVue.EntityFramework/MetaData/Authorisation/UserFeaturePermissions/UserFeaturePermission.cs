using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserFeaturePermissions
{
    public class UserFeaturePermission
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int UserRoleId { get; set; }
        public Role UserRole { get; set; }
        public string UpdatedByUserId { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class UserFeaturePermissionConfiguration : IEntityTypeConfiguration<UserFeaturePermission>
    {
        public void Configure(EntityTypeBuilder<UserFeaturePermission> builder)
        {
            builder.HasKey(p => p.Id);

            builder.ToTable("UserFeaturePermissions", nameof(SqlPermissionsSchema.UserFeaturePermissions));

            builder.ToTable(tb => tb
                .IsTemporal(x =>
                {
                    x.HasPeriodStart("SysStartTime");
                    x.HasPeriodEnd("SysEndTime");
                }));

            builder
                .Property(p => p.UserId)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthUserIdLength);

            builder
                .Property(p => p.UpdatedByUserId)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthUserIdLength);

            builder
                .Property(p => p.UpdatedDate)
                .IsRequired()
                .HasComputedColumnSql("SysStartTime");

            builder
                .HasOne(p => p.UserRole)
                .WithMany()
                .HasForeignKey(p => p.UserRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.UserId, p.UserRoleId }).IsUnique();
        }
    }
}

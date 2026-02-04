using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions
{
    public class UserDataPermission
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public int RuleId { get; set; }

        public BaseRule Rule { get; set; }

        public string UpdatedByUserId { get; set; }

        public DateTime UpdatedDate { get; set; }

    }

    public class UserDataPermissionConfiguration : IEntityTypeConfiguration<UserDataPermission>
    {
        public void Configure(EntityTypeBuilder<UserDataPermission> builder)
        {
            builder.ToTable("UserDataPermissions", nameof(SqlPermissionsSchema.UserDataPermissions));

            // Configure temporal table
            builder.ToTable(tb =>
                tb.IsTemporal(x =>
                {
                    x.HasPeriodStart("SysStartTime");
                    x.HasPeriodEnd("SysEndTime");
                })
            );

            builder.HasKey(udp => udp.Id);
            builder.Property(udp => udp.UserId)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthUserIdLength);
            builder.Property(udp => udp.RuleId)
                .IsRequired();
            builder.Property(udp => udp.UpdatedByUserId)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthUserIdLength);

            // UpdatedDate configuration
            builder.Property<DateTime>("UpdatedDate")
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime2")
                .HasComputedColumnSql("SysStartTime");

            // Configure relationships
            builder.HasOne(udp => udp.Rule)
                .WithMany()
                .HasForeignKey(udp => udp.RuleId);

            // Configure unique index
            builder.HasIndex(udp => new { udp.UserId, udp.RuleId }).IsUnique();
        }
    }
}

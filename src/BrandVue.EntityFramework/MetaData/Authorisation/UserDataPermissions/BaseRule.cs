using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions
{
    public abstract class BaseRule // for reusability in other systems
    {
        public int Id { get; set; }

        public string RuleName { get; set; }

        public string UpdatedByUserId { get; set; }

        public DateTime UpdatedDate { get; set; }

        // this is the system that the rule is for, so we can have different rules for different systems. Also helpful for filtering data
        public SystemKey SystemKey { get; set; }
    }

    public class BaseRuleConfiguration : IEntityTypeConfiguration<BaseRule>
    {
        public void Configure(EntityTypeBuilder<BaseRule> builder)
        {
            builder.ToTable("BaseRules", nameof(SqlPermissionsSchema.UserDataPermissions));
            builder.UseTptMappingStrategy();
            builder.HasKey(b => b.Id);
            builder.Property(b => b.RuleName)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.MaxPermissionNameLength);
            builder.Property(b => b.UpdatedByUserId)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthUserIdLength);
            builder.Property(b => b.UpdatedDate)
                .IsRequired();
            builder.Property(b => b.SystemKey)
                .IsRequired();
        }
    }
}

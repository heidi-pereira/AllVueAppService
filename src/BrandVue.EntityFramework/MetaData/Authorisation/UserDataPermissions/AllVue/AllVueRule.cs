using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue
{
    public class AllVueRule : BaseRule // depending on different systems we can have different rules
    {
        public bool AllUserAccessForSubProduct { get; set; }
        public string Organisation { get; set; }

        public ProjectType ProjectType { get; set; }
        public int ProjectOrProductId { get; set; }

        public ICollection<int> AvailableVariableIds { get; set; } = new List<int>();

        public ICollection<AllVueFilter> Filters { get; set; } = new List<AllVueFilter>();
    }

    public class AllVueRuleConfiguration : IEntityTypeConfiguration<AllVueRule>
    {
        public void Configure(EntityTypeBuilder<AllVueRule> builder)
        {
            builder.ToTable("AllVueRules", nameof(SqlPermissionsSchema.UserDataPermissions));

            builder.Property(r => r.Organisation)
                .IsRequired()
                .HasMaxLength(SqlLengthConstants.AuthOrganisationLength);

            builder.Property(r => r.AvailableVariableIds)
                .IsRequired()
                .HasJsonConversion();

            builder.Property(r => r.AllUserAccessForSubProduct).IsRequired().HasDefaultValue(false);

            builder.HasIndex(r => new { r.Organisation, r.ProjectType, r.ProjectOrProductId, r.AllUserAccessForSubProduct })
                .IsUnique()
                .HasFilter($"[{nameof(AllVueRule.AllUserAccessForSubProduct)}] = 1");

            // Configure relationships
            builder.HasMany(r => r.Filters)
                .WithOne(f => f.AllVueRule)
                .HasForeignKey(f => f.AllVueRuleId);
        }
    }
}

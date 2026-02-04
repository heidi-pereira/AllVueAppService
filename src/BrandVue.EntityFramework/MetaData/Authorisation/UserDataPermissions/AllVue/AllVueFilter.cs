using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Authorisation.UserDataPermissions.AllVue
{
    public class AllVueFilter
    {
        public int Id { get; set; }

        public int AllVueRuleId { get; set; }

        public AllVueRule AllVueRule { get; set; }

        public int VariableConfigurationId { get; set; } // this is specific to the AllVue system and we create a separate table for this and no FK for a cross-system join

        public int EntitySetId { get; set; } // again, no FK to another DB, just saving Id

        public int[] EntityIds { get; set; }
    }

    public class AllVueFilterConfiguration : IEntityTypeConfiguration<AllVueFilter>
    {
        public void Configure(EntityTypeBuilder<AllVueFilter> builder)
        {
            builder.ToTable("AllVueFilters", nameof(SqlPermissionsSchema.UserDataPermissions));

            builder.HasKey(f => f.Id);
            builder.Property(f => f.AllVueRuleId)
                .IsRequired();
            builder.Property(f => f.VariableConfigurationId)
                .IsRequired();
            builder.Property(f => f.EntitySetId)
                .IsRequired();
            builder.Property(f => f.EntityIds)
                .IsRequired()
                .HasJsonConversion();

            // Configure relationships
            builder.HasOne(f => f.AllVueRule)
                .WithMany(r => r.Filters)
                .HasForeignKey(f => f.AllVueRuleId);
        }
    }
}

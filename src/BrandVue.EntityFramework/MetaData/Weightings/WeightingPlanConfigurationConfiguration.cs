using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    public class WeightingPlanConfigurationConfiguration : IEntityTypeConfiguration<WeightingPlanConfiguration>
    {
        public void Configure(EntityTypeBuilder<WeightingPlanConfiguration> builder)
        {
            builder.HasKey(ws => ws.Id);
        }
    }
}
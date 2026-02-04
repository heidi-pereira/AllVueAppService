using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.Measures
{
    internal static class DbSetOfMetricConfigurationExtensions
    {
        public static IQueryable<MetricConfiguration> ForProductContext(this DbSet<MetricConfiguration> metricConfigurations, IProductContext productContext)
        {
            return ForProductContext(metricConfigurations
                .Include(x => x.VariableConfiguration)
                .Include(x => x.BaseVariableConfiguration), productContext);
        }

        public static IQueryable<MetricConfiguration> ForProductContext(this IQueryable<MetricConfiguration> configurations, IProductContext productContext) =>
            configurations.Where(mc => mc.ProductShortCode == productContext.ShortCode && mc.SubProductId == productContext.SubProductId);
    }
}
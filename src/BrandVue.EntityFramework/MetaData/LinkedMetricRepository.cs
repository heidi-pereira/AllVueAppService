using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.MetaData
{
    public class LinkedMetricRepository : ILinkedMetricRepository
    {
        private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
        private readonly IProductContext _productContext;

        public LinkedMetricRepository(IDbContextFactory<MetaDataContext> dbContextFactory, IProductContext productContext)
        {
            _dbContextFactory = dbContextFactory;
            _productContext = productContext;
        }

        public async Task<LinkedMetric> GetLinkedMetricsForMetric(string metricName)
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            LinkedMetric currentMetric;
            if (string.IsNullOrWhiteSpace(_productContext.SubProductId))
            {
                currentMetric = await dbContext.LinkedMetrics.FirstOrDefaultAsync(lm => lm.ProductShortCode == _productContext.ShortCode 
                    && lm.MetricName.ToLower() == metricName.ToLower() && lm.SubProductId == null);
            }
            else
            {
                currentMetric = await dbContext.LinkedMetrics.FirstOrDefaultAsync(lm => lm.ProductShortCode == _productContext.ShortCode &&
                    lm.SubProductId == _productContext.SubProductId && lm.MetricName.ToLower() == metricName.ToLower());
            }

            return currentMetric;
        }
    }
}
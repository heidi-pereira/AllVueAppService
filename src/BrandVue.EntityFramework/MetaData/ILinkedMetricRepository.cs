using System.Threading.Tasks;

namespace BrandVue.EntityFramework.MetaData
{
    public interface ILinkedMetricRepository
    {
        public Task<LinkedMetric> GetLinkedMetricsForMetric(string metricName);
    }
}
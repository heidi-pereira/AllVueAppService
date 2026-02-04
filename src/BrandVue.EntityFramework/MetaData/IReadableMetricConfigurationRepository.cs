namespace BrandVue.EntityFramework.MetaData
{
    public interface IReadableMetricConfigurationRepository
    {
        IReadOnlyCollection<MetricConfiguration> GetAll();
        MetricConfiguration Get(int id);
        MetricConfiguration Get(string name);
    }
}
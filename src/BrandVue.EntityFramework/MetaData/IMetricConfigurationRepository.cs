namespace BrandVue.EntityFramework.MetaData
{
    public interface IMetricConfigurationRepository : IReadableMetricConfigurationRepository
    {
        void Create(MetricConfiguration metricConfiguration, bool shouldValidate=true);
        void Update(MetricConfiguration metricConfiguration, bool shouldValidate=true);
        void Delete(MetricConfiguration metricConfiguration);
        void Delete(int metricConfigurationId);
    }
}

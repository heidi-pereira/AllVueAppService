using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Measures
{
    public interface IMetricFactory
    {
        /// <summary>
        /// Creates a Measure instance based on a configuration object.
        /// If some data is invalid, it throws an exception explaining the reason for failure.
        /// </summary>
        Measure CreateMetric(MetricConfiguration metricConfiguration);

        /// <summary>
        /// Loads all fields from metric configuration.
        /// Use this if you already have an instance of a measure.
        /// </summary>
        void LoadMetric(MetricConfiguration metricConfiguration, Measure metric);

        /// <summary>
        /// Tries to create a Measure instance based on a configuration object.
        /// If successful, returns true and the created measure object.
        /// If not, returns false and a failure message.
        /// </summary>
        bool TryCreateMetric(MetricConfiguration metricConfiguration, out Measure metric, out string failureMessage);
    }
}

using BrandVue.EntityFramework.MetaData;

namespace BrandVue.SourceData.Variable;

public record CreateVariableResultModel
{
    public string UrlSafeMetricName { get; init; }
    public MetricConfiguration Metric { get; init; }
}

using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class MetricModalDataModel
    {
        public string MetricName { get; set; }
        public string DisplayName { get; set; }
        public string DisplayText { get; set; }
        [CanBeNull]
        public string EntityInstanceIdMeanCalculationValueMapping { get; set; }
    }
}

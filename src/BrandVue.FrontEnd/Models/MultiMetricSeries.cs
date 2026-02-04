using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;

namespace BrandVue.Models
{
    public class MultiMetricSeries
    {
        public EntityInstance EntityInstance { get; set; }
        public WeightedDailyResult[][] OrderedData { get; set; }
    }
}
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;

namespace BrandVue.PublicApi.Models
{
    public class MetricCalculationResult
    {
        public TargetInstances TargetInstances { get; }
        public TargetInstances[] FilterInstances { get; }
        public EntityWeightedDailyResults[] EntityWeightedDailyResults { get; }

        public MetricCalculationResult(TargetInstances targetInstances, TargetInstances[] filterInstances, EntityWeightedDailyResults[] entityWeightedDailyResults)
        {
            TargetInstances = targetInstances;
            FilterInstances = filterInstances;
            EntityWeightedDailyResults = entityWeightedDailyResults;
        }
    }
}

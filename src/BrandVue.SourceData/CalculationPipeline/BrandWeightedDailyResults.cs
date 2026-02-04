using BrandVue.SourceData.Calculation;
using Newtonsoft.Json;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class EntityWeightedDailyResults
    {
        
        public EntityInstance EntityInstance { get; }
        public IList<WeightedDailyResult> WeightedDailyResults { get; }
        public uint UnweightedResponseCount { get; }
        public double WeightedResponseCount { get; }

        [JsonConstructor]
        public EntityWeightedDailyResults(EntityInstance entityInstance, IList<WeightedDailyResult> weightedDailyResults)
        {
            EntityInstance = entityInstance;
            WeightedDailyResults = weightedDailyResults;
        }

        public EntityWeightedDailyResults(EntityInstance entityInstance, IList<WeightedDailyResult> weightedDailyResults, uint unweightedResponseCount, double weightedResponseCount)
        {
            EntityInstance = entityInstance;
            WeightedDailyResults = weightedDailyResults;
            UnweightedResponseCount = unweightedResponseCount;
            WeightedResponseCount = weightedResponseCount;
        }
    }
}
using BrandVue.EntityFramework.MetaData.Weightings;

namespace BrandVue.EntityFramework.MetaData
{
    public sealed record TargetInstance(string FilterMetricName, int? FilterInstanceId);

    public interface IResponseWeightingRepository
    {
        public bool AreThereAnyRootResponseWeights(string subsetId);
        public ResponseWeightingContext GetRootResponseWeightingContextWithWeightsForSubset(string subsetId);
        public bool CreateResponseWeightsForRoot(string subsetId, IList<ResponseWeightConfiguration> weights);

        public int CreateResponseWeights(string subsetId,
            IReadOnlyCollection<WeightingPlanConfiguration> plans,
            IList<TargetInstance> pathOfTargetInstances,
            IEnumerable<ResponseWeightConfiguration> weights);

        public void DeleteResponseWeights(string subsetId);
        public void DeleteResponseWeightsForTarget(string subsetId, int weightingTargetId);
    }
}

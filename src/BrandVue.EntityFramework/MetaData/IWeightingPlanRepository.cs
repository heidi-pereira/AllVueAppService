using BrandVue.EntityFramework.MetaData.Weightings;

namespace BrandVue.EntityFramework.MetaData
{
    public interface IWeightingPlanRepository
    {
        IReadOnlyCollection<WeightingPlanConfiguration> GetLoaderWeightingPlansForSubset(string product, string subProductIdOrNull, string subsetId);
        IReadOnlyCollection<WeightingPlanConfiguration> GetWeightingPlans(string product, string subProductIdOrNull);
        IReadOnlyCollection<(string subsetId,IReadOnlyCollection<WeightingPlanConfiguration> plans) > GetWeightingPlansBySubsetId(string product, string subProductIdOrNull);
        IReadOnlyCollection<WeightingPlanConfiguration> GetWeightingPlansForSubset(string product, string subProductIdOrNull, string subsetId);
        void CreateWeightingPlan(string product, string subProductIdOrNull, WeightingPlanConfiguration weightingPlanConfiguration);
        void UpdateAllWeightingPlans(string product, string subProductIdOrNull, IList<WeightingPlanConfiguration> newWeightingPlans);
        void UpdateWeightingPlan(string product, string subProductIdOrNull, WeightingPlanConfiguration weightingPlanConfiguration);
        void DeleteWeightingPlan(string product, string subProductIdOrNull, int planId);
        void DeleteWeightingChildPlansForTarget(string subset, string product, string subProductIdOrNull, int targetId);
        void DeleteWeightingPlanForSubset(string product, string subProductIdOrNull, string subsetId);
        void DeleteWeightingTarget(string subsetId, string product, string subProductIdOrNull, int targetId);
        void UpdateWeightingPlanForSubset(string shortCode, string subProductId, string subsetId, IReadOnlyCollection<WeightingPlanConfiguration> dbPlans);
    }
}
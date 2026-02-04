using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.EntityFramework;

namespace BrandVue.Models
{
    public sealed record UiWeightingConfigurationRoot(string SubsetId) : ISubsetIdProvider
    {
        public List<UiWeightingPlanConfiguration> UiWeightingPlans { get; init; } = new List<UiWeightingPlanConfiguration>();
        public IList<WeightingPlanConfiguration> ToWeightingPlanConfiguration(string productShortCode, string subProductId)
        {
            var result = new List<WeightingPlanConfiguration>();
            foreach (var item in UiWeightingPlans)
            {
                result.Add(item.ToWeightingPlanConfiguration(productShortCode, subProductId, this));
            }
            return result;
        }
        public bool Equals(UiWeightingConfigurationRoot? other) => SubsetId == other?.SubsetId && UiWeightingPlans.Count == (other?.UiWeightingPlans.Count ?? 0);


        // Optional: warning generated if not supplied when Equals(R?) is user-defined.
        public override int GetHashCode() => base.GetHashCode();

        public static IList<UiWeightingConfigurationRoot> ToUIWeightingRoots(IReadOnlyCollection<WeightingPlanConfiguration> plans)
        {
            var weightPerSubset = new Dictionary<string, UiWeightingConfigurationRoot>();
            foreach (var plan in plans)
            {
                if (plan.ParentTarget == null)
                {
                    if (!weightPerSubset.ContainsKey(plan.SubsetId))
                    {
                        weightPerSubset[plan.SubsetId] = new UiWeightingConfigurationRoot(plan.SubsetId);
                    }
                    weightPerSubset[plan.SubsetId].UiWeightingPlans.Add(UiWeightingPlanConfiguration.ToUIWeightingPlan(plan));
                }
            }

            return weightPerSubset.Values.ToList();
        }

        internal UiWeightingConfigurationRoot CloneTreeFor(string subsetId)
        {
            var result = new UiWeightingConfigurationRoot(subsetId);
            foreach (var item in UiWeightingPlans)
            {
                result.UiWeightingPlans.Add(item.CloneTree());
            }

            return result;
        }
    }

    public record UiWeightingPlanConfiguration
    {
        public string VariableIdentifier { get; set; } // Never null - potentially multiple plans can come back with no parent
        public int Id { get; init; }
        public bool IsWeightingGroupRoot { get; set; }

        public List<UiWeightingTargetConfiguration> UiChildTargets { get; init; } = new List<UiWeightingTargetConfiguration>();

        public WeightingPlanConfiguration ToWeightingPlanConfiguration(string productShortCode, string subProductId, UiWeightingConfigurationRoot root, WeightingTargetConfiguration parent = null)
        {
            var result = new WeightingPlanConfiguration
            {
                Id = Id,
                VariableIdentifier = VariableIdentifier,
                IsWeightingGroupRoot = IsWeightingGroupRoot,
                ParentTarget = parent,
                ParentWeightingTargetId = parent?.Id ?? null,
                ProductShortCode = productShortCode,
                SubProductId = subProductId,
                SubsetId = root.SubsetId,
            };
            if (UiChildTargets != null && UiChildTargets.Count > 0)
            {
                result.ChildTargets = new List<WeightingTargetConfiguration>();
                foreach (var child in UiChildTargets)
                {
                    result.ChildTargets.Add(child.ToWeightingTargetConfiguration(productShortCode, subProductId,root, result));
                }
            }
            return result;
        }

        public static UiWeightingPlanConfiguration ToUIWeightingPlan(WeightingPlanConfiguration plan, UiWeightingTargetConfiguration parent = null)
        {
            var weightingPlan = new UiWeightingPlanConfiguration
            {
                Id = plan.Id,
                VariableIdentifier = plan.VariableIdentifier,
                IsWeightingGroupRoot = plan.IsWeightingGroupRoot,
            };
            if (plan.ChildTargets != null)
            {
                foreach (var child in plan.ChildTargets.OrderBy(t => t.EntityInstanceId))
                {
                    weightingPlan.UiChildTargets.Add(UiWeightingTargetConfiguration.ToUIWeightingTarget(child, weightingPlan));
                }
            }
            return weightingPlan;
        }

        internal UiWeightingPlanConfiguration CloneTree()
        {
            var result = new UiWeightingPlanConfiguration();
            result.VariableIdentifier = VariableIdentifier;
            result.IsWeightingGroupRoot = IsWeightingGroupRoot;
            foreach (var item in this.UiChildTargets)
            {
                result.UiChildTargets.Add(item.CloneTree());
            }
            return result;
        }
    }
    public record UiWeightingTargetConfiguration
    {
        public int Id { get; init; }
        public int EntityInstanceId { get; set; }
        public decimal? Target { get; set; }
        public int? TargetPopulation { get; set; }
        public List<UiWeightingPlanConfiguration> UiChildPlans { get; init; } = new List<UiWeightingPlanConfiguration>();
        public int? ResponseWeightingContextId { get; set; }

        public static UiWeightingTargetConfiguration ToUIWeightingTarget(WeightingTargetConfiguration child, UiWeightingPlanConfiguration parent)
        {
            var weightingTarget = new UiWeightingTargetConfiguration
            {
                Id = child.Id,
                EntityInstanceId = child.EntityInstanceId,
                Target = child.Target,
                TargetPopulation = child.TargetPopulation,
                ResponseWeightingContextId = child.ResponseWeightingContext?.Id,
            };
            if (child.ChildPlans != null)
            {
                foreach (var plan in child.ChildPlans.OrderBy(p => p.VariableIdentifier))
                {
                    weightingTarget.UiChildPlans.Add(UiWeightingPlanConfiguration.ToUIWeightingPlan(plan, weightingTarget));
                }
            }
            return weightingTarget;
        }

        internal UiWeightingTargetConfiguration CloneTree()
        {
            var result = new UiWeightingTargetConfiguration();
            result.EntityInstanceId = EntityInstanceId;
            result.Target = Target;
            result.TargetPopulation = TargetPopulation;
            foreach (var plan in UiChildPlans)
            {
                result.UiChildPlans.Add(plan.CloneTree());
            }
            return result;
        }

        internal WeightingTargetConfiguration ToWeightingTargetConfiguration(string productShortCode, string subProductId, UiWeightingConfigurationRoot root, WeightingPlanConfiguration parent)
        {
            var result = new WeightingTargetConfiguration()
            {
                Id = Id,
                EntityInstanceId = EntityInstanceId,
                Target = Target,
                TargetPopulation = TargetPopulation,
                ParentWeightingPlan = parent,
                ParentWeightingPlanId = parent.Id,
                SubProductId = subProductId,
                SubsetId = root.SubsetId,
                ProductShortCode = productShortCode,
            };
            if (UiChildPlans != null && UiChildPlans.Count > 0)
            {
                result.ChildPlans = new List<WeightingPlanConfiguration>();
                foreach (var child in UiChildPlans)
                {
                    result.ChildPlans.Add(child.ToWeightingPlanConfiguration(productShortCode, subProductId, root, result));
                }
            }
            return result;
        }
    }
}

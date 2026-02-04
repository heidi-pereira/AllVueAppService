using BrandVue.EntityFramework.MetaData.Weightings;

namespace BrandVue.SourceData.Weightings;

public static class WeightingDatabaseModelsExtensions
{
    public static IEnumerable<WeightingPlan> ToAppModel(this IEnumerable<WeightingPlanConfiguration> weightingPlanConfigurations, IResponseLevelQuotaCellLoader loader = null) => 
        new DbToAppModelWeightingPlanAdapter().ToWeightingPlans(weightingPlanConfigurations.ToList(), null, loader);

    public static IEnumerable<WeightingPlanConfiguration> FromAppModel(this IEnumerable<WeightingPlan> weightingPlans, string shortCode, string subProduct, string subsetId) =>
    new DbFromAppModelWeightingPlanAdapter().ToWeightingPlans(weightingPlans.ToList(), shortCode, subProduct, subsetId);


    public static List<WeightingPlanConfiguration> FlattenAndClonePlans(List<WeightingPlanConfiguration> plans)
    {
        var clonedPlans = new List<WeightingPlanConfiguration>();
        FlaternPlans(plans.First().ParentTarget, clonedPlans, plans);
        return clonedPlans;
    }

    private static void FlaternPlans(WeightingTargetConfiguration parentWeightingTarget, List<WeightingPlanConfiguration> clonedPlans, List<WeightingPlanConfiguration> plans)
    {
        if (plans != null)
        {
            foreach (var originalPlan in plans)
            {
                var similarPlan = clonedPlans.SingleOrDefault(x => x.VariableIdentifier == originalPlan.VariableIdentifier);
                if (similarPlan == null)
                {
                    var newPlan = new WeightingPlanConfiguration
                    {
                        Id = 0,
                        VariableIdentifier = originalPlan.VariableIdentifier,
                        ParentWeightingTargetId = parentWeightingTarget.Id,
                        IsWeightingGroupRoot = originalPlan.IsWeightingGroupRoot,
                        ParentTarget = parentWeightingTarget,
                        ChildTargets = null,
                        ProductShortCode = originalPlan.ProductShortCode,
                        SubProductId = originalPlan.SubProductId,
                        SubsetId = originalPlan.SubsetId
                    };
                    newPlan.ChildTargets = FlattenTargets(newPlan, originalPlan.ChildTargets);
                    clonedPlans.Add(newPlan);
                }
                foreach (var item in originalPlan.ChildTargets)
                {
                    FlaternPlans(parentWeightingTarget, clonedPlans, item.ChildPlans);
                }
            }
        }
    }

    private static List<WeightingTargetConfiguration> FlattenTargets(WeightingPlanConfiguration parent, List<WeightingTargetConfiguration> childTargets)
    {
        var clonedTargets = new List<WeightingTargetConfiguration>();
        decimal? target = 1.0m;
        foreach (var originalTarget in childTargets)
        {
            var newTarget = new WeightingTargetConfiguration
            {
                Id = 0,
                EntityInstanceId = originalTarget.EntityInstanceId,
                Target = target,
                TargetPopulation = null,
                ParentWeightingPlanId = parent.Id,
                ParentWeightingPlan = parent,
                ChildPlans = null,
                ProductShortCode = originalTarget.ProductShortCode,
                SubProductId = originalTarget.SubProductId,
                SubsetId = originalTarget.SubsetId
            };
            target = null;
            clonedTargets.Add(newTarget);
        }
        return clonedTargets;
    }

    public static List<WeightingPlanConfiguration> ClonePlans(List<WeightingPlanConfiguration> plans)
    {
        var clonedPlans = new List<WeightingPlanConfiguration>();

        foreach (var originalPlan in plans)
        {
            var newPlan = new WeightingPlanConfiguration
            {
                Id = 0,
                VariableIdentifier = originalPlan.VariableIdentifier,
                ParentWeightingTargetId = originalPlan.ParentWeightingTargetId,
                IsWeightingGroupRoot = originalPlan.IsWeightingGroupRoot,
                ParentTarget = originalPlan.ParentTarget,
                ChildTargets = originalPlan.ChildTargets is not null ? CloneTargets(originalPlan.ChildTargets) : null,
                ProductShortCode = originalPlan.ProductShortCode,
                SubProductId = originalPlan.SubProductId,
                SubsetId = originalPlan.SubsetId
            };

            clonedPlans.Add(newPlan);
        }
        return clonedPlans;
    }

    public static List<WeightingTargetConfiguration> CloneTargets(List<WeightingTargetConfiguration> targets)
    {
        var clonedTargets = new List<WeightingTargetConfiguration>();

        foreach (var originalTarget in targets)
        {
            var newTarget = new WeightingTargetConfiguration
            {
                Id = 0,
                EntityInstanceId = originalTarget.EntityInstanceId,
                Target = originalTarget.Target,
                TargetPopulation = originalTarget.TargetPopulation,
                ParentWeightingPlanId = originalTarget.ParentWeightingPlanId,
                ParentWeightingPlan = originalTarget.ParentWeightingPlan,
                ChildPlans = originalTarget.ChildPlans is not null ? ClonePlans(originalTarget.ChildPlans) : null,
                ProductShortCode = originalTarget.ProductShortCode,
                SubProductId = originalTarget.SubProductId,
                SubsetId = originalTarget.SubsetId
            };

            clonedTargets.Add(newTarget);
        }
        return clonedTargets;
    }

    public static WeightingPlanConfiguration GetTargetParentPlan(IList<WeightingPlanConfiguration> plans, int targetId)
    {
        foreach (var plan in plans)
        {
            if (plan.ChildTargets is not null && plan.ChildTargets.Count > 0)
            {
                if (plan.ChildTargets.Select(t => t.Id).Contains(targetId))
                {
                    return plan;
                }

                foreach (var target in plan.ChildTargets)
                {
                    if (target.ChildPlans is not null && target.ChildPlans.Count > 0)
                    {
                        var foundPlan = GetTargetParentPlan(target.ChildPlans, targetId);
                        if (foundPlan != null)
                        {
                            return foundPlan;
                        }
                    }
                }
            }
        }
        return null;
    }

    private class DbToAppModelWeightingPlanAdapter
    {
        private int _weightingGroupId;
        public IEnumerable<WeightingPlan> ToWeightingPlans(IReadOnlyCollection<WeightingPlanConfiguration> weightingPlanConfigurations, int? groupId, IResponseLevelQuotaCellLoader loader)
        {
            var rootLevelPlanOrNoPlan = weightingPlanConfigurations?.FirstOrDefault()?.ParentTarget == null;
            if (rootLevelPlanOrNoPlan)
            {
                var responseWeightingForRoot = loader?.GetPossibleRootResponseWeightingsForSubset();
                if (responseWeightingForRoot != null)
                {
                    var result = new List<WeightingPlan>();
                    var targets = responseWeightingForRoot.QuotaCellIdToWeight.Select(kvp => new WeightingTarget(null, kvp.Key, kvp.Value, null, null, null)).ToList();
                    result.Add(new WeightingPlan(responseWeightingForRoot.FieldName, targets, false, null, responseWeightingForRoot));
                    return result;
                }
            }

            if (weightingPlanConfigurations?.Count > 1)
            {
                groupId ??= ++_weightingGroupId;
            }
            return weightingPlanConfigurations?.Select(w => ToWeightingPlan(w, groupId, loader));
        }

        private WeightingPlan ToWeightingPlan(WeightingPlanConfiguration weightingPlanConfiguration, int? groupId, IResponseLevelQuotaCellLoader loader)
        {
            return new (weightingPlanConfiguration.VariableIdentifier,
                weightingPlanConfiguration.ChildTargets?.Select(t =>
                {
                    var childGroupId = groupId;
                    if (weightingPlanConfiguration.IsWeightingGroupRoot)
                    {
                        childGroupId ??= ++_weightingGroupId;
                    }
                    return ToWeightingTarget(t, childGroupId, loader);
                }).ToList(),
                weightingPlanConfiguration.IsWeightingGroupRoot, weightingPlanConfiguration.Id);
        }

        private WeightingTarget ToWeightingTarget(WeightingTargetConfiguration weightingTargetConfiguration, int? groupId, IResponseLevelQuotaCellLoader loader)
        {
            if (weightingTargetConfiguration.ParentWeightingPlan?.VariableIdentifier is not null && weightingTargetConfiguration.EntityInstanceId > 0)
            {
                var targetResponseWeighting = loader?.GetPossibleResponseWeightings(weightingTargetConfiguration);
                if (targetResponseWeighting != null)
                {
                    var childPlans = new List<WeightingPlan>();
                    var targets = targetResponseWeighting.QuotaCellIdToWeight.Select(kvp => new WeightingTarget(null, kvp.Key, kvp.Value, null, null, null)).ToList();
                    childPlans.Add(new WeightingPlan(targetResponseWeighting.FieldName, targets, false, null, targetResponseWeighting));

                    return new WeightingTarget(childPlans,
                        weightingTargetConfiguration.EntityInstanceId,
                        weightingTargetConfiguration.Target,
                        weightingTargetConfiguration.TargetPopulation,
                        childPlans?.Any() == true ? null : groupId,
                        weightingTargetConfiguration.Id,
                        weightingTargetConfiguration.ResponseWeightingContext);
                }
            }

            var children = ToWeightingPlans(weightingTargetConfiguration.ChildPlans, groupId, loader)?.ToList();
            return new WeightingTarget(children,
                weightingTargetConfiguration.EntityInstanceId,
                weightingTargetConfiguration.Target,
                weightingTargetConfiguration.TargetPopulation,
                children?.Any() == true ? null : groupId,
                weightingTargetConfiguration.Id,
                weightingTargetConfiguration.ResponseWeightingContext);
        }
    }


    private class DbFromAppModelWeightingPlanAdapter
    {
        public IEnumerable<WeightingPlanConfiguration> ToWeightingPlans(IReadOnlyCollection<WeightingPlan> weightingPlan, string productShortCode, string subProductId, string subsetId)
        {
            return weightingPlan?.Where(x=> x != null).Select(w => ToWeightingPlanConfiguration(null, w, productShortCode, subProductId, subsetId));
        }

        private WeightingPlanConfiguration ToWeightingPlanConfiguration(WeightingTargetConfiguration parentTarget, WeightingPlan weightingPlan, string productShortCode, string subProductId, string subsetId)
        {
            var configuration =  new WeightingPlanConfiguration() 
            {
                VariableIdentifier= weightingPlan.FilterMetricName,
                IsWeightingGroupRoot = weightingPlan.IsWeightingGroupRoot, 
                ParentTarget = parentTarget, 
                ProductShortCode = productShortCode, 
                SubProductId = subProductId, 
                SubsetId = subsetId, 
            };

            configuration.ChildTargets = weightingPlan.Targets.Select(t =>
            {
                return ToWeightingTarget(configuration, t, productShortCode, subProductId, subsetId);
            }).ToList();


            return configuration;
        }

        private WeightingTargetConfiguration ToWeightingTarget(WeightingPlanConfiguration parentPlan, WeightingTarget weightingTargetConfiguration, string productShortCode, string subProductId, string subsetId)
        {
            var target = new WeightingTargetConfiguration()
            {
                EntityInstanceId = weightingTargetConfiguration.FilterMetricEntityId,
                ParentWeightingPlan = parentPlan,
                ProductShortCode = productShortCode,
                SubProductId = subProductId,
                SubsetId = subsetId,
                Target = weightingTargetConfiguration.Target,
                TargetPopulation = weightingTargetConfiguration.TargetPopulation,
            };
            if (weightingTargetConfiguration.Plans != null)
            {
                target.ChildPlans = weightingTargetConfiguration.Plans.Select(p => ToWeightingPlanConfiguration(target, p, productShortCode, subProductId, subsetId)).ToList();
            }
            return target;
        
        }
    }
}


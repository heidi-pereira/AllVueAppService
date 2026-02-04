using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Weightings.Rim
{
    internal static class WeightingPlansExtensions
    {
        public static bool IsPercentageWeighting(this WeightingPlan plan)
        {
            var haveAnyChildren = plan.Targets?.Any(x => x.Plans != null && x.Plans.Any())??false;
            return !haveAnyChildren && plan.Targets?.Sum(x => x.Target.GetValueOrDefault()) == 1.0m;
        }

        public static bool IsExpansionWeightingWithNoChildren(this WeightingPlan plan)
        {
            var haveAnyChildren = plan.Targets?.Any(x => x.Plans != null && x.Plans.Any()) ?? false;
            return !haveAnyChildren && plan.Targets?.Sum(x => x.TargetPopulation.GetValueOrDefault()) > 0;
        }

        internal static bool IsTargetWeighted(this IReadOnlyCollection<WeightingPlan> plans)
        {
            if (plans.Count() == 1)
            {
                if (plans.InterlockedDepth() > 1)
                {
                    return plans.Single().Targets.All(t => (t.Plans == null || TargetWeighted(t.Plans)));
                }
            }
            return false;
        }
        private static bool TargetWeighted(IReadOnlyCollection<WeightingPlan> plans)
        {
            if (plans.Count() == 1)
            {
                return plans.Single().Targets.All(t => (t.Plans == null || TargetWeighted(t.Plans)));
            }
            return false;
        }

        private static bool IsRimWeighted(this WeightingPlan plan)
        {
            if (!string.IsNullOrEmpty(plan.FilterMetricName))
            {
                return plan.Targets.All(target => (target.Plans == null || target.Plans.Count== 0)) && 
                    plan.Targets.Any(target => target.Target.HasValue || target.TargetPopulation.HasValue);
            }
            return false;
        }

        public static bool AreAllPlansRimWeighted(this IReadOnlyCollection<WeightingPlan> plans)
        {
            return plans.All(plan => plan.IsRimWeighted());
        }

        private static int InterlockedDepth(this IReadOnlyCollection<WeightingPlan> newPlans, int depth = 0)
        {
            if (newPlans == null)
            {
                return depth;
            }    
            if (newPlans.Count() != 1)
            {
                return 0;
            }
            var targets = newPlans.First().Targets;
            return targets.Max(target=>InterlockedDepth(target.Plans, depth+1));
        }

        public static bool IsWavePlan(this IReadOnlyCollection<WeightingPlan> weightingPlans)
        {
            return weightingPlans.Count() == 1 && weightingPlans.Any(p => p.IsWeightingGroupRoot);
        }

        

        private static IList<string> InterlockedVariableIdentifiers (this WeightingPlan plan, IList<string> variables)
        {
            variables.Add(plan.FilterMetricName);
            if (plan.Targets != null && plan.Targets.Any())
            {
                var plansToInspect = plan.Targets.First().Plans;
                if (plansToInspect != null)
                {
                    foreach (var childPlan in plansToInspect)
                    {
                        InterlockedVariableIdentifiers(childPlan, variables);
                    }
                }
            }
            return variables;
        }

        private static void GetQuota(WeightingPlan plan, Dictionary<string, decimal> lookup, string[] filterMetricNames, int depth, Stack<string> filterMetricValues)
        {
            if (plan.FilterMetricName == filterMetricNames[depth])
            {
                foreach (var target in plan.Targets)
                {
                    filterMetricValues.Push(target.FilterMetricEntityId.ToString());
                    if (target.Plans != null && target.Plans.Any())
                    {
                        foreach (var targetPlan in target.Plans)
                        {
                            GetQuota(targetPlan, lookup, filterMetricNames, depth + 1, filterMetricValues);
                        }
                    }
                    else
                    {
                        lookup.Add(string.Join(QuotaCell.PartSeparator, filterMetricValues.AsEnumerable().Reverse()), target.Target.Value);
                    }
                    filterMetricValues.Pop();
                }
            }
        }

        public static Dimension PlanToRimWeightedDimension(this WeightingPlan plan)
        {
            return new Dimension()
            {
                InterlockedVariableIdentifiers = new[] { plan.FilterMetricName },
                CellKeyToTarget = plan.Targets.ToDictionary(target => target.FilterMetricEntityId.ToString(), target => target.Target.Value)
            };
        }

        public static Dimension PlanToTargetWeightedDimension(this WeightingPlan plan)
        {
            var variables = new List<string>();
            var interlockedVariables = InterlockedVariableIdentifiers(plan, variables).ToArray();
            var cellKeyToTarget = new Dictionary<string, decimal>();
            GetQuota(plan, cellKeyToTarget, interlockedVariables, 0, new Stack<string>());
            return new Dimension()
            {
                InterlockedVariableIdentifiers = interlockedVariables,
                CellKeyToTarget = cellKeyToTarget
            };
        }
    }
}

using BrandVue.SourceData.Weightings;
using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.SourceData.Weightings
{
    internal static class WeightingPlanExtensionMethod
    {
        /// <summary>
        /// This only works for TargetWeighted scenarios
        /// </summary>
        internal static ReferenceWeightingTestCase GenerateMatchingReferenceWeightingTestCase(this WeightingPlan[] weightingPlansForDatabase, int totalRespondents)
        {
            var descriptors = new Dictionary<string, QuotaCellQuestionAndInstances>();
            var dataDistribution = new List<NumberOfResponsesForQuotaCell>();
            GenerateMatchingTargetDistribution(weightingPlansForDatabase, descriptors, dataDistribution, totalRespondents);
            return new ReferenceWeightingTestCase(descriptors.Values.ToList(), dataDistribution);
        }
        const int MinusOneNumberOfRespondents = 2;
        private static void GenerateMatchingTargetDistribution(WeightingPlan[] weightingPlansForDatabase, Dictionary<string, QuotaCellQuestionAndInstances> descriptors, List<NumberOfResponsesForQuotaCell> dataDistribution, int totalRespondents, int[] quotas = null)
        {
            foreach (var weightingPlan in weightingPlansForDatabase)
            {
                if (!descriptors.ContainsKey(weightingPlan.FilterMetricName))
                {
                    descriptors[weightingPlan.FilterMetricName] = new QuotaCellQuestionAndInstances(weightingPlan.FilterMetricName, new int[] { });
                }
                foreach (var target in weightingPlan.Targets)
                {
                    var myQuota = new List<int>();
                    if (quotas != null)
                    {
                        myQuota.AddRange(quotas);
                    }
                    myQuota.Add(target.FilterMetricEntityId);
                    descriptors[weightingPlan.FilterMetricName].AddInstanceId(target.FilterMetricEntityId);
                    if (target.Plans != null)
                    {
                        GenerateMatchingTargetDistribution(target.Plans.ToArray(), descriptors, dataDistribution, totalRespondents, myQuota.ToArray());
                    }
                    else
                    {
                        var quotaCell = string.Join(":", myQuota);
                        if (target.Target.Value == -1)
                        {
                            dataDistribution.Add(new NumberOfResponsesForQuotaCell(quotaCell, MinusOneNumberOfRespondents));
                        }
                        else
                        {
                            dataDistribution.Add(new NumberOfResponsesForQuotaCell(quotaCell, (int)(target.Target.Value * totalRespondents+0.5m)));
                        }
                    }
                }
            }
        }
    }
}

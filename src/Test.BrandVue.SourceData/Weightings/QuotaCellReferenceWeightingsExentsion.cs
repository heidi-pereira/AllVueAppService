using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Weightings;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Test.BrandVue.SourceData.Weightings
{
    internal static class QuotaCellReferenceWeightingsExentsion
    { 
        internal static void VerifyTargetWeightsModelMatch(this QuotaCellReferenceWeightings results, WeightingPlan[] weightingPlansForDatabase, int[] quotas = null, double errorMargin = 0.0000000001)
        {
            foreach (var weightingPlan in weightingPlansForDatabase)
            {
                foreach (var target in weightingPlan.Targets)
                {
                    var myQuota = new List<int>();
                    if (quotas != null)
                    {
                        myQuota.AddRange(quotas);
                    }
                    myQuota.Add(target.FilterMetricEntityId);
                    if (target.Plans != null)
                    {
                        VerifyTargetWeightsModelMatch(results, target.Plans.ToArray(), myQuota.ToArray(), errorMargin);
                    }
                    else
                    {
                        var quotaCell = string.Join(":", myQuota);
                        var difference = decimal.ToDouble(target.Target.Value);
                        try
                        {
                            var value = results.GetReferenceWeightingFor(quotaCell);
                            if (value.Weight.HasValue)
                            {
                                difference = Math.Abs(value.Weight.Value - (decimal.ToDouble(target.Target.Value)));
                            }
                            else
                            {
                                difference = Math.Abs(-1 - decimal.ToDouble(target.Target.Value));
                            }
                            var message = $"Quota cell {quotaCell} target {decimal.ToDouble(target.Target.Value)} {value}";
                            Assert.That(difference, Is.LessThan(errorMargin), message);
                        }
                        catch (KeyNotFoundException)
                        {
                            Assert.That(difference, Is.LessThan(errorMargin), $"Missing {quotaCell} target {target.Target.Value}");
                        }
                        
                    }
                }
            }
        }
    }
}

using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.Rim;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.QuotaCells
{

    internal class ReferenceWeightingCalculator : IReferenceWeightingCalculator
    {
        private readonly ILogger _logger;

        private static IReadOnlyCollection<(string QuotaCellString, WeightingValue Target)> WeightToZero(IReadOnlyCollection<QuotaCell> indexedQuotaCells) => indexedQuotaCells.Select(q => (q.ToString(), new WeightingValue(0f, false))).ToArray();

        public ReferenceWeightingCalculator(ILogger logger)
        {
            _logger = logger;
        }

        public QuotaCellReferenceWeightings CalculateReferenceWeightings(IProfileResponseAccessor profileResponseAccessor, IGroupedQuotaCells weightedCellsGroup, IReadOnlyCollection<WeightingPlan> weightingPlans)
        {
            var quotaCellToSampleSize = profileResponseAccessor.GetResponses(weightedCellsGroup)
                .ToDictionary(r => r.QuotaCell, r => (double)r.Profiles.Length);
            var targetWeightings = GetTargetsForWaveGroup(weightingPlans, quotaCellToSampleSize, weightedCellsGroup.Cells);
            return new QuotaCellReferenceWeightings(targetWeightings);
        }

        private Dictionary<string, WeightingValue> GetTargetsForWaveGroup(IReadOnlyCollection<WeightingPlan> weightingPlans,
            Dictionary<QuotaCell, double> quotaCellToSampleSize, IReadOnlyCollection<QuotaCell> cells)
        {
            var topLevelNodeParameters = new CurrentNodeParameters(cells, null);
            var quotaCellToTargets =
                GetTargetsForWaveGroup(topLevelNodeParameters, weightingPlans, quotaCellToSampleSize).ToList();
            var targetWeightings = quotaCellToTargets.ToDictionary(qd => qd.CellKey, qd => qd.Target);
            return targetWeightings;
        }

        private record CurrentNodeParameters(IReadOnlyCollection<QuotaCell> ParentCells, int? WeightingGroupId);
        private IEnumerable<(string CellKey, WeightingValue Target)> GetTargetsForWaveGroup(CurrentNodeParameters currentNodeParameters, IReadOnlyCollection<WeightingPlan> plans, IReadOnlyDictionary<QuotaCell, double> quotaCellToSampleSize)
        {
            var quotaCellToSampleSizeForGroup =
                currentNodeParameters.ParentCells.Select(q => (QuotaCell: q, SampleSize: quotaCellToSampleSize[q])).ToList();
            if (HasNoSample(currentNodeParameters.ParentCells, plans, quotaCellToSampleSizeForGroup))
            {
                foreach (var result in WeightToZero(currentNodeParameters.ParentCells)) yield return result;
            }
            else if (plans.OnlyOrDefault() is {} singlePlan)
            {
                var cellsByEntityId = currentNodeParameters.ParentCells.ToLookup(c => int.Parse(c.GetKeyPartForFieldGroup(singlePlan.FilterMetricName)));
                var totalSampleSize = cellsByEntityId.Sum(x => x.Sum(q => quotaCellToSampleSize[q]));

                if (singlePlan.IsPercentageWeighting())
                {
                    foreach (var cellKeyToTarget in RimWeightLeaf(plans, quotaCellToSampleSizeForGroup)) yield return cellKeyToTarget;
                }
                else if (singlePlan.IsExpansionWeightingWithNoChildren())
                {
                    foreach (var target in singlePlan.Targets)
                    {
                        var targetCell = cellsByEntityId[target.FilterMetricEntityId].Single();
                        var sampleSize = quotaCellToSampleSize[targetCell];
                        if (sampleSize < 1)
                        {
                            yield return (targetCell.ToString(), new WeightingValue(0f, false));
                        }
                        else
                        {
                            var unweightedWeight = sampleSize / totalSampleSize;
                            var result = (targetCell.ToString(), new WeightingValue(unweightedWeight, false));
                            if (target.TargetPopulation.HasValue)
                            {
                                result = AddExpansionFactorToResults(sampleSize, target.TargetPopulation.Value, [result]).Single();
                            }
                            yield return result;
                        }
                    }
                }
                else
                {
                    foreach (var target in singlePlan.Targets)
                    {
                        var targetCells = cellsByEntityId[target.FilterMetricEntityId].ToList();
                        if (target.Plans is { } targetChildren)
                        {
                            var nodeParameters = GetTargetParameters(currentNodeParameters, target, targetCells);
                            var results = GetTargetsForWaveGroup(nodeParameters, targetChildren, quotaCellToSampleSize);

                            if (target.Target.HasValue || target.TargetPopulation.HasValue)
                            {
                                if (totalSampleSize <= 0)
                                {
                                    _logger.LogWarning("Failed to target weight as totalSampleSize is {totalSampleSize} for {Target}", totalSampleSize, target.ExistingDatabaseId);
                                }
                                else
                                {
                                    var sampleSize = targetCells.Sum(x => quotaCellToSampleSize[x]);
                                    if (sampleSize <= 0)
                                    {
                                        _logger.LogWarning("Failed to target weight as sampleSize is {sampleSize} for {Target}", sampleSize, target.ExistingDatabaseId);
                                    }
                                    else
                                    {
                                        if (target.Target.HasValue)
                                        {
                                            results = MultiplyResultsByTargetValue((double)sampleSize / totalSampleSize, target.Target.Value, results);
                                        }
                                        else if (target.TargetPopulation.HasValue)
                                        {
                                            results = AddExpansionFactorToResults(sampleSize, target.TargetPopulation.Value, results);
                                        }
                                    }
                                }
                            }
                            foreach (var result in results) yield return (result.CellKey, result.Target);
                        }
                        else if (target.Target.HasValue)
                        {
                            foreach (var cell in targetCells) yield return (cell.ToString(), new WeightingValue(decimal.ToDouble(target.Target.Value), cell.IsResponseLevelWeighting));
                        }
                    }
                }
            }
            else
            {
                foreach (var cellKeyToTarget in RimWeightLeaf(plans, quotaCellToSampleSizeForGroup)) yield return cellKeyToTarget;
            }
        }

        private static IEnumerable<(string CellKey, WeightingValue Target)> MultiplyResultsByTargetValue(
            double actualPercentage, decimal targetValue, IEnumerable<(string CellKey, WeightingValue Target)> results)
        {
            var resultList = results.ToList();

            var sumOfSubTargets = resultList.Where(r=>r.Target.Weight>=0).Sum(r => r.Target.Weight);
            var multiplierToSumToTarget = sumOfSubTargets == 0.0 ? 0 : decimal.ToDouble(targetValue) / sumOfSubTargets;

            multiplierToSumToTarget = multiplierToSumToTarget / actualPercentage ;

            return resultList.Select(resultTarget => (resultTarget.CellKey, resultTarget.Target.Weight == -1 ? resultTarget.Target : resultTarget.Target with { Weight = (resultTarget.Target.Weight * multiplierToSumToTarget) }));
        }

        private static IEnumerable<(string CellKey, WeightingValue Target)> AddExpansionFactorToResults(
            double actualSampleSize, int targetPopulation, IEnumerable<(string CellKey, WeightingValue Target)> results)
        {
            var multiplier = (double)targetPopulation / actualSampleSize;
            return results.Select(resultTarget => (resultTarget.CellKey, resultTarget.Target with { ExpansionFactor = multiplier }));
        }

        private bool HasNoSample(IReadOnlyCollection<QuotaCell> parentCells, IReadOnlyCollection<WeightingPlan> plans, List<(QuotaCell QuotaCell, double SampleSize)> quotaCellToSampleSizeForGroup)
        {
            var sampleSizeForGroup = quotaCellToSampleSizeForGroup.Sum(s => s.SampleSize);
            bool hasNoSample = sampleSizeForGroup < 1;
            if (hasNoSample && parentCells.Count > 0)
            {
                // Don't throw, because then the dashboard crashes and the config issue can't be fixed by an end user. But weight to zero so that they don't see unweighted data if our check here was somehow wrong.
                var logText = $"Sample size was zero for all {parentCells.Count} quota cells near plan with id {plans.First().ExistingDatabaseId.GetValueOrDefault()}. Continuing with zero target weightings.";
                if (plans.First().ExistingDatabaseId.GetValueOrDefault() == 0)
                {
                    _logger.LogWarning(logText);
                }
                else
                {
                    _logger.LogError(logText);
                }
            }

            return hasNoSample;
        }

        private static CurrentNodeParameters GetTargetParameters(CurrentNodeParameters currentNodeParameters, WeightingTarget target, IReadOnlyCollection<QuotaCell> childCells)
        {
            if (!target.Target.HasValue)
            {
                return currentNodeParameters with { ParentCells = childCells };
            }

            int? newWeightingGroupId = currentNodeParameters.WeightingGroupId ?? target.WeightingGroupId;
            return new CurrentNodeParameters(childCells, newWeightingGroupId);
        }
        private static IEnumerable<(string, WeightingValue)> RimWeightLeaf(IReadOnlyCollection<WeightingPlan> plans, List<(QuotaCell QuotaCell, double SampleSize)> quotaCellToSampleSizeForGroup)
        {
            var rimDimensions = CalcDimensions(plans, quotaCellToSampleSizeForGroup);

            var rimWeightingCalculator = new RimWeightingCalculator();
            var rimWeightingCalculationResult =
                rimWeightingCalculator.Calculate(quotaCellToSampleSizeForGroup, rimDimensions, true);
            foreach (var result in rimWeightingCalculationResult.QuotaDetails.Select(qd =>
                         (qd.QuotaCell.ToString(), new WeightingValue(qd.Target, qd.QuotaCell.IsResponseLevelWeighting))))
            {
                yield return result;
            }
        }

        private static Dictionary<string, Dictionary<int, double>> CalcDimensions(IReadOnlyCollection<WeightingPlan> plans, List<(QuotaCell QuotaCell, double SampleSize)> quotaCellToSampleSizeForGroup)
        {
            var totalSampleForThisGroup = quotaCellToSampleSizeForGroup.Sum(qs => qs.SampleSize);

            var rimDimensionsWithNulls = plans.ToDictionary(p => p.FilterMetricName,
                p => p.Targets.ToDictionary(t => t.FilterMetricEntityId, t =>
                (targetValue: t.Target,
                sampleSize: quotaCellToSampleSizeForGroup.Where(qs => int.Parse(qs.QuotaCell.GetKeyPartForFieldGroup(p.FilterMetricName)) == t.FilterMetricEntityId)
                .Sum(qs => qs.SampleSize))));

            var rimDimensions = new Dictionary<string, Dictionary<int, double>>();

            foreach (var pair in rimDimensionsWithNulls)
            {
                var idToSampleCount = pair.Value;
                var result = new Dictionary<int, double>();
                if (idToSampleCount.Any(x => !x.Value.targetValue.HasValue))
                {
                    result.AddRange(NormalizeNullTargets(totalSampleForThisGroup, idToSampleCount));
                }
                else
                {
                    result.AddRange(idToSampleCount.Select(x => new KeyValuePair<int, double>(x.Key, decimal.ToDouble(x.Value.targetValue.Value) * totalSampleForThisGroup)));
                }
                rimDimensions[pair.Key] = result;
            }

            return rimDimensions;
        }

        private static List<KeyValuePair<int, double>> NormalizeNullTargets(double totalSampleForThisGroup, Dictionary<int, (decimal? targetValue, double sampleSize)> idToSampleCount)
        {
            var targetsWithNullTargets = idToSampleCount
                .Where(x => !x.Value.targetValue.HasValue)
                .Select(x => new KeyValuePair<int, double>(x.Key, x.Value.sampleSize)).ToList();

            var targets = targetsWithNullTargets;

            var totalSizeSampleMinusNullTargetsSampleSize = totalSampleForThisGroup - targetsWithNullTargets.Sum(x => x.Value);

            targets.AddRange(
                idToSampleCount.Where(x => x.Value.targetValue.HasValue)
                .Select(x => new KeyValuePair<int, double>(x.Key, decimal.ToDouble(x.Value.targetValue.Value) * totalSizeSampleMinusNullTargetsSampleSize)));

            return targets;
        }
    }
}

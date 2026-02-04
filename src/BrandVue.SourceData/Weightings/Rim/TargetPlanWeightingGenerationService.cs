using System.Globalization;
using System.IO;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using CsvHelper;
using CsvHelper.TypeConversion;
using MissingFieldException = CsvHelper.MissingFieldException;

namespace BrandVue.SourceData.Weightings.Rim
{
    public class TargetPlanWeightingGenerationService
    {
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IMeasureRepository _measureRepository;

        public record GeneratedWeightings(ILookup<Subset, List<WeightingPlan>> SubsetToPlans,
            IReadOnlyCollection<string> Warnings, IReadOnlyCollection<string> Errors);

        private record WeightingCsvRow(int ResponseId, string Weight);

        public TargetPlanWeightingGenerationService(IRespondentRepositorySource respondentRepositorySource,
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IMeasureRepository measureRepository)
        {
            _respondentRepositorySource = respondentRepositorySource;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _measureRepository = measureRepository;
        }

        public GeneratedWeightings ReverseScaleFactors(
            IEnumerable<(Subset Subset, IReadOnlyCollection<WeightingPlan> Plans)> subsetPlansNotToReplace,
            IEnumerable<(Subset Subset, IReadOnlyCollection<WeightingPlan> Plans)> subsetPlansToReplace,
            Stream fileStream)
        {
            var warnings = new List<string>();
            var errors = new List<string>();

            try
            {
                var reverseScaleFactors = ReverseScaleFactors(subsetPlansNotToReplace, subsetPlansToReplace, GetWeights(fileStream), warnings);
                var subsetPlanLookup = reverseScaleFactors.ToLookup(
                    subsetAndWeightingPlans => subsetAndWeightingPlans.Subset, 
                    subsetAndWeightingPlans => subsetAndWeightingPlans.Plans.ToList());

                return new GeneratedWeightings(subsetPlanLookup, warnings, errors);
            }
            catch (Exception e)
            {
                var allSubsets = subsetPlansNotToReplace.Select(x => x.Subset)
                    .Concat(subsetPlansToReplace.Select(x => x.Subset));

                var subsetAndEmptyWeightingPlans = allSubsets.ToLookup(s => s, s => new List<WeightingPlan>());
                return new GeneratedWeightings(subsetAndEmptyWeightingPlans, warnings.ToList(),
                    ("FATAL ERROR: " + e.Message).Yield().Concat(errors).ToList());
            }
        }

        private IEnumerable<(Subset Subset, IReadOnlyCollection<WeightingPlan> Plans)> ReverseScaleFactors(
            IEnumerable<(Subset Subset, IReadOnlyCollection<WeightingPlan> Plans)> subsetPlansNotToReplace,
            IEnumerable<(Subset Subset, IReadOnlyCollection<WeightingPlan> Plans)> subsetPlansToReplace,
            Dictionary<int, decimal> responseWeights,
            List<string> warnings)
        {
            CheckAllRespondentsInAllSubsetsHaveAWeight(subsetPlansToReplace, responseWeights, warnings);

            var newPlans = subsetPlansToReplace.Select(subsetPlans =>
            {
                var targetWeightsForPlan =
                    GetScaleFactors(subsetPlans.Subset, subsetPlans.Plans, responseWeights, warnings);
                var plans = (IReadOnlyCollection<WeightingPlan>)(new List<WeightingPlan> { CreateTargetWeightingPlan(subsetPlans.Subset, subsetPlans.Plans, targetWeightsForPlan, warnings) }).AsReadOnly();
                return (subsetPlans.Subset, Plans: plans);
            });

            return subsetPlansNotToReplace.Concat(newPlans);
        }

        private void CheckAllRespondentsInAllSubsetsHaveAWeight(
            IEnumerable<(Subset Subset, IReadOnlyCollection<WeightingPlan> Plans)> activeSubsets,
            Dictionary<int, decimal> responseWeights, List<string> warnings)
        {
            var firstLevelPlansWithWave = activeSubsets.SelectMany(subsetPlans => subsetPlans.Plans.Where(p => p.IsWeightingGroupRoot));
            var filterMetricIds = firstLevelPlansWithWave.SelectMany(p => p.Targets).Select(t => t.FilterMetricEntityId)
                .Distinct().ToArray();

            var allSubsetProfilesById = activeSubsets.SelectMany(s =>
                {
                    var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(s.Subset);
                    var cells = _respondentRepositorySource.GetForSubset(s.Subset).AllCellsGroup;
                    return profileResponseAccessor.GetResponses(cells).ToArray();
                }, (subsetPlan, quotaCell) => (subsetPlan, quotaCell))
                .SelectMany(t => t.quotaCell.Profiles.Span.ToArray(),
                    (subsetPlan, profile) => (subsetPlan, profile))
                .ToLookup(t => t.profile.Id);

            var unweightedCell = QuotaCell.UnweightedQuotaCell(null);

            var incorrectlyArchivedResponseIds = responseWeights.Where(r => r.Value != 0)
                .Where(weightedResponse => !allSubsetProfilesById[weightedResponse.Key].Any())
                .Select(r => r.Key)
                .ToArray();

            if (incorrectlyArchivedResponseIds.Any())
            {
                warnings.Add(
                    $"Response weight defined in csv, but respondent not in vue - possibly archived or has no enabled subset for segment: '{string.Join(",", incorrectlyArchivedResponseIds)}'");
            }

            var suspiciouslyUnweightedResponseIds = responseWeights.Where(r => r.Value != 0).Select(r => r.Key)
                .Where(weightedResponseId => allSubsetProfilesById[weightedResponseId].Any(vueLoadedResponse =>
                    vueLoadedResponse.subsetPlan.quotaCell.QuotaCell.Equals(unweightedCell)))
                .ToArray();

            if (suspiciouslyUnweightedResponseIds.Any())
            {
                warnings.Add(
                    $"Will not be weighted: Response weights defined but in vue unweighted cell: '{string.Join(",", suspiciouslyUnweightedResponseIds)}'");
            }

            foreach (var subsetPlan in activeSubsets.Where(sp => sp.Plans.IsWavePlan()))
            {
                var cells = _respondentRepositorySource.GetForSubset(subsetPlan.Subset).AllCellsGroup;
                var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(subsetPlan.Subset);

                var responses = profileResponseAccessor.GetResponses(cells).ToArray();
                var measure = _measureRepository.Get(subsetPlan.Plans.Single().FilterMetricName);
                var inWaveFuncs = filterMetricIds.Select(i => measure.PrimaryFieldValueCalculator(
                    new EntityValueCombination(new EntityValue(measure.EntityCombination.Single(), i)))).ToArray();
                var unweightedResponses = responses.Where(r => r.QuotaCell.Equals(unweightedCell))
                    .SelectMany(r => r.Profiles.Span.ToArray())
                    .Where(p => responseWeights.TryGetValue(p.Id, out var weight) && weight > 0);
                var waves = unweightedResponses.Select(w => (w.Id, Wave: GetWave(inWaveFuncs, w)))
                    .Where(p => p.Wave.HasValue).ToLookup(p => p.Wave, p => p.Id);
                var schemeIssueWarnings = waves.Select(missingWaveRespondents =>
                    $"ERROR: Subset '{subsetPlan.Subset.Id}' weighting scheme does not include these respondents in wave id {missingWaveRespondents.Key}:\r\n{string.Join(", ", missingWaveRespondents)}");
                warnings.AddRange(schemeIssueWarnings);
            }
        }

        private static int? GetWave(Func<IProfileResponseEntity, int?>[] inWaveFuncs,
            IProfileResponseEntity unweightedResponse)
        {
            return inWaveFuncs.Select(w => (int?)w(unweightedResponse)).FirstOrDefault(w => w > 0);
        }

        private static Dictionary<int, decimal> GetWeights(Stream stream)
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            //Case/space insensitive header match: https://stackoverflow.com/a/49730650/1128762
            csv.Configuration.PrepareHeaderForMatch = (string header, int _) => header.Replace(" ", "").ToLower();
            try
            {
                var responseWeights = csv.GetRecords<WeightingCsvRow>()
                    .Where(r => !string.IsNullOrWhiteSpace(r.Weight))
                    .ToDictionary(r => r.ResponseId, r => decimal.Parse(r.Weight));
                return responseWeights;
            }
            catch (TypeConverterException tex)
            {
                throw new Exception($"Failed to read CSV file. Type conversion field: '{tex.ReadingContext.Field}' , row: {tex.ReadingContext.RawRow} : '{tex.ReadingContext.RawRecord}'", tex);

            }
            catch (MissingFieldException mex)
            {
                throw new Exception($"Failed to read CSV file. Missing field {mex.ReadingContext.Field}", mex);
            }
            catch (HeaderValidationException ex)
            {
                throw new Exception($"Failed to read CSV file. Missing column {string.Join(",", ex.HeaderNames)}", ex);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private WeightingPlan CreateTargetWeightingPlan(Subset subset,IReadOnlyCollection<WeightingPlan> plans, (int? FilterMetricId, IReadOnlyDictionary<string, decimal> CellTargetWeights)[] targetWeightsForPlan, List<string> warnings)
        {
            if (plans.IsWavePlan())
            {
                return CreateTargetWeightingPlanByWave(plans.Single(), targetWeightsForPlan);
            }
            if (plans.AreAllPlansRimWeighted())
            {
                if (targetWeightsForPlan.Any())
                {
                    if (targetWeightsForPlan.Length != 1 || targetWeightsForPlan[0].FilterMetricId.HasValue)
                    {
                        warnings.Add($"All Plans are RIM weighted for survey segment {subset.Id} but targetWeights for plan is mismatched");
                    }
                    return CreateTargetWeightingPlanForRim(plans, targetWeightsForPlan.First().CellTargetWeights);
                }
            }
            if (plans.Count() == 1)
            {
                warnings.Add($"Ignoring plan for survey segment {subset.Id} as it cannot be turned into TargetWeighted");
                return plans.First();
            }
            warnings.Add($"ERROR: Plans for survey segment {subset.Id} cannot be turned into TargetWeighted");
            return null;
        }

        private WeightingPlan CreateTargetWeightingPlanByWave(WeightingPlan wavePlan, (int? FilterMetricId, IReadOnlyDictionary<string, decimal> CellTargetWeights)[] targetWeightsForPlan)
        {
            var targetsForNewPlan = new List<WeightingTarget>();
            var targetsCreated = new List<WeightingTarget>();
            foreach (var targetTuple in targetWeightsForPlan)
            {
                var originalPlanForWave = wavePlan.Targets.Single(t => t.FilterMetricEntityId == targetTuple.FilterMetricId);
                if (originalPlanForWave.Plans.AreAllPlansRimWeighted())
                {
                    var newPlan = CreateTargetWeightingPlanForRim(originalPlanForWave.Plans, targetTuple.CellTargetWeights);
                    if (newPlan != null)
                    {
                        targetsCreated.Add(new WeightingTarget(new[] { newPlan }, targetTuple.FilterMetricId.Value, null, null, null, 0));
                    }
                }
            }

            foreach (var existing in wavePlan.Targets)
            {
                var targetCreated = targetsCreated.SingleOrDefault(x => x.FilterMetricEntityId == existing.FilterMetricEntityId);
                targetsForNewPlan.Add(targetCreated??existing);
                
            }
            return new WeightingPlan(wavePlan.FilterMetricName, targetsForNewPlan, wavePlan.IsWeightingGroupRoot, 0);           
        }

        private WeightingPlan CreateNewSubPlan(WeightingPlan weightingPlan, IReadOnlyCollection<WeightingPlan> nested, IReadOnlyDictionary<string, decimal> cellTargetWeight, int[] quotaCell)
        {
            var newTargets = new List<WeightingTarget>();
            foreach (var target in weightingPlan.Targets)
            {
                var quotaCellForTarget = new List<int>(quotaCell) { target.FilterMetricEntityId };

                if (nested != null && nested.Any())
                {
                    var childItems = CreateNewSubPlan(nested.First(), nested.Skip(1).ToList(), cellTargetWeight, quotaCellForTarget.ToArray());
                    if (childItems != null && childItems.Targets.Any())
                    {
                        var myTarget = new WeightingTarget(
                            new[]
                            {
                           childItems
                            },
                            target.FilterMetricEntityId,
                            null,
                            null,
                            null,
                            0);

                        newTargets.Add(myTarget);
                    }
                }
                else
                {
                    var quotaCellForTargetString = string.Join(QuotaCell.PartSeparator, quotaCellForTarget);
                    if (cellTargetWeight.ContainsKey(quotaCellForTargetString))
                    {
                        newTargets.Add(new WeightingTarget(null, target.FilterMetricEntityId, cellTargetWeight[quotaCellForTargetString], null, null, 0));
                    }
                }
            }
            return new WeightingPlan(weightingPlan.FilterMetricName, newTargets, false, 0);
        }

        private WeightingPlan CreateTargetWeightingPlanForRim(IReadOnlyCollection<WeightingPlan> oldRimPlans, IReadOnlyDictionary<string, decimal> cellTargetWeights)
        {
            return CreateNewSubPlan(oldRimPlans.First(), oldRimPlans.Skip(1).ToList(), cellTargetWeights, Array.Empty<int>());
        }

        private static string StripFilterMetricId(int? argFilterMetricId, string previousQuotaCellKey)
        {
            if (!argFilterMetricId.HasValue) return previousQuotaCellKey;
            string[] parts = previousQuotaCellKey.Split(QuotaCell.PartSeparator);
            if (parts[0] != argFilterMetricId.Value.ToString())
            {
                throw new InvalidOperationException(
                    $"Respondents from {previousQuotaCellKey}, wave {parts[0]} are moving to wave {argFilterMetricId}");
            }
            return QuotaCell.GenerateKey(parts.Skip(1));
        }

        private (int? FilterMetricId, IReadOnlyDictionary<string, decimal> CellTargetWeights)[] GetScaleFactors(
            Subset subset, IReadOnlyCollection<WeightingPlan> subsetPlans,
            IReadOnlyDictionary<int, decimal> weightsByResponseId, List<string> warnings)
        {
            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(subset);
            var quotaCells = _respondentRepositorySource.GetForSubset(subset).WeightedCellsGroup;
            var targetWeightsForScheme = quotaCells.IndependentlyWeightedGroups
                .Select(waveCells =>
                {
                    int? filterInstanceId = subsetPlans.IsWavePlan() ? int.Parse(waveCells.Value.Cells.First().ToString().Split(QuotaCell.PartSeparator).First()) : null;
                    return (FilterMetricId: filterInstanceId,
                        CellTargetWeights: CalculateTargetWeightsForWave(subset, subsetPlans,
                            profileResponseAccessor, waveCells.Value, weightsByResponseId, warnings,
                            filterInstanceId));
                })
                .Where(x => x.CellTargetWeights != null)
                .OrderBy(w => w.FilterMetricId)
                .ToArray();
            return targetWeightsForScheme;
        }

        private IReadOnlyDictionary<string, decimal> CalculateTargetWeightsForWave(Subset subset,
            IReadOnlyCollection<WeightingPlan> subsetPlans, IProfileResponseAccessor profileResponseAccessor,
            IGroupedQuotaCells waveCells,
            IReadOnlyDictionary<int, decimal> weightsByResponseId, List<string> warnings, int? filterInstanceId)
        {
            var originalTarget = subsetPlans.IsWavePlan() ? subsetPlans.Single().Targets.FirstOrDefault(x => x.FilterMetricEntityId == filterInstanceId).Plans : subsetPlans;

            if (originalTarget.IsTargetWeighted())
            {
                var originalCellKeyToTarget = originalTarget.Single().PlanToTargetWeightedDimension().CellKeyToTarget;
                return originalCellKeyToTarget.ToDictionary(x => x.Key, x => x.Value);
            }

            var responses = profileResponseAccessor.GetResponses(waveCells).ToArray();

            var scaleFactors = responses
                .Where(q => q.QuotaCell.WeightingGroupId.HasValue)
                .Select(q =>
                    GetValidatedQuotaCellResponseWeight(subset, q, weightsByResponseId,
                        filterInstanceId, warnings)
                ).Where(x => x.HasValue).Select(x => x.Value).ToList();

            if (!scaleFactors.Any()) return null;

            var totalSampleSize = scaleFactors.Sum(x => x.SampleCount);
            var targetWeightsForWave =
                WeightingHelper.ConvertScaleFactorsToTargetWeights(scaleFactors, totalSampleSize);

            var sum = targetWeightsForWave.Values.Sum();
            if (Math.Abs(1 - sum) > RimWeightingCalculator.PointTolerance)
            {
                warnings.Add($"Normalizing wave id {filterInstanceId} which has weights summing to {sum}");
                targetWeightsForWave = targetWeightsForWave.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / sum);
                sum = targetWeightsForWave.Values.Sum();
                if (Math.Abs(1 - sum) > RimWeightingCalculator.PointTolerance)
                    warnings.Add(
                        $"For wave id {filterInstanceId}, cannot normalize weights to within tolerance. Sum is: {sum}");
            }
            return targetWeightsForWave.ToDictionary(x => StripFilterMetricId(filterInstanceId, x.Key),
                x => Convert.ToDecimal(x.Value));
        }

        

        private (QuotaCell QuotaCell, double SampleCount, double ResponseWeight)? GetValidatedQuotaCellResponseWeight(Subset subset,
            PopulatedQuotaCell q,
            IReadOnlyDictionary<int, decimal> weightsByResponseId, int? waveCellsKey, List<string> warnings)
        {
            var profiles = q.Profiles.ToArray().Select(profileResponseEntity =>
            {
                if (!weightsByResponseId.TryGetValue(profileResponseEntity.Id, out var responseWeight))
                {
                    return (profileResponseEntity, null);
                }

                return (profileResponseEntity, responseWeight: (decimal?)responseWeight);
            }).ToLookup(w => w.responseWeight.HasValue);
            var weightedProfiles = profiles[true].ToArray();
            var unweightedProfiles = profiles[false].ToArray();

            if (!weightedProfiles.Any())
            {
                warnings.Add(
                    $"In subset {subset.Id}: Omitting wave id {waveCellsKey}, quota cell (in full) {q.QuotaCell} which has no response weights provided");
                return null;
            }

            if (unweightedProfiles.Any())
            {
                warnings.Add(
                    $"In subset {subset.Id}: Omitting wave id {waveCellsKey}, quota cell (in full) {q.QuotaCell}, which has no response weights provided for these response ids:\r\n{string.Join(", ", unweightedProfiles.Select(p => p.profileResponseEntity.Id))}");
                return null;
            }

            var first = weightedProfiles.First();
            var different = weightedProfiles.Where(profileResponseAndWeight =>
                    profileResponseAndWeight.responseWeight != first.responseWeight)
                .Select(profileResponseAndWeight => (profileResponseAndWeight.profileResponseEntity.Id,
                    profileResponseAndWeight.responseWeight)).ToArray();

            if (different.Any())
            {
                warnings.Add(
                    $"In subset {subset.Id}: Omitting wave id {waveCellsKey}, quota cell (in full) {q.QuotaCell}, which has differing response weights: ({first.profileResponseEntity.Id}, {first.responseWeight}), {string.Join(", ", different)}");
                return null;
            }
            return (q.QuotaCell, q.Profiles.Length, Convert.ToDouble(first.responseWeight.Value));
        }
    }
}
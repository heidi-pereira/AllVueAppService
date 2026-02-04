using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Weightings;
using BrandVue.SourceData.Weightings.Rim;
using Microsoft.Scripting.Utils;

namespace BrandVue.SourceData.QuotaCells
{
    /// <summary>
    /// Rules for the validator:
    /// Only a single Variable/Node through out the tree, except for the RIM nodes
    /// The Rim Nodes must be the leaf nodes
    /// Nested weighting does not support -1 or null at non-leaf nodes
    /// Nested weighting %s must add up to 100% (The code does deal with this not happenning)
    /// 
    /// This validator is closely related to ReferenceWeightingCalculator.cs
    /// </summary>

    public class ReferenceWeightingValidator 
    {
        private const decimal MaxToleranceFor100Percent = 0.00001M;

        private static bool FeatureFlagNewWeightingUIAvailable => "true".Equals(new AppSettings().GetSetting("FeatureFlagNewWeightingUIAvailable"), StringComparison.InvariantCultureIgnoreCase);

        public enum ErrorMessageLevel
        {
            Error,
            Warning,
        }

        public enum ErrorMessageType
        {
            MissingVariable,
            MissingSubtreeForInstance,
            InvalidNestedTarget,
            OverlappingWave,
            QuestionUsedMoreThanOnce,
            QuestionHasNoTargets,
            QuestionNotValid,
            QuestionMarkedAsGrouped,
            EmptyPlan,
            MixedTargetPercentageAndPopulation,
            TargetPopulationOutsideOfRoot,
        }

        public record Message(string Path, string MessageText, ErrorMessageLevel ErrorLevel);

        public record ValidationFilterMetric(string Name, IEnumerable<int> InstanceIds);
        public record ValidationTarget(int EntityId, decimal? Target);
        public record WeightingValidationMessage(ErrorMessageLevel ErrorLevel, ErrorMessageType ErrorType, IList<TargetValue> ParentVariables = null, string Path = "", string FilterMetricName = "", string Variable = "", string ParentWithGrouping = "", IEnumerable<ValidationFilterMetric> SuspectMetrics = null, IEnumerable<int> InstanceIds = null, IEnumerable<ValidationTarget> Targets= null){}

        public static IList<Message> ConvertMessages(IList<WeightingValidationMessage> messages)
        {
            return messages.Select(x => new Message(x.Path, MessageTextFromValidationMessage(x), x.ErrorLevel)).ToList();
        }

        private static string MessageTextFromValidationMessage(WeightingValidationMessage message)
        {
            return message.ErrorType switch
            {
                ErrorMessageType.MissingVariable => $"Variable {message.Variable} undefined",
                ErrorMessageType.MissingSubtreeForInstance => $"RIM ({message.Path}) contains sub-trees ({string.Join(",", message.SuspectMetrics.Select(x => $"<{x.Name}> - Instances {String.Join(",", x.InstanceIds)}"))})",
                ErrorMessageType.InvalidNestedTarget => $"Invalid nested target for {message.Path} <{message.FilterMetricName}> {string.Join(",", message.Targets.Select(x => $"{x.EntityId}={x.Target}"))}. Adds up to {message.Targets.Sum(x => x.Target)}",
                ErrorMessageType.OverlappingWave => $"Overlapping waves for {message.Path} <{message.FilterMetricName}> {string.Join(",", message.InstanceIds)}",
                ErrorMessageType.QuestionUsedMoreThanOnce => $"Question {message.Path} <{message.FilterMetricName}> used more than once.",
                ErrorMessageType.QuestionHasNoTargets => $"Question {message.Path} <{message.FilterMetricName}> has no targets",
                ErrorMessageType.QuestionNotValid => $"Question {message.Path} <{message.FilterMetricName}> not valid{(message.ErrorLevel == ErrorMessageLevel.Error ? " on its own" : "")}. Either add targets to ({string.Join(",", message.InstanceIds)}) or add question with RIM{(FeatureFlagNewWeightingUIAvailable ? " or Response Level" : "")} weightings below it.",
                ErrorMessageType.QuestionMarkedAsGrouped => $"Question {message.Path} <{message.FilterMetricName}> not valid. The parent '{message.ParentWithGrouping}' has been marked as grouped for weighting",
                ErrorMessageType.EmptyPlan => $"Empty plans are not valid",
                ErrorMessageType.MixedTargetPercentageAndPopulation => $"Question {message.Path} <{message.FilterMetricName}> has both target percentages and target sample",
                ErrorMessageType.TargetPopulationOutsideOfRoot => $"Question {message.Path} <{message.FilterMetricName}> has target sample - only a singular root question can have target sample",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public bool IsValid(bool hasRootResponseLevelWeighting, IReadOnlyCollection<WeightingPlan> weightingPlans, IMeasureRepository _measureRepository, out IList<WeightingValidationMessage> messages)
        {
            messages = new List<WeightingValidationMessage>();
            if (!weightingPlans.Any())
            {
                if (hasRootResponseLevelWeighting)
                {
                    return true;
                }
                messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.EmptyPlan, null, "Root"));
                return false;
            }
            return ValidateTargetsForWaveGroup(weightingPlans, new List<TargetValue> (), _measureRepository, messages);
        }

        public bool ValidateVariablesExist(IReadOnlyCollection<WeightingPlanConfiguration> weightingPlans, IMeasureRepository _measureRepository, IList<WeightingValidationMessage> messages)
        {
            var missingIdentifiers = new HashSet<string>();
            foreach (var plan in weightingPlans)
            {
                if (!_measureRepository.TryGet(plan.VariableIdentifier, out var value))
                {
                    missingIdentifiers.Add(plan.VariableIdentifier);
                }
            }
            messages.AddRange(missingIdentifiers.Select(x => new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.MissingVariable, Variable: x)));
            return !missingIdentifiers.Any();
        }

        public record TargetValue(string VariableName, int InstanceId);
        private static string TargetValuesToPath(IList<TargetValue> values)
        {
            if (values == null || !values.Any())
                return "Root";
            return string.Join(",", values.Select (x=> $"{x.VariableName}:{x.InstanceId}" ));
        }

        private bool IsTargetSumValid(IList<TargetValue> parentVariables, WeightingPlan singlePlan,
            IList<WeightingValidationMessage> messages)
        {
            const decimal OneHundredPercent = 1M;

            var total = singlePlan.Targets.Sum(x => x.Target.HasValue ? x.Target.Value : 0);
            var isValidTarget = (Math.Abs(OneHundredPercent - total) < MaxToleranceFor100Percent);
            if (!isValidTarget)
            {
                messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.InvalidNestedTarget, parentVariables,
                    TargetValuesToPath(parentVariables), InstanceIds: singlePlan.Targets.Select(target => target.FilterMetricEntityId), FilterMetricName: singlePlan.FilterMetricName, Targets: singlePlan.Targets.Select(target => new ValidationTarget(target.FilterMetricEntityId, target.Target))));
            }
            return isValidTarget;
        }
        private bool IsValidTargetToBeAppliedToWave(IList<TargetValue> parentVariables, WeightingPlan singlePlan, IList<WeightingValidationMessage> messages)
        {
            bool isLeafNode = !singlePlan.Targets.Any( target => target.Plans != null && target.Plans.Count > 0);
            if (isLeafNode)
            {
                return true;
            }
            if (singlePlan.Targets.All(target => !target.Target.HasValue))
            {
                return true;
            }
            if (singlePlan.Targets.All(x => !x.Target.HasValue) && singlePlan.Targets.All(x => x.TargetPopulation.HasValue))
            {
                return true;
            }
            return IsTargetSumValid(parentVariables, singlePlan, messages);
        }

        private bool IsOverlapBetweenWaves(DateRangeVariableComponent waveOne, DateRangeVariableComponent waveTwo)
        {
            return waveOne.MinDate <= waveTwo.MaxDate && waveTwo.MinDate <= waveOne.MaxDate;
        }

        private int[] OverlappingWaveIds(DataWaveVariable variable)
        {
            var waveIds = variable.WaveIdToWaveConditions.Select(x => x.Key).ToArray();
            var overlappingWaves = new List<int>();
            for (var i = 0; i < waveIds.Length; i++)
            {
                for (var j = i + 1; j < waveIds.Length; j++)
                {
                    if (IsOverlapBetweenWaves(variable.WaveIdToWaveConditions[waveIds[i]], variable.WaveIdToWaveConditions[waveIds[j]]))
                    {
                        overlappingWaves.Add(waveIds[i]);
                        overlappingWaves.Add(waveIds[j]);
                    }
                }
            }
            return overlappingWaves.ToArray();
        }

        private bool ValidateTargetsForWaveGroup(IReadOnlyCollection<WeightingPlan> plans, IList<TargetValue> parentVariables, IMeasureRepository _measureRepository, IList<WeightingValidationMessage> messages, bool parentIsGrouped = false, string parentWithGrouping = null)
        {
            if (plans.OnlyOrDefault() is {} singlePlan)
            {
                var targetsWithPercentage = singlePlan.Targets?.Count(x => x.Target.HasValue) ?? 0;
                var targetsWithPopulation = singlePlan.Targets?.Count(x => x.TargetPopulation.HasValue) ?? 0;
                var isRootPlan = !parentVariables.Any();
                if (targetsWithPercentage != 0 && targetsWithPopulation != 0)
                {
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error,
                        ErrorMessageType.MixedTargetPercentageAndPopulation,
                        parentVariables,
                        TargetValuesToPath(parentVariables),
                        FilterMetricName: singlePlan.FilterMetricName));
                    return false;
                }
                if (targetsWithPopulation != 0 && !isRootPlan)
                {
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error,
                        ErrorMessageType.TargetPopulationOutsideOfRoot,
                        parentVariables,
                        TargetValuesToPath(parentVariables),
                        FilterMetricName: singlePlan.FilterMetricName));
                    return false;
                }

                if (singlePlan.IsPercentageWeighting() || singlePlan.IsExpansionWeightingWithNoChildren())
                {
                    return true;
                }
                if (singlePlan.Targets == null || singlePlan.Targets.Count == 0)
                {
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.QuestionHasNoTargets, parentVariables, Path: TargetValuesToPath(parentVariables), FilterMetricName: singlePlan.FilterMetricName));
                    return false;
                }
                if (!IsValidTargetToBeAppliedToWave(parentVariables, singlePlan, messages))
                {
                    return false;
                }

                if (parentVariables.Any(x => x.VariableName == singlePlan.FilterMetricName))
                {
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.QuestionUsedMoreThanOnce, parentVariables, Path: TargetValuesToPath(parentVariables), FilterMetricName: singlePlan.FilterMetricName));
                    return false;
                }
                
                if (_measureRepository.TryGet(singlePlan.FilterMetricName, out var measure))
                {
                    if (measure.PrimaryVariable is DataWaveVariable variable)
                    {
                        var overlappingWaveInstanceIds = OverlappingWaveIds(variable);
                        if (overlappingWaveInstanceIds.Length > 0) 
                        {
                            messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error,
                                ErrorMessageType.OverlappingWave, parentVariables, Path: TargetValuesToPath(parentVariables), FilterMetricName: singlePlan.FilterMetricName, InstanceIds: overlappingWaveInstanceIds));
                            return false;
                        }
                    }
                }

                var areAllTargetsValid = true;
                var missingInstances = new List<int>();
                if (parentIsGrouped && singlePlan.IsWeightingGroupRoot)
                {
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.QuestionMarkedAsGrouped,
                        Path: TargetValuesToPath(parentVariables),
                        FilterMetricName: singlePlan.FilterMetricName,
                        ParentWithGrouping: parentWithGrouping));
                    areAllTargetsValid = false;
                }

                areAllTargetsValid = ValidateWaves(parentVariables, _measureRepository, messages, parentIsGrouped, parentWithGrouping, singlePlan, areAllTargetsValid, missingInstances);

                if (missingInstances.Any())
                {
                    areAllTargetsValid = false;
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Warning, ErrorMessageType.QuestionNotValid, parentVariables,
                        Path: TargetValuesToPath(parentVariables), FilterMetricName: singlePlan.FilterMetricName,
                        InstanceIds: missingInstances));
                }
                return areAllTargetsValid;
            }
            if (!plans.Any())
            {
                messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.EmptyPlan, parentVariables, Path: TargetValuesToPath(parentVariables)));
                return false;
            }
            return ValidateRimWeightLeaf(plans, parentVariables, messages);
        }

        private bool ValidateWaves(IList<TargetValue> parentVariables, IMeasureRepository _measureRepository, IList<WeightingValidationMessage> messages, bool parentIsGrouped,
            string parentWithGrouping, WeightingPlan singlePlan, bool areAllTargetsValid, List<int> missingInstances)
        {
            foreach (var target in singlePlan.Targets)
            {
                var variables = new List<TargetValue>(parentVariables)
                {
                    new TargetValue(singlePlan.FilterMetricName, target.FilterMetricEntityId)
                };

                if (target.ResponseWeightingContext is null)
                {
                    if (target.Plans is { } targetChildren)
                    {
                        var isValid = ValidateTargetsForWaveGroup(targetChildren,
                            variables, _measureRepository, messages, singlePlan.IsWeightingGroupRoot || parentIsGrouped,
                            singlePlan.IsWeightingGroupRoot
                                ? TargetValuesToPath(parentVariables)
                                : parentWithGrouping);
                        if (!isValid)
                        {
                            areAllTargetsValid = false;
                        }
                    }
                    else if (!target.Target.HasValue && !target.TargetPopulation.HasValue)
                    {
                        missingInstances.Add(target.FilterMetricEntityId);
                    }
                }
            }

            return areAllTargetsValid;
        }

        private bool ValidateRimWeightLeaf(IReadOnlyCollection<WeightingPlan> plans, IList<TargetValue> variables, IList<WeightingValidationMessage> messages)
        {
            IEnumerable<int> InstancesWithTargets(WeightingPlan plan)
            {
                return plan.Targets.Where(target => target.Plans != null && target.Plans.Any()).Select(x => x.FilterMetricEntityId);
            }

            if (plans.FirstOrDefault(p => p.Targets.Any(t => t.TargetPopulation.HasValue)) is { } invalidPlan)
            {
                messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error,
                    ErrorMessageType.TargetPopulationOutsideOfRoot,
                    variables,
                    TargetValuesToPath(variables),
                    FilterMetricName: invalidPlan.FilterMetricName));
                return false;
            }

            var variablesInUse = variables.Select(x => x.VariableName).ToList();
            var isValid = true;
            foreach(var plan in plans)
            {
                if (variablesInUse.Any(x => x== plan.FilterMetricName))
                {
                    messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.QuestionUsedMoreThanOnce, variables, Path: TargetValuesToPath(variables), FilterMetricName: plan.FilterMetricName));
                    return false;
                }
                variablesInUse.Add(plan.FilterMetricName); 
                isValid = IsTargetSumValid(variables, plan, messages) && isValid;
            }
            var result = plans.All(plan => plan.Targets.All(target => target.Plans == null || !target.Plans.Any()));

            if (!result)
            {
                var suspectPlans = plans.Where(p => p.Targets.Any(y => y.Plans != null && y.Plans.Any()));

                var suspectMetrics = suspectPlans.Select(x =>
                    new ValidationFilterMetric(x.FilterMetricName, InstancesWithTargets(x)));
                messages.Add(new WeightingValidationMessage(ErrorMessageLevel.Error, ErrorMessageType.MissingSubtreeForInstance, variables, Path: TargetValuesToPath(variables), SuspectMetrics: suspectMetrics, InstanceIds: suspectPlans.SelectMany(x => x.Targets.Where(y => y.Plans != null).Select(y => y.FilterMetricEntityId)).ToList()));
            }
            return result && isValid;
        }
    }
}
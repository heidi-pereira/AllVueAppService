using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Variable;

namespace BrandVue.SourceData.Weightings
{

    public enum WeightingType
    {
        Unknown,
        Adhoc,
        Tracker,
    }

    [Flags]
    public enum WeightingStyle
    {
        Unknown = 0,
        Interlocked = 1 << 0,
        RIM = 1 << 1,
        ResponseWeighting = 1 << 2,
        Expansion = 1 << 3,
    }

    public record WeightingTypeStyle(WeightingType Type, WeightingStyle Style);

    public class WeightingHelperService
    {
        private readonly IProductContext _productContext;
        private readonly IResponseWeightingRepository _responseWeightingRepository;
        private readonly IWeightingPlanRepository _weightingPlanRepository;
        private readonly IMetricConfigurationRepository _metricConfigRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;

        public WeightingHelperService(IProductContext productContext, IResponseWeightingRepository responseWeightingRepository, IWeightingPlanRepository weightingPlanRepository, IMetricConfigurationRepository metricConfigRepository, IVariableConfigurationRepository variableConfigurationRepository)
        {
            _productContext = productContext;
            _responseWeightingRepository = responseWeightingRepository;
            _weightingPlanRepository = weightingPlanRepository;
            _metricConfigRepository = metricConfigRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
        }

        private bool Traverse(string subsetId, IEnumerable<WeightingPlan> plans, ref WeightingStyle style)
        {
            bool hasChildren = false;
            var targets = plans.SelectMany(p => p.Targets);

            if (targets.Any(t => t.ResponseWeightingContext is not null))
            {
                style |= WeightingStyle.ResponseWeighting;
            }
            else if (plans != null)
            {
                hasChildren = plans.Any();
                if (plans.Count() > 1)
                {
                    style |= WeightingStyle.RIM;
                }
                else
                {
                    foreach (var weightingPlan in plans)
                    {
                        Traverse(subsetId, weightingPlan.Targets, ref style);
                    }
                }
            }
            return hasChildren;
        }

        private void Traverse(string subsetId, IEnumerable<WeightingTarget> targets, ref WeightingStyle style)
        {
            bool hasChildren = false;
            foreach (var target in targets)
            {
                if (target.Plans != null)
                {
                    hasChildren = Traverse(subsetId, target.Plans, ref style);
                }
            }

            if (!hasChildren)
            {
                var total = targets.Sum(x => x.Target.GetValueOrDefault());
                if (total > 0 && total < 1)
                {
                    style |= WeightingStyle.Interlocked;
                }
                else if (total == 1)
                {
                    style |= WeightingStyle.RIM;
                }
                if (targets.Sum(x => x.TargetPopulation.GetValueOrDefault(0)) > 0M)
                {
                    style |= WeightingStyle.Expansion;
                }
            }
        }

        private bool isValidWaveComponent(VariableComponent component)  {
            var isWaveVariable =  component is DateRangeVariableComponent || component is SurveyIdVariableComponent;
            if (!isWaveVariable)
            {
                if (component is InstanceListVariableComponent instanceListVariableComponent)
                {
                    return IsVariableWaveBased(_variableConfigurationRepository.GetByIdentifier(instanceListVariableComponent.FromVariableIdentifier));
                }
                if (component is CompositeVariableComponent compositeVariable)
                {
                    return compositeVariable.CompositeVariableComponents.Any(c => isValidWaveComponent(c));
                }
            }
            return isWaveVariable;
        }

        private bool IsWaveVariable (VariableDefinition variableDefinition) {
            if (variableDefinition is GroupedVariableDefinition groupedVariable) {
                return groupedVariable.Groups.All(g => isValidWaveComponent(g.Component));
            }
            return false;
        }

        private bool IsVariableWaveBased(string name)
        {
            var measure = _metricConfigRepository.Get(name);
            return IsVariableWaveBased(measure?.VariableConfiguration);
        }

        private bool IsVariableWaveBased(VariableConfiguration variableConfiguration)
        {
            if (variableConfiguration?.Definition is GroupedVariableDefinition groupedVariableDefinition)
            {
                if (IsWaveVariable(groupedVariableDefinition))
                {
                    return true;
                }
            }
            return false;
        }

        public WeightingTypeStyle WeightingTypeAndStyle(string subsetId)
        {
            var weightingType = WeightingType.Unknown;
            var weightingStyle = WeightingStyle.Unknown;
            var hasRootResponseWeights = _responseWeightingRepository.AreThereAnyRootResponseWeights(subsetId);

            if (hasRootResponseWeights)
            {
                weightingType = WeightingType.Adhoc;
                weightingStyle |= WeightingStyle.ResponseWeighting;
            }
            else
            {
                var weightingPlans = _weightingPlanRepository.GetLoaderWeightingPlansForSubset(_productContext.ShortCode, _productContext.SubProductId, subsetId);
                var plans = weightingPlans.ToAppModel().ToList();
                var targetsWithResponseWeightsFound =
                    plans.SelectMany(p => p.Targets).Any(t => t.ResponseWeightingContext is not null);

                if (targetsWithResponseWeightsFound)
                {
                    weightingStyle |= WeightingStyle.ResponseWeighting;
                }

                switch (plans.Count())
                {
                    case 0: //This is an error, as in nothing here
                        break;

                    case 1:
                    {
                        var firstAndOnlyPlan = plans.First();
                        var targets = firstAndOnlyPlan.Targets;
                        if (targets.Sum(x => x.Target.GetValueOrDefault(0M)) == 1.0m)
                        {
                            weightingStyle |= WeightingStyle.RIM;
                        }
                        if (targets.Sum(x => x.TargetPopulation.GetValueOrDefault(0)) > 0M)
                        {
                            weightingStyle |= WeightingStyle.Expansion;
                        }
                        if (weightingStyle == WeightingStyle.ResponseWeighting)
                        {
                            weightingType = WeightingType.Tracker;
                        }
                        else
                        {
                            if (IsVariableWaveBased(firstAndOnlyPlan.FilterMetricName))
                            {
                                weightingType = WeightingType.Tracker;
                            }
                            else
                            {
                                weightingType = WeightingType.Adhoc;
                            }
                            Traverse(subsetId, plans, ref weightingStyle);
                        }
                    }
                    break;

                    default:
                        weightingType = WeightingType.Adhoc;
                        weightingStyle |= WeightingStyle.RIM;
                        break;
                }
            }
            return new WeightingTypeStyle(weightingType, weightingStyle);
        }
    }
}

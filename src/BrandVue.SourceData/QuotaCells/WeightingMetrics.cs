using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Weightings;

namespace BrandVue.SourceData.QuotaCells;

/// <summary>
/// This class provides the internal structure for building quota cells with multi-phase weighting methods. 
/// </summary>
public class WeightingMetrics
{
    public IReadOnlyCollection<MetricEntityData> AllMeasureDependencies { get; }
    public IReadOnlyCollection<string> IntersectedMeasureDependencies { get; }

    public Func<QuotaCellFactory> CreateQuotaCellFactory { get; }

    public static WeightingMetrics CreateForPartialPlans(IMeasureRepository measureRepository, IEntityRepository entityRepository, Subset subset, List<WeightingFilterInstance> weightingFilterInstances, IEnumerable<WeightingPlan> weightingPlans)
    {
        return new WeightingMetrics(measureRepository, entityRepository, subset, weightingFilterInstances, weightingPlans, null);
    }

    public WeightingMetrics(IMeasureRepository measureRepository, IEntityRepository entityRepository,
        Subset subset, IEnumerable<WeightingPlan> weightingPlans = null, IResponseLevelQuotaCellLoader loader = null) : this(measureRepository, entityRepository, subset, new List<WeightingFilterInstance>(), weightingPlans ?? Enumerable.Empty<WeightingPlan>(), loader)
    {
    }
    

    private WeightingMetrics(IMeasureRepository measureRepository, IEntityRepository entityRepository,
        Subset subset, List<WeightingFilterInstance> weightingFilterInstances, IEnumerable<WeightingPlan> weightingPlans,
        IResponseLevelQuotaCellLoader loader)
    {
        var quotaCellTree = weightingPlans.ToQuotaCellTree();
        var selfAndDescendantPlans = quotaCellTree.GetSelfAndDescendantNodes();

        var distinctMeasureNames = weightingFilterInstances.Select(wfi => wfi.FilterMetricName)
            .Concat(selfAndDescendantPlans.Select(p => p.FilterMetricName))
            .Distinct().Where ( x=> !x.StartsWith(ResponseLevelQuotaCellLoader.GetMagicFieldName))
            .ToArray();

        var instancesLookup = weightingFilterInstances.ToDictionary(i => i.FilterMetricName, i => i.FilterInstanceId);


        AllMeasureDependencies = measureRepository
            .GetMany(distinctMeasureNames)
            .Select(m => GetMeasureEntityData(entityRepository, subset, m, instancesLookup))
            .ToArray();

        var metricNameChains = quotaCellTree.GetPlanChain().Select(c => c.Select(p => p.FilterMetricName));
        
        IntersectedMeasureDependencies = metricNameChains.Aggregate(
                Enumerable.Empty<string>(),
                (current, next) => current == null ? next : current.Intersect(next))
            .ToArray();

        CreateQuotaCellFactory = () => new QuotaCellFactory(subset, quotaCellTree, AllMeasureDependencies, loader, weightingPlans);
    }

    private static MetricEntityData GetMeasureEntityData(IEntityRepository entityRepository, Subset subset, Measure m,
        Dictionary<string, int?> instancesLookup)
    {
        var measureEntityData = MetricEntityData.From(subset, m, entityRepository);

        if (instancesLookup.ContainsKey(measureEntityData.Metric.Name))
        {
            measureEntityData = measureEntityData with
            {
                EntityInstances = entityRepository.GetInstances(measureEntityData.EntityType.Identifier,
                    instancesLookup[measureEntityData.Metric.Name].Value.Yield(), subset).ToArray()
            };
        }

        return measureEntityData;
    }
}
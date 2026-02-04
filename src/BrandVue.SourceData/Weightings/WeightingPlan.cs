using BrandVue.EntityFramework.MetaData.Weightings;

namespace BrandVue.SourceData.Weightings;
public sealed record WeightingFilterInstance(string FilterMetricName, int? FilterInstanceId);

public record ResponseWeighting(
    string FieldName,
    IDictionary<int, int> ResponseIdToQuotaCellId,
    IDictionary<int, decimal> QuotaCellIdToWeight);

public record WeightingPlan(
    string FilterMetricName,
    IReadOnlyCollection<WeightingTarget> Targets,
    bool IsWeightingGroupRoot,
    int? ExistingDatabaseId,
    ResponseWeighting ResponseLevelWeighting = null);

public record WeightingTarget(
    IReadOnlyCollection<WeightingPlan> Plans,
    int FilterMetricEntityId,
    decimal? Target,
    int? TargetPopulation,
    int? WeightingGroupId,
    int? ExistingDatabaseId,
    ResponseWeightingContext ResponseWeightingContext = null);
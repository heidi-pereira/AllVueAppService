using BrandVue.SourceData.Weightings;

namespace BrandVue.SourceData.QuotaCells;

/// <summary>
/// This class isn't thread safe, but is only used within the respondentrepository lock
/// </summary>
public class QuotaCellFactory
{
    private readonly QuotaCell _unweightedCell;
    private readonly QuotaCellNode _quotaCellNode;
    private readonly Dictionary<string, Func<IProfileResponseEntity, int?>> _dimensionCategoryFunctionByMeasureName;
    private List<IDictionary<int, int>> _dimensionCategoryInstanceLookups;
    private readonly List<QuotaCell> _allQuotaCells;

    public QuotaCellFactory(Subset subset, QuotaCellNode quotaCellNode, IReadOnlyCollection<MetricEntityData> allMeasureDependencies, IResponseLevelQuotaCellLoader loader, IEnumerable<WeightingPlan> weightingPlans)
    {
        _quotaCellNode = quotaCellNode;
        _unweightedCell = QuotaCell.UnweightedQuotaCell(subset);
        _dimensionCategoryFunctionByMeasureName = allMeasureDependencies.ToDictionary(
            measureEntityData => measureEntityData.Metric.Name, 
            measureEntityData => measureEntityData.CreateRespondentToDimensionCategoryFunction());
        _dimensionCategoryInstanceLookups = new List<IDictionary<int, int>>();

        _allQuotaCells = new List<QuotaCell>() { _unweightedCell };
        if (loader != null)
        {
            TraverseAndMutateForResponseLevelWeighting(loader, _quotaCellNode, null, weightingPlans);
        }
    }

    private Func<IProfileResponseEntity, int?> CreateRespondentToDimensionCategoryFunction(IDictionary<int, int> lookup)
    {
        _dimensionCategoryInstanceLookups.Add(lookup);
            return p =>
            {
                IDictionary<int, int> entityInstanceDict;

                try
                {
                    entityInstanceDict = _dimensionCategoryInstanceLookups.SingleOrDefault(d => d.Keys.Contains(p.Id));
                }
                catch(InvalidOperationException)
                {
                    throw new InvalidOperationException($"ProfileResponseEntity with Id {p.Id} in multiple lookups");
                }
                return (entityInstanceDict != null && entityInstanceDict.TryGetValue(p.Id, out var result)) ? result : null;
            };
    }

    private void TraverseAndMutateForResponseLevelWeighting(IResponseLevelQuotaCellLoader loader, QuotaCellNode node, WeightingTarget weightingPlanParentTarget, IEnumerable<WeightingPlan> weightingPlans)
    {
        var respondents = loader.GetPossibleResponseWeightings(weightingPlanParentTarget);

        if (respondents != null)
        {
            _dimensionCategoryFunctionByMeasureName[node.FilterMetricName] =
                CreateRespondentToDimensionCategoryFunction(respondents.ResponseIdToQuotaCellId);
            foreach (var child in node.Children)
            {
                switch (child.Value)
                {
                    case QuotaCellLeaf leaf: //Mutate this (YUK) so that others know later not to re-evaulate
                        leaf.IsResponseLevelWeighting = true;
                        break;
                }
            }
        }
        else
        {
            if (node != null && weightingPlans is not null)
            {
                foreach (var item in node.Children)
                {
                    var target = weightingPlans.First(p => p.FilterMetricName == node.FilterMetricName).Targets.FirstOrDefault(t => t.FilterMetricEntityId == item.Key);
                    if (target is not null)
                    {
                        Traverse(loader, item.Value, target, target.Plans?.ToList());
                    }
                }
            }
        }
    }

    private void TraverseAndMutateForResponseLevelWeighting(IResponseLevelQuotaCellLoader loader, QuotaCellLeaf node, WeightingTarget weightingPlanParentTarget)
    {
        var subsetRespondents = loader.GetPossibleResponseWeightings(weightingPlanParentTarget);

        if (subsetRespondents != null)
        {
            node.IsResponseLevelWeighting = true;
        }
    }

    private void Traverse(IResponseLevelQuotaCellLoader loader, QuotaCellTree child, WeightingTarget weightingPlanParentTarget, List<WeightingPlan> plans)
    {
        switch (child)
        {
            case QuotaCellLeaf leafNode:
                TraverseAndMutateForResponseLevelWeighting(loader, leafNode, weightingPlanParentTarget);
                break;
            case QuotaCellNode answerNode:
                TraverseAndMutateForResponseLevelWeighting(loader, answerNode, weightingPlanParentTarget, plans);
                break;
        }
    }

    public List<QuotaCellAllocationReason> QuotaCellAllocationReason(IProfileResponseEntity profileResponseEntity)
    {
        var partsForResponse = new List<QuotaCellAllocationReason> ();
        var currentNode = _quotaCellNode;
        List<string> filterMetricNamesAlreadyFound= new List<string> ();
        bool navigating = true;
        while (navigating && currentNode != null)
        {
            filterMetricNamesAlreadyFound.Add(currentNode.FilterMetricName);
            if (GetDimensionAnswerOrNull(profileResponseEntity, currentNode) is { } answerValue)
            {
                if (GetChildOrNull(answerValue, currentNode) is { } child)
                {
                    partsForResponse.Add(new QuotaCellAllocationReason(currentNode.FilterMetricName, answerValue, ""));
                    switch (child)
                    {
                        case QuotaCellNode answerNode:
                            currentNode = answerNode;
                            break;
                        case QuotaCellLeaf answerLeaf:
                            //PERF/MEM: Avoid this ToString()
                            navigating = false;
                            break;
                    }
                }
                else
                {
                    partsForResponse.Add(new QuotaCellAllocationReason(currentNode.FilterMetricName, answerValue, $"No weighting"));
                    navigating = false;
                }
            }
            else
            {
                partsForResponse.Add(new QuotaCellAllocationReason(currentNode.FilterMetricName, null, $"No data"));
                navigating= false;
            }
        }
        var missingCategoryFunctions = _dimensionCategoryFunctionByMeasureName.Select(kvp => kvp.Key).Where(name=> !filterMetricNamesAlreadyFound.Contains(name));
        partsForResponse.AddRange( missingCategoryFunctions.Select(name =>
        {
            var dimensionFunction = _dimensionCategoryFunctionByMeasureName[name];
            var value = dimensionFunction(profileResponseEntity);
            return new QuotaCellAllocationReason(name, value, value.HasValue ? "" : "No data, possibly OK");
        }));
        return partsForResponse;
    }

    public QuotaCell GetQuotaCell(IProfileResponseEntity profileResponseEntity)
    {
        if (_quotaCellNode is null) return _unweightedCell;

        var partsForResponse = new List<(string Dimension, int AnswerValue)>();
        var currentNode = _quotaCellNode;
        while (GetDimensionAnswerOrNull(profileResponseEntity, currentNode) is {} answerValue &&
               GetChildOrNull(answerValue, currentNode) is { } child)
        {
            partsForResponse.Add((currentNode.FilterMetricName, answerValue));
            switch (child)
            {
                case QuotaCellNode answerNode:
                    currentNode = answerNode;
                    break;
                case QuotaCellLeaf answerLeaf:
                    //PERF/MEM: Avoid this ToString()
                    return answerLeaf.ExistingQuotaCell ??= CreateQuotaCell(partsForResponse, answerLeaf);
            }
        }

        return _unweightedCell;
    }

    private QuotaCell CreateQuotaCell(List<(string Dimension, int AnswerValue)> partsForResponse, QuotaCellLeaf answerLeaf)
    {
        var quotaCell = new QuotaCell(_allQuotaCells.Count - 1, _unweightedCell.Subset,
            partsForResponse.ToDictionary(p => p.Dimension, p => p.AnswerValue.ToString()),
            answerLeaf.WeightingGroupId, answerLeaf.IsResponseLevelWeighting)
        {
            Index = _allQuotaCells.Count
        };
        _allQuotaCells.Add(quotaCell);
        return quotaCell;
    }

    private int? GetDimensionAnswerOrNull(IProfileResponseEntity profileResponseEntity, QuotaCellNode currentNode)
    {
        try
        {
            return _dimensionCategoryFunctionByMeasureName[currentNode.FilterMetricName](profileResponseEntity);
        }
        catch (Exception )
        {
            return null;
        }
    }

    private static QuotaCellTree GetChildOrNull(int? answerValue, QuotaCellNode currentNode)
    {
        if (answerValue.HasValue && currentNode.Children.TryGetValue(answerValue.Value, out var answerTree)) return answerTree;
        return null;
    }
}

public abstract record QuotaCellTree;
public record QuotaCellNode(string FilterMetricName, IReadOnlyDictionary<int, QuotaCellTree> Children) : QuotaCellTree;

public record QuotaCellLeaf(int? WeightingGroupId) : QuotaCellTree
{
    public bool IsResponseLevelWeighting { get; set; }
    public QuotaCell ExistingQuotaCell { get; set; }
};

public static class WeightingPlanExtensions
{
    public static QuotaCellNode ToQuotaCellTree(this IEnumerable<WeightingPlan> weightingPlans)
    {
        static QuotaCellTree TargetChildren(WeightingTarget t) => t.Plans == null ? new QuotaCellLeaf(t.WeightingGroupId) : ToQuotaCellTree(t.Plans);

        var list = weightingPlans.ToList();
        if (!list.Any()) return null;
        var first = list.First();
        var rest = list.Skip(1).ToList();

        var quotaCellTree = first.Targets.ToDictionary(
            t => t.FilterMetricEntityId,
            t => rest.Any() ? ToQuotaCellTree(rest) : TargetChildren(t)
        );
        return new QuotaCellNode(first.FilterMetricName, quotaCellTree);
    }
}

public static class QuotaCellNodeExtensions
{
    public static IEnumerable<QuotaCellNode> GetSelfAndDescendantNodes(this QuotaCellNode plan)
    {
        if (plan == null) return Enumerable.Empty<QuotaCellNode>();
        return plan.FollowMany(p => p.Children.Values.OfType<QuotaCellNode>());
    }

    public static IEnumerable<IEnumerable<QuotaCellNode>> GetPlanChain(this QuotaCellNode plan,
        IEnumerable<QuotaCellNode> ancestorPlans = null)
    {
        if (plan == null) return Enumerable.Empty<IEnumerable<QuotaCellNode>>();
        ancestorPlans ??= Array.Empty<QuotaCellNode>();
        return plan.Children.Values.OfType<QuotaCellNode>()
            .SelectMany(c => GetPlanChain(c, ancestorPlans.Concat(c.Yield())));
    }
}
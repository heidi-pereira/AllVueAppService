using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Utils;
using EntityType = BrandVue.SourceData.Entity.EntityType;

namespace BrandVue.SourceData.Calculation.Variables;

/// <summary>
/// Provide much faster singlecode lookup for specific variable configuration shapes
/// </summary>
internal class InstanceListVariable : IVariable<Numeric>
{
    private readonly Dictionary<int?[], int[]> _lookup;
    private readonly Variable<Numeric> _expressionVariable;
    private readonly IVariableInstance[] _orderedVariableDependencies;
    private static readonly IEqualityComparer<int?[]> ArraySequenceComparer = SequenceComparer<int?>.ForArray();
    private readonly int _maxAnswersPerInput;

    private InstanceListVariable(IReadOnlyCollection<(string Identifier, string EntityIdentifier)> orderedIdentifiers, Dictionary<int?[], int[]> lookup,
        Variable<Numeric> expressionVariable)
    {
        _lookup = lookup;
        _expressionVariable = expressionVariable;
        _maxAnswersPerInput = lookup.Max(kvp => kvp.Value.Length);
        var variableByIdentifier = _expressionVariable.VariableInstanceDependencies.ToDictionary(v => v.Identifier);
        _orderedVariableDependencies = orderedIdentifiers.Select(i => variableByIdentifier[i.Identifier]).ToArray();
    }

    public Func<IProfileResponseEntity, Numeric> CreateForEntityValues(EntityValueCombination entityValues)
    {
        return _expressionVariable.CreateForEntityValues(entityValues);
    }

    public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<Numeric, bool> valuePredicate)
    {
        var memoryPool = new ManagedMemoryPool<int>();
        //ALLOC Reuse array - the getters may themselves contain thread unsafe arrays, hence calling here rather than constructor
        var threadUnsafeGetters = _orderedVariableDependencies.Select(x => x.CreateForSingleEntity()).ToArray();
        var rawSubAnswers = new Memory<int>[_orderedVariableDependencies.Length];
        var answerCombination = new int?[_orderedVariableDependencies.Length];
        return profile =>
        {
            memoryPool.FreeAll();
            int combinations = 1;
            for (int i = 0; i < _orderedVariableDependencies.Length; i++)
            {
                var inputEntityIds = threadUnsafeGetters[i](profile);
                rawSubAnswers[i] = inputEntityIds;
                if (inputEntityIds.Length > 0) combinations *= inputEntityIds.Length;
            }
            var managedMemory = memoryPool.Rent(_maxAnswersPerInput * combinations);

            int answerIndex = WriteAnswerCombinationsToMemory(rawSubAnswers, 0, answerCombination, 0, managedMemory.Span, valuePredicate);

            return managedMemory.Take(answerIndex);
        };
    }

    /// <summary>
    /// Lookup the cartesian product of <paramref name="rawSubAnswers"/>, and write the transformed result to <paramref name="outputSpan"/>
    /// </summary>
    private int WriteAnswerCombinationsToMemory(Memory<int>[] rawSubAnswers, int rawSubAnswerIndex,
        int?[] answerCombination, int answerIndex, Span<int> outputSpan, Func<Numeric, bool> valuePredicate)
    {
        var currentRawSubAnswer = rawSubAnswers[rawSubAnswerIndex].Span;
        if (currentRawSubAnswer.Length == 0)
        {
            answerCombination[rawSubAnswerIndex] = null;
            answerIndex = WriteAnswerCombination(outputSpan);
        }
        else
        {
            foreach (int rawSubAnswer in currentRawSubAnswer)
            {
                answerCombination[rawSubAnswerIndex] = rawSubAnswer;
                answerIndex = WriteAnswerCombination(outputSpan);
            }
        }

        return answerIndex;

        int WriteAnswerCombination(Span<int> outputSpan)
        {
            if (rawSubAnswerIndex < rawSubAnswers.Length - 1)
            {
                answerIndex = WriteAnswerCombinationsToMemory(rawSubAnswers, rawSubAnswerIndex + 1, answerCombination, answerIndex, outputSpan, valuePredicate);
            }
            else if (_lookup.TryGetValue(answerCombination, out var entityIds))
            {
                foreach (int entityId in entityIds)
                {
                    //this should never return the same ID multiple times
                    if (valuePredicate(entityId) && !EntityIdAlreadyAdded(entityId, outputSpan))
                    {
                        outputSpan[answerIndex++] = entityId;
                    }
                }
            }

            return answerIndex;
        }

        bool EntityIdAlreadyAdded(int entityId, Span<int> outputSpan)
        {
            for (var i = 0; i < answerIndex; i++)
            {
                if (outputSpan[i] == entityId)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) => _expressionVariable.GetDatabaseOnlyDataTargets(subset);

    public IReadOnlyCollection<EntityType> UserEntityCombination => _expressionVariable.UserEntityCombination;
    public bool OnlyDimensionIsEntityType() => true;

    public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies => _expressionVariable.FieldDependencies;

    public static bool TryCreate(IEntityRepository entityRepository, GroupedVariableDefinition variableDefinition, Variable<Numeric> expressionVariable,
        out IVariable<Numeric> variable)
    {
        variable = default;

        // It's possible to extend the approach to multiple entities, but not necessary right now, so keep it simple
        if (expressionVariable.UserEntityCombination.Count != 1) return false;

        // In this situation, someone has conditions depending on multiple answers from the same variable.
        // Such a case is complex enough to deserve a totally separate optimised code path if it becomes common
        if (variableDefinition.Groups.Any(RequiresMultipleAnswers)) return false;

        // Can't handle nested expressions. The UI doesn't create them at the time of writing
        if (variableDefinition.Groups.Any(g => g.Component.GetDescendants().OfType<CompositeVariableComponent>().Count() > 1)) return false;

        // Multiple choice questions would cause issues if used in combination with InstanceVariableComponentOperator.Not
        if (variableDefinition.Groups.Any(g => g.Component.GetDescendants().OfType<InstanceListVariableComponent>().Any(c => c.Operator == InstanceVariableComponentOperator.Not)) &&
            expressionVariable.VariableInstanceDependencies.Any(HasMultiChoiceFieldDependancy))
        {
            return false;
        }

        var groups = variableDefinition.Groups
            .Select(g =>
            {
                var variableComponents = g.Component.GetDescendants(true).ToArray();
                var instanceListComponents = variableComponents.OfType<InstanceListVariableComponent>().ToArray();
                return (g.ToEntityInstanceId,
                    Separator: IsCompositeOr(g.Component)
                        ? CompositeVariableSeparator.Or
                        : CompositeVariableSeparator.And,
                    LeafComponents: instanceListComponents,
                    HasOnlyInstanceListLeaves: variableComponents.Length == instanceListComponents.Length);
            }).ToArray();

        // Future: Could support others, but would need to set up getters to get the value rather than entity id
        if (groups.Any(g => !g.HasOnlyInstanceListLeaves)) return false;

        (string VariableIdentifier, string EntityTypeName)[] indexedIdentifiers = groups.SelectMany(g => g.LeafComponents,
                (_, l) => (l.FromVariableIdentifier, l.FromEntityTypeName)).Distinct().OrderBy(x => x).ToArray();

        var indexedEntityInstances = indexedIdentifiers.Select(ids => entityRepository.GetSubsetUnionedInstanceIdsOf(ids.EntityTypeName).Select<int, int?>(x => x).Concat(new[]{default(int?)}).ToArray()).ToArray();

        // PERF guard: Bail out if we're going to create a very large lookup. This is just pre-emptive in case someone uses a variable referencing many fields with lots of entities.
        // Remove/increase limit if you have a real case like this that seems fine.
        if (groups.Count(g => g.Separator == CompositeVariableSeparator.Or)
            * indexedEntityInstances.Aggregate(1, (acc, c) => acc * c.Length) > 1_000_000)
        {
            return false;
        }

        var inputToOutputMappings = groups.Select(g => (Combos: GetLookups(g.Separator, g.LeafComponents), g.ToEntityInstanceId));
        var combinationsForGroup = inputToOutputMappings.SelectMany(combos => combos.Combos.SelectMany(EnumerableExtensions.CartesianProduct), (g, c) => (EntityInstanceId: g.ToEntityInstanceId, Combo: c))
            .ToLookup(g => g.Combo, g => g.EntityInstanceId, ArraySequenceComparer);

        variable = new InstanceListVariable(indexedIdentifiers, combinationsForGroup.ToDictionary(g => g.Key, g => g.Distinct().ToArray(), ArraySequenceComparer), expressionVariable);
        return true;

        int?[][][] GetLookups(CompositeVariableSeparator separator, InstanceListVariableComponent[] leafComponents)
        {
            var componentByVariableIdentifier = leafComponents.ToLookup(c => c.FromVariableIdentifier);

            switch (separator)
            {
                // For AND:
                // Get all instances for each entity
                // instanceIdsForField1 x instanceIdsForField2 x ... x instanceIdsForFieldN
                // Should include a none instance too in the "all" collection
                case CompositeVariableSeparator.And:
                    var indexedInstances = indexedIdentifiers.Select((id, i) =>
                        componentByVariableIdentifier.Contains(id.VariableIdentifier)
                            ? GetInstanceIds(componentByVariableIdentifier[id.VariableIdentifier], i)
                            : indexedEntityInstances[i]
                    ).ToArray();
                    return new[] { indexedInstances };

                // For OR:
                // Get allowed instances for each field combined with every other possible entry for the other fields
                // instanceIdsForField1 x allEntitiesForField2 x ... x allEntitiesForFieldN
                // allEntitiesForField1 x instanceIdsForField2 x ... x allEntitiesForFieldN
                // ...
                // allEntitiesForField1 x allEntitiesForField2 x ... x instanceIdsForFieldN
                // Should include a none instance too in the "all" collection
                case CompositeVariableSeparator.Or:
                    return componentByVariableIdentifier.Select(kvp =>
                        indexedIdentifiers.Select((id, i) => id.VariableIdentifier == kvp.Key
                            ? GetInstanceIds(kvp, i)
                            : indexedEntityInstances[i]
                        ).ToArray()
                    ).ToArray();
                default:
                    throw new ArgumentOutOfRangeException(nameof(separator), separator, null);
            }
        }


        int?[] GetInstanceIds(IEnumerable<InstanceListVariableComponent> valueTuples, int index)
        {
            return valueTuples.SelectMany(x =>
            {
                var instanceIds = x.InstanceIds.Select<int, int?>(y => y);
                return x.Operator switch
                {
                    InstanceVariableComponentOperator.Not => indexedEntityInstances[index].Except(instanceIds),
                    _ => instanceIds
                };
            }).ToArray();
        }
    }

    private static bool IsCompositeOr(VariableComponent component)
    {
        return component is CompositeVariableComponent { CompositeVariableSeparator: CompositeVariableSeparator.Or };
    }

    private static bool RequiresMultipleAnswers(VariableGrouping group)
    {
        bool requiresMultipleAnswers = group.Component.GetDescendants().Any(RequiresMultipleAnswers);
        return requiresMultipleAnswers || SameVariableReferencedTwice(group);
    }

    private static bool RequiresMultipleAnswers(VariableComponent x)
    {
        return x is InstanceListVariableComponent
        {
            Operator: InstanceVariableComponentOperator.And, InstanceIds.Count: > 1
        };
    }

    private static bool HasMultiChoiceFieldDependancy(IVariableInstance x)
    {
        return x.FieldDependencies.Any(field => field.IsMultiChoiceForAnySubsets());
    }

    private static bool SameVariableReferencedTwice(VariableGrouping group)
    {
        string[] variableIdentifiers = group.Component.GetDescendants().OfType<InstanceListVariableComponent>().Select(l => l.FromVariableIdentifier).ToArray();
        return variableIdentifiers.Distinct().Count() < variableIdentifiers.Count();
    }
}
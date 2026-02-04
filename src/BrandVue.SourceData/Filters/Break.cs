using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Filters;

public class Break
{
    private readonly Func<IProfileResponseEntity, Memory<int>> _getVariableValue;
    private readonly Dictionary<int, int> _instanceIndexFromVariableValue;
    private readonly ManagedMemoryPool<int> _memoryPool;
    private Dictionary<int,int[]> _lastProfileLookup = new();
    private readonly Func<IProfileResponseEntity, Memory<int>, Memory<int>> _filterByBaseEntities;
    private readonly Func<IProfileResponseEntity, bool> _simpleBaseCheck;
    public IVariable<int?> Variable { get; }
    public IVariable<bool> BaseVariable { get; }
    public int[] Instances { get; }
    public int[] BaseInstances { get; }
    public Break[] ChildBreak { get; }
    public int ResultStartIndex { get; }

    public Break(IVariable<int?> variable, IVariable<bool> baseVariable, int[] instances, int[] baseInstances, Break[] childBreak, int resultIndexOffset)
    {
        Variable = variable;
        BaseVariable = baseVariable;
        Instances = instances;
        BaseInstances = baseInstances;
        ChildBreak = childBreak;
        ResultStartIndex = resultIndexOffset;
        _getVariableValue = Variable.CreateForSingleEntity(_ => true);
        _instanceIndexFromVariableValue = Instances.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => ResultStartIndex + x.index);
        _memoryPool = new ManagedMemoryPool<int>();
        if (BaseVariable.UserEntityCombination.Any())
        {
            // Entity in the main variable: include the respondent in the result indexes for the intersection of base and main
            var getAllValues = BaseVariable.CreateForSingleEntity(_ => true);
            _filterByBaseEntities = (p, mainValues) =>
            {
                var baseValues = getAllValues(p);
                return Intersection(baseValues.Span, mainValues.Span);
            };
            _simpleBaseCheck = _ => true;
        }
        else
        {
            // the variable expression has no entity: If we return anything at all, include it
            _simpleBaseCheck = BaseVariable.CreateForEntityValues(default);
            _filterByBaseEntities = (_, entityIds) => entityIds;
        }
    }

    /// <summary>
    /// Not thread safe, use DeepClone at the point of parallelising this
    /// Caches most recent profile's indexes
    /// </summary>
    public Span<int> GetInstanceIndexes(IProfileResponseEntity p)
    {
        if (_lastProfileLookup.TryGetValue(p.Id, out var cachedMemory))
        {
            return cachedMemory;
        }

        Memory<int> memory = Memory<int>.Empty;

        if (_simpleBaseCheck(p))
        {
            _memoryPool.FreeAll();
            var originalAnswers = _getVariableValue(p);
            var answers = _filterByBaseEntities(p, originalAnswers).Span;
            var instanceIndexesMemory = _memoryPool.Rent(answers.Length);
            var instanceIndexesSpan = instanceIndexesMemory.Span;
            int instanceIndexesCount = 0;

            foreach (int answerValue in answers)
            {
                if (_instanceIndexFromVariableValue.TryGetValue(answerValue, out var index))
                {
                    instanceIndexesSpan[instanceIndexesCount++] = index;
                }
            }

            memory = instanceIndexesMemory.Take(instanceIndexesCount);

        }

        _lastProfileLookup[p.Id] = memory.ToArray();
        return memory.Span;
    }

    private Memory<int> Intersection(Span<int> input1, Span<int> input2)
    {
        int n = input1.Length;
        int m = input2.Length;
        int index1 = 0;
        int index2 = 0;
        int outputIndex = 0;
        input1.Sort();
        input2.Sort();
        var managedMemory = _memoryPool.Rent(Math.Min(input1.Length, input2.Length));
        var output = managedMemory.Span;
        while (index1 < n && index2 < m)
        {
            if (input1[index1] == input2[index2])
            {
                output[outputIndex] = input1[index1];
                outputIndex++;
                index1++;
                index2++;
            }
            else if (input1[index1] < input2[index2])
            {
                index1++;
            }
            else
            {
                index2++;
            }
        }
        return managedMemory.Take(outputIndex);
    }

    /// <summary>
    /// Use this when you need to a separate version to use on another thread in parallel
    /// </summary>
    public Break DeepClone() => new(Variable, BaseVariable, Instances, BaseInstances, ChildBreak.Select(c => c.DeepClone()).ToArray(), ResultStartIndex);
}
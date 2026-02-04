using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Variable;

public class BooleanFromValueVariable : VariableDecoratorBase<int?>, IVariable<bool>
{
    private readonly Func<int?, bool> _predicate;

    public BooleanFromValueVariable(IVariable<int?> variableImplementation, Func<int?, bool> predicate) : base(variableImplementation)
        => _predicate = predicate;

    public bool OnlyDimensionIsEntityType() => _variableImplementation.UserEntityCombination.Count == 1;

    public Func<IProfileResponseEntity, bool> CreateForEntityValues(EntityValueCombination entityValues)
    {
        var getInt = _variableImplementation.CreateForEntityValues(entityValues);
        return p => _predicate(getInt(p));
    }

    public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<bool, bool> valuePredicate)
    {
        Func<int?, bool> fullPredicate = v => valuePredicate(v.HasValue && v != 0) && _predicate(v);
        return _variableImplementation.CreateForSingleEntity(fullPredicate);
    }
}
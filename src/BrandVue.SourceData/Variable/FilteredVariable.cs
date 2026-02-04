using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Variable;

public class FilteredVariable : VariableDecoratorBase<int?>, IVariable<int?>
{
    private readonly Func<int?, bool> _predicate;

    public FilteredVariable(IVariable<int?> variableImplementation, Func<int?, bool> predicate) : base(variableImplementation)
    {
        _predicate = predicate;
    }

    public bool OnlyDimensionIsEntityType() => _variableImplementation.UserEntityCombination.Count == 1;

    public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
    {
        var getInt = _variableImplementation.CreateForEntityValues(entityValues);
        return p =>
        {
            int? val = getInt(p);
            return _predicate(val) ? val : null;
        };
    }

    public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
    {
        Func<int?, bool> fullPredicate = p => valuePredicate(p) && _predicate(p);
        return _variableImplementation.CreateForSingleEntity(fullPredicate);
    }
}
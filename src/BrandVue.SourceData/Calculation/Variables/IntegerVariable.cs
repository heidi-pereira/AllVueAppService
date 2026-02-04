using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Variable;

namespace BrandVue.SourceData.Calculation.Variables;

internal class IntegerVariable : VariableDecoratorBase<Numeric>, IVariable<int?>
{
    internal static IVariable<int?> Create(IVariable<Numeric> variableImplementation)
    {
        return variableImplementation switch
        {
            // ReSharper disable once SuspiciousTypeConversion.Global - future proofing
            IVariable<int?> alreadyInt => alreadyInt,
            NumericVariable numeric => numeric.WrappedVariable,
            _ => new IntegerVariable(variableImplementation)
        };
    }

    private IntegerVariable(IVariable<Numeric> variableImplementation):base(variableImplementation) { }
    public bool OnlyDimensionIsEntityType() => WrappedVariable.OnlyDimensionIsEntityType();

    public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
    {
        var calculateValue = WrappedVariable.CreateForEntityValues(entityValues);
        return p => calculateValue(p).AsNullable();
    }

    public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
    {
        Func<Numeric, bool> fullPredicate = v => valuePredicate(v.AsNullable());
        return WrappedVariable.CreateForSingleEntity(fullPredicate);
    }
}
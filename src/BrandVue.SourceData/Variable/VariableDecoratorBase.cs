using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Variable;

public abstract class VariableDecoratorBase<T> : IDecoratedVariable
{
    public IVariable<T> WrappedVariable => _variableImplementation;
    public IVariable DecoratedVariable => _variableImplementation;
    protected readonly IVariable<T> _variableImplementation;

    protected VariableDecoratorBase(IVariable<T> variableImplementation) => _variableImplementation = variableImplementation;

    public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies => _variableImplementation?.FieldDependencies;
    public IReadOnlyCollection<EntityType> UserEntityCombination => _variableImplementation?.UserEntityCombination;
    public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) => _variableImplementation?.GetDatabaseOnlyDataTargets(subset);
    public int? ConstantValue => _variableImplementation?.ConstantValue;
}
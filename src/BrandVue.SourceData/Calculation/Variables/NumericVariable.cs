using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Variable;

namespace BrandVue.SourceData.Calculation.Variables
{
    internal class NumericVariable : VariableDecoratorBase<int?>, IVariable<Numeric>
    {

        private NumericVariable(IVariable<int?> variableImplementation) : base(variableImplementation) { }

        public static IVariable<Numeric> Create(IVariable<int?> variableImplementation) => variableImplementation switch
        {
            // ReSharper disable once SuspiciousTypeConversion.Global - future proofing
            IVariable<Numeric> alreadyNumeric => alreadyNumeric,
            IntegerVariable iv => iv.WrappedVariable,
            _ => new NumericVariable(variableImplementation)
        };

        public bool OnlyDimensionIsEntityType() => _variableImplementation.OnlyDimensionIsEntityType();

        public Func<IProfileResponseEntity, Numeric> CreateForEntityValues(EntityValueCombination entityValues)
        {
            var getValue = _variableImplementation.CreateForEntityValues(entityValues);
            return p => getValue(p);
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<Numeric, bool> valuePredicate)
        {
            Func<int?, bool> fullPredicate = v => valuePredicate(new Numeric(v));
            return _variableImplementation.CreateForSingleEntity(fullPredicate);
        }
    }
}
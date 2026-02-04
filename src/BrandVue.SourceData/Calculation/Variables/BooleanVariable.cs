using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Variable;

namespace BrandVue.SourceData.Calculation.Variables
{
    internal class BooleanVariable : VariableDecoratorBase<Numeric>, IVariable<bool>
    {
        internal static IVariable<bool> Create(EvaluatedVariableInstance evaluatedVariableInstance) =>
            new BooleanVariable(evaluatedVariableInstance.Variable);

        private BooleanVariable(IVariable<Numeric> variableImplementation):base(variableImplementation) { }
        
        /// <summary>
        /// For display purposes. Do not parse this, if you need the parsed version, get the stored version from FieldExpressionParser.
        /// </summary>
        public string DisplayExpressionString =>
            WrappedVariable is Variable<Numeric> { DisplayExpressionString: { } str } ? str : null;

        public bool OnlyDimensionIsEntityType() => WrappedVariable.OnlyDimensionIsEntityType();

        public Func<IProfileResponseEntity, bool> CreateForEntityValues(EntityValueCombination entityValues)
        {
            var calculateValue = WrappedVariable.CreateForEntityValues(entityValues);
            return p => calculateValue(p).HasValue;
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<bool, bool> valuePredicate)
        {
            Func<Numeric, bool> fullPredicate = v => valuePredicate(v.IsTruthy);
            return WrappedVariable.CreateForSingleEntity(fullPredicate);
        }

        public static IVariable<bool> Create(IVariable<int?> evaluatedVariableInstance)
        {
            return new BooleanVariable(NumericVariable.Create(evaluatedVariableInstance));
        }
    }
}
using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Variable
{
    interface IDecoratedVariable
    {
        IVariable DecoratedVariable { get; }
    }
}
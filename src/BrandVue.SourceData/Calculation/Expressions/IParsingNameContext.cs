using IronPython.Compiler.Ast;

namespace BrandVue.SourceData.Calculation.Expressions
{
    internal interface IParsingNameContext
    {
        EntitiesReducer<Numeric> ParseName(NameExpression ne);
        EntitiesReducer<Memory<Numeric>> ParseCallMemberExpression(MemberExpression memberExpression,
            IReadOnlyCollection<ParsedArg> parsedArgs);

        EntitiesReducer<Numeric> ParseMember(MemberExpression me);
    }
}
using Py = IronPython.Compiler.Ast;

namespace BrandVue.SourceData.Calculation.Expressions
{
    internal class LambdaAwareContext : IParsingNameContext
    {
        private readonly IParsingNameContext _wrappedContext;
        private readonly Stack<string> _parseTimeArgNames = new Stack<string>();

        public LambdaAwareContext(IParsingNameContext wrappedContext)
        {
            _wrappedContext = wrappedContext;
        }

        public EntitiesReducer<Numeric> ParseName(Py.NameExpression ne)
        {
            switch (ne.Name)
            {
                case "False": return new(0);
                case "True": return new(1);
            }
            var containsArg = _parseTimeArgNames.Contains(ne.Name);
            if (containsArg)
            {
                if (_parseTimeArgNames.Count == 1) return new RespondentReducer<Numeric>(r => r.Arg0);
                throw ne.NotSupported("Only single argument can be in scope at a time");
            }

            return _wrappedContext.ParseName(ne);
        }

        public EntitiesReducer<Memory<Numeric>> ParseCallMemberExpression(Py.MemberExpression memberExpression,
            IReadOnlyCollection<ParsedArg> parsedArgs) =>
            _wrappedContext.ParseCallMemberExpression(memberExpression, parsedArgs);

        public EntitiesReducer<Numeric> ParseMember(Py.MemberExpression me) =>
            _wrappedContext.ParseMember(me);

        public void Push(string varName) => _parseTimeArgNames.Push(varName);

        public void Pop() => _parseTimeArgNames.Pop();
    }
}
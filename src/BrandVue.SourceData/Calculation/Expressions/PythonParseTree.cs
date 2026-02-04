using IronPython;
using IronPython.Compiler;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
using System.IO;
using IronPython.Compiler.Ast;

namespace BrandVue.SourceData.Calculation.Expressions;

internal static class PythonParseTree
{
    private static readonly ScriptEngine Engine = Python.CreateEngine();

    public static Expression CreateFrom(string pythonExpression)
    {
        pythonExpression = pythonExpression.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        var langContext = HostingHelpers.GetLanguageContext(Engine);
        var scope = new Scope();
        var compilerOptions = langContext.GetCompilerOptions(scope);
        var source =
            HostingHelpers.GetSourceUnit(
                Engine.CreateScriptSourceFromString(pythonExpression, SourceCodeKind.Expression));
        var parser = Parser.CreateParser(new CompilerContext(source, compilerOptions, ErrorSink.Default),
            new PythonOptions());
        var parsed = parser.ParseTopExpression();
        if (parsed.Body is ReturnStatement ret)
        {
            var retExpression = ret.Expression;
            DepthCheckerWalker.ThrowIfExceeds(retExpression, 600);
            return retExpression;
        }

        throw parsed.Body.NotSupported($"Expression cannot be of type {parsed.Type}");
    }
    private class DepthCheckerWalker : IronPython.Compiler.Ast.PythonWalker
    {
        private readonly DepthChecker _depthChecker;

        private DepthCheckerWalker(int maxNodes) => _depthChecker = new DepthChecker(maxNodes);

        private class DepthChecker
        {
            private readonly int _maxNodes;
            private int _depth = 1;

            public DepthChecker(int maxNodes) => _maxNodes = maxNodes;

            public bool Walk(Expression node)
            {
                if (_depth++ >= _maxNodes)
                    throw new InvalidDataException($"Expression with {_depth} nodes is too large");
                return true;
            }

            public void PostWalk(Expression node) => _depth--;

        }

        public static void ThrowIfExceeds(Expression node, int maxNodes)
        {
            var walker = new DepthCheckerWalker(maxNodes);
            node.Walk(walker);
        }

        #region Delegating overrides for all expressions
        public override bool Walk(AndExpression node) => _depthChecker.Walk(node);
        public override bool Walk(BackQuoteExpression node) => _depthChecker.Walk(node);
        public override bool Walk(BinaryExpression node) => _depthChecker.Walk(node);
        public override bool Walk(CallExpression node) => _depthChecker.Walk(node);
        public override bool Walk(ConditionalExpression node) => _depthChecker.Walk(node);
        public override bool Walk(ConstantExpression node) => _depthChecker.Walk(node);
        public override bool Walk(DictionaryComprehension node) => _depthChecker.Walk(node);
        public override bool Walk(DictionaryExpression node) => _depthChecker.Walk(node);
        public override bool Walk(ErrorExpression node) => _depthChecker.Walk(node);
        public override bool Walk(GeneratorExpression node) => _depthChecker.Walk(node);
        public override bool Walk(IndexExpression node) => _depthChecker.Walk(node);
        public override bool Walk(LambdaExpression node) => _depthChecker.Walk(node);
        public override bool Walk(ListComprehension node) => _depthChecker.Walk(node);
        public override bool Walk(ListExpression node) => _depthChecker.Walk(node);
        public override bool Walk(MemberExpression node) => _depthChecker.Walk(node);
        public override bool Walk(NameExpression node) => _depthChecker.Walk(node);
        public override bool Walk(OrExpression node) => _depthChecker.Walk(node);
        public override bool Walk(ParenthesisExpression node) => _depthChecker.Walk(node);
        public override bool Walk(SetComprehension node) => _depthChecker.Walk(node);
        public override bool Walk(SetExpression node) => _depthChecker.Walk(node);
        public override bool Walk(SliceExpression node) => _depthChecker.Walk(node);
        public override bool Walk(TupleExpression node) => _depthChecker.Walk(node);
        public override bool Walk(UnaryExpression node) => _depthChecker.Walk(node);
        public override bool Walk(YieldExpression node) => _depthChecker.Walk(node);
        public override void PostWalk(AndExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(BackQuoteExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(BinaryExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(CallExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(ConditionalExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(ConstantExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(DictionaryComprehension node) => _depthChecker.PostWalk(node);
        public override void PostWalk(DictionaryExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(ErrorExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(GeneratorExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(IndexExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(LambdaExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(ListComprehension node) => _depthChecker.PostWalk(node);
        public override void PostWalk(ListExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(MemberExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(NameExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(OrExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(ParenthesisExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(SetComprehension node) => _depthChecker.PostWalk(node);
        public override void PostWalk(SetExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(SliceExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(TupleExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(UnaryExpression node) => _depthChecker.PostWalk(node);
        public override void PostWalk(YieldExpression node) => _depthChecker.PostWalk(node);
        #endregion
    }
}
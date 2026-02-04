using System.Text;
using Py = IronPython.Compiler.Ast;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// There are two almost identical syntax structures to make an enumerable.
    /// This helper just pulls out the common parts of them.
    /// </summary>
    internal static class PythonExpressionEnumerablePartsExtensions
    {
        public static (Py.NameExpression Variable, Py.Expression Input, Py.Expression Output, Py.Expression
            OptionalCondition)
            GetEnumerableParts(this Py.GeneratorExpression generatorExpression)
        {
            if (generatorExpression.Function.Body is Py.ForStatement forStatement &&
                forStatement.Left is Py.NameExpression varName)
            {

                Py.Expression optionalCondition = null;
                if (forStatement.Body is Py.IfStatement ifStatement &&
                    ifStatement.Tests.Single() is Py.IfStatementTest ifStatementTest &&
                    ifStatementTest.Body is Py.ExpressionStatement expressionStatement &&
                    expressionStatement.Expression is Py.YieldExpression yieldExpression)
                {
                    optionalCondition = ifStatementTest.Test;
                }
                else if (forStatement.Body is Py.ExpressionStatement bodyExpressionStatement
                         && bodyExpressionStatement.Expression is Py.YieldExpression bodyYieldExpression)
                {
                    yieldExpression = bodyYieldExpression;
                }
                else
                {
                    throw forStatement.NotSupported("Could not find yield expression");
                }

                return (Variable: varName, Input: generatorExpression.Iterable, Output: yieldExpression.Expression,
                    OptionalCondition: optionalCondition);
            }

            throw generatorExpression.NotSupported("Generator expression not supported");
        }

        public static (Py.NameExpression Variable, Py.Expression Input, Py.Expression Output, Py.Expression OptionalCondition)
            GetEnumerableParts(this Py.ListComprehension listComprehension)
        {
            if (listComprehension.Iterators.First() is Py.ComprehensionFor forExp &&
                forExp.Left is Py.NameExpression forVar)
            {
                var optionalCondition =
                    listComprehension.Iterators.ElementAtOrDefault(1) is Py.ComprehensionIf c ? c.Test : null;
                return (Variable: forVar, Input: forExp.List, Output: listComprehension.Item,
                    OptionalCondition: optionalCondition);
            }

            throw listComprehension.NotSupported("Could not parse list comprehension");
        }
    }
}
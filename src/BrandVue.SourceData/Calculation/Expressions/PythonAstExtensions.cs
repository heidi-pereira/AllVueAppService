using System.Reflection;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using Microsoft.Scripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#nullable enable

namespace BrandVue.SourceData.Calculation.Expressions
{
    public record MethodCall(
        string Name,
        Expression? Instance,
        IReadOnlyList<Expression> PositionalArgs,
        IReadOnlyDictionary<string, Expression> KeywordArgs)
    {
        public Expression? GetArgOrNull(int index)
        {
            UnusedPositionalArgs.Remove(index);
            return PositionalArgs.ElementAtOrDefault(index);
        }
        public Expression? GetArgOrNull(string keywordArgName)
        {
            UnusedKeywordArgs.Remove(keywordArgName);
            return KeywordArgs.GetValueOrDefault(keywordArgName);
        }

        public void ThrowForUnusedArgs()
        {
            if (UnusedPositionalArgs.Any() || UnusedKeywordArgs.Any())
            {
                throw new SyntaxErrorException(
                    $"Unexpected args to {Name}: " +
                    $"{string.Join(", ", UnusedPositionalArgs.Select(x => x.ToString()).Concat(UnusedKeywordArgs))}"
                );
            }
        }
        private IReadOnlyList<Expression> PositionalArgs { get; init; } = PositionalArgs;
        private HashSet<int> UnusedPositionalArgs { get; init; } = PositionalArgs.Select((_, i) => i).ToHashSet();
        private IReadOnlyDictionary<string, Expression> KeywordArgs { get; init; } = KeywordArgs;
        private HashSet<string> UnusedKeywordArgs { get; init; } = KeywordArgs.Keys.ToHashSet();
        
    }

    internal static class PythonAstExtensions
    {
        public static Expression SkipParens(this Expression expression)
        {
            while (expression is ParenthesisExpression parenExpression) expression = parenExpression.Expression;
            return expression;
        }

        public static MethodCall GetMethodCall(this CallExpression callExpression)
        {
            var positionalArgs = callExpression.Args.TakeWhile(a => a.Name is null).Select(a => a.Expression).ToArray();
            var keywordArgs = callExpression.Args.SkipWhile(a => a.Name is null).ToDictionary(a => a.Name, a => a.Expression);
            return callExpression.Target switch
            {
                NameExpression nameExpression => new(nameExpression.Name, null, positionalArgs, keywordArgs),
                MemberExpression memberExpression => new(memberExpression.Name, memberExpression.Target, positionalArgs, keywordArgs),
                _ => throw new SyntaxErrorException($"Unrecognised method style at {callExpression.Span}")
            };
        }

        public static bool IsName(this Expression potentialNameExpression, string name)
        {
            return potentialNameExpression is NameExpression memberName && memberName.Name == name;
        }

        public static Func<Numeric, Numeric, Numeric> GetBinaryOperator(this PythonOperator pyOp)
        {
            // In Python, Equal and NotEqual can be used on `None` (i.e. null in our context). All other binary operators error
            switch (pyOp)
            {
                case PythonOperator.Equal:
                case PythonOperator.Is:
                    return (l, r) => l == r;
                case PythonOperator.NotEqual:
                case PythonOperator.IsNot:
                    return (l, r) => l != r;

                case PythonOperator.Add:
                    return (l, r) => (int) l + (int) r;
                case PythonOperator.Subtract:
                    return (l, r) => (int) l - (int) r;
                case PythonOperator.Multiply:
                    return (l, r) => (int) l * (int) r;
                case PythonOperator.Divide:
                    return (l, r) => (int) l / (int) r;
                case PythonOperator.TrueDivide:
                    return (l, r) => (int) l / (int) r;
                case PythonOperator.FloorDivide:
                    return (l, r) => (int) l / (int) r;
                case PythonOperator.Mod:
                    return (l, r) => (int) l % (int) r;
                case PythonOperator.BitwiseAnd:
                    return (l, r) => (int) l & (int) r;
                case PythonOperator.BitwiseOr:
                    return (l, r) => (int) l | (int) r;
                case PythonOperator.Xor:
                    return (l, r) => (int) l ^ (int) r;
                case PythonOperator.LeftShift:
                    return (l, r) => (int) l << (int) r;
                case PythonOperator.RightShift:
                    return (l, r) => (int) l >> (int) r;
                case PythonOperator.Power:
                    return (l, r) => (Numeric) Math.Pow((double) l, (double) r); // `None ** 3` and `3 ** None` both throw
                case PythonOperator.LessThan:
                    return (l, r) => (int) l < (int) r;
                case PythonOperator.LessThanOrEqual:
                    return (l, r) => (int) l <= (int) r;
                case PythonOperator.GreaterThan:
                    return (l, r) => (int) l > (int) r;
                case PythonOperator.GreaterThanOrEqual:
                    return (l, r) => (int) l >= (int) r;
                default:
                    throw new NotSupportedException($"Operator {pyOp} not supported in this context");
            }
        }

        public static Func<Numeric, Numeric> GetUnaryOperator(this PythonOperator pyOp)
        {
            switch (pyOp)
            {
                case PythonOperator.Not:
                    return x => !x.IsTruthy;
                case PythonOperator.Pos:
                    return x => x;
                case PythonOperator.Invert:
                    return x => ~ (int)x;
                case PythonOperator.Negate:
                    return x => - (int)x;
            }

            throw new NotSupportedException($"Operator {pyOp} not supported in this context");
        }

        public static Exception NotSupported(this Node expression, string message)
        {
            return new NotSupportedException(message + Environment.NewLine +
                                             $"in {expression.NodeName} from character {expression.Span.Start.Index} to {expression.Span.End.Index}"
            );
        }

        private class AvoidVerboseTreesContractResolver : DefaultContractResolver
        {
            public static AvoidVerboseTreesContractResolver Instance { get; } = new AvoidVerboseTreesContractResolver();

            private static readonly string[] Blacklist =
                {"Parent", "Type", "IndexSpan", "Start", "End", "StartIndex", "EndIndex", "Span", "CanReduce"};

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);

                if (property.DeclaringType == typeof(Node) && Blacklist.Contains(property.PropertyName))
                {
                    property.ShouldSerialize = instance => false;
                }

                return property;
            }
        }
    }
}
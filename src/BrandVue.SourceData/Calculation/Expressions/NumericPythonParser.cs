using System.Collections.Frozen;
using IronPython.Compiler;
using Py = IronPython.Compiler.Ast;
using static BrandVue.SourceData.Calculation.Expressions.ProfileValueFactoryHelpers;
using Microsoft.Scripting;
using System.IO;

namespace BrandVue.SourceData.Calculation.Expressions;

/// <summary>
/// Computes arbitrary deterministic expressions for a subset of python. Delegates to parsing context for all knowledge of inputs.
/// So it doesn't know about respondents, responses, entities etc, but it knows how to create a lambda for things like "len([x for x in [1,2,3] if x > 2])"
/// </summary>
/// <remarks>
/// Why not just execute with IronPython directly?
/// * This is intended to be more performant when re-executing the same expression many times (i.e. per respondent) than the IronPython compiled code could be without the inside knowledge we have.
/// * Python injection. Users have some control over the python. Rather than meticulously avoid them doing a huge range of bad things to our server, we whitelist the small set of things they are allowed to do.
/// </remarks>
internal class NumericPythonParser
{
    private const int MaxNumericRecursionDepth = 400;
    private readonly LambdaAwareContext _parsingNameContext;

    public NumericPythonParser(IParsingNameContext parsingNameContext) => _parsingNameContext = new LambdaAwareContext(parsingNameContext);

    public EntitiesReducer<Numeric> ParseNumericExpression(string expressionString) =>
        CreateNumeric(PythonParseTree.CreateFrom(expressionString));

    private EntitiesReducer<Numeric> CreateNumeric(Py.Expression expression)
    {
        EntitiesReducer<Numeric>? numeric;
        try
        {
            numeric = CreateNumericOrNull(expression);
        }
        catch (InvalidDataException e)
        {
            throw new SyntaxErrorException($"Expression is too complex ({e.Message})");
        }
        
        if (numeric is { } n) return n;

        try
        {
            CreateEnumerable(expression);
        }
        catch
        {
            throw expression.NotSupported("Unsupported numeric expression type");
        }
        throw expression.NotSupported("This part of the expression returns a list but should return a single number. Use a list function such as 'max' or 'len'.");
    }

    private ICurriedReducer CreateNumericOrEnumerable(Py.Arg a) =>
        (ICurriedReducer)CreateNumericOrNull(a.Expression) ?? CreateEnumerable(a.Expression);

    /// <remarks>Do not call CreateNumeric, or other methods which could throw if the expression was actually of a different type</remarks>
    private EntitiesReducer<Numeric>? CreateNumericOrNull(Py.Expression expression, int depth = 0)
    {
        if (depth > MaxNumericRecursionDepth)
            throw new InvalidDataException($"Numeric expression is too complex (recursion depth > {MaxNumericRecursionDepth})");

        expression = expression.SkipParens();

        switch (expression)
        {
            case Py.NameExpression ne:
                return _parsingNameContext.ParseName(ne);
            case Py.MemberExpression me:
                return _parsingNameContext.ParseMember(me);
            case Py.ConstantExpression constantExpression:
                var constantExpressionValue = constantExpression.Value is null ? default(Numeric) : Convert.ToInt32(constantExpression.Value);
                return new(constantExpressionValue);
            case Py.UnaryExpression unaryExpression when CreateNumericOrNull(unaryExpression.Expression, depth + 1) is { } operand:
                if (unaryExpression.Op == PythonOperator.Pos) return operand;
                var op = unaryExpression.Op.GetUnaryOperator();
                return operand.Call(op);
            case Py.CallExpression callExpression:
                var methodCall = callExpression.GetMethodCall();
                var numericMethodCallOrNull = CreateNumericMethodCallOrNull(methodCall);
                if (numericMethodCallOrNull is not null) methodCall.ThrowForUnusedArgs();
                return numericMethodCallOrNull;
            case Py.BinaryExpression { Operator: PythonOperator.In } binaryExpression
                when CreateNumericOrNull(binaryExpression.Left, depth + 1) is { } left:
                {
                    var getList = CreateEnumerable(binaryExpression.Right);
                    return left.Call(getList,
                        (f, list) => new Numeric(list.Contains(f))
                    );
                }
            case Py.BinaryExpression { Right: Py.BinaryExpression { NodeName: "comparison", Left: var middleExpression } } binaryExpression
                when CreateNumericOrNull(binaryExpression.Left, depth + 1) is { } getLeft &&
                     CreateNumericOrNull(middleExpression, depth + 1) is { } getMiddle &&
                     CreateNumericOrNull(binaryExpression.Right, depth + 1) is { } getRight:
                {
                    return And(getLeft.Call(getMiddle, binaryExpression.Operator.GetBinaryOperator()), getRight);
                }
            case Py.BinaryExpression binaryExpression
                when CreateNumericOrNull(binaryExpression.Left, depth + 1) is { } getLeft &&
                     CreateNumericOrNull(binaryExpression.Right, depth + 1) is { } getRight:
                {
                    return getLeft.Call(getRight, binaryExpression.Operator.GetBinaryOperator());
                }
            case Py.AndExpression andExpression
                when CreateNumericOrNull(andExpression.Left, depth + 1) is { } getLeft &&
                     CreateNumericOrNull(andExpression.Right, depth + 1) is { } getRight:
                {
                    return And(getLeft, getRight);
                }
            case Py.OrExpression orExpression
                when CreateNumericOrNull(orExpression.Left, depth + 1) is { } createLeftGetter &&
                     CreateNumericOrNull(orExpression.Right, depth + 1) is { } createRightGetter:
                {
                    return LazyCall<Numeric, Numeric, Numeric>(createLeftGetter, createRightGetter,
                        (getLeft, getRight) => new(p =>
                        {
                            var left = getLeft.Reduce(p);
                            return !left.IsTruthy ? getRight.Reduce(p) : left;
                        })
                    );
                }
            case Py.ConditionalExpression conditionalExpression
                when CreateNumericOrNull(conditionalExpression.TrueExpression, depth + 1) is { } createTrueGetter &&
                     CreateNumericOrNull(conditionalExpression.Test, depth + 1) is { } createTestGetter &&
                     CreateNumericOrNull(conditionalExpression.FalseExpression, depth + 1) is { } createFalseGetter:
                {
                    return TernaryConditional(createTrueGetter, createTestGetter, createFalseGetter);
                }
        }
        return null;
    }

    private EntitiesReducer<IReadOnlyDictionary<Numeric, T>>? CreateDictionaryOrNull<T>(Py.Expression expression, Func<Py.Expression, EntitiesReducer<T>?> parseValueOrNull)
    {
        var dictionaryExpression = (Py.DictionaryExpression)expression.SkipParens();
        var nullableIvg = dictionaryExpression.Items.Select(i => KeyValuePair.Create(CreateNumeric(i.SliceStart), parseValueOrNull(i.SliceStop))).ToArray();
        if (nullableIvg.Any(x => x.Value is null)) return null;
        var forEntityItems = nullableIvg.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.Value)).ToArray();
        if (forEntityItems.All(kvp => kvp.Key.IsConstant && kvp.Value.IsConstant))
        {
            return forEntityItems
                .Select(i => KeyValuePair.Create(i.Key.Value, i.Value.Value))
                .ToFrozenDictionary();
        }
        return new(e =>
        {
            var itemGettersByIsConstant = forEntityItems
                .Select(i => KeyValuePair.Create(i.Key.Reduce(e), i.Value.Reduce(e)))
                .ToLookup(i => i.Key.IsConstant && i.Value.IsConstant);
            if (!itemGettersByIsConstant[false].Any())
            {
                return itemGettersByIsConstant[true]
                    .Select(i => KeyValuePair.Create(i.Key.Value, i.Value.Value))
                    .ToFrozenDictionary();
            }

            return new(p =>
            {
                var keyValuePairs = itemGettersByIsConstant[true].Concat(itemGettersByIsConstant[false]);
                return keyValuePairs
                    .Select(i => KeyValuePair.Create(i.Key.Reduce(p), i.Value.Reduce(p)))
                    .ToDictionary();//Freezing a dictionary takes extra time, not worth it for one use
            });
        });
    }

    private static EntitiesReducer<TReturn> TernaryConditional<TReturn>(EntitiesReducer<TReturn> createTrueGetter,
        EntitiesReducer<Numeric> createTestGetter, EntitiesReducer<TReturn> createFalseGetter)
    {
        return LazyCall<TReturn, Numeric, TReturn, TReturn>(createTrueGetter, createTestGetter, createFalseGetter,
            (getTrue, getTest, getFalse) => p =>
            {
                var testValue = getTest.Reduce(p);
                return testValue.IsTruthy ? getTrue.Reduce(p) : getFalse.Reduce(p);
            }
        );
    }

    private static EntitiesReducer<Numeric> And(EntitiesReducer<Numeric> createLeftGetter, EntitiesReducer<Numeric> createRightGetter)
    {
        return LazyCall<Numeric, Numeric, Numeric>(createLeftGetter, createRightGetter,
            (getLeft, getRight) => new(p =>
            {
                var left = getLeft.Reduce(p); // `(None and True) == None`, `(0 and True) == 0`, `True and 0 == 0`
                return !left.IsTruthy ? left : getRight.Reduce(p);
            })
        );
    }

    public EntitiesReducer<Memory<Numeric>> CreateEnumerable(Py.Expression expression)
    {
        expression = expression.SkipParens();

        switch (expression)
        {
            case Py.CallExpression callExpression:
                {
                    var methodCall = callExpression.GetMethodCall();
                    if (CreateEnumerableMethodCallOrNull(methodCall) is { } enumerableMethodCall)
                    {
                        methodCall.ThrowForUnusedArgs();
                        return enumerableMethodCall;
                    }

                    var args = callExpression.Args.Select(a => new ParsedArg(a.Name, CreateNumericOrEnumerable(a))).ToArray();
                    return callExpression.Target is Py.MemberExpression me
                        ? _parsingNameContext.ParseCallMemberExpression(me, args)
                        : throw new SyntaxErrorException($"Invalid method call - {methodCall.Name}");
                }
            case Py.GeneratorExpression generatorExpression:
                {
                    var (variable, input, output, optionalCondition) = generatorExpression.GetEnumerableParts();
                    return EnumerableFromParts(variable, input, output, optionalCondition);
                }
            case Py.ListComprehension listComprehension:
                {
                    var (variable, input, output, optionalCondition) = listComprehension.GetEnumerableParts();
                    return EnumerableFromParts(variable, input, output, optionalCondition);
                }
            case Py.ListExpression listExpression:
                {
                    var cSharpArray = listExpression.Items.Select(CreateNumeric).Select(x => x).ToArray();
                    return cSharpArray.Reduce();
                }
            case Py.ConditionalExpression conditionalExpression
                when CreateEnumerable(conditionalExpression.TrueExpression) is { } createTrueGetter &&
                     CreateNumericOrNull(conditionalExpression.Test) is { } createTestGetter &&
                     CreateEnumerable(conditionalExpression.FalseExpression) is { } createFalseGetter:
                {
                    return TernaryConditional(createTrueGetter, createTestGetter, createFalseGetter);
                }
        }

        throw expression.NotSupported("Unsupported enumerable expression");

        EntitiesReducer<Memory<Numeric>> EnumerableFromParts(Py.NameExpression variable, Py.Expression input,
            Py.Expression output, Py.Expression optionalCondition)
        {
            var setValueFactory = CreateEnumerable(input);

            _parsingNameContext.Push(variable.Name);
            var testValueFactory = optionalCondition != null ? CreateNumeric(optionalCondition) : new(true);
            var yieldedValueFactory = CreateNumeric(output);
            _parsingNameContext.Pop();

            return LazyCall(setValueFactory, testValueFactory, yieldedValueFactory, (Func<RespondentReducer<Memory<Numeric>>, RespondentReducer<Numeric>, RespondentReducer<Numeric>, RespondentFunc<Memory<Numeric>>>)((setFactory, testFactory, yieldFactory) =>
                    profileEvalContext => WhereSelect(setFactory, testFactory, yieldFactory, profileEvalContext))
            );
        }
    }

    /// <summary>
    /// e.g. sum(1 for r in [1] if None) or sum([1 for r in [1] if None])
    /// </summary>
    private static Memory<Numeric> WhereSelect(RespondentReducer<Memory<Numeric>> setFactory, RespondentReducer<Numeric> testFactory, RespondentReducer<Numeric> yieldFactory, ExpressionEvaluationContext profileEvalContext)
    {
        int newIndex = 0;
        var set = setFactory.Reduce(profileEvalContext);
        var previousArg = profileEvalContext.Arg0;
        for (int existingIndex = 0; existingIndex < set.Length; existingIndex++)
        {
            profileEvalContext.Arg0 = set.Span[existingIndex];
            if (testFactory.Reduce(profileEvalContext).IsTruthy) set.Span[newIndex++] = yieldFactory.Reduce(profileEvalContext);
        }

        profileEvalContext.Arg0 = previousArg;
        return set[..newIndex];
    }

    private EntitiesReducer<Numeric>? CreateNumericMethodCallOrNull(MethodCall methodCall)
    {
        if (methodCall.Instance is null)
        {
            var getSet = CreateEnumerable(methodCall.GetArgOrNull(0));
            var getNamedDefaultArg = methodCall.GetArgOrNull("default");
            return methodCall.Name switch
            {
                "len" => getSet.Call(set => (Numeric)set.Length),
                "min" => getNamedDefaultArg is null ? getSet.Call(s => s.Min()) : getSet.Call(CreateNumeric(getNamedDefaultArg), (s, a) => s.Min(a)),
                "max" => getNamedDefaultArg is null ? getSet.Call(s => s.Max()) : getSet.Call(CreateNumeric(getNamedDefaultArg), (s, a) => s.Max(a)),
                "sum" => getSet.Call(set => set.Sum()), // sum([None]) throws
                "any" => getSet.Call(set => set.Any()), // any([None, False]) == False
                _ => default(EntitiesReducer<Numeric>?)
            };
        }
        if (methodCall.Instance is not null)
        {
            var getArg = CreateNumeric(methodCall.GetArgOrNull(0));
            return methodCall.Name switch
            {
                "count" => CreateEnumerable(methodCall.Instance).Call(getArg, (set, arg) => set.Count(arg)),
                "get" => GetNumericDictionaryOrNull(methodCall, getArg),
                _ => default(EntitiesReducer<Numeric>?)
            };
        }

        return default(EntitiesReducer<Numeric>?);
    }

    private EntitiesReducer<Numeric>? GetNumericDictionaryOrNull(MethodCall methodCall, EntitiesReducer<Numeric> getArg)
    {
        var defaultArg = methodCall.GetArgOrNull(1) is { } e && CreateNumericOrNull(e) is { } argGetter ? argGetter : new(default(Numeric));
        return GetFromDictionaryOrNull(methodCall.Instance, getArg, defaultArg, expr => CreateNumericOrNull(expr));
    }

    private EntitiesReducer<Memory<Numeric>>? CreateEnumerableMethodCallOrNull(MethodCall methodCall)
    {
        if (methodCall.Instance is not null && methodCall.GetArgOrNull(0) is { } firstArg)
        {
            var getArg = CreateNumeric(firstArg);
            var defaultArg = methodCall.GetArgOrNull(1) is { } e ? CreateEnumerable(e) : new(default(Memory<Numeric>));
            return methodCall.Name switch
            {
                "get" => GetFromDictionaryOrNull(methodCall.Instance, getArg, defaultArg, expression => CreateEnumerable(expression)),
                _ => null
            };
        }

        return null;
    }

    private EntitiesReducer<T>? GetFromDictionaryOrNull<T>(Py.Expression expression,
        EntitiesReducer<Numeric> getArg, EntitiesReducer<T> defaultArgOrNull,
        Func<Py.Expression, EntitiesReducer<T>?> parseOrNull)
    {
        var nullableForEntities = CreateDictionaryOrNull(expression, parseOrNull);
        if (nullableForEntities is not { } forEntities) return null;
        return forEntities.Call(getArg, defaultArgOrNull,
            (dictionary, index, defaultArg) => dictionary.GetValueOrDefault(index, defaultArg)
        );
    }
}
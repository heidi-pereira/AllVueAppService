namespace BrandVue.SourceData.Calculation.Expressions;

public delegate T RespondentFunc<out T>(ExpressionEvaluationContext context);
public delegate RespondentReducer<T> EntitiesFunc<T>(EntityValueCombination entityValues);

/// <summary>
/// Represents a curried function that takes EntityValueCombination then ExpressionEvaluationContext (i.e. respondent) to return a T.
/// Reducing this returns RespondentReducer of T, i.e. a function that takes a respondent and returns a value of type T.
/// This allows performance optimizations by precomputing as much as possible *before* looping over a huge number of respondents.
/// </summary>
/// <remarks>In this context, a "reducer" represents a step in the calculation, i.e. a reduction step in lambda calculus.</remarks>
/// <typeparam name="T">usually int, int?, Numeric, or bool</typeparam>
public readonly struct EntitiesReducer<T> : ICurriedReducer, IReducer<T>
{
    public T Value { get; }
    public EntitiesFunc<T> Reduce { get; }
    public bool IsConstant { get; }
    public bool IsConstantFunc { get; }

    public EntitiesReducer(T value)
    {
        Reduce = _ => value;
        Value = value;
        IsConstant = true;
    }

    /// <summary>Separated this case in case so it's easy to see whether there's value in optimizing the "constant for respondent but not for entities" case</summary>
    public EntitiesReducer(RespondentReducer<T> reduceRespondent)
    {
        Reduce = _ => reduceRespondent;
        IsConstantFunc = true;
    }
    public EntitiesReducer(EntitiesFunc<T> reduceEntities)
    {
        Reduce = reduceEntities;
    }

    // From most wanted to least wanted - i.e. it's best to know that something is a constant value
    public static implicit operator EntitiesReducer<T>(T value) => new(value);
    public static implicit operator EntitiesReducer<T>(RespondentReducer<T> respondentReducer) => new(respondentReducer);
    public static implicit operator EntitiesReducer<T>(RespondentFunc<T> respondentFunc) => new(respondentFunc);
    public static implicit operator EntitiesReducer<T>(EntitiesFunc<T> entitiesFunc) => new(entitiesFunc);
    public static implicit operator EntitiesReducer<T>(Func<EntityValueCombination, RespondentReducer<T>> forEntitiesFunc) => new(new EntitiesFunc<T>(forEntitiesFunc));

}

/// <summary>
/// Represents a function that takes ExpressionEvaluationContext (i.e. respondent) to return a T.
/// Reducing this returns a value of type T.
/// This allows performance optimizations by precomputing as much as possible *before* looping over a huge number of respondents.
/// </summary>
/// <remarks>In this context, a "reducer" represents a step in the calculation, i.e. a reduction step in lambda calculus.</remarks>
/// <typeparam name="T">usually int, int?, Numeric, or bool</typeparam>
public readonly struct RespondentReducer<T> : IReducer<T>
{
    public T Value { get; }
    public RespondentFunc<T> Reduce { get; }
    public bool IsConstant { get; }

    public RespondentReducer(T value)
    {
        Value = value;
        Reduce = _ => value;
        IsConstant = true;
    }

    public RespondentReducer(RespondentFunc<T> reduce)
    {
        Reduce = reduce;
        Value = default;
        IsConstant = false;
    }

    public static implicit operator RespondentReducer<T>(RespondentFunc<T> getValue) => new(getValue);
    public static implicit operator RespondentReducer<T>(Func<ExpressionEvaluationContext, T> getValue) => new(new RespondentFunc<T>(getValue));
    public static implicit operator RespondentReducer<T>(T getValue) => new(getValue);
}

/// <summary>
/// Result entity context is passed first so it doesn't need to be recalculated for every single profile response.
/// Profile evaluation context is the lambda that gets the result for a specific profile.
/// </summary>
/// <remarks>
/// There's nothing inherently specific to BrandVue here other than the names, and the name-only dependency on IProfileResponseEntity.
/// I just didn't make it generic so that people have some concrete concepts to hold on when looking at the code.
/// </remarks>
internal static class ProfileValueFactoryHelpers
{

    public static EntitiesReducer<TOut> Call<TIn, TOut>(this EntitiesReducer<TIn> createReducer, Func<TIn, TOut> selector)
    {
        if (createReducer.IsConstant) return selector(createReducer.Value);

        return new(resultEntityContext =>
        {
            var getValue = createReducer.Reduce(resultEntityContext);
            if (getValue.IsConstant) return selector(getValue.Value);
            return new(profileEvaluationContext => selector(getValue.Reduce(profileEvaluationContext)));
        });
    }

    public static EntitiesReducer<TOut> Call<TIn1, TIn2, TOut>(this EntitiesReducer<TIn1> createReducer1,
        EntitiesReducer<TIn2> createReducer2, Func<TIn1, TIn2, TOut> selector)
    {
        if (createReducer1.IsConstant && createReducer2.IsConstant) return selector(createReducer1.Value, createReducer2.Value);

        return new(resultEntityContext =>
        {
            var getValue1 = createReducer1.Reduce(resultEntityContext);
            var getValue2 = createReducer2.Reduce(resultEntityContext);

            if (getValue1.IsConstant && getValue2.IsConstant) return selector(getValue1.Value, getValue2.Value);

            return new(profileEvaluationContext =>
            {
                var value1 = getValue1.Reduce(profileEvaluationContext);
                var value2 = getValue2.Reduce(profileEvaluationContext);
                return selector(value1, value2);
            });
        });
    }

    public static EntitiesReducer<TOut> Call<TIn1, TIn2, TIn3, TOut>(this EntitiesReducer<TIn1> createReducer1, EntitiesReducer<TIn2> createReducer2, EntitiesReducer<TIn3> createReducer3, Func<TIn1, TIn2, TIn3, TOut> selector)
    {
        if (createReducer1.IsConstant && createReducer2.IsConstant && createReducer3.IsConstant) return selector(createReducer1.Value, createReducer2.Value, createReducer3.Value);

        return new(resultEntityContext =>
        {
            var getValue1 = createReducer1.Reduce(resultEntityContext);
            var getValue2 = createReducer2.Reduce(resultEntityContext);
            var getValue3 = createReducer3.Reduce(resultEntityContext);

            if (getValue1.IsConstant && getValue2.IsConstant && getValue3.IsConstant) return selector(getValue1.Value, getValue2.Value, getValue3.Value);

            return new(profileEvaluationContext =>
            {
                var value1 = getValue1.Reduce(profileEvaluationContext);
                var value2 = getValue2.Reduce(profileEvaluationContext);
                var value3 = getValue3.Reduce(profileEvaluationContext);
                return selector(value1, value2, value3);
            });
        });
    }

    /// <summary>
    /// Creates a value Reducer of a list of numerics from a list of numeric Reducers
    /// </summary>
    public static EntitiesReducer<Memory<Numeric>> Reduce(this IReadOnlyCollection<EntitiesReducer<Numeric>> createReducers)
    {
        if (createReducers.All(x => x.IsConstant)) return new(createReducers.Select(field => field.Value).ToArray().AsMemory());

        return new(resultEntityContext =>
        {
            var forEntitiesItems = createReducers.Select(getField => getField.Reduce(resultEntityContext)).ToArray();
            if (forEntitiesItems.All(v => v.IsConstant))
            {
                return new(forEntitiesItems.Select(field => field.Value).ToArray().AsMemory());
            }

            return new(profileEvaluationContext => SelectReduced(forEntitiesItems, profileEvaluationContext));
        });
    }

    /// <summary>
    /// ALLOC/PERF: Use ManagedMemory to avoid per-respondent allocation
    /// </summary>
    private static Memory<Numeric> SelectReduced(RespondentReducer<Numeric>[] forEntitiesItems,
        ExpressionEvaluationContext profileEvaluationContext)
    {
        int length = forEntitiesItems.Length;
        var memory = profileEvaluationContext.ManagedMemory.Rent(length);
        var span = memory.Span;

        for (int i = 0; i < length; i++)
        {
            var value = forEntitiesItems[i].Reduce(profileEvaluationContext);
            span[i] = value;
        }

        return memory;
    }

    public static EntitiesReducer<TOut> Reduce<TIn, TOut>(this IReadOnlyCollection<EntitiesReducer<TIn>> entitiesReducers, Func<IEnumerable<TIn>, TOut> selector)
    {
        if (entitiesReducers.All(x => x.IsConstant)) return new(selector(entitiesReducers.Select(createforEntities => createforEntities.Value).ToArray()));

        return new(resultEntityContext =>
        {
            var respondentReducers = entitiesReducers.Select(entitiesReducer => entitiesReducer.Reduce(resultEntityContext)).ToArray();
            if (respondentReducers.All(v => v.IsConstant))
            {
                return new(selector(respondentReducers.Select(respondentReducer => respondentReducer.Value).ToArray()));
            }
            return new(profileEvaluationContext =>
            {
                var profileValue = respondentReducers.Select(field => field.Reduce(profileEvaluationContext)).ToArray();
                return selector(profileValue);
            });
        });
    }

    public static EntitiesReducer<TOut> LazyCall<TIn1, TIn2, TOut>(EntitiesReducer<TIn1> getFirstReducer, EntitiesReducer<TIn2> getSecondReducer, Func<RespondentReducer<TIn1>, RespondentReducer<TIn2>, RespondentReducer<TOut>> selector)
    {
        if (getFirstReducer.IsConstant && getSecondReducer.IsConstant) return selector(getFirstReducer.Value, getSecondReducer.Value);
        return new(resultEntityContext =>
        {
            var getFirst = getFirstReducer.Reduce(resultEntityContext);
            var getSecond = getSecondReducer.Reduce(resultEntityContext);

            if (getFirst.IsConstant && getSecond.IsConstant) return selector(getFirst.Value, getSecond.Value);
            return selector(getFirst, getSecond);
        });
    }

    public static EntitiesReducer<TOut> LazyCall<TIn1, TIn2, TIn3, TOut>(EntitiesReducer<TIn1> getFirstReducer, EntitiesReducer<TIn2> getSecondReducer, EntitiesReducer<TIn3> getThirdReducer, Func<RespondentReducer<TIn1>, RespondentReducer<TIn2>, RespondentReducer<TIn3>, RespondentFunc<TOut>> selector)
    {
        if (getFirstReducer.IsConstant && getSecondReducer.IsConstant && getThirdReducer.IsConstant) return selector(getFirstReducer.Value, getSecondReducer.Value, getThirdReducer.Value);
        return new(resultEntityContext =>
        {
            var getFirst = getFirstReducer.Reduce(resultEntityContext);
            var getSecond = getSecondReducer.Reduce(resultEntityContext);
            var getThird = getThirdReducer.Reduce(resultEntityContext);
            if (getFirst.IsConstant && getSecond.IsConstant && getThird.IsConstant) return selector(getFirst.Value, getSecond.Value, getThird.Value);
            return selector(getFirst, getSecond, getThird);
        });
    }

    public static EntitiesReducer<Func<TIn, bool>> All<TIn>(this IReadOnlyCollection<EntitiesReducer<Func<TIn, bool>>> predicates)
    {
        if (predicates.Count == 0) return new(_ => true);
        if (predicates.Count == 1) return predicates.Single();
        if (predicates.Count == 2) return new(e =>
        {
            var getFirst = predicates.ElementAt(0).Reduce(e);
            var getSecond = predicates.ElementAt(1).Reduce(e);
            if (getFirst.IsConstant && getSecond.IsConstant)
            {
                return new(v => getFirst.Value(v) && getSecond.Value(v));
            }
            return new(p => v => getFirst.Reduce(p)(v) && getSecond.Reduce(p)(v));
        });
        if (predicates.Count == 3) return new(e =>
        {
            var getFirst = predicates.ElementAt(0).Reduce(e);
            var getSecond = predicates.ElementAt(1).Reduce(e);
            var getThird = predicates.ElementAt(2).Reduce(e);
            if (getFirst.IsConstant && getSecond.IsConstant && getThird.IsConstant)
            {
                return new(v => getFirst.Value(v) && getSecond.Value(v) && getThird.Value(v));
            }
            return new(p => v => getFirst.Reduce(p)(v) && getSecond.Reduce(p)(v) && getThird.Reduce(p)(v));
        });
        return predicates.Reduce(x =>
            x.Aggregate((acc, current) => collection => acc(collection) && current(collection))
        );
    }
}
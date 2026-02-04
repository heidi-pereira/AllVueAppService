using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// A general purpose python name that maps to a python Numeric - pretty much any C# lambda could be contained within the IVariable passed to the constructor
    /// </summary>
    internal class EvaluatedVariableInstance : IVariableInstance
    {
        public IVariable<Numeric> Variable { get; }
        private readonly IResponseEntityTypeRepository _entityTypeRepository;
        private readonly IEntityRepository _entityInstanceRepository;

        public IReadOnlyCollection<EntityType> UserEntityCombination => Variable.UserEntityCombination;
        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies => Variable.FieldDependencies;

        public string Identifier { get; }

        public EvaluatedVariableInstance(string identifier, IVariable<Numeric> variable,
            IResponseEntityTypeRepository entityTypeRepository,
            IEntityRepository entityInstanceRepository)
        {
            Variable = variable;
            _entityTypeRepository = entityTypeRepository;
            _entityInstanceRepository = entityInstanceRepository;
            Identifier = identifier;
        }

        public EntitiesReducer<Numeric> CreateNumericForEntities()
        {
            return new(entityValues =>
            {
                var getter = Variable.CreateForEntityValues(entityValues);
                // We throw away the context. This is ok since the variable can't possibly depend upon it - it's defined independently. The parameters to it were already parsed with the context.
                return new (p => getter(p.Profile));
            });
        }

        public EntitiesReducer<Memory<Numeric>> EnumerableForEntities(
            IReadOnlyCollection<ParsedArg> parsedArgs)
        {
            var omittedEntityTypeNames = VariableInstanceArgumentHelper.ValidateContextSensitiveEntityTypes(parsedArgs, Variable.UserEntityCombination).Select(t => t.Identifier).ToHashSet();

            EntitiesReducer<Func<IProfileResponseEntity, Numeric>[]> expressionGetter;
            if (UserEntityCombination.Any())
            {
                var allUnspecifiedInstances = omittedEntityTypeNames.Select(t =>
                {
                    var type = _entityTypeRepository.Get(t);
                    return _entityInstanceRepository.GetSubsetUnionedInstanceIdsOf(t).Select(i => new EntityValue(type, i)).ToArray();
                }).ToArray();
                var specifiedEntityInstanceGetter = parsedArgs.Select(GetEntityCombinations).ToArray().Reduce(x => x);

                // Because the expression is opaque, we have to call it with every possible combination of entity instances
                // PERF: To *try* to alleviate the abysmal perf, we try to pull as many operations to a level not requiring the profile using Call
                //  This will only benefit perf if other bits are efficiently implemented in a similar way
                expressionGetter = specifiedEntityInstanceGetter.Call(extraEntities => EnumerableExtensions
                    .CartesianProduct(allUnspecifiedInstances.Concat(extraEntities).ToArray())
                    .Select(entityCombination =>
                        Variable.CreateForEntityValues(new EntityValueCombination(entityCombination)))
                    .ToArray()
                );
            }
            else
            {
                var forEntityValues = Variable.CreateForEntityValues(new EntityValueCombination());
                var profileEntityValueCombination = forEntityValues.Yield().ToArray();
                expressionGetter = new(_ => profileEntityValueCombination);
            }

            if (expressionGetter.IsConstant) return new RespondentReducer<Memory<Numeric>>(r => SelectEvaluatedValue(expressionGetter.Value, r));

            return new(e =>
            {
                var getAllExpressions = expressionGetter.Reduce(e);

                return new (r =>
                {
                    var funcArray = getAllExpressions.Reduce(r);
                    return SelectEvaluatedValue(funcArray, r);
                });
            });
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity() => Variable.CreateForSingleEntity(_ => true);

        public bool OnlyDimensionIsEntityType() => Variable.OnlyDimensionIsEntityType();

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)>
            GetDatabaseOnlyDataTargets(Subset subset) => Variable.GetDatabaseOnlyDataTargets(subset);

        /// <remarks>Possible perf improvements here by special casing things like single constant value for example</remarks>
        private EntitiesReducer<EntityValue[]> GetEntityCombinations(ParsedArg parsedArg)
        {
            var type = _entityTypeRepository.Get(parsedArg.Name);
            switch (parsedArg.ForEntities)
            {
                case EntitiesReducer<Numeric> getNumeric:
                    return getNumeric.Call(arg => new EntityValue(type, (int) arg).Yield().ToArray());
                case EntitiesReducer<Memory<Numeric>> getEnumerable:
                    return getEnumerable.Call(args => args.ToArray().Select(a => new EntityValue(type, (int)a)).ToArray());
                default:
                    throw new ArgumentOutOfRangeException($"Received unknown argument expression type {parsedArg.ForEntities.GetType()}");
            }
        }

        /// <summary>
        /// ALLOC/PERF: Use ManagedMemory to avoid per-respondent allocation
        /// </summary>
        private static Memory<Numeric> SelectEvaluatedValue(Func<IProfileResponseEntity, Numeric>[] funcArray, ExpressionEvaluationContext r)
        {
            var memory = r.ManagedMemory.Rent(funcArray.Length);
            var span = memory.Span;
            int outputIndex = 0;
            for (int i = 0; i < funcArray.Length; i++)
            {
                var numeric = funcArray[i](r.Profile);
                if (numeric.HasValue)
                {
                    span[outputIndex++] = numeric;
                }
            }

            return memory.Take(outputIndex);
        }
    }
}

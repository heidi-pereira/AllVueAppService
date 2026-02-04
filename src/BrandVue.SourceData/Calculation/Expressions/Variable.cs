using System.Diagnostics;
using System.Threading;
using BrandVue.SourceData.LazyLoading;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// Core app model which can actually calcualte the value of a variable for a given respondent and entity values.
    /// </summary>
    /// <typeparam name="TOut"></typeparam>
    [DebuggerDisplay("{DisplayExpressionString,nq}")]
    public class Variable<TOut> : IVariable<TOut>, IVariableWithDependencies
    {
        private readonly IEntityRepository _entityRepository;
        private readonly IReadOnlyCollection<EntityType> _databaseOnlyEntityTypes;
        private readonly bool _onlyDimensionIsEntityType;
        private readonly EntitiesReducer<TOut> _createEntityReducerCombination;
        private readonly HashSet<EntityType> _userEntityCombination;

        public IReadOnlyCollection<EntityType> UserEntityCombination => _userEntityCombination;
        public bool OnlyDimensionIsEntityType() => _onlyDimensionIsEntityType;

        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }
        internal IReadOnlyCollection<IVariableInstance> VariableInstanceDependencies { get; }

        public IReadOnlyCollection<string> VariableDependencyIdentifiers =>
            VariableInstanceDependencies.Select(v => v.Identifier).ToArray();

        /// <summary>
        /// For display purposes. Do not parse this, if you need the parsed version, get the stored version from FieldExpressionParser.
        /// </summary>
        public string DisplayExpressionString { get; }

        public Func<IProfileResponseEntity, TOut> CreateForEntityValues(EntityValueCombination entityValues)
        {
            AssertCorrectEntitiesSpecified(entityValues);
            var filterFactory = _createEntityReducerCombination.Reduce(entityValues);

            // Benchmarks show a ~5% perf hit to using this thread local, but it allows us to reuse this memory for all respondents on the same thread without risking threads overwriting each other's work.
            var context = new ThreadLocal<ExpressionEvaluationContext>(() => new ExpressionEvaluationContext());
            return p =>
            {
                var expressionForDebug = DisplayExpressionString;
                var expressionEvaluationContext = context.Value;
                expressionEvaluationContext.Reset(p);
                return filterFactory.Reduce(expressionEvaluationContext);
            };
        }

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<TOut, bool> valuePredicate)
        {
            var responseEntityType = _userEntityCombination.Single();
            string entityType = responseEntityType.Identifier;
            var getters =
                _entityRepository.GetSubsetUnionedInstanceIdsOf(entityType)
                    .Select(id => (Id: id, Get: _createEntityReducerCombination.Reduce(new EntityValueCombination(new EntityValue(responseEntityType, id)))))
                    .ToArray();
            var evaluationContext = new ExpressionEvaluationContext(); // Resulting function is not thread safe due to reuse of this. If this becomes an issue, can use ThreadLocal as in CreateForEntityValues.
            var memoryPool = new ManagedMemoryPool<int>();
            return p =>
            {
                evaluationContext.Reset(p);
                memoryPool.FreeAll();
                var memory = memoryPool.Rent(getters.Length);
                var span = memory.Span;
                int answerIndex = 0;
                // ReSharper disable once ForCanBeConvertedToForeach - PERF on hot path
                for (int index = 0; index < getters.Length; index++)
                {
                    var (id, getter) = getters[index];
                    var value = getter.Reduce(evaluationContext);
                    if (!EqualityComparer<TOut>.Default.Equals(value, default) && valuePredicate(value))
                    {
                        span[answerIndex++] = id;
                    }
                }
                return memory.Take(answerIndex);
            };
        }

        [Conditional("DEBUG")] //PERF: Might help debugging locally, but it's too expensive to run every single loop in production
        private void AssertCorrectEntitiesSpecified(EntityValueCombination entityValues)
        {
            var specifiedEntityTypes = entityValues.AsReadOnlyCollection().Select(e => e.EntityType).ToHashSet();
            if (!_userEntityCombination.IsSubsetOf(specifiedEntityTypes))
            {
                var specifiedNames = entityValues.AsReadOnlyCollection().Select(e => e.EntityType.Identifier).JoinAsQuotedList();
                var required = UserEntityCombination.Select(t => t.Identifier).JoinAsQuotedList();
                throw new ArgumentOutOfRangeException($"Required entity values with types: [{required}]. Received entity values with types: [{specifiedNames}].");
            }
        }

        internal Variable(EntitiesReducer<TOut> numericExpression,
            IEntityRepository entityRepository, IReadOnlyCollection<ResponseFieldDescriptor> fieldDependencies,
            IReadOnlyCollection<IVariableInstance> variableInstanceDependencies,
            IReadOnlyCollection<EntityType> databaseOnlyEntityTypes,
            IEnumerable<EntityType> resultEntityTypes,
            bool outputIsEntityType = false, //If you're not sure, pass false
            string optionalExpression = null)
        {
            _entityRepository = entityRepository ?? throw new ArgumentNullException(nameof(entityRepository));
            _databaseOnlyEntityTypes = databaseOnlyEntityTypes ?? throw new ArgumentNullException(nameof(databaseOnlyEntityTypes));
            VariableInstanceDependencies = variableInstanceDependencies ?? throw new ArgumentNullException(nameof(variableInstanceDependencies));
            #if DEBUG
            DisplayExpressionString = optionalExpression;
            #endif
            _createEntityReducerCombination = numericExpression;
            FieldDependencies = fieldDependencies?.ToList() ?? throw new ArgumentNullException(nameof(fieldDependencies));
            _userEntityCombination = resultEntityTypes.ToHashSet();
            _onlyDimensionIsEntityType = outputIsEntityType && _userEntityCombination.Count == 1;
        }

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset)
        {
            return FieldDependencies.Select(field =>
            (
                Field: field,
                DataTargets: FieldToTargetInstances(field).ToArray()
            ));

            IEnumerable<IDataTarget> FieldToTargetInstances(ResponseFieldDescriptor field)
            {
                foreach (var type in field.EntityCombination)
                {
                    //excluding result entity types
                    if (!_userEntityCombination.Contains(type))
                    {
                        var instances = _entityRepository.GetInstancesOf(type.Identifier, subset);
                        if (instances.Any())
                        {
                            yield return new TargetInstances(type, instances);
                        }
                    }
                }
            }
        }
    }
}

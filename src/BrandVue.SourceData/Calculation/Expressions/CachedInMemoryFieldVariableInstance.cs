namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// The Vue engine guarantees that the FieldDependencies will be in memory before this is called, hence it only grabs the value for a field out of memory and converts it to a Python Numeric type
    /// </summary>
    internal class CachedInMemoryFieldVariableInstance : IVariableInstance, IVariable<int?>
    {
        private readonly ResponseFieldDescriptor _field;

        public string Identifier => _field.Name;

        public CachedInMemoryFieldVariableInstance(ResponseFieldDescriptor field)
        {
            _field = field;
            FieldDependencies = new[] {_field};
            ConstantValue = _field.IsMultiChoiceForAllSubsets() ? 1 : null;
        }

        public EntitiesReducer<Numeric> CreateNumericForEntities()
        {
            return new(entityValues =>
            {
                var getValue = CreateForEntityValues(entityValues);
                return new (context => getValue(context.Profile));
            });
        }

        public Func<IProfileResponseEntity, int?> CreateForEntityValues(EntityValueCombination entityValues)
        {
            var relevantEntityValues = new EntityValueCombination(entityValues.GetRelevantEntityValues(_field.EntityCombination));
            return p => ExtractFieldVal(_field, p, relevantEntityValues);
        }

        public EntitiesReducer<Memory<Numeric>> EnumerableForEntities(
            IReadOnlyCollection<ParsedArg> parsedArgs)
        {
            var omittedEntityTypeNames =
                VariableInstanceArgumentHelper.ValidateContextSensitiveEntityTypes(parsedArgs, _field.EntityCombination).Select(t => t.Identifier).ToHashSet();

            var predicateFactory = parsedArgs.Select(tuple => GetPredicate(_field, tuple)).ToArray().All();
            if (predicateFactory.IsConstant)
            {
                return new RespondentReducer<Memory<Numeric>>(r => GetFromProfile(r, predicateFactory.Value));
            }
            return new(e =>
            {
                var getPredicate = predicateFactory.Reduce(e);
                return new(r => GetFromProfile(r, getPredicate.Reduce(r)));
            });
            
            Memory<Numeric> GetFromProfile(ExpressionEvaluationContext r, Func<KeyValuePair<EntityIds, int>, bool> predicate) => 
                r.Profile.GetIntegerFieldValues(_field, predicate, val => new Numeric(val.Value), r.ManagedMemory);
        }


        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity() => CreateForSingleEntity(_ => true);

        public Func<IProfileResponseEntity, Memory<int>> CreateForSingleEntity(Func<int?, bool> valuePredicate)
        {
            var memoryPool = new ManagedMemoryPool<int>();
            Func<KeyValuePair<EntityIds, int>, bool> predicate = _field.IsMultiChoiceForAllSubsets()
                ? kvp => kvp.Value == 1 && valuePredicate(kvp.Value)
                : kvp => valuePredicate(kvp.Value);
            return p =>
            {
                memoryPool.FreeAll();
                return p.GetIntegerFieldValues(_field, predicate, x => x.Key[0], memoryPool);
            };
        }

        public IEnumerable<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)> GetDatabaseOnlyDataTargets(Subset subset) =>
            Enumerable.Empty<(ResponseFieldDescriptor Field, IDataTarget[] DataTargets)>();

        public bool OnlyDimensionIsEntityType() => _field.OnlyDimensionIsEntityType();

        public int? ConstantValue { get; }

        public IReadOnlyCollection<EntityType> UserEntityCombination => _field.EntityCombination;
        public IReadOnlyCollection<ResponseFieldDescriptor> FieldDependencies { get; }

        /// <remarks>Possible perf improvements here by special casing things like single constant value for example</remarks>
        private static EntitiesReducer<Func<KeyValuePair<EntityIds, int>, bool>> GetPredicate(ResponseFieldDescriptor field, ParsedArg parsedArg)
        {
            var getEntityValue = EntityIds.EntityIdGetterFor(field, parsedArg.Name);
            switch (parsedArg.ForEntities)
            {
                case EntitiesReducer<Numeric> getNumeric:
                    return getNumeric.Call<Numeric, Func<KeyValuePair<EntityIds, int>, bool>>(i => ev =>
                        getEntityValue(ev.Key) == i
                    );
                case EntitiesReducer<Memory<Numeric>> getEnumerable:
                    return getEnumerable.Call<Memory<Numeric>, Func<KeyValuePair<EntityIds, int>, bool>>(mem =>
                        ev => mem.Contains(getEntityValue(ev.Key))
                    );
                default:
                    throw new ArgumentOutOfRangeException($"Received unknown argument expression type {parsedArg.ForEntities.GetType()}");
            }
        }

        private static int? ExtractFieldVal(ResponseFieldDescriptor field, IProfileResponseEntity responseEntity, EntityValueCombination relevantEntityValues)
        {
            return responseEntity.GetIntegerFieldValue(field, relevantEntityValues);
        }
    }
}

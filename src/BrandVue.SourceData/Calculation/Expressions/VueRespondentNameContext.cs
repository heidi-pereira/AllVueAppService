using Py = IronPython.Compiler.Ast;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// Resolves names such as "field_name", "variable_name", "response.field_name()", "response.variable_name(entity1=3, entity2=result.entity2)".
    /// </summary>
    internal class VueRespondentNameContext : IParsingNameContext
    {
        private const string ResponseObjectName = "response";
        private const string ResultEntityContextObjectName = "result";

        private readonly IReadOnlyDictionary<string, IVariableInstance> _declaredVariableInstances;
        public HashSet<ResponseFieldDescriptor> FieldDependencies { get; } = new();

        public IReadOnlyCollection<EntityType> DatabaseOnlyEntityTypes =>
            FieldDependencies.SelectMany(f => f.EntityCombination).Where(t => !_resultEntityTypeNamesUsed.Contains(t.Identifier)).ToHashSet();

        private readonly HashSet<string> _resultEntityTypeNamesUsed = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyCollection<string> ResultEntityTypeNames => _resultEntityTypeNamesUsed;

        private readonly HashSet<IVariableInstance> _variableInstanceDependencies = new();
        public IReadOnlyCollection<IVariableInstance> VariableInstanceDependencies => _variableInstanceDependencies;


        public VueRespondentNameContext(IReadOnlyDictionary<string, IVariableInstance> declaredVariableInstances)
        {
            _declaredVariableInstances = declaredVariableInstances;
        }

        public EntitiesReducer<Numeric> ParseName(Py.NameExpression ne)
        {
            var variableInstance = GetVariableInstance(ne.Name);
            foreach (var t in variableInstance.UserEntityCombination) _resultEntityTypeNamesUsed.Add(t.Identifier); // Implicit use e.g. consumer_segment uses result.brand

            return variableInstance.CreateNumericForEntities();
        }

        public EntitiesReducer<Numeric> ParseMember(Py.MemberExpression me)
        {
            if (me.Target.IsName(ResultEntityContextObjectName))
            {
                _resultEntityTypeNamesUsed.Add(me.Name); // Explicitly referencing e.g. result.brand
                return new(resultEntityContext =>
                {
                    var entityValue = resultEntityContext.AsReadOnlyCollection().First(e => e.EntityType.Identifier == me.Name).Value;
                    return new(entityValue);
                });
            }

            throw me.NotSupported($"Only `{ResultEntityContextObjectName}.anEntityType` is supported, e.g. `{ResultEntityContextObjectName}.brand`");
        }

        public EntitiesReducer<Memory<Numeric>> ParseCallMemberExpression(Py.MemberExpression memberExpression,
            IReadOnlyCollection<ParsedArg> parsedArgs)
        {
            if (memberExpression.Target.IsName(ResponseObjectName))
            {
                var field = GetVariableInstance(memberExpression.Name);

                return field.EnumerableForEntities(parsedArgs);
            }

            throw memberExpression.NotSupported($"Function called on unknown member, try `{ResponseObjectName}`");
        }

        private IVariableInstance GetVariableInstance(string variableInstanceName)
        {
            if (!_declaredVariableInstances.TryGetValue(variableInstanceName, out var variableInstance))
            {
                variableInstance = new UnresolvedReference(variableInstanceName);
            }

            foreach (var f in variableInstance.FieldDependencies) FieldDependencies.Add(f);

            _variableInstanceDependencies.Add(variableInstance);
            return variableInstance;
        }
    }
}

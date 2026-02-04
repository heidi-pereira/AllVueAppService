using System.Data;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Variables;
using BrandVue.SourceData.Import;

namespace BrandVue.SourceData.Calculation.Expressions
{
    /// <summary>
    /// PERF: Because the filter expression gets applied for each profile, we must keep all logic except profile-specific logic out of the filter that gets created.
    /// This specifically includes selecting the right entity values
    ///
    /// At the moment, the most obvious syntax is directly mapped to its C# equivalent. If we wanted, we could provide a ScriptScope and actually execute the python for cases we can't handle.
    /// </summary>
    /// <remarks>
    /// Below is a sketch of the shape of the python context the expression is evaluated in.
    /// For performance reasons, field3BrandEntityValues and field1EntityValues would be shared between all profiles and all calls to the same field/entity context
    /// If future performance is needed, we could turn the lambdas currently used into Expression objects (these can be efficiently combined and compiled upfront).
    /// If future flexibility is needed, we could execute python directly. In this file's history there's an example of using a python walker to detect dependencies and not do unnecessary work: 743611c3e51574ee95ae152ffbda6e43c982cbfd
    /// </remarks>
    /// <code>
    /// class ResultContext(object):
    ///     def __init__(self):
    ///         self.brand = 7
    ///         self.occasion = 2
    ///
    /// class RespondentResponse(object):
    ///     def Field3(self, brand, occasion):
    ///         # The result of calling C# functions something like profileResponse.GetFieldValues(field3, field3BrandEntityValues).Where(x => x.EntityType == EntityType.Brand && x.Value == 6)
    ///         if (brand == 6 and occasion == None) return [1,4,7];
    ///         if (brand == 7 and occasion == None) return [2,3];
    ///         raise Error("Unexpected method call")
    ///
    /// # The result of calling C# functions something like profileResponse.GetFieldValue(field1, field1EntityValues)
    /// Field1 = 2
    /// Field2 = -99
    /// response = RespondentResponse()
    /// result = ResultContext()
    ///
    /// #Line below is the expression we're parsing
    /// (Field1 + Field2 + sum(response.Field3(brand = 6)) + len(response.Field3(brand = result.brand))) > 4
    /// </code>
    internal class FieldExpressionParser : IFieldExpressionParser
    {
        private readonly Dictionary<string, IVariableInstance> _variableInstances;
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly IEntityRepository _entityRepository;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;

        public FieldExpressionParser(IResponseFieldManager responseFieldManager, IEntityRepository entityRepository,
            IResponseEntityTypeRepository responseEntityTypeRepository)
        {
            _variableInstances = new Dictionary<string, IVariableInstance>(StringComparer.OrdinalIgnoreCase);
            _responseFieldManager = responseFieldManager;
            _entityRepository = entityRepository;
            _responseEntityTypeRepository = responseEntityTypeRepository;
        }

        public Variable<bool> ParseUserBooleanExpression(string fieldDefinitionFilterExpression)
        {
            var parsingNameContext = new VueRespondentNameContext(_variableInstances);

            var cSharpLambdaExpression = string.IsNullOrWhiteSpace(fieldDefinitionFilterExpression)
                ? new(true)
                : ParseNumeric(parsingNameContext, fieldDefinitionFilterExpression, "base expression")
                    .Call(x => x.IsTruthy);

            return CreateFieldExpression(cSharpLambdaExpression, parsingNameContext, fieldDefinitionFilterExpression);
        }

        public Variable<int?> ParseUserNumericExpressionOrNull(string numericExpression) =>
            ParseUserNumericExpressionOrNull(numericExpression, true);

        internal Variable<int?> ParseUserNumericExpressionOrNull(string numericExpression, bool throwForUnresolved)
        {
            if (string.IsNullOrWhiteSpace(numericExpression)) return null;

            var parsingNameContext = new VueRespondentNameContext(_variableInstances);
            var parsedNumeric = ParseNumeric(parsingNameContext, numericExpression, "metric variable expression", throwForUnresolved: throwForUnresolved);
            var cSharpLambdaExpression = parsedNumeric.Call(x => x.AsNullable());

            return CreateFieldExpression(cSharpLambdaExpression, parsingNameContext, numericExpression);
        }

        public IReadOnlyCollection<string> ParseResultEntityTypeNames(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return Array.Empty<string>();

            var parsingNameContext = new VueRespondentNameContext(_variableInstances);
            ParseNumeric(parsingNameContext, expression, "metric variable expression");
            return parsingNameContext.ResultEntityTypeNames;
        }

        public IVariable<int?> GetDeclaredVariableOrNull(string variableIdentifier) =>
            GetVariableFromVariableInstance(GetDeclaredVariableInstanceOrNull(variableIdentifier));

        public IVariable<bool> GetDeclaredBooleanVariableOrNull(string variableIdentifier) =>
            GetBooleanVariableFromVariableInstance(GetDeclaredVariableInstanceOrNull(variableIdentifier));

        public IVariable<int?> GetDeclaredVariableOrNull(ResponseFieldDescriptor responseFieldDescriptor) => 
            GetDeclaredVariableInstanceOrNull(responseFieldDescriptor.Name) is CachedInMemoryFieldVariableInstance v
               ? GetVariableFromVariableInstance(v) 
               : null;

        public void Delete(VariableConfiguration variableConfiguration) => _variableInstances.Remove(variableConfiguration.Identifier);

        private IVariableInstance GetDeclaredVariableInstanceOrNull(string variableIdentifier) =>
            _variableInstances.TryGetValue(variableIdentifier, out var variableInstance) ? variableInstance : null;

        public IReadOnlyCollection<VariableInstanceModel> GetDeclaredVariableModels() => _variableInstances.Values.Select(ConvertToModel).ToList();

        internal IReadOnlyCollection<IVariableInstance> GetDeclaredVariables() => _variableInstances.Values.ToList();

        private VariableInstanceModel ConvertToModel(IVariableInstance variableInstance)
        {
            return new VariableInstanceModel
            {
                Identifier = variableInstance.Identifier,
                ResponseEntityTypes = variableInstance.UserEntityCombination,
            };
        }

        private IVariable<int?> GetVariableFromVariableInstance(IVariableInstance variableInstance) =>
            variableInstance switch
            {
                IVariable<int?> questionInstance => questionInstance,
                EvaluatedVariableInstance evi => IntegerVariable.Create(evi.Variable),
                _ => null
            };

        private IVariable<bool> GetBooleanVariableFromVariableInstance(IVariableInstance variableInstance) =>
            variableInstance switch
            {
                IVariable<bool> questionInstance => questionInstance,
                EvaluatedVariableInstance v => BooleanVariable.Create(v),
                IVariable<int?> v => BooleanVariable.Create(v),
                _ => null
            };

        public IVariable<int?> DeclareOrUpdateVariable(VariableConfiguration variableConfiguration)
        {
            IVariableInstance variableInstance;
            if (variableConfiguration.Definition is QuestionVariableDefinition)
            {
                var field = _responseFieldManager.Get(variableConfiguration.Identifier);
                variableInstance = new CachedInMemoryFieldVariableInstance(field);
            }
            else
            {
                var variable = CreateNumericVariable(variableConfiguration);
                variableInstance = CreateEvaluatedVariableInstance(variableConfiguration, variable);
            }
            _variableInstances[variableConfiguration.Identifier] = variableInstance;
            return GetVariableFromVariableInstance(variableInstance);
        }

        public IVariable<int?> CreateTemporaryVariable(VariableConfiguration variableConfiguration)
        {
            if (variableConfiguration.Definition is not GroupedVariableDefinition groupedDefinition)
            {
                throw new InvalidOperationException("Non-grouped variable is not supported yet");
            }
            var (temporaryEntityType, entityRepository) = CreateTemporaryEntityRepository(groupedDefinition);

            var expression = groupedDefinition.GetPythonExpression();
            var parsingNameContext = new VueRespondentNameContext(_variableInstances);
            var cSharpLambdaExpression = ParseNumeric(parsingNameContext, expression, $"variable '{variableConfiguration.Identifier}'");

            var resultEntityTypes = _responseEntityTypeRepository.Where(r => parsingNameContext.ResultEntityTypeNames.Contains(r.Identifier))
                .Prepend(temporaryEntityType);
            var expressionVariable = new Variable<Numeric>(
                cSharpLambdaExpression,
                entityRepository,
                parsingNameContext.FieldDependencies,
                parsingNameContext.VariableInstanceDependencies,
                parsingNameContext.DatabaseOnlyEntityTypes,
                resultEntityTypes,
                OutputIsEntityType(variableConfiguration),
                expression);

            if (InstanceListVariable.TryCreate(entityRepository, groupedDefinition, expressionVariable, out var variable))
            {
                return IntegerVariable.Create(variable);
            }
            return IntegerVariable.Create(expressionVariable);
        }

        private (EntityType TemporaryEntityType, IEntityRepository EntityRepository) CreateTemporaryEntityRepository(GroupedVariableDefinition definition)
        {
            var entityType = new EntityType
            {
                Identifier = definition.ToEntityTypeName,
                DisplayNameSingular = definition.ToEntityTypeName,
                DisplayNamePlural = definition.ToEntityTypeDisplayNamePlural,
                CreatedFrom = EntityTypeCreatedFrom.Variable
            };
            var instances = definition.Groups.Select(group => new EntityInstance
            {
                Id = group.ToEntityInstanceId,
                Name = group.ToEntityInstanceName
            }).ToArray();

            var entityInstanceRepository = new TemporaryEntityInstanceRepository(_entityRepository, [(entityType, instances)]);
            return (entityType, entityInstanceRepository);
        }

        private EvaluatedVariableInstance CreateEvaluatedVariableInstance(VariableConfiguration variableConfiguration, IVariable<Numeric> variable)
        {
            return new EvaluatedVariableInstance(variableConfiguration.Identifier, variable, _responseEntityTypeRepository, _entityRepository);
        }

        private IVariable<Numeric> CreateNumericVariable(VariableConfiguration variableConfiguration)
        {
            if (variableConfiguration.Definition.ContainsPythonExpression())
            {
                var expression = variableConfiguration.Definition.GetPythonExpression();
                var parsingNameContext = new VueRespondentNameContext(_variableInstances);
                var cSharpLambdaExpression = ParseNumeric(parsingNameContext, expression, $"variable '{variableConfiguration.Identifier}'");

                Variable<Numeric> expressionVariable = CreateFieldExpression(cSharpLambdaExpression, parsingNameContext, expression, OutputIsEntityType(variableConfiguration));
                if (variableConfiguration.Definition is GroupedVariableDefinition groupVariableDefinition && InstanceListVariable.TryCreate(_entityRepository, groupVariableDefinition, expressionVariable, out var variable))
                {
                    return variable;
                }
                return expressionVariable;
            }
            else if (variableConfiguration.Definition is GroupedVariableDefinition groupVariableDefinition)
            {
                if (groupVariableDefinition.Groups.All(group => group.Component is DateRangeVariableComponent))
                {
                    return NumericVariable.Create(new DataWaveVariable(groupVariableDefinition));
                }
                else if (groupVariableDefinition.Groups.All(group => group.Component is SurveyIdVariableComponent))
                {
                    return NumericVariable.Create(new SurveyIdVariable(groupVariableDefinition));
                }
            }

            throw new ArgumentOutOfRangeException(nameof(variableConfiguration), variableConfiguration, null);
        }

        private static bool OutputIsEntityType(VariableConfiguration variableConfiguration) =>
            variableConfiguration.Definition is GroupedVariableDefinition groupVariableDefinition;

        private EntitiesReducer<Numeric> ParseNumeric(VueRespondentNameContext parsingNameContext,
            string numericExpression, string contextLogDescription, bool throwForUnresolved = true)
        {
            var parser = new NumericPythonParser(parsingNameContext);
            var parsedNumeric = parser.ParseNumericExpression(numericExpression);
            if (throwForUnresolved) ThrowErrorForUnresolvedReferences(parsingNameContext, contextLogDescription);
            return parsedNumeric;
        }

        private Variable<T> CreateFieldExpression<T>(EntitiesReducer<T> cSharpLambdaExpression,
            VueRespondentNameContext parsingNameContext, string optionalExpression,
            bool outputIsEntityType = false)
        {
            var resultEntityTypes =
                _responseEntityTypeRepository.Where(r => parsingNameContext.ResultEntityTypeNames.Contains(r.Identifier));
            return new(cSharpLambdaExpression, _entityRepository, parsingNameContext.FieldDependencies,
                parsingNameContext.VariableInstanceDependencies, parsingNameContext.DatabaseOnlyEntityTypes,
             resultEntityTypes, outputIsEntityType, optionalExpression);
        }

        /// <summary>
        /// These are non-structural issues, so doing this at the end allows us to report multiple issues from a single rather than fixing one-by-one
        /// </summary>
        private static void ThrowErrorForUnresolvedReferences(VueRespondentNameContext parsingNameContext, string contextLogDescription)
        {
            var unresolvedReferences = parsingNameContext.VariableInstanceDependencies.OfType<UnresolvedReference>().ToArray();
            if (unresolvedReferences.Any())
            {
                throw new InvalidExpressionException($"Errors in {contextLogDescription}:\r\n  " + string.Join("\r\n  ", unresolvedReferences.Select(r => r.ErrorMessage).Distinct()));
            }
        }

        /// <summary>
        /// For legacy map file BrandVues (and convenience in unit tests) create variable for all fields
        /// At some point, we should be able to move BrandVues to have fields generated. We'd have to diff the existing map-file ones,especially ValueDbLocation
        /// In the ideal world we'd only have one question variable per question. For convenience, we generate two fields per question (one drops the answer entity, so we can easily check if it was "asked").
        /// So here we add a dummy variable, so it can be referenced by other variables (in this case a question variable, which just means "go and look for a field with that name")
        /// </summary>
        /// <param name="mapFileFields">ONLY fields generated by the map file</param>
        public void DeclareDummyQuestionVariables(ICollection<ResponseFieldDescriptor> mapFileFields)
        {
            mapFileFields = mapFileFields.AsHashSet(); //PERF
            var allFields = _responseFieldManager.GetAllFields();
            var requireDummyFields = allFields.Where(f => f.Name.EndsWith("_asked") || mapFileFields.Contains(f));
            foreach (var field in requireDummyFields)
            {
                DeclareOrUpdateVariableFromField(field.Name);
            }
        }

        private void DeclareOrUpdateVariableFromField(string fieldName)
        {
            string identifier = NameGenerator.EnsureValidPythonIdentifier(fieldName);
            DeclareOrUpdateVariable(new VariableConfiguration
                { Identifier = identifier, Definition = new QuestionVariableDefinition() }
            );
        }
    }
}

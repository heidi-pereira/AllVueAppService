using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;

namespace BrandVue.SourceData.Variable
{
    public class VariableValidator : IVariableValidator
    {
        private readonly IReadableVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IReadableMetricConfigurationRepository _metricConfigurationRepository;
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly IFieldExpressionParser _fieldExpressionParser;
        private readonly IEntityRepository _entityRepository;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;

        public VariableValidator(IFieldExpressionParser fieldExpressionParser,
            IReadableVariableConfigurationRepository variableConfigurationRepository,
            IEntityRepository entityRepository,
            IResponseEntityTypeRepository responseEntityTypeRepository,
            IReadableMetricConfigurationRepository metricConfigurationRepository,
            IResponseFieldManager responseFieldManager)
        {
            _variableConfigurationRepository = variableConfigurationRepository;
            _metricConfigurationRepository = metricConfigurationRepository;
            _responseFieldManager = responseFieldManager;
            _fieldExpressionParser = fieldExpressionParser;
            _entityRepository = entityRepository;
            _responseEntityTypeRepository = responseEntityTypeRepository;
        }

        public void Validate(VariableConfiguration variableConfiguration, out IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers, out IReadOnlyCollection<string> entityTypeNames, bool shouldVerify = true)
        {
            if (variableConfiguration == null)
            {
                throw new BadRequestException("Value cannot be null");
            }

            if (variableConfiguration.Definition == null)
            {
                throw new BadRequestException("Variables must have a definition");
            }


            int existingVariableId = variableConfiguration.Id == 0 ? -1 : variableConfiguration.Id;
            ValidateName(variableConfiguration.Identifier, variableConfiguration.DisplayName, variableConfiguration.Identifier, existingVariableId);
            if (shouldVerify)
            {
                VerifyVariable(variableConfiguration, out dependencyVariableInstanceIdentifiers, out entityTypeNames);
            }
            else
            {
                dependencyVariableInstanceIdentifiers = Array.Empty<string>();
                entityTypeNames = Array.Empty<string>();
            }

            if (dependencyVariableInstanceIdentifiers != null &&
                dependencyVariableInstanceIdentifiers.Contains(variableConfiguration.Identifier,
                    StringComparer.InvariantCultureIgnoreCase))
            {
                throw new BadRequestException(
                    "A variable cannot reference itself or reference another variable which references itself");
            }

            if (variableConfiguration.Definition is FieldExpressionVariableDefinition valueVar)
            {
                if (string.IsNullOrWhiteSpace(valueVar.Expression))
                {
                    throw new BadRequestException("Value variable must have a non-empty expression");
                }
            }

            if (variableConfiguration.Definition is GroupedVariableDefinition groupedVariable)
            {
                if (groupedVariable.Groups == null || !groupedVariable.Groups.Any())
                {
                    throw new BadRequestException("Variables must have at least one group");
                }

                //assumption: if this is updating an existing variable, it will not modify the variable's entity type name
                var isNewVariable = existingVariableId < 0;
                if (isNewVariable)
                {
                    var typeNameExists = _responseEntityTypeRepository
                        .Any(t => t.Identifier == groupedVariable.ToEntityTypeName);
                    if (typeNameExists)
                    {
                        throw new BadRequestException(
                            "A variable or question with this name already exists or is too similar to this name");
                    }
                }

                var newTypeIds = groupedVariable.Groups.Select(g => g.ToEntityInstanceId).ToList();
                if (newTypeIds.Distinct().Count() != newTypeIds.Count)
                {
                    throw new BadRequestException("Multiple groups have been defined with the same ID");
                }

                foreach (var group in groupedVariable.Groups)
                {
                    var component = group.Component;
                    if (component is InstanceListVariableComponent instanceListComponent)
                    {
                        var sourceTypeExists =
                            _responseEntityTypeRepository.Any(t => t.Identifier.Equals(instanceListComponent.FromEntityTypeName, StringComparison.CurrentCultureIgnoreCase)); //Ignore case because the base repo does to ToLower() to get a case-insensitive lookup
                        if (!sourceTypeExists)
                        {
                            throw new BadRequestException(
                                $"Invalid type selected for group: {group.ToEntityInstanceName}");
                        }

                        var allowedInstances = _entityRepository
                            .GetSubsetUnionedInstanceIdsOf(instanceListComponent.FromEntityTypeName).ToHashSet();
                        if (instanceListComponent.InstanceIds.Any(id => !allowedInstances.Contains(id)))
                        {
                            throw new BadRequestException(
                                $"Invalid choice selected for group: {@group.ToEntityInstanceName}");
                        }
                    }
                }
            }
        }

        private void VerifyVariable(VariableConfiguration variableConfiguration,
            out IReadOnlyCollection<string> dependencyVariableInstanceIdentifiers, out IReadOnlyCollection<string> entityTypeNames)
        {
            dependencyVariableInstanceIdentifiers = Array.Empty<string>();
            entityTypeNames = Array.Empty<string>();
            if (!variableConfiguration.Definition.ContainsPythonExpression())
                return;

            try
            {
                var parsedExpression =
                    _fieldExpressionParser.ParseUserNumericExpressionOrNull(variableConfiguration.Definition.GetPythonExpression());
                dependencyVariableInstanceIdentifiers = parsedExpression?.VariableInstanceDependencies
                    .OfType<EvaluatedVariableInstance>().Select(v => v.Identifier).ToArray();
                entityTypeNames = parsedExpression?.UserEntityCombination.Select(t => t.Identifier).ToArray();
            }
            catch (Exception x)
            {
                throw new BadRequestException($"Parsing variable failed: {x.Message}");
            }
        }

        private void ValidateName(string newVariableIdentifier, string newVariableDisplayName, string newMetricName, int existingVariableId)
        {
            var variableNameExists = _variableConfigurationRepository.GetAll()
                .Where(v => v.Id != existingVariableId) // If we're renaming, don't check against the variable itself
                .Any(v => string.Equals(v.DisplayName, newVariableDisplayName, StringComparison.InvariantCultureIgnoreCase)
                          || string.Equals(v.Identifier, newVariableIdentifier, StringComparison.InvariantCultureIgnoreCase));
            var metricNameExists = _metricConfigurationRepository.GetAll()
                .Where(m => m.VariableConfigurationId != existingVariableId)
                .Any(m => string.Equals(m.Name, newMetricName, StringComparison.InvariantCultureIgnoreCase));
            var fieldNameExists = _responseFieldManager.GetAllFields()
                .Any(f => string.Equals(f.Name, newVariableDisplayName, StringComparison.InvariantCultureIgnoreCase));
            if (variableNameExists || metricNameExists || fieldNameExists)
            {
                throw new BadRequestException("A variable or question with this name already exists");
            }
        }
    }
}

using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using Microsoft.Extensions.Logging;
using BrandVue.SourceData.Variable;

namespace BrandVue.SourceData.Import;

public class VariableLoader
{
    private readonly ILogger _logger;
    private readonly IFieldExpressionParser _fieldExpressionParser;

    public VariableLoader(IFieldExpressionParser fieldExpressionParser, ILogger logger)
    {
        _logger = logger;
        _fieldExpressionParser = fieldExpressionParser;
    }

    public void ParsePythonExpressionVariablesInDependencyOrder(IReadOnlyCollection<VariableConfiguration> variableConfigurations)
    {
        var dependencyOrderedVariables = variableConfigurations
            .Select(v => (Variable: v, Order: v.GetTransitiveDependencies(_logger)?.Count()))
            .Where(v => v.Order.HasValue) // null when cyclic dependency
            // Once we change user-created variables to reference question variables (and migrate existing db variables), we won't need this line
            .OrderByDescending(v => v.Variable.Definition is QuestionVariableDefinition)
            .ThenBy(v => v.Order)
            .Select(v => v.Variable)
            .ToArray();

        foreach (var variableConfiguration in dependencyOrderedVariables)
        {
            try
            {
                _fieldExpressionParser.DeclareOrUpdateVariable(variableConfiguration);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,
                    $"Failed to declare variable {variableConfiguration.Id} : {variableConfiguration.DisplayName} for sub product {variableConfiguration.SubProductId} {LoggingTags.Variable} {LoggingTags.Config}"
                );
            }
        }
    }
}
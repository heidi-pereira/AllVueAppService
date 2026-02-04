using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Import;
using Humanizer;

namespace BrandVue.SourceData.Variable;

public class VariableFactory : IVariableFactory
{
    private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;
    private readonly IFieldExpressionParser _fieldExpressionParser;

    public VariableFactory(IFieldExpressionParser fieldExpressionParser, IResponseEntityTypeRepository responseEntityTypeRepository)
    {
        _fieldExpressionParser = fieldExpressionParser;
        _responseEntityTypeRepository = responseEntityTypeRepository;
    }

    public IVariable<int?> GetDeclaredVariable(VariableConfiguration variableConfig)
    {
        return _fieldExpressionParser.GetDeclaredVariableOrNull(variableConfig.Identifier)
               ?? throw new BadRequestException($"Variable configuration type is not supported or variable has not been declared for `{variableConfig.Identifier}`");
    }

    public IReadOnlyCollection<string> ParseResultEntityTypeNames(VariableConfiguration variableConfig)
    {
        if (variableConfig.Definition.ContainsPythonExpression())
        {
            var expression = variableConfig.Definition.GetPythonExpression();
            return _fieldExpressionParser.ParseResultEntityTypeNames(expression);
        }

        if (variableConfig.Definition is GroupedVariableDefinition groupDefinition)
        {
            //DataWaveVariable or SurveyIdVariable
            return new[] { groupDefinition.ToEntityTypeName };
        }

        throw new BadRequestException("Variable configuration type is not supported!");
    }

    public VariableDefinition SanitizeVariableEntityTypeName(VariableConfiguration originalVariableConfiguration, VariableDefinition updatedVariableDefinition)
    {
        if (updatedVariableDefinition is GroupedVariableDefinition updatedGroupedVariableDefinition)
        {
            if (originalVariableConfiguration.Definition is GroupedVariableDefinition originalGroupedVariableDefinition)
            {
                updatedGroupedVariableDefinition.ToEntityTypeName = originalGroupedVariableDefinition.ToEntityTypeName;
                updatedGroupedVariableDefinition.ToEntityTypeDisplayNamePlural = originalGroupedVariableDefinition.ToEntityTypeDisplayNamePlural;
            }
            else
            {
                string uniqueTypeName = CreateUniqueTypeName(updatedGroupedVariableDefinition.ToEntityTypeName, 0, originalVariableConfiguration.Identifier);
                updatedGroupedVariableDefinition.ToEntityTypeName = uniqueTypeName;
            }
        }

        return updatedVariableDefinition;
    }

    public string CreateUniqueTypeName(string typeName, int attemptCount, string identifier)
    {
        var uniqueTypeName = typeName.Dehumanize();
        if (attemptCount != 0)
        {
            uniqueTypeName += attemptCount;
        }

        var sanitizedName = NameGenerator.EnsureValidPythonIdentifier(uniqueTypeName);
        var typeNameExists = _responseEntityTypeRepository
            .Where(t => t.Identifier != identifier)
            .Any(t => string.Equals(t.Identifier, sanitizedName, StringComparison.CurrentCultureIgnoreCase));

        if (!typeNameExists)
        {
            return sanitizedName;
        }

        attemptCount += 1;
        return CreateUniqueTypeName(typeName, attemptCount, identifier);
    }
}
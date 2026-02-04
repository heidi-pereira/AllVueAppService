using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.AutoGeneration.Buckets;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Variable;
using JetBrains.Annotations;
using static BrandVue.SourceData.AutoGeneration.AutoGenerationConstants;

namespace BrandVue.SourceData.AutoGeneration;

public class BucketedVariableConfigurationCreator
{
    private readonly IVariableConfigurationRepository _variableConfigurationRepository;
    private readonly IVariableConfigurationFactory _variableFactory;

    public BucketedVariableConfigurationCreator(IVariableConfigurationRepository variableConfigurationRepository, IVariableConfigurationFactory  variableFactory)
    {
        _variableFactory = variableFactory;
        _variableConfigurationRepository = variableConfigurationRepository;
    }

    public VariableConfiguration CreateBucketedVariable(NumericFieldData numericFieldData, IEnumerable<NumericBucket> buckets)
    {
        var field = numericFieldData.GetFieldDefinitionModel();
        string uniqueName = numericFieldData.GetUniqueName();
        var definition = CreateGroupedVariableDefinitionFromBuckets(buckets, field);
        var identifier = _variableFactory.CreateIdentifierFromName(uniqueName);

        var config = _variableFactory.CreateVariableConfigFromParameters(uniqueName,
            identifier,
            definition,
            out var dependencyVariableInstanceIdentifiers,
            out _,
            false);
        return _variableConfigurationRepository.Create(config, dependencyVariableInstanceIdentifiers);
    }

    private static GroupedVariableDefinition CreateGroupedVariableDefinitionFromBuckets(IEnumerable<NumericBucket> buckets, FieldDefinitionModel field)
    {
        string variableName = NumericIdentifier + field.Name;

        return buckets
            .ToList()
            .Aggregate(
                new GroupedVariableDefinitionBuilder(variableName),
                (definitionBuilder, bucket) => definitionBuilder.WithInclusiveRangeGroupFromNumericBucket(bucket, field.Name, null)
            )
            .Build();
    }
}

public static class VariableDefinitionBuilderExtensions
{
    public static GroupedVariableDefinitionBuilder WithInclusiveRangeGroupFromNumericBucket( this GroupedVariableDefinitionBuilder builder, NumericBucket bucket, string fieldId, [CanBeNull] string numFormat)
    {
        string descriptor = bucket.GetBucketDescriptor(numFormat);
        int min = bucket.MinimumInclusive;
        var op = bucket.GetBucketOperator();

        if (bucket.MaximumInclusive != null)
        {
            int max = (int)bucket.MaximumInclusive;
            return builder.WithInclusiveRangeGroup(descriptor, min, max, fieldId, op);
        }

        return builder.WithGreaterThanGroup(descriptor, min, fieldId);
    }
}
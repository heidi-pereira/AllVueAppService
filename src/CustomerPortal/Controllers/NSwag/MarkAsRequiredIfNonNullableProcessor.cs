using JetBrains.Annotations;
using NJsonSchema;
using NJsonSchema.Generation;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace CustomerPortal.Controllers.NSwag;

/// <summary>
/// From https://github.com/RicoSuter/NSwag/issues/3110#issuecomment-1218533312
/// </summary>
public class MarkAsRequiredIfNonNullableProcessor : ISchemaProcessor, IOperationProcessor
{
    [UsedImplicitly]
    public void Process(SchemaProcessorContext context)
    {
        var schemaActualProperties = context.Schema.ActualProperties;
        foreach (var (_, prop) in schemaActualProperties)
        {
            if (!prop.IsNullable(SchemaType.OpenApi3))
            {
                prop.IsRequired = true;
            }
        }
    }

    [UsedImplicitly]
    public bool Process(OperationProcessorContext context)
    {
        foreach (var (_, parameter) in context.Parameters)
        {
            if (!parameter.IsNullable(SchemaType.OpenApi3))
            {
                parameter.IsRequired = true;
            }
        }

        return true;
    }
}
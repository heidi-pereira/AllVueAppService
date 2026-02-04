using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.BackEnd.Models;

public class StringEnumSchemaFilter : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        var typeInfo = context.JsonTypeInfo;
        if (typeInfo?.Type != null && typeInfo.Type.IsEnum)
        {
            schema.Type = "string";
            schema.Format = null;
            schema.Enum.Clear();
            foreach (var name in Enum.GetNames(typeInfo.Type))
            {
                schema.Enum.Add(new OpenApiString(name));
            }
        }
        if (typeInfo?.Type == typeof(Variable))
        {
            schema.Properties["surveySegments"] = new OpenApiSchema
            {
                Type = "array",
                Items = new OpenApiSchema { Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = "SurveySegment" } }
            };
        }
        return Task.CompletedTask;
    }
}
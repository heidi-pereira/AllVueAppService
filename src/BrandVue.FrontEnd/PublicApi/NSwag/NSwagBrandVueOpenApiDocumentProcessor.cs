using JetBrains.Annotations;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace BrandVue.PublicApi.NSwag;

public class NSwagBrandVueOpenApiDocumentProcessor : IDocumentProcessor
{
    [UsedImplicitly]
    public void Process(DocumentProcessorContext context)
    {
        context.Document.SecurityDefinitions.Add("APIKey", new OpenApiSecurityScheme
        {
            Type = OpenApiSecuritySchemeType.ApiKey,
            Scheme = "bearer",
            In = OpenApiSecurityApiKeyLocation.Header,
            Name = "Authorization"
        });
        var security = context.Document.Security;
        var securityRequirement1 = new OpenApiSecurityRequirement
        {
            {"APIKey", new string[0]}
        };
        var securityRequirement2 = securityRequirement1;
        security.Add(securityRequirement2);
        foreach (var operation in context.Document.Operations)
        {
            foreach (var parameter in operation.Operation.Parameters)
            {
                parameter.IsNullableRaw = parameter.Schema.IsNullableRaw = false;
                parameter.IsRequired = true;
                foreach (var property in parameter.Schema.Properties)
                {
                    property.Value.IsNullableRaw = false;
                }
            }
        }
    }
}
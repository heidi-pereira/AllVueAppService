using Microsoft.AspNetCore.Mvc.Routing;

namespace BrandVue.PublicApi.ModelBinding
{
    /// <summary>
    /// Enables post AND *get* using a compressed uri.
    /// The name is misleading in order to let nswag only generate one method:
    ///     https://github.com/RicoSuter/NSwag/blob/6b49eefa4048cecfd41ba0d7484bd1f25543d22d/src/NSwag.Generation.WebApi/WebApiOpenApiDocumentGenerator.cs#L579-L582
    /// Use in combination with <see cref="FromCompressedUriAttribute"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class CompressedGetOrPostAttribute : Attribute, IActionHttpMethodProvider
    {
        IEnumerable<string> IActionHttpMethodProvider.HttpMethods { get; } = new[]
        {
            "GET", "POST"
        };

    }
}
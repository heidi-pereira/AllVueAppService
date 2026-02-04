using System.Reflection;
using BrandVue.PublicApi.ModelBinding;
using JetBrains.Annotations;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace BrandVue.PublicApi.NSwag
{
    public class OnlyGeneratePostForCompressedUris : IOperationProcessor
    {
        [UsedImplicitly]
        public bool Process(OperationProcessorContext context)
        {
            if (HasCompressedAttribute(context.MethodInfo) || context.Parameters.Any(HasCompressedAttribute))
            {
                return string.Equals(context.OperationDescription.Method, "POST", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        private static bool HasCompressedAttribute(MethodInfo contextMethodInfo) =>
            contextMethodInfo.GetCustomAttributes(typeof(CompressedGetOrPostAttribute), true).Any();

        private static bool HasCompressedAttribute(KeyValuePair<ParameterInfo, OpenApiParameter> x) =>
            x.Key.CustomAttributes.Any(c => c.AttributeType == typeof(FromCompressedUriAttribute));
    }
}
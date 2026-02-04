using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BrandVue.PublicApi.ModelBinding
{
    public static class TrickNSwag
    {
        /// <summary>
        /// NSwag checks for inheritance from FromBodyAttribute
        /// We want it to use its logic that creates interfaces for the model rather than expanding all its properties and passing them as separate parameters
        /// If this stops working, we may be able to use ParameterBindingAttribute instead https://github.com/RicoSuter/NSwag/blob/6b49eefa4048cecfd41ba0d7484bd1f25543d22d/src/NSwag.Generation.WebApi/Processors/OperationParameterProcessor.cs#L121
        /// </summary>
        public class FromBodyAttribute : ModelBinderAttribute
        {
            public FromBodyAttribute(Type binderType) : base(binderType)
            {
                BindingSource = BindingSource.Body;
            }
        }
    }
}
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BrandVue.PublicApi.ModelBinding
{
    internal abstract class RawStringModelBinderBase<T> : IModelBinder where T : class
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.Result = BindModel(bindingContext, valueProviderResult.FirstValue) is {} result
                ? ModelBindingResult.Success(result)
                : ModelBindingResult.Failed();

            return Task.CompletedTask;
        }

        /// <summary>
        /// Either returns a model, or returns null and adds a model error using <code>bindingContext.AddError</code>
        /// </summary>
        protected abstract T BindModel(ModelBindingContext bindingContext, string modelAsRawString);
    }
}
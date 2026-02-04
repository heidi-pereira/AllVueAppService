using BrandVue.Services;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.ModelBinding
{
    internal abstract class SurveyDependantModelBinderBase<T> : RawStringModelBinderBase<T> where T : class
    {
        protected override T BindModel(ModelBindingContext bindingContext, string modelAsRawString) =>
            TryGetSubset(bindingContext, out var subset) ? BindModelForSubset(bindingContext, subset, modelAsRawString) : null;

        protected abstract T BindModelForSubset(ModelBindingContext bindingContext, Subset subset, string modelAsRawString);

        public static bool TryGetSubset(ModelBindingContext bindingContext, out Subset subset)
        {
            var claimRestrictedSubsetRepository = bindingContext.HttpContext.GetService<IClaimRestrictedSubsetRepository>();

            // Need original value provider to get the route info if currently binding a query parameter
            var valueProvider = bindingContext is DefaultModelBindingContext {} d
                ? d.OriginalValueProvider
                : bindingContext.ValueProvider;

            string alias = valueProvider.GetValue(BindingConstants.JsonProperties.SurveySet).FirstValue;
            if (!claimRestrictedSubsetRepository.HasSubsetWithAlias(alias))
            {
                subset = null;
                bindingContext.Result = ModelBindingResult.Failed();
                bindingContext.ModelState.AddModelError("subset","Subset does not exist, or you do not have access to it, or it is not enabled for API access.");
                return false;
            }

            subset = claimRestrictedSubsetRepository.GetWithAlias(alias);
            return true;
        }
    }
}
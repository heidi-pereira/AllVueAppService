using System.Net;
using BrandVue.PublicApi.Services;
using BrandVue.Services;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;
using AverageDescriptor = BrandVue.PublicApi.Models.AverageDescriptor;

namespace BrandVue.PublicApi.ModelBinding
{
    internal class AverageModelBinder : SurveyDependantModelBinderBase<AverageDescriptor>
    {
        protected override AverageDescriptor BindModelForSubset(ModelBindingContext bindingContext, Subset subset, string averageString)
        {
            var averageDescriptors = bindingContext.HttpContext.GetService<IApiAverageProvider>()
                .GetAllAvailableAverageDescriptors(subset)
                .Where(ad => ad.AverageId.Equals(averageString, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (averageDescriptors.Length == 1)
            {
                var bindingContextModel = averageDescriptors.Single();
                return new AverageDescriptor(bindingContextModel);
            }

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{averageString}' is not a recognized average period, use the averages endpoint to discover available averages");
            return null;
        }
    }
}
using System.Net;
using BrandVue.PublicApi.Models;
using BrandVue.Services;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.ModelBinding
{
    internal class MetricModelBinder : SurveyDependantModelBinderBase<MetricDescriptor>
    {
        protected override MetricDescriptor BindModelForSubset(ModelBindingContext bindingContext, Subset subset, string metricString)
        {            var claimRestrictedSubsetRepository = bindingContext.HttpContext.GetService<IClaimRestrictedSubsetRepository>();

            var metricDescriptors = bindingContext.HttpContext.GetService<IClaimRestrictedMetricRepository>()
                .GetAllowed(subset)
                .Where(m => m.UrlSafeName.Equals(metricString, StringComparison.OrdinalIgnoreCase)).ToArray();

            if (metricDescriptors.Length == 1)
            {
                var bindingContextModel = metricDescriptors.Single();
                return new MetricDescriptor(bindingContextModel);
            }

            bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{metricString}' is not a recognized metric for the requested subset, use the metrics endpoint to discover available metrics");
            return null;
        }
    }
}
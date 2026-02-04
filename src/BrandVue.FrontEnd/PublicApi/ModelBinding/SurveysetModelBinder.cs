using System.Net;
using BrandVue.PublicApi.Models;
using BrandVue.Services;
using BrandVue.SourceData.Respondents;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Vue.AuthMiddleware;

namespace BrandVue.PublicApi.ModelBinding
{
    internal class SurveysetModelBinder : RawStringModelBinderBase<SurveysetDescriptor>
    {
        protected override SurveysetDescriptor BindModel(ModelBindingContext bindingContext, string surveysetString)
        {
            var subsetRepository = bindingContext.HttpContext.GetService<IClaimRestrictedSubsetRepository>();
            if (!subsetRepository.HasSubsetWithAlias(surveysetString))
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, $"'{surveysetString}' is not a recognized surveyset, use the surveysets endpoint to discover available surveysets");
                return null;
            }

            var subset = subsetRepository.GetWithAlias(surveysetString);
            return new SurveysetDescriptor(subset);
        }
    }
}
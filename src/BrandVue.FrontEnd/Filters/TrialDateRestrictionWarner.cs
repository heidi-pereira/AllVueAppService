using BrandVue.Models;
using BrandVue.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrandVue.Filters
{
    /// <summary>
    /// Sets a flag so we can inform the user that the <see cref="TrialRestrictingMetricCalculationOrchestrator"/> has zeroed some of the output
    /// </summary>
    public class TrialDateRestrictionWarner : ActionFilterAttribute
    {
        public const string ItemKey = "Trial_Data_Was_Restricted";

        public override void OnActionExecuted(ActionExecutedContext actionExecutedContext)
        {
            if (!actionExecutedContext.HttpContext.Items.TryGetValue(ItemKey, out var value)
                || value is bool restricted && !restricted)
            {
                return;
            }

            if (actionExecutedContext.Result is ObjectResult objectContent
                && objectContent.Value is ICommonResultsInformation commonResults)
            {
                commonResults.TrialRestrictedData = true;
            }
        }
    }
}
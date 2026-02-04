using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vue.AuthMiddleware;

namespace BrandVue.Filters
{
    public class SubsetAuthorisationAttribute : ActionFilterAttribute
    {
        public string StringSubsetIdParameterName { get; }

        public SubsetAuthorisationAttribute(string stringSubsetIdParameterName = null) => StringSubsetIdParameterName = stringSubsetIdParameterName;

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var actionSubsetIds = actionContext.GetModelSubsetIds(StringSubsetIdParameterName);
            string[] unauthorisedSubsetIds = actionContext.HttpContext.UserHasAccessToSubsets(actionSubsetIds);
            if (unauthorisedSubsetIds.Length == 0)
            {
                base.OnActionExecuting(actionContext);
                return;
            }
            
            var jsonResult = new
            {
                message = $"Check subset access for {string.Join(", ", unauthorisedSubsetIds)}",
            };

            actionContext.Result = new JsonResult(jsonResult) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}
using BrandVue.PublicApi.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Vue.AuthMiddleware;

namespace BrandVue.Filters
{
    public class AuthorizedToExportResponseLevelDataAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var claimsProvider = actionContext.HttpContext.GetService<IUserContext>();

            if (claimsProvider.CanAccessRespondentLevelDownload())
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            var jsonResult = new
            {
                message = $"Not permitted to export response-level data",
                role = "Unauthorized",
            };

            actionContext.Result = new JsonResult(jsonResult) { StatusCode = StatusCodes.Status403Forbidden };
        }
    }

}
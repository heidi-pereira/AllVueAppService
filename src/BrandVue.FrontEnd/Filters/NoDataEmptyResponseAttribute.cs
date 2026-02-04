using System.Net;
using BrandVue.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BrandVue.Filters
{
    public class NoDataEmptyResponse : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result is ObjectResult objectContent
                && objectContent.Value is IDataResultsInformation commonResults
                && !commonResults.HasData)
            {
                context.Result = new NoContentResult();
            }
        }
    }
}
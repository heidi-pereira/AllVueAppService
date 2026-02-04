using BrandVue.EntityFramework.Exceptions;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BrandVue.Filters
{
    public class ValidateModelFilter : IActionFilter
    {

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        /// <summary>
        /// Pass through if model is valid, throw a known validation exception otherwise.
        /// </summary>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            ValidateNullArguments(context);
            ValidateArgumentValues(context);
        }

        private void ValidateNullArguments(ActionExecutingContext actionContext)
        {
            if (IsPublicApi(actionContext))
                return;

            actionContext.ActionArguments.ToList().ForEach(argument => 
                {
                    if (argument.Value is null)
                    {
                        throw new BadRequestException($"{argument.Key} cannot be null");
                    }
                });
        }

        private void ValidateArgumentValues(ActionExecutingContext actionContext)
        {
            if (actionContext.ModelState.IsValid)
            {
                return;
            }

            var errorMessage = GetErrorMessageFromModelState(actionContext.ModelState);
            if (IsGetRequestWithInvalidPath(actionContext))
            {
                throw new NotFoundException(errorMessage);
            }

            throw new BadRequestException(errorMessage);
        }

        private static bool IsGetRequestWithInvalidPath(ActionExecutingContext actionContext)
        {
            return actionContext.HttpContext.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                actionContext.ModelState.Any(arg => arg.Value.Errors.Any() && !actionContext.HttpContext.Request.Query.ContainsKey(arg.Key) && !arg.Key.Contains('.'));
        }

        /// <summary>
        /// Return a contatenation of one error message for each invalid field, e.g.: "startDate: value cannot be in the future, endDate: value cannot be in the past".
        /// </summary>
        private static string GetErrorMessageFromModelState(ModelStateDictionary modelState)
        {
            var serializableError = new SerializableError(modelState);
            var fieldErrors = serializableError.Select(kvp => new KeyValuePair<string, string>(kvp.Key, ((string[])kvp.Value).FirstOrDefault())).ToArray();
            if (fieldErrors.Length == 1 && string.IsNullOrWhiteSpace(fieldErrors.Single().Key))
            {
                var fieldError = fieldErrors.Single();
                // If the whole object is invalid, there will be one error with no Key and a Value.
                return string.IsNullOrWhiteSpace(fieldError.Value) ? "Invalid request. Check request parameters." : fieldError.Value;
            }

            return string.Join(',', fieldErrors.Select(fldError => $"{fldError.Key}: {fldError.Value}"));
        }

        private static bool IsPublicApi(ActionExecutingContext actionContext) => actionContext.HttpContext.GetOrCreateRequestScope().Resource == RequestResource.PublicApi;
    }
}
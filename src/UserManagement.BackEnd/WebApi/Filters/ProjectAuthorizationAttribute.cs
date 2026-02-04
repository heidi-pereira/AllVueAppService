using BrandVue.EntityFramework.MetaData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vue.Common.Auth;
using Vue.Common.Constants.Constants;

namespace UserManagement.BackEnd.WebApi.Filters
{
    public class ProjectAuthorizationAttribute(string companyIdParameterName, string projectTypeParameterName,
        string projectIdParameterName) : ActionFilterAttribute
    {
        public string CompanyIdParameterName { get; } = companyIdParameterName;
        public string ProjectTypeParameterName { get; } = projectTypeParameterName;
        public string ProjectIdParameterName { get; } = projectIdParameterName;

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userContext = context.HttpContext.RequestServices.GetRequiredService<IUserContext>();
            if (userContext.Role == Roles.SystemAdministrator || userContext.IsAuthorizedSavantaUser)
            {
                await next();
                return;
            }

            var companyId = GetParameterValue<string>(context, CompanyIdParameterName);
            var projectType = GetParameterValue<ProjectType>(context, ProjectTypeParameterName);
            var projectId = GetParameterValue<int>(context, ProjectIdParameterName);
            if (companyId != null && projectType != null && projectId != null)
            {
                var userDataPermissionsService = context.HttpContext.RequestServices
                    .GetRequiredService<Application.UserDataPermissions.Services.IUserDataPermissionsService>();
                var allVueUserDataPermission =
                    await userDataPermissionsService.GetByUserIdByCompanyAndProjectAsync(userContext.UserId, companyId,
                        new ProjectOrProduct(projectType, projectId), CancellationToken.None);
                if (allVueUserDataPermission != null)
                {
                    await next();
                    return;
                }
            }

            var jsonResult = new
            {
                error = "Access denied",
                userContext.Role,
                userContext.UserName,
                userContext.UserId
            };

            context.Result = new JsonResult(jsonResult) { StatusCode = StatusCodes.Status403Forbidden };
        }
        private static T? GetParameterValue<T>(ActionExecutingContext actionContext, string paramName)
        {
            T? parameterValue = default;
            if (actionContext.ActionArguments.TryGetValue(paramName, out var objModel) &&
                objModel is T strModel)
            {
                parameterValue = strModel;
            }

            return parameterValue;
        }
    }
}

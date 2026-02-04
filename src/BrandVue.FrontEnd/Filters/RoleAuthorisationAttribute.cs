using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Vue.AuthMiddleware;

namespace BrandVue.Filters
{
    public class RoleAuthorisationAttribute : ActionFilterAttribute
    {
        public string[] Roles { get; }
        public PermissionScope Scope { get; }

        /// <param name="roles">Roles except this and system admin will be forbidden)</param>
        /// <param name="permissionScope">If in doubt, leave as the defaults AllClients for safety. If the scope of the change is within their organisation, then pass SingleClient</param>
        public RoleAuthorisationAttribute(string roles, PermissionScope permissionScope = PermissionScope.AllClients)
        {
            Roles = [roles];
            Scope = permissionScope;
        }

        public override void OnActionExecuting(ActionExecutingContext actionContext)
        {
            var claimsProvider = actionContext.HttpContext.GetService<IUserContext>();

            string currentRole = claimsProvider.Role;

            if (Roles.Contains(currentRole) && CoversScope(actionContext.HttpContext) || currentRole == Vue.Common.Constants.Constants.Roles.SystemAdministrator)
            {
                base.OnActionExecuting(actionContext);
                return;
            }

            var jsonResult = new
            {
                message = $"Not permitted for {currentRole} login in scope {Scope}",
                role = currentRole
            };

            actionContext.Result = new JsonResult(jsonResult) { StatusCode = StatusCodes.Status403Forbidden };
        }

        /// <summary>
        /// Assumes you're authed for your client organisation
        /// </summary>
        private bool CoversScope(HttpContext ctx) => Scope switch
        {
            PermissionScope.AllClients => ctx.GetService<IProductContext>().HasSingleClient, // If there's only one client then authed for one == authed for all
            PermissionScope.SingleClient => true,
            _ => throw new NotImplementedException()
        };
    }

    public enum PermissionScope
    {
        /// <summary>
        /// e.g. Making a change to subsets which affects all clients
        /// </summary>
        AllClients,
        /// <summary>
        /// e.g. Making a change to colours for an organisation which only affects a single client
        /// </summary>
        SingleClient
    }
}
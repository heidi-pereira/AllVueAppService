using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Vue.AuthMiddleware;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    public class RequestScopeAccessHandler : AuthorizationHandler<RequestScopeAccessHandler.Requirement>
    {
        public class Requirement : IAuthorizationRequirement
        {
        }

        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestScopeAccessHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Requirement requirement)
        {
            if (_httpContextAccessor.HttpContext.IsAuthorizedWithinThisRequestScope())
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }
    }
}

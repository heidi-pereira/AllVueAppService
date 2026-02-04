using BrandVue.Middleware;
using BrandVue.SourceData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vue.AuthMiddleware;
using Vue.Common.Constants.Constants;

namespace BrandVue.AuthMiddleware.AuthorizationHandlers
{
    internal class TrialDateHandler : AuthorizationHandler<TrialDateHandler.Requirement>
    {
        public class Requirement : IAuthorizationRequirement
        {
        }

        private readonly IReadOnlySet<string> _allowedRolesByDefault = new HashSet<string> { Roles.SystemAdministrator, Roles.Administrator, Roles.User, Roles.ReportViewer };
        private readonly IUserContext _userContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TrialDateHandler> _logger;

        public TrialDateHandler(IUserContext userContext, IHttpContextAccessor httpContextAccessor, ILogger<TrialDateHandler> logger)
        {
            _userContext = userContext;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Requirement requirement)
        {
            var requestScope = _httpContextAccessor.HttpContext.GetOrCreateRequestScope();
            //Explicit check for public API or for roles that don't need to check for trial end date.
            if (requestScope.Resource == RequestResource.PublicApi || _allowedRolesByDefault.Contains(_userContext.Role))
            {
                context.Succeed(requirement);
            }
            else
            {
                //Fallback to checking the trial end date against the current date.
                //If not present play safe and don't allow this requirement to succeed
                var trialEndDate = _userContext.TrialEndDate ?? DateTimeOffset.MinValue;
                if (trialEndDate >= DateTimeOffset.Now.ToDateInstance())
                {
                    context.Succeed(requirement);
                }
                else
                {
                    var claimTypeValuePairs = context.User.Claims.Select(c => new { c.Type, c.Value });
                    _logger.LogInformation($"The trial has expired. The end date was {trialEndDate} and claims were: {claimTypeValuePairs.Stringify()}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
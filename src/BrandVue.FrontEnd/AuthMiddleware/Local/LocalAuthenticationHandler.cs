using System.IO;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BrandVue.Middleware;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vue.AuthMiddleware.Api;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware.Local
{
    public class LocalAuthenticationHandler : AuthenticationHandler<LocalAuthenticationOptions>
    {
        private readonly IOptions<AuthenticationOptions> _globalAuthOptions;

        public LocalAuthenticationHandler(IOptionsMonitor<LocalAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IOptions<AuthenticationOptions> globalAuthOptions) : base(options, logger, encoder, clock)
        {
            _globalAuthOptions = globalAuthOptions;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.IsLocalWithAuthBypass() || !Options.FabricateClaimsIfLocal)
            {
                return AuthenticateResult.NoResult();
            }

            //This will bypass auth, but this will double check that this is definitely local!
            var requestResource = Context.GetOrCreateRequestScope().Resource;
            bool signIn = requestResource == RequestResource.Ui || //For local debug without auth server
                          requestResource == RequestResource.InternalApi; // For reporting
            if (signIn || IsPublicApiWithValidLocalDebugApiKey(requestResource))
            {
                var claimsPrincipal = FabricatePrincipal();
                if (signIn)
                {
                    await Request.HttpContext.SignInAsync(SignInScheme, claimsPrincipal);
                }

                return AuthenticateResult.Success(new AuthenticationTicket(claimsPrincipal, SignInScheme));
            }

            return AuthenticateResult.Fail("No valid local auth");
        }

        private bool IsPublicApiWithValidLocalDebugApiKey(RequestResource requestResource)
        {
            return requestResource == RequestResource.PublicApi && ApiKeyIntrospectionEvents.IsValidLocalDebugApiKey(Context);
        }

        private ClaimsPrincipal FabricatePrincipal()
        {
            var claims = Constants.FabricateClaims(Context.GetOrCreateRequestScope().Organization, Options.Role, Options.Products, Options.Subsets, Options.Resources, Options.TrialEndDate);
            if (Options.ClaimTypes is not null)
            {
                claims = claims.Where(c => Options.ClaimTypes.Contains(c.Type)).ToArray();
            }

            var identity = new ClaimsIdentity(claims, SignInScheme, null, RequiredClaims.Role);
            var claimsPrincipal = new ClaimsPrincipal(identity);
            return claimsPrincipal;
        }

        private string SignInScheme => Options.SignInScheme ?? _globalAuthOptions.Value.DefaultSignInScheme ?? _globalAuthOptions.Value.DefaultScheme;
    }
}
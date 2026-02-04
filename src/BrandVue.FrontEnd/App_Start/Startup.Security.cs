using System.Net;
using System.Web;
using BrandVue.EntityFramework;
using BrandVue.Middleware;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NWebsec.Core.Common.Middleware.Options;
using Vue.AuthMiddleware;
using Vue.AuthMiddleware.OAuth.OpenIdConnect;
using Vue.Common.Constants.Constants;
using static Vue.AuthMiddleware.Constants;

namespace BrandVue
{
    public partial class Startup
    {
        public void AddSecurityHeaders(IApplicationBuilder app, ContentSecurityPolicy policy, AppSettings appSettings)
        {
            var cspSecurityOptions = CommonCspSecurityOptions(policy, appSettings);

            if (!AppSettings.IsDeployedEnvironmentOneOfThese(AppSettings.DevEnvironmentName))
            {
                cspSecurityOptions += (opts) =>
                {
                    opts.UpgradeInsecureRequests();
                };
                app.UseHsts(h => h.MaxAge(365).IncludeSubdomains().AllResponses().Preload());
            }

            app.UseCsp(cspSecurityOptions);

            app.UseXContentTypeOptions();
            app.UseReferrerPolicy(opts => opts.NoReferrer());
            app.UseXfo(options => options.Deny());
        }

        private static Action<IFluentCspOptions> CommonCspSecurityOptions(ContentSecurityPolicy policy, AppSettings appSettings)
        {
            //this is for UAT which needs to load stylesheet/savanta logo from beta auth (different domain)
            var authServerUrl = appSettings.GetAuthServerUrl().Replace("https://", "https://*.");
            var stylesheetUrl = $"{authServerUrl}/api/theme/stylesheet.css";
            var logoUrl = $"{authServerUrl}/logos/";
            return opts => opts
                .BlockAllMixedContent()
                .ScriptSources(s => s.Self().UnsafeInline().CustomSourcesIfAny(policy.ScriptSources.GetJoinedList()))
                .StyleSources(s => s.Self().UnsafeInline().CustomSourcesIfAny(policy.StyleSources.GetJoinedList().Append(stylesheetUrl).ToArray()))
                .FontSources(s => s.Self().CustomSourcesIfAny(policy.FontSources.GetJoinedList()))
                .FormActions(s => s.Self().CustomSourcesIfAny(policy.FormActions.GetJoinedList()))
                .FrameAncestors(s => s.Self().CustomSourcesIfAny(policy.FrameAncestors.GetJoinedList()))
                .ImageSources(s => s.Self().CustomSourcesIfAny(policy.ImageSources.GetJoinedList().Append(logoUrl).ToArray()));
        }

        private void ConfigureSavantaOpenId(OpenIdConnectOptions options)
        {
            options.Authority = AppSettings.GetAuthServerUrl();
            options.ClientId = AppSettings.GetSetting("authServerClientId");
            options.ClientSecret = AppSettings.GetSetting("authServerClientSecret");

            options.ClaimActions.Clear();
            foreach (var knownClaim in RequiredClaimTypes.Concat(OptionalClaimTypes))
            {
                options.ClaimActions.MapJsonKey(knownClaim, knownClaim);
            }

            options.RequireHttpsMetadata = !AppSettings.IsDeployedEnvironmentOneOfThese(AppSettings.DevEnvironmentName);
            options.UsePkce = true;
            options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
            options.Scope.Add("openid"); // default scope
            options.Scope.Add("profile"); // default scope
            options.Scope.Add("email");
            options.Scope.Add("role");
            options.Scope.Add("groups");
            options.GetClaimsFromUserInfoEndpoint = true;
            options.UseTokenLifetime = false;
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = false,
                RoleClaimType = RequiredClaims.Role
            };
            options.SaveTokens = true; // Required for logout

            options.Events = new OpenIdConnectEvents()
            {
                OnRedirectToIdentityProvider = context =>
                {
                    var requestScope = context.HttpContext.GetOrCreateRequestScope();
                    context.ProtocolMessage.RedirectUri = Notifications.GetRedirectUri(context.HttpContext);
                    context.ProtocolMessage.AcrValues = $"tenant:{requestScope.Organization}";
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    context.HandleResponse();
                    string reason = HttpUtility.UrlEncode(context.ProtocolMessage.ErrorDescription);
                    context.Response.Redirect(
                        $"{context.Request.Scheme}{Uri.SchemeDelimiter}{context.Request.Host}{context.Request.PathBase}" +
                        $"{LoginFailedRedirect}?{LoginFailureMessageQueryStringKey}={reason}");
                    return Task.CompletedTask;
                },
            };
        }

        private void ConfigureTokenIntrospection(OAuth2IntrospectionOptions options)
        {
            // In the ideal world we'd figure out how to map claims here, the same as the OpenIdScheme, but the middleware doesn't seem to have a way to do that
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            options.Authority = AppSettings.GetAuthServerUrl();
            options.ClientId = AppSettings.GetSetting("BrandVueApi.ApiResourceId");
            options.ClientSecret = AppSettings.GetSetting("BrandVueApi.ApiResourceSecret");
            options.EnableCaching = true;
        }
    }
}

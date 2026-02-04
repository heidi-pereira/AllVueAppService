using System.Reflection;
using System.Security.Claims;
using BrandVue.EntityFramework;
using IdentityModel;
using IdentityModel.AspNetCore.OAuth2Introspection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Newtonsoft.Json;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware
{
    public static class Constants
    {
        public static class RequestHeaders
        {
            public const string UserAgent = "User-Agent";
        }

        public static class Schemes
        {
            /// <summary>
            /// Fabricates claims if local, otherwise 401
            /// </summary>
            public const string LocalOrUnauthorizedScheme = "LocalOrUnauthorized";
            /// <summary>
            /// Always checked first - you can auth with a cookie even for the public API
            /// </summary>
            public const string CookieScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            /// <summary>
            /// Used in setting a cookie to share authorisation details with reporting app
            /// </summary>
            public const string ReportingCookie = "ReportingCookie";
            /// <summary>
            /// Redirect the browser to Savanta's auth server to sign in
            /// </summary>
            public const string OpenIdScheme = OpenIdConnectDefaults.AuthenticationScheme;
            /// <summary>
            /// Use a back-channel to the auth server to get the claims for the API key
            /// </summary>
            public const string ApiKeyScheme = OAuth2IntrospectionDefaults.AuthenticationScheme;
            /// <summary>
            /// Forwards on to the correct auth for the requested resource
            /// </summary>
            public static string AuthForResource = nameof(AuthForResource);
        }
        public static class PolicyNames
        {
            public const string SavantaEmailPolicy = "SavantaEmailPolicy";
        }

        public static class RateLimitPolicyNames
        {
            public const string ApiSlidingWindow = nameof(ApiSlidingWindow);
        }

        public const string LoginFailedRedirect = "/account/loginfailure/";
        public const string LoginFailureMessageQueryStringKey = "error";
        public const string UnauthorizedForProduct = "You do not have access to this application.";
        public const string OpenIdConnectSignInPath = "/signin-oidc";

        public const string AuthServerIdentityProvider = "local"; // that denotes our 'local' auth server, as apposed to a third party

        // this cookie needs to be shared with reporting, so change with care
        public const string CookieName = ".AspNet.SharedCookie";

        public const string AllSubsetsForProduct = "AllSubsets";

        public static class ResourceNames
        {
            public const string MetricResults = "metricresultsapi";
            public const string RawSurveyData = "surveyresponseapi";

            public static readonly IReadOnlyDictionary<string, string> DisplayNames = new Dictionary<string, string>
            {
                {MetricResults, "Metric Results API"},
                {RawSurveyData, "Survey Response API"}
            };
        }

        private static IEnumerable<string> EnumerateClaimTypesFromConstants<T>()
        {
            // get all the static string field values from the type parameter
            return typeof(T)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => (string) f.GetValue(null));
        }

        private static IReadOnlySet<string> _requiredClaimTypes;

        public static IReadOnlySet<string> RequiredClaimTypes =>
            _requiredClaimTypes ??= new HashSet<string>(EnumerateClaimTypesFromConstants<RequiredClaims>());

        private static IEnumerable<string> _optionalClaimTypes;

        public static IEnumerable<string> OptionalClaimTypes =>
            _optionalClaimTypes ??= EnumerateClaimTypesFromConstants<OptionalClaims>();

        public static Claim[] FabricateClaims(string actAsAuthOrganization, string role,
            IReadOnlyCollection<string> products, IReadOnlyCollection<string> subsets,
            IReadOnlyCollection<string> resources, string trialEndDate = null)
        {
            return FabricateClaims(actAsAuthOrganization, role,
                products.ToDictionary(p => p, p => subsets.ToArray()),
                products.ToDictionary(p => p, p => resources.ToArray()),
                trialEndDate
            );
        }

        private static Claim[] FabricateClaims(string actAsAuthOrganization, string role, IDictionary<string, string[]> productsToSubsets, IDictionary<string, string[]> productsToResources,
            string trialEndDate = null)
        {
            var claims = new List<Claim>
            {
                new Claim(RequiredClaims.UserId, "LocalUserId"),
                new Claim(RequiredClaims.Username, "tech@savanta.com"),
                new Claim(RequiredClaims.FirstName, "Local User"),
                new Claim(RequiredClaims.LastName, "Savanta"),
                new Claim(RequiredClaims.Role, role),
                new Claim(RequiredClaims.IdentityProvider, "anon"),
                new Claim(RequiredClaims.Products, JsonConvert.SerializeObject(productsToSubsets.Keys.ToArray())),
                new Claim(RequiredClaims.Subsets, JsonConvert.SerializeObject(productsToSubsets)),
                new Claim(RequiredClaims.CurrentCompanyShortCode, actAsAuthOrganization),
                new Claim(OptionalClaims.UserCompanyShortCode, actAsAuthOrganization),
                new Claim(OptionalClaims.Groups, JsonConvert.SerializeObject(new[] { new AppSettings().GetSetting("AzureADGroupCanAccessRespondentLevelDownload") })),
            };
            if (productsToResources != null) claims.Add(new Claim(OptionalClaims.BrandVueApi, JsonConvert.SerializeObject(productsToResources)));
            if (trialEndDate != null) claims.Add(new Claim(OptionalClaims.TrialEndDate, trialEndDate));
            return claims.ToArray();
        }

        public static readonly string[] AllProducts = new [] {
            "brandvue", // AKA "BrandVue 360"
            "charities",
            "covid19",
            "eatingout",
            "eatingoutv2",
            "barometer",
            "deliveroo",
            "delivervue",
            "drinks",
            "finance",
            "retail",
            "survey",
            "wealth",
        };

        public static readonly string[] AllResourceNames = ResourceNames.DisplayNames.Keys.ToArray();

        public const string PublicApiResourcePolicyPrefix = "PublicApiResource_";
        public const string UserRoleOrAbove = "UserRoleOrAbove";
        public const int HotDevPort = 8082;
        public const string SavantaCompany = "savanta";
    }
}
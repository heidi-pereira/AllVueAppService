using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware.Local
{
    public class LocalAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string SignInScheme { get; set; }
        public bool FabricateClaimsIfLocal { get; set; } = true;
        public string Role { get; set; } = Roles.SystemAdministrator;
        public string[] Products { get; set; } = Constants.AllProducts;

        public string[] Subsets { get; set; } =
        {
            Constants.AllSubsetsForProduct
        };

        public string[] Resources { get; set; } = Constants.AllResourceNames;
        public string TrialEndDate { get; set; } = null;

        /// <summary>
        /// Useful for restricting claim types before user identity is created for testing purposes
        /// </summary>
        public IReadOnlySet<string> ClaimTypes { get; set; } = null;
    }
}
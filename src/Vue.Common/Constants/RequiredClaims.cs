using IdentityModel;

namespace Vue.Common.Constants.Constants;

public class RequiredClaims
{
    // NOTE: ensure that the wsgn claims parser and local auth claims
    // array are updated if you add to or remove from this list of static consts.
    public const string UserId = JwtClaimTypes.Subject;
    public const string Username = JwtClaimTypes.Name;
    public const string FirstName = JwtClaimTypes.GivenName;
    public const string LastName = JwtClaimTypes.FamilyName;
    public const string Role = JwtClaimTypes.Role;
    public const string IdentityProvider = "http://schemas.microsoft.com/identity/claims/identityprovider";
    public const string Products = "morar_hpi_products";
    public const string Subsets = "morar_hpi_subsets";
    public const string CurrentCompanyShortCode = "morar_hpi_company_name";
}
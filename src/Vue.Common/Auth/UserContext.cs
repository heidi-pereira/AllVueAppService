using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Vue.Common.Constants;
using Vue.Common.Constants.Constants;
using Vue.Common.Extensions;

namespace Vue.Common.Auth
{
    public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
    {
        private const int NumberOfMonthToRestrictForTrial = 1;
        private const string ExternalCompanyClaim = "wgsn";
        private IReadOnlyCollection<Claim>? _claims = null;
        private readonly string[] _orgsAllowingExternalCompanyClaim = [ExternalCompanyClaim];
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        private readonly object _claimsLock = new();

        public IReadOnlyCollection<Claim> Claims {
            get
            {
                lock (_claimsLock)
                {
                    return _claims ?? GetClaims();
                }
            }
        }

        private IReadOnlyCollection<Claim> GetClaims()
        {
            return GetCurrentPrincipal()?.Claims.ToArray() ?? [];
        }
        
        private ClaimsIdentity GetCurrentPrincipal()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var currentUser = httpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

            return currentUser.Identity as ClaimsIdentity ?? new ClaimsIdentity();
        }

        public string UserId => Claims.GetClaimValue(RequiredClaims.UserId);
        public bool IsThirdPartyLoginAuth => Claims.GetClaimValue(RequiredClaims.IdentityProvider) != AuthConstants.AuthServerIdentityProvider;
        public bool IsAdministrator => Role.Contains("Administrator");
        public bool IsSystemAdministrator => Role == Roles.SystemAdministrator;
        public bool IsReportViewer => Role == Roles.ReportViewer;
        public bool IsTrialUser => Role == Roles.TrialUser;
        public bool CanEditMetricAbouts
        {
            get
            {
                try
                {
                    return IsSystemAdministrator && Claims.GetNullableClaimValue<bool>(OptionalClaims.CanEditMetricAbouts) == true;
                }
                catch
                {
                    return false;
                }
            }
        }
        public DateTime? TrialEndDate => Claims.GetNullableClaimValue<DateTime>(OptionalClaims.TrialEndDate);
        public DateTimeOffset GetTrialDataRestrictedDate(DateTimeOffset subsetEndDate) => subsetEndDate.GetLastDayOfMonthOnOrPreceding().AddMonths(-1 * NumberOfMonthToRestrictForTrial);
        public string UserName => Claims.GetClaimValue(RequiredClaims.Username);
        public string Role => Claims.GetClaimValue(RequiredClaims.Role);

        public string UserOrganisation
        {
            get
            {
                var company = Claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);

                if (!_orgsAllowingExternalCompanyClaim.Contains(company, StringComparer.InvariantCultureIgnoreCase))
                    return company;

                var externalCompany = Claims.GetClaimValue(OptionalClaims.ExternalCompany);

                return string.IsNullOrWhiteSpace(externalCompany)
                    ? company
                    : externalCompany;
            }
        }
        public IReadOnlyCollection<string> SecurityGroups
        {
            get
            {
                try
                {
                    return Claims.GetClaimValue<string[]>(OptionalClaims.Groups);
                }
                catch
                {
                    return Array.Empty<string>();
                }
            }
        }
        public bool HasSecurityGroupAccess(string securityGroup) => SecurityGroups.Contains(securityGroup);

        public void FreezeClaims()
        {
            lock (_claimsLock)
            {
                _claims = GetClaims();
            }
        }

        public string AuthCompany => Claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);
        public string[] Products => Claims.GetClaimValue<string[]>(RequiredClaims.Products);
        public string FirstName => Claims.GetClaimValue(RequiredClaims.FirstName);
        public string LastName => Claims.GetClaimValue(RequiredClaims.LastName);
        public string AccountName
        {
            get
            {
                var company = Claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);

                if (!_orgsAllowingExternalCompanyClaim.Contains(company, StringComparer.InvariantCultureIgnoreCase))
                    return company;

                var externalCompany = Claims.GetClaimValue(OptionalClaims.ExternalCompany);

                return string.IsNullOrWhiteSpace(externalCompany)
                    ? company
                    : externalCompany;
            }
        }
        public bool IsInSavantaRequestScope => AuthConstants.SavantaCompany.Equals(AuthCompany, StringComparison.OrdinalIgnoreCase);
        public string UserCompanyShortCode => Claims.GetClaimValue(OptionalClaims.UserCompanyShortCode);
        public bool IsAuthorizedSavantaUser => AuthConstants.SavantaCompany.Equals(UserCompanyShortCode, StringComparison.OrdinalIgnoreCase);

        
    }
    
    public static class UserContextExtensions
    {
        public static bool IsAuthorizedWithinThisCompanyScope(this IUserContext userContext, string securityGroup)
        {
            return userContext.IsAuthorizedSavantaUserWithSecurityGroup(securityGroup) || userContext.IsAuthorizedExternalUser();
        }
        private static bool IsAuthorizedExternalUser(this IUserContext userContext)
        {
            return !userContext.IsAuthorizedSavantaUser;
        }
        private static bool IsAuthorizedSavantaUserWithSecurityGroup(this IUserContext userContext, string securityGroup)
        {
            return userContext.IsAuthorizedSavantaUser && (string.IsNullOrEmpty(securityGroup) || userContext.HasSecurityGroupAccess(securityGroup));
        }

    }
}
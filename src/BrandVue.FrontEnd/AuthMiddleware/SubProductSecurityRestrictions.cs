using BrandVue.EntityFramework;
using System.Security.Claims;
using System.Threading;
using Vue.Common.AuthApi;
using Vue.Common.Constants.Constants;

namespace Vue.AuthMiddleware
{
    public interface ISubProductSecurityRestrictions
    {
        bool IsAuthorizedForThisOrganisation(IReadOnlyCollection<Claim> claims);
        Task<bool> IsAuthorizedForThisProject(IReadOnlyCollection<Claim> claims, CancellationToken cancellationToken);
        IReadOnlyCollection<string> RequiredCompanyShortcodes { get; }
        string GetStringDescription();
    }
    public record SurveyCompanyShortCodeRequirement(int SurveyId, string[] OwnedAndSharedCompanyShortCodes)
    {
        public override string ToString()
        {
            return $"SurveyId: {SurveyId}, CompanyShortCodes: [{string.Join(", ", OwnedAndSharedCompanyShortCodes)}]";
        }
    }
    public class SubProductSecurityRestrictions
    {

        private class UnrestrictedSubProductSecurityRestrictions : ISubProductSecurityRestrictions
        {
            public bool IsAuthorizedForThisOrganisation(IReadOnlyCollection<Claim> claims)
            {
                return true;
            }
            public IReadOnlyCollection<string> RequiredCompanyShortcodes => [];
            public Task<bool> IsAuthorizedForThisProject(IReadOnlyCollection<Claim> claims,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
            public string GetStringDescription()
            {
                return "Unrestricted: Requires system admin: No";
            }
        }

        private class RestrictedSubProductSecurityRestrictions: ISubProductSecurityRestrictions
        {
            private readonly string _requiredShortCode;
            private readonly string _requiredProjectId;
            private readonly IAuthApiClient _authApiClient;
            private readonly IPermissionService _dataPermissionsService;
            private readonly IReadOnlyCollection<string> _requiredSecurityGroupIds;
            private readonly IReadOnlyCollection<SurveyCompanyShortCodeRequirement> _requiredCompaniesBySurvey;


            public RestrictedSubProductSecurityRestrictions
            (
                IEnumerable<string> requiredSecurityGroupIds,
                IEnumerable<SurveyCompanyShortCodeRequirement> requiredCompaniesBySurvey,
                string requiredProjectId,
                IAuthApiClient authApiClient,
                IPermissionService dataPermissionsService
            )
            {
                if (authApiClient is null)
                    throw new ArgumentNullException(nameof(authApiClient));

                if (dataPermissionsService is null)
                    throw new ArgumentNullException(nameof(dataPermissionsService));

                if (string.IsNullOrWhiteSpace(requiredProjectId))
                    throw new ArgumentException("Argument cannot be null, empty, or whitespace.", nameof(requiredProjectId));

                _requiredShortCode = SavantaConstants.AllVueShortCode;
                _requiredProjectId = requiredProjectId;
                _requiredCompaniesBySurvey = requiredCompaniesBySurvey.ToArray();
                _requiredSecurityGroupIds = requiredSecurityGroupIds.ToArray();
                _authApiClient = authApiClient;
                _dataPermissionsService = dataPermissionsService;
            }

            public bool IsAuthorizedForThisOrganisation(IReadOnlyCollection<Claim> claims) =>
                IsAuthorizedSavantaUser(claims) || IsAuthorizedExternalUser(claims);

            private static string GetCompanyShortCode(IReadOnlyCollection<Claim> claims) =>
                claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);

            private bool IsAuthorizedSavantaUser(IReadOnlyCollection<Claim> claims)
            {
                string company = GetCompanyShortCode(claims);
                return Constants.SavantaCompany.Equals(company, StringComparison.OrdinalIgnoreCase) &&
                       _requiredSecurityGroupIds.All(g =>
                           claims.Any(c => c.Type == OptionalClaims.Groups && c.Value == g));
            }

            private bool IsAuthorizedExternalUser(IReadOnlyCollection<Claim> claims)
            {
                return AreAllSurveysAuthorizedThroughAnyOwnedOrSharedCompany(GetCompanyShortCode(claims));
            }

            private bool AreAllSurveysAuthorizedThroughAnyOwnedOrSharedCompany(string companyShortCode)
            {
                return _requiredCompaniesBySurvey.All(c =>
                    c.OwnedAndSharedCompanyShortCodes.Any(x => string.Equals(x, companyShortCode, StringComparison.OrdinalIgnoreCase))
                );
            }
            public IReadOnlyCollection<string> RequiredCompanyShortcodes => _requiredCompaniesBySurvey.SelectMany(x => x.OwnedAndSharedCompanyShortCodes).ToArray();

            public async Task<bool> IsAuthorizedForThisProject(IReadOnlyCollection<Claim> claims, CancellationToken cancellationToken)
            {
                string userId = claims.GetClaimValue(RequiredClaims.UserId);
                string orgShortCode = claims.GetClaimValue(RequiredClaims.CurrentCompanyShortCode);

                return !string.IsNullOrWhiteSpace(userId)
                    && !string.IsNullOrWhiteSpace(orgShortCode)
                    && ( (await _authApiClient.CanUserAccessProject(orgShortCode, userId, _requiredProjectId, cancellationToken)) ||
                         (await UserHasDataPermissions(userId, orgShortCode, cancellationToken)) );
            }

            private async Task<bool> UserHasDataPermissions(string userId, string companyShortCode, CancellationToken token)
            {
                var company = await _authApiClient.GetCompanyByShortcode(companyShortCode, token);
                var permissions = await _dataPermissionsService.GetUserDataPermissionForCompanyAndProjectAsync(company.Id, _requiredShortCode, _requiredProjectId, userId);
                return permissions != null;
            }

            public virtual string GetStringDescription()
            {
                return $@"Required security group IDs: {string.Join(", ", _requiredSecurityGroupIds)}
Required companies: {string.Join(", ", _requiredCompaniesBySurvey)}
";
            }
        }


        public static ISubProductSecurityRestrictions Unrestricted()
        {
            return new UnrestrictedSubProductSecurityRestrictions();
        }

        public static ISubProductSecurityRestrictions Restricted(IEnumerable<string> savantaSecurityGroupsRequired,
            IEnumerable<SurveyCompanyShortCodeRequirement> externalCompaniesRequired, string projectIdRequired, IAuthApiClient authApiClient, IPermissionService dataPermissionsService)
        {
            return new RestrictedSubProductSecurityRestrictions(savantaSecurityGroupsRequired,
                externalCompaniesRequired, projectIdRequired, authApiClient, dataPermissionsService);
        }
    }
}
using Vue.Common.Auth.Permissions;

namespace Vue.Common.Auth.Ui
{
    public record UserContext(
            string UserOrganisation,
            string AuthCompany,
            string UserName,
            string Role,
            string[] Products,
            string FirstName,
            string LastName,
            string AccountName,
            string UserId,
            bool IsThirdPartyLoginAuth,
            bool IsAdministrator,
            bool IsSystemAdministrator,
            bool IsReportViewer,
            bool IsTrialUser,
            bool CanEditMetricAbouts,
            DateTime? TrialEndDate,
            bool IsInSavantaRequestScope,
            string UserCompanyShortCode,
            bool IsAuthorizedSavantaUser,
            IReadOnlyCollection<PermissionFeatureOptionWithCode> FeaturePermissions
        ) : IUserContext
    {
        public static UserContext FromUserContext(IUserContextBase userContext)
        {
            return new UserContext(userContext.UserOrganisation,
                userContext.AuthCompany,
                userContext.UserName,
                userContext.Role,
                userContext.Products,
                userContext.FirstName,
                userContext.LastName,
                userContext.AccountName,
                userContext.UserId,
                userContext.IsThirdPartyLoginAuth,
                userContext.IsAdministrator,
                userContext.IsSystemAdministrator,
                userContext.IsReportViewer,
                userContext.IsTrialUser,
                userContext.CanEditMetricAbouts,
                userContext.TrialEndDate,
                userContext.IsInSavantaRequestScope,
                userContext.UserCompanyShortCode,
                userContext.IsAuthorizedSavantaUser,
                new List<PermissionFeatureOptionWithCode>());
        }

        public static UserContext FromUserContextAndPermissions(IUserContextBase userContext,
            List<PermissionFeatureOptionWithCode> featurePermissions)
        {
            return FromUserContext(userContext) with { FeaturePermissions = featurePermissions };
        }
    }
}

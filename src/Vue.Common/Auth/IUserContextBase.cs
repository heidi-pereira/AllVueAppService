namespace Vue.Common.Auth
{
    public interface IUserContextBase
    { 
        /// This will return the organisation the user works for. This isn't necessarily the auth company
        /// e.g. The user org could be New Look, and the auth company would be WGSN
        /// </summary>
        string UserOrganisation { get; }
        /// <summary>
        /// This will return the auth server company such as WGSN.
        /// </summary>
        string AuthCompany { get; }
        string UserName { get; }
        string Role { get; }
        string[] Products { get; }
        string FirstName { get; }
        string LastName { get; }
        string AccountName { get; }
        string UserId { get; }
        bool IsThirdPartyLoginAuth { get; }
        bool IsAdministrator { get; }
        bool IsSystemAdministrator { get; }
        bool IsReportViewer { get; }
        bool IsTrialUser { get; }
        bool CanEditMetricAbouts { get; }
        DateTime? TrialEndDate { get; }
        public bool IsInSavantaRequestScope { get; }
        public string UserCompanyShortCode { get; }
        public bool IsAuthorizedSavantaUser { get; }
    }
}

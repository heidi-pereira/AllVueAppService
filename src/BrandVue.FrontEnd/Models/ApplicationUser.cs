using Vue.Common.Auth.Permissions;

namespace BrandVue.Models;

public class ApplicationUser
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Name { get; set; }
    public string Surname { get; set; }
    public string AccountName { get; set; }
    public IEnumerable<string> Products { get; set; }
    public bool IsAdministrator { get; set; }
    public bool IsSystemAdministrator { get; set; }
    public bool IsThirdPartyLoginAuth { get; set; }
    public bool IsReportViewer { get; set; }
    public bool IsTrialUser { get; set; }
    public bool CanEditMetricAbouts { get; set; }
    public bool CanAccessRespondentLevelDownload { get; set; }
    public string RunningEnvironmentDescription { get; set; }
    public RunningEnvironment RunningEnvironment { get; set; }

    //Note this is a very simplistic authorization model which will be replaced in the coming months
    //hopefully with the work on AllVue Authorization
    public bool DoesUserHaveAccessToInternalSavantaSystems { get; set; }
    
    /// <summary>
    /// Feature-based permissions for the user. This is the modern permission system
    /// for more granular, role-based access control model.
    /// </summary>
    public IReadOnlyCollection<PermissionFeatureOptionWithCode> FeaturePermissions { get; set; } = new List<PermissionFeatureOptionWithCode>();

    public DataPermissionDto? DataPermission { get; set; }
}
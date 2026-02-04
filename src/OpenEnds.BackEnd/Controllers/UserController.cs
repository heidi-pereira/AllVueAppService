using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenEnds.BackEnd.Library;
using Vue.Common.Auth;
using Vue.Common.Extensions;

namespace OpenEnds.BackEnd.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class UserController(ILogger<UserController> logger,
    IUserContext userContext,
    IUserFeaturePermissionsService userFeaturePermissionsService,
    ThemeClient themeClient,
    IOptions<Settings> settings,
    OpenEndsService openEndsService) : ControllerBase
{
    [HttpGet("getglobaldetails/{surveyId?}")]
    public async Task<IActionResult> GetGlobalDetails(string? surveyId)
    {
        var (surveyName, surveyConfig) = surveyId is not null ? await openEndsService.GetSurveyDetails(surveyId) : (null, null);

        var faviconUrl = await themeClient.GetFaviconUrl(Request.OriginalUrl());

        var host = Request.Host.Host == "localhost" ? settings.Value.OverrideLocalOrg + ".all-vue.com" : Request.Host.Host;
        var stylesheetUrl = "https://" + host + "/auth/api/theme/stylesheet.css";

        // Ensure feature permissions are loaded before returning response
        var featurePermissions = await userFeaturePermissionsService.GetFeaturePermissionsAsync();

        return new OkObjectResult(new
        {
            settings.Value.OverrideLocalOrg,
            settings.Value.MixPanelToken,
            User = new
            {
                userContext.IsAdministrator,
                userContext.IsAuthorizedSavantaUser,
                userContext.IsInSavantaRequestScope,
                userContext.IsReportViewer,
                userContext.IsSystemAdministrator,
                userContext.IsThirdPartyLoginAuth,
                userContext.IsTrialUser,
                userContext.UserOrganisation,
                userContext.UserCompanyShortCode,
                userContext.FirstName,
                userContext.LastName,
                userContext.UserName,
                FeaturePermissions = featurePermissions
            },
            FaviconUrl = faviconUrl,
            BasePath = "/" + settings.Value.ApplicationBasePath,
            StylesheetUrl = stylesheetUrl,
            settings.Value.MaxTexts,
            SurveyName = surveyName,
            NavigationTabs = surveyConfig?.GetNavigationTabs(),
            CustomUiIntegrations = surveyConfig?.GetCustomUIIntegrations()
        });
    }
}
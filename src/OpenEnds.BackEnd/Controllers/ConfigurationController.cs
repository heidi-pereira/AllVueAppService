using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenEnds.BackEnd.Library;

namespace OpenEnds.BackEnd.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class ConfigurationController(
    ILogger<ConfigurationController> logger,
    SavantaTextThemeAnalyzer savantaTextThemeAnalyzer,
    OpenEndsContext ctx,
    ThemeClient themeClient,
    IOptions<Settings> settings) : ControllerBase
{
    [HttpPost("surveys/{surveyId}/questions/{questionId}/configuration/themes/preview")]
    public async Task<IActionResult> PreviewMatch(string surveyId, int questionId, [FromBody] MatchPayload themePreviewPayload)
    {
        var result = await savantaTextThemeAnalyzer.GetMatchPreview(surveyId, questionId, themePreviewPayload.ThemeName);
        return Ok(result);
    }
    
    [HttpPost("surveys/{surveyId}/questions/{questionId}/configuration/themes/{themeId}/previewstats")]
    public async Task<IActionResult> PreviewStats(string surveyId, int questionId, int themeId, [FromBody] MatchPatternPayload matchPatterns)
    {
        var result = await savantaTextThemeAnalyzer.GetStatsPreview(surveyId, questionId, themeId, matchPatterns.Threshold, matchPatterns.Keywords, matchPatterns.MatchingExamples);
        return Ok(result);
    }
    
    [HttpPost("surveys/{surveyId}/questions/{questionId}/configuration/themes")]
    public async Task<IActionResult> AddTheme(string surveyId, int questionId, [FromBody] CreateThemePayload createTheme)
    {
        await savantaTextThemeAnalyzer.CreateTheme(surveyId, questionId, createTheme.ThemeName, createTheme.Keywords);
        return Ok();
    }

    [HttpDelete("surveys/{surveyId}/questions/{questionId}/configuration/themes/{themeId}")]
    public async Task<IActionResult> DeleteTheme(string surveyId, int questionId, int themeId)
    {
        await savantaTextThemeAnalyzer.DeleteTheme(surveyId, questionId, themeId);
        return Ok();
    }

    [HttpGet("surveys/{surveyId}/questions/{questionId}/configuration/themes")]
    public async Task<IActionResult> GetThemes(string surveyId, int questionId)
    {
        var themeConfigurations = await savantaTextThemeAnalyzer.GetThemes(surveyId, questionId);
        return Ok(new
        {
            Themes = themeConfigurations
        });
    }

    [HttpPut("surveys/{surveyId}/questions/{questionId}/configuration/themes/{themeId}/parent")]
    public async Task<IActionResult> UpdateThemeParent(string surveyId, int questionId, int themeId, int? newParentId)
    {
        await savantaTextThemeAnalyzer.UpdateThemeParent(surveyId, questionId, themeId, newParentId);
        return Ok();
    }

    [HttpPut("surveys/{surveyId}/questions/{questionId}/configuration/themes/{themeId}")]
    public async Task<IActionResult> UpdateThemeConfiguration(string surveyId, int questionId, int themeId, [FromBody]ThemeConfigurationUpdate configurationUpdate)
    {
        await savantaTextThemeAnalyzer.UpdateThemeConfiguration(surveyId, questionId, themeId,
            configurationUpdate.Sensitivity, configurationUpdate.MatchingExamples, configurationUpdate.Keywords, configurationUpdate.DisplayName);
        return Ok();
    }
    
    [HttpPatch("surveys/{surveyId}/questions/{questionId}/configuration/themes/{themeId}/merge")]
    public async Task<IActionResult> UpdateThemeSensitivity(string surveyId, int questionId, int themeId, [FromBody]ThemeMerge themeMerge)
    {
        await savantaTextThemeAnalyzer.MergeTheme(surveyId, questionId, themeId, themeMerge.targetThemeId);
        return Ok();
    }

    [HttpGet("surveys/{surveyId}/questions/{questionId}/configuration/themes/{themeId}/sensitivity")]
    public async Task<IActionResult> GetThemeSensitivity(string surveyId, int questionId, int themeId)
    {
        var themeSensitivityConfigurations = await savantaTextThemeAnalyzer.GetThemeSensitivity(surveyId, questionId, themeId);
        return Ok(themeSensitivityConfigurations);
    }

    public class ThemeConfigurationUpdate
    {
        public double Sensitivity { get; set; }
        public string[] MatchingExamples { get; set; }
        public string[] Keywords { get; set; }
        public string? DisplayName { get; set; }
    }

    public class CreateThemePayload
    {
        public string ThemeName { get; set; }
        public string[]? Keywords { get; set; }
    }

    public class ThemeMerge
    {
        public int targetThemeId { get; set; }
    }

    public class MatchPatternPayload
    {
        public string[] MatchingExamples { get; set; }
        public string[] Keywords { get; set; }
        public double Threshold { get; set; }
    }
    public class MatchPayload
    {
        public string ThemeName { get; set; }
    }
}



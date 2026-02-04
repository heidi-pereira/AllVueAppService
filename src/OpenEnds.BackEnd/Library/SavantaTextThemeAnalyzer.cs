using Azure;
using OpenEnds.BackEnd.Model;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenEnds.BackEnd.Library;

public class SavantaTextThemeAnalyzer(HttpClient httpClient, OpenEndsContext ctx, IDataGroupProjectService dataGroupProjectService)
{
    public async Task<OpenEndQuestionSummaryResult> RetrieveSummary(string surveyId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);

        var project = await GetProject(projectId);
        var summaryResult = await GetSummaryResult(surveyId, questionId);
        var dataResult = await GetDataResult(projectId);

        var openEndThemes = CalculateThemePercentages(dataResult);

        return new OpenEndQuestionSummaryResult
        {
            Summary = summaryResult!.Summary,
            TextThemes = dataResult.Data.Select(d => new TextTheme { Id = d.Id, Text = d.Text, Themes = d.Themes }),
            Themes = openEndThemes,
            AdditionalInstructions = ExtractInstructionsFromAdditionalContext(project?.AdditionalContext)
        };
    }

    private async Task<SummaryResult?> GetSummaryResult(string surveyId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);

        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"/v2/projects/{projectId}/summary", content);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var summaryResult = JsonSerializer.Deserialize<SummaryResult>(responseContent);
        return summaryResult;
    }

    public async Task<DataResult?> GetDataResult(string projectId)
    {
        var response = await httpClient.GetAsync($"/v2/projects/{projectId}/data");

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var dataResult = JsonSerializer.Deserialize<DataResult>(responseContent);
        return dataResult;
    }

    private List<OpenEndTheme> CalculateThemePercentages(DataResult? dataResult)
    {
        if (dataResult == null) return [];

        var totalItems = dataResult.Data.Count;

        var themeCounts = dataResult.Data
            .SelectMany(dataItem => dataItem.Themes)
            .GroupBy(themeIndex => dataResult.Themes[themeIndex].Name)
            .ToDictionary(group => group.Key, group => group.Count());

        var themePercentages = dataResult.Themes
            .Select((theme, index) => new OpenEndTheme
            {
                ThemeId = theme.Id,
                ThemeSensitivity = theme.MatchingBehaviour.MatchingSensitivity,
                ThemeText = theme.Name,
                ThemeIndex = index,
                Count = themeCounts.GetValueOrDefault(theme.Name, 0),
                Percentage = themeCounts.TryGetValue(theme.Name, out int themeCount) ? (double)themeCount / totalItems * 100 : 0,
                ParentId = theme.ParentId
            })
            .OrderByDescending(t => t.Percentage)
            .ToList();

        return themePercentages;
    }

    public class SummaryResult
    {
        [JsonPropertyName("summary")] public required string Summary { get; set; }
    }

    public class DataResult
    {
        [JsonPropertyName("themes")]
        public List<ThemeConfiguration> Themes { get; set; } = new();

        [JsonPropertyName("top10")]
        public List<List<string>> Top10 { get; set; } = new();

        [JsonPropertyName("data")]
        public List<DataItem> Data { get; set; } = new();
    }

    public class DataItem
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("themes")]
        public List<int> Themes { get; set; } = new();
    }

    public async Task ProcessProjectThemes(string surveyId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);

        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"/v2/projects/{projectId}/themes/process", content);
        response.EnsureSuccessStatusCode();
    }

    internal class DataTextUploadContainer
    {
        [JsonPropertyName("items")]
        public IEnumerable<DataTextUpload> Items { get; set; }
    }

    public class DataTextUpload
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }

    }

    public async Task<int> UploadData(string projectId, IList<OpenEndResponse> data)
    {
        var dataContainer = new DataTextUploadContainer
        {
            Items = data.Select(a => new DataTextUpload
            {
                Id = UniqueIdFromResponseAndQuestionAndChoice(a.ResponseId, a.SectionChoiceId, a.QuestionChoiceId),
                Text = a.AnswerText
            })
        };

        // Check for duplicate Ids
        var duplicateIds = dataContainer.Items
            .GroupBy(item => item.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicateIds.Any())
        {
            throw new Exception($"Duplicate Ids found in data container: {string.Join(", ", duplicateIds)}");
        }

        var jsonPayload = JsonSerializer.Serialize(dataContainer);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"/projects/{projectId}/data", content);
        response.EnsureSuccessStatusCode();

        return data.Count;

        string UniqueIdFromResponseAndQuestionAndChoice(int responseId, int? sectionChoiceId, int? questionChoiceId)
        {
            return responseId + "_" + (sectionChoiceId ?? 0) + "_" + (questionChoiceId ?? 0);
        }
    }

    public async Task<Project?> GetProject(string projectId)
    {
        var response = await httpClient.GetAsync($"/v2/projects/{projectId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var project = JsonSerializer.Deserialize<Project>(responseContent);
        return project;
    }

    private async Task CreateProject(string projectId, int questionId, string additionalInstructions)
    {
        var questionText = ctx.Questions.Single(q => q.QuestionId == questionId).QuestionText;
        var additionalContext = $"Survey question: {questionText}";
        if (!string.IsNullOrEmpty(additionalInstructions))
        {
            additionalContext += $"\nUser Guidance: {additionalInstructions}";
        }

        var jsonPayload = JsonSerializer.Serialize(new { additionalContext });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"/v2/projects/{projectId}", content);
        response.EnsureSuccessStatusCode();
    }

    public class Project
    {
        [JsonPropertyName("dataItemsStored")]
        public int DataItemsStored { get; set; }
        [JsonPropertyName("dataItemsProcessed")]
        public int DataItemsProcessed { get; set; }
        [JsonPropertyName("tasks")]
        public List<ProjectTask> Tasks { get; set; }
        [JsonPropertyName("lastThemeTask")]
        public DateTime? LastThemeTask { get; set; }
        [JsonPropertyName("additionalContext")]
        public string AdditionalContext { get; set; }
    }

    public class ProjectTask
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("start_time")]
        public DateTime? StartTime { get; set; }
    }

    public async Task<ThemeConfiguration[]> GetThemes(string surveyId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var themes = await httpClient.GetFromJsonAsync<ThemeConfiguration[]>($"/v2/projects/{projectId}/themes");
        return themes.OrderBy(t => t.Name).ToArray();
    }

    public async Task UpdateThemeParent(string surveyId, int questionId, int themeId, int? newParentId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);

        var jsonPayload = JsonSerializer.Serialize(newParentId);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await httpClient.PutAsync($"/v2/projects/{projectId}/themes/{themeId}/parent", content);

        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateThemeConfiguration(string surveyId, int questionId, int themeId, double sensitivity, string[] matchingExamples, string[] keywords, string? displayName)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);

        var jsonPayload = JsonSerializer.Serialize(new {
            matchingExamples,
            keywords,
            cosineThreshold = sensitivity,
            name = displayName,
        });

        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        await httpClient.PutAsync($"/v2/projects/{projectId}/themes/{themeId}", content);
    }

    public async Task<ThemeSensitivityConfiguration> GetThemeSensitivity(string surveyId, int questionId, int themeId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var project = await GetProject(projectId);

        var themeSensitivityConfigurations = await httpClient.GetFromJsonAsync<ThemeSensitivityConfigurationItem[]>($"/v2/projects/{projectId}/themes/{themeId}/tuning");
        return new ThemeSensitivityConfiguration
        {
            TotalTexts = project.DataItemsStored,
            Themes = themeSensitivityConfigurations
        };
    }

    public async Task CreateTheme(string surveyId, int questionId, string themeName, string[]? keywords, bool delegatedMatching = false)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var payload = new ThemeConfiguration
        {
            Name = themeName,
            MatchingBehaviour = new MatchingBehaviour
            {
                Keywords = keywords?.ToList(),
                DelegatedMatching = delegatedMatching,
                ExclusiveSubthemes = false,
                IncludeOtherSubtheme = false,
            }
        };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync($"/v2/projects/{projectId}/themes", content);

        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteTheme(string surveyId, int questionId, int themeId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var response = await httpClient.DeleteAsync($"/v2/projects/{projectId}/themes/{themeId}");
        response.EnsureSuccessStatusCode();
    }

    private static Boolean IsParentTheme(ThemeConfiguration[] themes, ThemeConfiguration themeToCheck)
    {
        return themes.Any(theme => theme.ParentId == themeToCheck.Id);
    }

    public async Task MergeTheme(string surveyId, int questionId, int themeOneId, int themeTwoId)
    {
        var themes = await GetThemes(surveyId, questionId);

        var themeOne = themes.Single(t => t.Id == themeOneId);
        var themeTwo = themes.Single(t => t.Id == themeTwoId);

        // parent + parent
        if (IsParentTheme(themes, themeOne) && IsParentTheme(themes, themeTwo))
        {
            var themeOneSubthemes = themes.Where(t => t.ParentId == themeOne.Id).ToList();
            var updateTasks = themeOneSubthemes.Select(t => UpdateThemeParent(surveyId, questionId, t.Id, themeTwo.Id));
            await Task.WhenAll(updateTasks);
            await DeleteTheme(surveyId, questionId, themeOne.Id);
        }

        // parent + orphan
        if (themeOne.ParentId is null && !IsParentTheme(themes, themeOne) && IsParentTheme(themes, themeTwo))
        {
            await UpdateThemeParent(surveyId, questionId, themeOne.Id, themeTwo.Id);
        }
        if (IsParentTheme(themes, themeOne) && (themeTwo.ParentId is null && !IsParentTheme(themes, themeTwo)))
        {
            await UpdateThemeParent(surveyId, questionId, themeTwo.Id, themeOne.Id);
        }

        // orphan + orphan
        if (themeOne.ParentId is null && !IsParentTheme(themes, themeOne) && (themeTwo.ParentId is null && !IsParentTheme(themes, themeTwo)))
        {
            string newThemeName = $"{themeOne.Name} & {themeTwo.Name}";
            var combinedKeywords = themeOne.MatchingBehaviour.Keywords.Concat(themeTwo.MatchingBehaviour.Keywords).ToArray();

            await CreateTheme(surveyId, questionId, newThemeName, combinedKeywords, delegatedMatching: true);

            var updatedThemes = await GetThemes(surveyId, questionId);
            var newTheme = updatedThemes.FirstOrDefault(t => t.Name == newThemeName);

            await UpdateThemeParent(surveyId, questionId, themeOne.Id, newTheme.Id);
            await UpdateThemeParent(surveyId, questionId, themeTwo.Id, newTheme.Id);
        }
    }

    public async Task<StatsPreviewResult> GetStatsPreview(string surveyId, int questionId, int themeId, double threshold, string[] keywords, string[] matchingExamples)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var jsonPayload = JsonSerializer.Serialize(new { matchingExamples, keywords, threshold });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"/v2/projects/{projectId}/themes/{themeId}/previewstats", content);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<StatsPreviewResult>(responseContent);
        return result;
    }

    public async Task<MatchPreviewResult> GetMatchPreview(string surveyId, int questionId, string themeName)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var jsonPayload = JsonSerializer.Serialize(new { name = themeName });
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync($"/projects/{projectId}/themes/preview", content);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<MatchPreviewResult>(responseContent);
        return result;
    }

    public async Task DeleteQuestion(string surveyId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        await httpClient.DeleteAsync($"/v2/projects/{projectId}");
    }

    public async Task RecalculateQuestion(string surveyId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);

        var project = await GetProject(projectId);
        var additionalContext = ExtractInstructionsFromAdditionalContext(project?.AdditionalContext);

        await httpClient.DeleteAsync($"/v2/projects/{projectId}");
        await CreateProject(projectId, questionId, additionalContext);
    }

    public async Task InitialiseProject(string surveyId, int questionId, string additionalInstructions)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(surveyId, questionId);
        var existingProject = await GetProject(projectId);
        if (existingProject != null)
        {
            throw new Exception($"Project {projectId} already exists");
        }
        await CreateProject(projectId, questionId, additionalInstructions);
    }

    public static string ExtractInstructionsFromAdditionalContext(string context)
    {
        if (string.IsNullOrEmpty(context))
            return string.Empty;

        const string marker = "\nUser Guidance: ";
        var index = context.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index == -1)
            return string.Empty;

        var start = index + marker.Length;
        if (start >= context.Length)
            return string.Empty;

        return context.Substring(start).Trim();
    }
}

public class StatsPreviewResult
{
    [JsonPropertyName("keywordMatches")]
    public int KeywordMatches { get; set; }
    [JsonPropertyName("fuzzyMatches")]
    public int FuzzyMatches { get; set; }
    [JsonPropertyName("combinedMatches")]
    public int CombinedMatches { get; set; }
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class MatchPreviewResult
{
    [JsonPropertyName("matchPatterns")]
    public string[] MatchPatterns { get; set; }
    [JsonPropertyName("matches")]
    public int Matches { get; set; }
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public class BasicTheme
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("group")]
    public string Group { get; set; }
    [JsonPropertyName("matches")]
    public int Matches { get; set; }
    [JsonPropertyName("matchPatterns")]
    public string[]? MatchPatterns { get; set; }
}

public class ThemeSensitivityConfiguration
{
    public int TotalTexts { get; set; }
    public ThemeSensitivityConfigurationItem[] Themes { get; set; }
}

public class ThemeSensitivityConfigurationItem
{
    [JsonPropertyName("userSuppliedId")]
    public string UserSuppliedId { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("distanceScore")]
    public double DistanceScore { get; set; }

    [JsonPropertyName("isFuzzyMatch")]
    public bool IsFuzzyMatch { get; set; }

    [JsonPropertyName("isKeywordMatch")]
    public bool IsKeywordMatch { get; set; }

    [JsonPropertyName("isManuallyIncluded")]
    public bool IsManuallyIncluded { get; set; }

    [JsonPropertyName("isManuallyExcluded")]
    public bool IsManuallyExcluded { get; set; }
}

public class ThemeConfiguration
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("parentId")]
    public int? ParentId { get; set; }

    [JsonPropertyName("matchingBehaviour")]
    public MatchingBehaviour MatchingBehaviour { get; set; }
}

public class MatchingBehaviour
{
    [JsonPropertyName("matchingExamples")]
    public List<string> MatchingExamples { get; set; }

    [JsonPropertyName("keywords")]
    public List<string> Keywords { get; set; }

    [JsonPropertyName("delegatedMatching")]
    public bool DelegatedMatching { get; set; }

    [JsonPropertyName("exclusiveSubthemes")]
    public bool ExclusiveSubthemes { get; set; }

    [JsonPropertyName("includeOtherSubtheme")]
    public bool IncludeOtherSubtheme { get; set; }

    [JsonPropertyName("matchingSensitivity")]
    public double MatchingSensitivity { get; set; }
}

public class StatusResult
{
    [JsonPropertyName("progress")]
    public int Progress { get; set; }
    [JsonPropertyName("message")]
    public string Message { get; set; }
    public StatusEvent StatusEvent { get; set; }
}

public enum StatusEvent
{
    NewProject,
    UploadingData,
    Analysing,
    Finished,
}

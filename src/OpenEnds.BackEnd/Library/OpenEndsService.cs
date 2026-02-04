using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenEnds.BackEnd.Model;
using static OpenEnds.BackEnd.Library.SavantaTextThemeAnalyzer;

namespace OpenEnds.BackEnd.Library;

public class OpenEndsService(IOpenEndsRepository openEndsRepository, SavantaTextThemeAnalyzer savantaTextThemeAnalyzer, IOptions<Settings> settings, ExportService exportService, IDataGroupProjectService dataGroupProjectService)
{
    private readonly IOpenEndsRepository _openEndsRepository = openEndsRepository;
    private readonly ExportService _exportService = exportService;
    private static readonly SemaphoreSlim uploadSemaphore = new(1, 1);
    private const double ScalingFactor = 3.0;

    public async Task<OpenEndQuestionsResult> GetQuestions(string subProductId)
    {
        var surveyIds = await _openEndsRepository.GetSurveyIdsAsync(subProductId);

        if (surveyIds == null || surveyIds.Count == 0)
        {
            return new OpenEndQuestionsResult
            {
                RespondentCount = 0,
                OpenTextQuestions = new List<OpenEndQuestion>()
            };
        }

        var unarchivedResponses = await _openEndsRepository.GetUnarchivedResponsesForSurvey(surveyIds);
        int respondentCount = unarchivedResponses.Count;
        var vueQuestions = (await _openEndsRepository.GetQuestionsWithAnswerCount(subProductId, surveyIds)).ToList();
        var vueQuestionsWithStatus = await GetStatusForQuestions(subProductId, vueQuestions);

        return new OpenEndQuestionsResult
        {
            RespondentCount = respondentCount,
            OpenTextQuestions = vueQuestionsWithStatus
        };
    }

    public async Task<OpenEndQuestionSummaryResult> GetQuestionSummary(string subProductId, int questionId)
    {
        var question = await GetQuestion(subProductId, questionId);
        var answerCount = await _openEndsRepository.AnswerCountForQuestion(questionId);

        var themeResult = await savantaTextThemeAnalyzer.RetrieveSummary(subProductId, questionId);
        themeResult.TotalCount = themeResult.TextThemes.Count();
        themeResult.Question = question;
        themeResult.OpenTextAnswerCount = answerCount;

        return themeResult;
    }

    public async Task<Question> GetQuestion(string subProductId, int questionId)
    {
        var vueQuestion = await _openEndsRepository.GetQuestionByIdAsync(subProductId, questionId);

        return new Question
        {
            Id = vueQuestion.QuestionId,
            VarCode = vueQuestion.VarCode,
            Text = vueQuestion.QuestionText
        };
    }

    private async Task<IList<OpenEndResponse>> AnswerData(int questionId)
    {
        var data = (await _openEndsRepository.GetAnswersForQuestion(questionId))
            .Select(a => new OpenEndResponse
            {
                ResponseId = a.ResponseId,
                QuestionChoiceId = a.QuestionChoiceId,
                SectionChoiceId = a.SectionChoiceId,
                AnswerText = a.AnswerText
            })
            .Distinct().ToList();

        return data;
    }

    private async Task<IList<OpenEndQuestion>> GetStatusForQuestions(string subProductId, List<QuestionWithAnswerStats> questions)
    {
        var openTextQuestions = new List<OpenEndQuestion>();

        foreach (var question in questions)
        {
            var openEndQuestion = new OpenEndQuestion
            {
                Question = new Question
                {
                    Id = question.Question.QuestionId,
                    VarCode = question.Question.VarCode,
                    Text = question.Question.QuestionText
                },
                QuestionCount = question.AnswerCount
            };

            var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(subProductId, question.Question.QuestionId);
            var project = await savantaTextThemeAnalyzer.GetProject(projectId);

            var statusResult = await RetrieveStatusFromProject(project, subProductId, question.Question.QuestionId, false);

            var rootThemeCount = 0;

            if (statusResult.StatusEvent == StatusEvent.Finished)
            {
                var themes = await savantaTextThemeAnalyzer.GetThemes(subProductId, question.Question.QuestionId);
                rootThemeCount = themes.Count(t => t.ParentId is null);
            }

            openEndQuestion.Status = statusResult;
            openEndQuestion.ThemeCount = rootThemeCount;
            openEndQuestion.AdditionalInstructions = ExtractInstructionsFromAdditionalContext(project?.AdditionalContext);

            openTextQuestions.Add(openEndQuestion);
        }

        return openTextQuestions;
    }

    private async Task<StatusResult> RetrieveStatusFromProject(Project project, string subProductId, int questionId, bool readOnly)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(subProductId, questionId);

        if (project == null)
        {
            return new StatusResult { Progress = 5, Message = "Creating project", StatusEvent = StatusEvent.NewProject };
        }

        if (project.Tasks.Count > 0)
        {
            int offsetGuess = (int)((DateTime.UtcNow - project.Tasks.Where(t => t.StartTime.HasValue).Max(t => t.StartTime.Value.ToUniversalTime())).TotalSeconds / ScalingFactor);
            int start;
            string message;
            StatusEvent statusEvent;
            switch (project.Tasks.OrderBy(t => t.StartTime).Last().Type)
            {
                case "IMPORT":
                    message = "Importing data...";
                    start = 25;
                    statusEvent = StatusEvent.UploadingData;
                    break;
                case "THEME_ANALYSIS":
                    message = "Analysing themes...";
                    start = 50;
                    statusEvent = StatusEvent.Analysing;
                    break;
                default:
                    throw new Exception("Unexpected task type");
            }

            int progress = start + offsetGuess;
            return new StatusResult { Progress = Math.Min(99, progress), Message = message, StatusEvent = statusEvent };
        }

        // Lock all actions right now
        await uploadSemaphore.WaitAsync();

        try
        {
            // If no items them upload
            if (project.DataItemsStored == 0)
            {
                if (!readOnly) await UploadData(projectId, questionId);
                return new StatusResult { Progress = 10, Message = "Uploaded data", StatusEvent = StatusEvent.UploadingData };
            }
            else
            {
                if (project.DataItemsStored != project.DataItemsProcessed)
                {
                    // This shouldn't happen!
                    throw new Exception("Processed and stored items are not the same for project: " + projectId);
                }

                if (project.LastThemeTask == null)
                {
                    if (!readOnly) await savantaTextThemeAnalyzer.ProcessProjectThemes(subProductId, questionId);
                    return new StatusResult { Progress = 50, Message = "Starting analysis...", StatusEvent = StatusEvent.Analysing };
                }
            }

            return new StatusResult { Progress = 100, Message = "Finished", StatusEvent = StatusEvent.Finished };
        }
        finally
        {
            uploadSemaphore.Release();
        }
    }

    public async Task<StatusResult> RetrieveStatus(string subProductId, int questionId, bool readOnly)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(subProductId, questionId);
        var project = await savantaTextThemeAnalyzer.GetProject(projectId);

        return await RetrieveStatusFromProject(project, subProductId, questionId, readOnly);
    }

    public async Task<int> GetCodedTextCount(string subProductId, int questionId)
    {
        var projectId = await dataGroupProjectService.GetProjectIdForDataGroupAsync(subProductId, questionId);
        var dataResult = await savantaTextThemeAnalyzer.GetDataResult(projectId);
        var codedTextCount = dataResult is null ? 0 : dataResult.Data.Count(d => d.Themes.Any());

        return codedTextCount;
    }

    private async Task<int> UploadData(string projectId, int questionId)
    {
        var data = await AnswerData(questionId);

        if (data.Count > settings.Value.MaxTexts)
        {
            throw new Exception($"The {data.Count} texts exceed the threshold of {settings.Value.MaxTexts}. Please choose a question with fewer responses or ");
        }

        return await savantaTextThemeAnalyzer.UploadData(projectId, data);
    }

    private async Task<AllVueConfiguration> GetSurveyConfiguration(string subProductId)
    {
        var surveyConfig = await _openEndsRepository.GetSurveyConfigurationAsync(subProductId);

        if (surveyConfig == null)
        {
            throw new Exception($"No configuration found for surveyId: {subProductId}");
        };

        return surveyConfig;
    }

    private async Task<string> GetSurveyName(string subProductId)
    {
        var surveyGroupName = await _openEndsRepository.GetSurveyGroupNameByUrlSafeNameAsync(subProductId);

        if (surveyGroupName is null && int.TryParse(subProductId, out var numericSurveyId))
        {
            var surveyName = await _openEndsRepository.GetSurveyNameByIdAsync(numericSurveyId);
            
            return surveyName;
        }

        return surveyGroupName;
    }

    public async Task<(string, AllVueConfiguration)> GetSurveyDetails(string subProductId)
    {
        var details = (await GetSurveyName(subProductId), await GetSurveyConfiguration(subProductId));
        return details;
    }

    public async Task<byte[]> GetExport(string subProductId, int questionId, ExportService.ExportFormat exportFormat)
    {
        var summary = await GetQuestionSummary(subProductId, questionId);
        if (summary == null || summary.Themes == null || summary.Themes.Count == 0)
        {
            throw new Exception("No themes found.");
        }

        return _exportService.GenerateExportFromSummary(summary, exportFormat);
    }
}
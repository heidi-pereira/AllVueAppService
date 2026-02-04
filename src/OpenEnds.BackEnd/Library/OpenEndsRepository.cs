using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using Microsoft.EntityFrameworkCore;
using OpenEnds.BackEnd.Model;

namespace OpenEnds.BackEnd.Library
{
    public class OpenEndsRepository : IOpenEndsRepository
    {
        private readonly OpenEndsContext _context;
        private readonly MetadataContext _metadataContext;
        private readonly IDataGroupProjectService _dataGroupProjectService;

        public OpenEndsRepository(OpenEndsContext context, MetadataContext metadataContext, IDataGroupProjectService dataGroupProjectService)
        {
            _context = context;
            _metadataContext = metadataContext;
            _dataGroupProjectService = dataGroupProjectService;
        }

        public async Task<List<int>> GetUnarchivedResponsesForSurvey(ICollection<int> surveyIds)
        {
            return await SurveyResponsesCompleteUnarchived()
            .Where(sr => surveyIds.Contains(sr.SurveyId))
            .Select(sr => sr.ResponseId)
            .ToListAsync();
        }

        private IQueryable<Model.SurveyResponse> SurveyResponsesCompleteUnarchived()
        {
            return _context.SurveyResponses
                .Where(sr => !sr.Archived && sr.Status == 6);
        }

        public async Task<IList<VueAnswer>> GetAnswersForQuestion(int questionId)
        {
            return await AnswersCompleteUnarchived()
                .Where(a => a.QuestionId == questionId)
                .ToListAsync();
        }

        private IQueryable<VueAnswer> AnswersCompleteUnarchived()
        {
            return _context.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                .Join(SurveyResponsesCompleteUnarchived(),
                    a => a.ResponseId,
                    sr => sr.ResponseId,
                    (a, sr) => a);
        }

        public async Task<int> AnswerCountForQuestion(int questionId)
        {
            return await AnswersCompleteUnarchived()
            .CountAsync(a => a.QuestionId == questionId);
        }

        public async Task<IList<QuestionWithAnswerStats>> GetQuestionsWithAnswerCount(string subProductId, List<int> surveyIds)
        {
            var vueQuestions = await GetQuestions(subProductId, surveyIds);

            return await vueQuestions
                .Join(AnswersCompleteUnarchived()
                .GroupBy(a => a.QuestionId)
                .Select(g => new { QuestionId = g.Key, MaxLength = g.Max(gg => gg.AnswerText.Length), Count = g.Count() }),
                q => q.QuestionId,
                a => a.QuestionId,
                (question, answerStats) => new { question, answerStats.MaxLength, answerStats.Count }
                )
                .Where(x => x.MaxLength > 5)
                .OrderBy(x => x.question.QuestionId)
                .Select(x => new QuestionWithAnswerStats(
                    x.question,
                    x.MaxLength,
                    x.Count
                    )
                ).ToListAsync();
        }

        public async Task<List<int>> GetSurveyIdsAsync(string subProductId)
        {
            var surveyIds = await _context.SurveyGroups
                .AsNoTracking()
                .Where(sg => sg.UrlSafeName == subProductId)
                .SelectMany(sg => _context.SurveyGroupSurveys
                    .Where(sgs => sgs.SurveyGroupId == sg.SurveyGroupId)
                    .Select(sgs => sgs.SurveyId))
                .ToListAsync();

            if (surveyIds.Count == 0 && int.TryParse(subProductId, out var singleSurveyId))
                surveyIds.Add(singleSurveyId);

            return surveyIds;
        }

        public async Task<VueQuestion> GetQuestionByIdAsync(string subProductId, int questionId)
        {
            return await (await GetPermittedQuestionsAsync(subProductId))
                .Where(q => q.QuestionId == questionId)
                .SingleAsync();
        }

        public async Task<string?> GetSurveyGroupNameByUrlSafeNameAsync(string urlSafeName)
        {
            return await _context.SurveyGroups
                .Where(sg => sg.UrlSafeName == urlSafeName)
                .Select(sg => sg.Name)
                .SingleOrDefaultAsync();
        }

        public async Task<string?> GetSurveyNameByIdAsync(int surveyId)
        {
            return await _context.Surveys
                .Where(s => s.SurveyId == surveyId)
                .Select(s => s.Name)
                .SingleOrDefaultAsync();
        }

        public async Task<Model.AllVueConfiguration?> GetSurveyConfigurationAsync(string subProductId)
        {
            return await _metadataContext.AllVueConfigurations
                .Where(config => config.SubProductId == subProductId.ToString())
                .SingleOrDefaultAsync();
        }

        private async Task<IQueryable<VueQuestion>> GetQuestions(string subProductId, List<int> surveyIds)
        {
            return (await GetPermittedQuestionsAsync(subProductId))
                .Where(q => surveyIds.Contains(q.SurveyId)
                            && q.QuestionShownInSurvey
                            && q.MasterType == "TEXTENTRY"
                            && !q.QuestionText.StartsWith("tag")
                            && !q.QuestionText.Contains("hidden"));
        }

        private async Task<IQueryable<VueQuestion>> GetPermittedQuestionsAsync(string subProductId)
        {
            var dataPermissions = await _dataGroupProjectService.GetDataPermissionsAsync(subProductId);

            var variableIds = dataPermissions?.VariableIds;
            var questionVarCodes = await GetQuestionVarCodesFromVariableIds(variableIds);

            var query = _context.Questions
                .AsNoTracking()
                .Where(q => questionVarCodes == null || questionVarCodes.Count == 0 || questionVarCodes.Contains(q.VarCode));

            return query;
        }

        private async Task<ICollection<string>> GetQuestionVarCodesFromVariableIds(ICollection<int> variableIds)
        {
            if (variableIds == null || variableIds.Count == 0)
            {
                return new List<string>();
            }

            var variableConfigs = await _metadataContext.VariableConfigurations
            .Where(config => variableIds.Contains(config.Id))
            .ToListAsync();

            return variableConfigs
                .Where(config => config.Definition is QuestionVariableDefinition)
                .Select(config => ((QuestionVariableDefinition)config.Definition).QuestionVarCode)
                .Where(varCode => !string.IsNullOrWhiteSpace(varCode))
                .Distinct()
                .ToList();
        }
    }
}
using BrandVue.EntityFramework.Answers.Model;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.EntityFramework.Answers
{
    public record ChoiceStat(int ChoiceId, int AnswerCount);
    public record QuestionWithAnswerStats(
        Question Question,
        List<ChoiceStat> ChoiceStats,
        int AnswerCount
    );
    public record SurveyDescriptor(int SurveyId, int[] SegmentIds);
    public record FilterVarCode(string VarCode, SurveyDescriptor[] Surveys, int EntitySetId ,  int[] EntityIds);


    public interface IQuestionRepository
    {
        Task<IList<QuestionWithAnswerStats>> GetQuestionsWithAnswerCount(IList<string> varCodes, IList<SurveyDescriptor> surveys, CancellationToken token);
        public Task<IList<int>> GetRespondentsWithFilter(IList<FilterVarCode> filters, CancellationToken token);
    }

    public class QuestionRepository : IQuestionRepository
    {
        private readonly AnswersDbContext _context;
        public QuestionRepository(AnswersDbContext context)
        {
            _context = context;
        }

        private async Task<IQueryable<Question>> GetPermittedQuestionsAsync(IList<string> questionVarCodes)
        {
            var query = _context.Questions
                .AsNoTracking()
                .Where(q => questionVarCodes == null || questionVarCodes.Count == 0 || questionVarCodes.Contains(q.VarCode));
            return query;
        }

        private async Task<IQueryable<Question>> GetQuestions(IList<string> varCodes, IList<SurveyDescriptor> surveyDescriptors)
        {
            var surveyIds = surveyDescriptors.Select(x => x.SurveyId);
            return (await GetPermittedQuestionsAsync(varCodes))
                .Where(q => surveyIds.Contains(q.SurveyId)
                            && q.QuestionShownInSurvey
                            );
        }

        private IQueryable<SurveyResponse> SurveyResponsesCompleteUnarchived()
        {
            return _context.SurveyResponses
                .Where(sr => !sr.Archived && sr.Status == SurveyCompletionStatus.Completed);
        }

        private IQueryable<Answer> AnswersCompleteUnarchived()
        {
            return _context.Answers
                .Join(SurveyResponsesCompleteUnarchived(),
                    a => a.ResponseId,
                    sr => sr.ResponseId,
                    (a, sr) => a);
        }

        public async Task<IList<QuestionWithAnswerStats>> GetQuestionsWithAnswerCount(
            IList<string> varCodes, IList<SurveyDescriptor> surveyIds, CancellationToken token)
        {
            var vueQuestions = await GetQuestions(varCodes, surveyIds);

            // Get all relevant answers, grouped by QuestionId and AnswerChoiceId
            var answerStats = await AnswersCompleteUnarchived()
                .Where(a => vueQuestions.Select(q => q.QuestionId).Contains(a.QuestionId))
                .GroupBy(a => new { a.QuestionId, a.AnswerChoiceId })
                .Select(g => new
                {
                    g.Key.QuestionId,
                    ChoiceId = g.Key.AnswerChoiceId ?? 0,
                    AnswerCount = g.Select(x => x.ResponseId).Distinct().Count()
                })
                .ToListAsync(token);

            // Group stats by question
            var statsByQuestion = answerStats
                .GroupBy(x => x.QuestionId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ChoiceStat(x.ChoiceId, x.AnswerCount)).ToList()
                );

            // Build the result
            var questions = await vueQuestions
                .ToArrayAsync(token);

            var result = questions.Select(q =>
                {
                    var choiceStats = statsByQuestion.TryGetValue(q.QuestionId, out var stats)
                        ? stats
                        : new List<ChoiceStat>();
                    var totalCount = choiceStats.Sum(cs => cs.AnswerCount);

                    return new QuestionWithAnswerStats(q, choiceStats, totalCount);
                })
                .ToList();

            return result;
        }

        public async Task<IList<int>> GetRespondentsWithFilter(IList<FilterVarCode> filters, CancellationToken token)
        {
            if (filters == null || filters.Count == 0)
            {
                return [];
            }

            // Build a query for each filter, then intersect the results
            IEnumerable<int> respondentIds = null;

            foreach (var filter in filters)
            {
                // Get all surveyIds and segmentIds for this filter
                var surveyIds = filter.Surveys.Select(s => s.SurveyId).ToList();
                var segmentIds = filter.Surveys.SelectMany(s => s.SegmentIds).ToList();

                // Query answers matching the filter's varCode, surveyIds, entitySetId, and entityIds
                var query = AnswersCompleteUnarchived()
                    .Where(a => a.Question.VarCode == filter.VarCode
                                && surveyIds.Contains(a.Question.SurveyId)
                                && (filter.EntityIds == null || filter.EntityIds.Length == 0 || (a.AnswerChoiceId.HasValue && filter.EntityIds.Contains(a.AnswerChoiceId.Value)))
                    );

                if (segmentIds.Count > 0)
                {
                    query = query.Where(a => segmentIds.Contains(a.Response.SegmentId));
                }

                var ids = await query
                    .Select(a => a.ResponseId)
                    .Distinct()
                    .ToListAsync(token);

                if (respondentIds == null)
                {
                    respondentIds = ids;
                }
                else
                {
                    respondentIds = respondentIds.Intersect(ids);
                }
            }

            return respondentIds?.ToList() ?? new List<int>();
        }
    }
}

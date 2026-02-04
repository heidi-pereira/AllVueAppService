using System.Data;
using System.Linq;
using BrandVue.EntityFramework.ResponseRepository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.EntityFramework.Answers.Model
{
    /// <summary>
    /// Schema defined at https://github.com/MIG-Global/SurveyPlatform/tree/master/DatabaseSchema/vue
    /// ConnectionString must be pointed at SurveyPortalMorar for this to work
    /// </summary>
    public class AnswerTextResponseRepository : IResponseRepository
    {
        private readonly IDbContextFactory<ResponseDataContext> _responseDataContextFactory;

        public AnswerTextResponseRepository(IDbContextFactory<ResponseDataContext> responseDataContextFactory)
        {
            _responseDataContextFactory = responseDataContextFactory;
        }

        public WeightedWordCount[] GetWeightedLoweredAndTrimmedTextCounts(ResponseWeight[] responseWeights, string varCodeBase,
            IReadOnlyCollection<(DbLocation Location, int Id)> filters)
        {
            if (!responseWeights.Any())
            {
                return Array.Empty<WeightedWordCount>();
            }

            var filterLookup = filters.ToLookup(f => f.Location, f => (int?)f.Id);
            var sectionFilterId = filterLookup[DbLocation.SectionEntity].FirstOrDefault();
            var pageFilterId = filterLookup[DbLocation.PageEntity].FirstOrDefault();
            var questionFilterId = filterLookup[DbLocation.QuestionEntity].FirstOrDefault();

            var sqlParameters = new object[]
            {
                new SqlParameter("@profileWeightings", SqlDbType.Structured)
                {
                    Value = ResponseHelper.GetSqlRecordSetFromResponseWeightings(responseWeights),
                    TypeName = "vue.ProfileWeightingLookupV2"
                },
                new SqlParameter("@varCode", SqlDbType.NVarChar, 100)
                {
                    Value = varCodeBase
                },
                new SqlParameter("@sectionChoiceId", SqlDbType.Int)
                {
                    Value = sectionFilterId ?? (object) DBNull.Value
                },
                new SqlParameter("@pageChoiceId", SqlDbType.Int)
                {
                    Value = pageFilterId ?? (object) DBNull.Value
                },
                new SqlParameter("@questionChoiceId", SqlDbType.Int)
                {
                    Value = questionFilterId ?? (object) DBNull.Value
                },
            };

            // Note the FORCE ORDER - this is required to make sure that the appropriate set of data into join to answers is calculated first therefore minimising the slow join to answers
            // Without this, SQL server doesn't seem to be able to optimise the query as well.
            // Before removing/changing this, please ensure to check the new query plans and also turn io statistics and especially take not of the column store segment read vs skipped
            var sql = @"SELECT 
	RTRIM(LTRIM(LOWER(AnswerText))) [Text],
	SUM(q.weighting) Result,
	COUNT(AnswerText) UnweightedResult
FROM (
	SELECT q.questionId, w.responseId, weighting FROM
	@profileWeightings w
	INNER JOIN surveyResponse sr ON sr.responseId = w.responseId
	INNER JOIN Vue.Questions q ON q.surveyid = sr.surveyId
	WHERE VarCode = @varCode
) as q
INNER JOIN Vue.Answers a ON a.QuestionId = q.QuestionId AND a.responseid = q.responseid
WHERE RTRIM(LTRIM(LOWER(AnswerText))) != ''"
    + (sectionFilterId != null ? " AND (SectionChoiceId = @sectionChoiceId) " : "")
    + (pageFilterId != null ? " AND (PageChoiceId = @pageChoiceId) " : "")
    + (questionFilterId != null ? " AND (QuestionChoiceId = @questionChoiceId) " : "")
    + @" GROUP BY RTRIM(LTRIM(LOWER(AnswerText)))
ORDER BY COUNT(*) DESC
OPTION (FORCE ORDER)";

            using var responseDataContext = _responseDataContextFactory.CreateDbContext();
            responseDataContext.Database.SetCommandTimeout(60);
            return responseDataContext.WeightedWordCounts.FromSqlRaw(sql, sqlParameters).ToArray();
        }

        public RawTextResponse[] GetRawTextTrimmed(IList<int> responseIds, string varCodeBase,
            IReadOnlyCollection<(DbLocation Location, int Id)> filters, int[] surveyIds)
        {
            if (!responseIds.Any())
            {
                return Array.Empty<RawTextResponse>();
            }

            var filterLookup = filters.ToLookup(f => f.Location, f => (int?)f.Id);
            var sectionFilterId = filterLookup[DbLocation.SectionEntity].FirstOrDefault();
            var pageFilterId = filterLookup[DbLocation.PageEntity].FirstOrDefault();
            var questionFilterId = filterLookup[DbLocation.QuestionEntity].FirstOrDefault();

            var sqlParameters = new object[]
            {
                new SqlParameter("@ids", SqlDbType.Structured)
                {
                    Value = ResponseHelper.GetSqlRecordSetFromResponseIds(responseIds),
                    TypeName = "dbo.IntIdList"
                },
                new SqlParameter("@varCode", SqlDbType.NVarChar, 100)
                {
                    Value = varCodeBase
                },
                new SqlParameter("@sectionChoiceId", SqlDbType.Int)
                {
                    Value = sectionFilterId ?? (object) DBNull.Value
                },
                new SqlParameter("@pageChoiceId", SqlDbType.Int)
                {
                    Value = pageFilterId ?? (object) DBNull.Value
                },
                new SqlParameter("@questionChoiceId", SqlDbType.Int)
                {
                    Value = questionFilterId ?? (object) DBNull.Value
                },
            };

            using var responseDataContext = _responseDataContextFactory.CreateDbContext();
            responseDataContext.Database.SetCommandTimeout(60);

            var sql = $@"
SELECT RTRIM(LTRIM(AnswerText)) Text
FROM vue.Answers a
INNER JOIN vue.Questions q ON a.QuestionId = q.QuestionId
INNER JOIN @ids ids ON a.ResponseId = ids.id

WHERE q.SurveyId IN ({surveyIds.CommaList()}) 
      AND VarCode = @varCode
      AND AnswerText <> ''
      AND (@sectionChoiceId IS NULL OR SectionChoiceId = @sectionChoiceId)
      AND (@pageChoiceId IS NULL OR PageChoiceId = @pageChoiceId)
      AND (@questionChoiceId IS NULL OR QuestionChoiceId = @questionChoiceId)
ORDER BY a.ResponseId DESC;";

            return responseDataContext.RawTextResponses.FromSqlRaw(sql, sqlParameters).ToArray();
        }
    }
}

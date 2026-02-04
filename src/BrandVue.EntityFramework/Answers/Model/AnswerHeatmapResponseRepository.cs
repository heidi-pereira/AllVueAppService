using BrandVue.EntityFramework.ResponseRepository;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;

namespace BrandVue.EntityFramework.Answers.Model
{
    public class AnswerHeatmapResponseRepository: IHeatmapResponseRepository
    {
        private readonly IDbContextFactory<ResponseDataContext> _responseDataContextFactory;

        public AnswerHeatmapResponseRepository(IDbContextFactory<ResponseDataContext> responseDataContextFactory)
        {
            _responseDataContextFactory = responseDataContextFactory;
        }

        public HeatmapResponse[] GetRawClickData(IList<int> responseIds, string varCode, IReadOnlyCollection<(DbLocation Location, int Id)> filters, int[] surveyIds)
        {
            if (!responseIds.Any())
            {
                return Array.Empty<HeatmapResponse>();
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
                    Value = varCode
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

            var sql = $@"
SELECT ResponseId, CAST([3] AS float) XPercent, CAST([4] AS float) YPercent, CAST([6] AS int) TimeOffset
FROM (
	SELECT ResponseId, seq, x.ordinal, x.value
	FROM (
		SELECT ResponseId, ordinal as seq, value
		FROM (
			SELECT a.ResponseId, 
CASE 
	WHEN LEN(RTRIM(LTRIM(AnswerText)) )= 0 THEN '-1,-1,-1,-1,-1,-1'
	ELSE RTRIM(LTRIM(AnswerText))
	END Text
			FROM vue.Answers a
			INNER JOIN vue.Questions q ON a.QuestionId = q.QuestionId
			INNER JOIN @ids ids ON a.ResponseId = ids.id

			WHERE q.SurveyId IN ({surveyIds.CommaList()}) 
				  AND VarCode = @varCode
				  AND (@sectionChoiceId IS NULL OR SectionChoiceId = @sectionChoiceId)
				  AND (@pageChoiceId IS NULL OR PageChoiceId = @pageChoiceId)
				  AND (@questionChoiceId IS NULL OR QuestionChoiceId = @questionChoiceId)
		) Data
		CROSS APPLY STRING_SPLIT(Text, '|', 1)
		WHERE value <> ''
	) as splitdata
	CROSS APPLY STRING_SPLIT(splitdata.value, ',', 1) x
) z
PIVOT (
MIN(z.value)
FOR z.ordinal IN ([3],[4],[6])
) pvt
WHERE [3] IS NOT NULL
AND	[4] IS NOT NULL
AND [6] IS NOT NULL
ORDER BY ResponseId DESC, TimeOffset;";

            return responseDataContext.HeatmapResponses.FromSqlRaw(sql, sqlParameters).ToArray();
        }
    }
}

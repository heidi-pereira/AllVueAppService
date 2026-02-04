using System.Threading.Tasks;
using Newtonsoft.Json;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Utils;
using BrandVue.SourceData.Snowflake;
using BrandVue.EntityFramework.Answers;

namespace BrandVue.SourceData.CalculationPipeline;

public class SnowflakeTextCountCalculator : BaseTextCountCalculator
{
    private readonly ISnowflakeRepository _snowflakeRepository;
    private readonly AppSettings _appSettings;

    public SnowflakeTextCountCalculator(
        IProfileResponseAccessorFactory profileResponseAccessorFactory,
        IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
        IMeasureRepository measureRepository,
        ISnowflakeRepository snowflakeRepository,
        IAsyncTotalisationOrchestrator resultsCalculator,
        AppSettings appSettings)
        : base(profileResponseAccessorFactory, quotaCellReferenceWeightingRepository, measureRepository, resultsCalculator)
    {
        _snowflakeRepository = snowflakeRepository;
        _appSettings = appSettings;
    }

    protected override async Task<WeightedWordCount[]> GetWeightedTextCountsAsync(
        ResponseWeight[] responseWeights,
        string varCodeBase,
        IReadOnlyCollection<(DbLocation Location, int Id)> filters)
    {
        if (!responseWeights.Any())
        {
            return Array.Empty<WeightedWordCount>();
        }

        var filterLookup = filters.ToLookup(f => f.Location, f => (int?) f.Id);
        int? sectionFilterId = filterLookup[DbLocation.SectionEntity].FirstOrDefault();
        int? pageFilterId = filterLookup[DbLocation.PageEntity].FirstOrDefault();
        int? questionFilterId = filterLookup[DbLocation.QuestionEntity].FirstOrDefault();

        var weightingsArray = responseWeights.Select(rw => new object[] { rw.ResponseId, rw.Weight }).ToArray();
        string profileWeightingsJson = JsonConvert.SerializeObject(weightingsArray);

        string database = _appSettings.SnowflakeDapperConfig.DatabaseName;

        if (!IsValidDatabaseName(database))
        {
            throw new ArgumentException($"Invalid database name: {database}");
        }

        string surveyResponseTable = $"{database}.RAW_SURVEY.SURVEY_RESPONSE";
        string questionsTable = $"{database}.RAW_SURVEY.ALL_QUESTIONS_INCLUDING_CONFIDENTIAL";
        string answersTable = $"{database}.RAW_SURVEY.ANSWERS";

        var param = new
        {
            profileWeightingsJson,
            varCodeBase,
            sectionFilterId,
            pageFilterId,
            questionFilterId
        };

        string sql = $@"
WITH profile_weightings (RESPONSE_ID, WEIGHTING) AS (
    SELECT
        v.value[0]::BIGINT,
        v.value[1]::FLOAT
    FROM
        TABLE(FLATTEN(input => PARSE_JSON(:profileWeightingsJson))) v
)

SELECT 
	TRIM(LOWER(ANSWER_TEXT)) AS Text,
	SUM(q.WEIGHTING) AS Result,
	COUNT(ANSWER_TEXT) AS UnweightedResult
FROM (
	SELECT q.QUESTION_ID, w.RESPONSE_ID, w.WEIGHTING FROM profile_weightings w
	INNER JOIN {surveyResponseTable} sr ON sr.RESPONSE_ID = w.RESPONSE_ID
	INNER JOIN {questionsTable} q ON q.SURVEY_ID = sr.SURVEY_ID
	WHERE VAR_CODE = :varCodeBase AND IS_CONFIDENTIAL = False
) as q
INNER JOIN {answersTable} a ON a.QUESTION_ID = q.QUESTION_ID AND a.RESPONSE_ID = q.RESPONSE_ID
WHERE Text != ''
{(sectionFilterId != null ? $"AND SECTION_CHOICE_ID = :sectionFilterId" : "")}
{(pageFilterId != null ? $"AND PAGE_CHOICE_ID = :pageFilterId" : "")}
{(questionFilterId != null ? $"AND QUESTION_CHOICE_ID = :questionFilterId" : "")}
GROUP BY Text
ORDER BY UnweightedResult DESC;";

        var results = await _snowflakeRepository.QueryAsync<WeightedWordCount>(sql, param);

        return results.ToArray();
    }

    private static bool IsValidDatabaseName(string databaseName)
    {
        string pattern = @"^[A-Za-z_][A-Za-z0-9_$]{0,254}$";
        return !string.IsNullOrWhiteSpace(databaseName) &&
            System.Text.RegularExpressions.Regex.IsMatch(databaseName, pattern);
    }
}
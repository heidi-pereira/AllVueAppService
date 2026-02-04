using System.Data;
using System.Text;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Import;

namespace BrandVue.SourceData.Calculation
{
    public class ProfileResultsCalculator : IProfileResultsCalculator
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IBrandVueDataLoaderSettings _settings;
        private readonly IProductContext _productContext;
        private readonly IEntityRepository _entityRepository;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;

        public ProfileResultsCalculator(ISubsetRepository subsetRepository,
            IAverageDescriptorRepository averageDescriptorRepository,
            IRespondentRepositorySource respondentRepositorySource,
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            IProductContext productContext,
            IBrandVueDataLoaderSettings settings,
            IEntityRepository entityRepository,
            IProfileResponseAccessorFactory profileResponseAccessorFactory)
        {
            _subsetRepository = subsetRepository;
            _averageDescriptorRepository = averageDescriptorRepository;
            _respondentRepositorySource = respondentRepositorySource;
            _productContext = productContext;
            _settings = settings;
            _entityRepository = entityRepository;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
        }

        public IEnumerable<CategoryResult> GetResults(IReadOnlyCollection<Measure> measures, string subsetId,
            CalculationPeriodSpan[] comparisonDates, string averageName, int[] brandsToIncludeInAverage,
            int activeBrand, string requestScopeOrganisation)
        {
            if (!measures.All(YesNoWithEntityCombinationLengthTwo))
            {
                throw new ArgumentException("Measure not allowed");
            }

            if (measures.GroupBy(m => m.Field.GetDataAccessModel(subsetId).FullV2VarCode).Any(g => g.Count() > 1))
            {
                throw new ArgumentException("Measures that share the same varcode cannot be used on the same card");
            }

            var average = _averageDescriptorRepository.Get(averageName, requestScopeOrganisation);
            if (average.TotalisationPeriodUnit == TotalisationPeriodUnit.Day)
                throw new ArgumentException("Daily totalisation not supported");

            var subset = _subsetRepository.Get(subsetId);
            var commonBaseVarCodes = measures
                .GroupBy(GetBaseFieldValueKey, m => m, new MetricBaseFieldComparer());

            var categoryResults = new List<CategoryResult>();
            foreach (var baseFieldGrouping in commonBaseVarCodes)
            {
                var baseDataAccessModel = baseFieldGrouping.Key.BaseField.GetDataAccessModel(subsetId);
                var trueDataAccessModels = baseFieldGrouping.Select(measure => measure.Field.GetDataAccessModel(subsetId)).ToArray();

                string sql = BuildSql(subset, baseDataAccessModel, baseFieldGrouping.Key.BaseValues, trueDataAccessModels, brandsToIncludeInAverage, activeBrand);

                var calculationPeriod = comparisonDates.Single();
                var endOfDayEndDate = calculationPeriod.EndDate.EndOfDay().NormalizeSqlDateTime();
                var startDate = calculationPeriod.StartDate
                    .AddDays(1 - calculationPeriod.StartDate.Day) //First day of month
                    .AddMonths(1 - average.NumberOfPeriodsInAverage)
                    .NormalizeSqlDateTime();

                var multipliers = GetScaleFactorPerQuotaYearAndMonth(subset, average, calculationPeriod.EndDate, requestScopeOrganisation);

                var quotaWeightingParameter = new SqlParameter("@quotaWeightings", SqlDbType.Structured)
                {
                    Value = GetSqlRecordSetFromQuotaCellWeightings(multipliers),
                    TypeName = "vue.QuotaWeightingLookupV2"
                };

                var sqlParameters =
                    new Dictionary<string, object>
                    {
                        {"startDate", startDate},
                        {"endDate", endOfDayEndDate},
                        {"quotaWeightings", quotaWeightingParameter}
                    };

                var sqlProvider = new SqlProvider(_settings.ConnectionString, _productContext.ShortCode);
                sqlProvider.ExecuteReader(sql, sqlParameters,
                    dataRecord =>
                    {
                        string resultsVarcode = dataRecord.GetString(0);
                        var measure = measures.Single(m => m.Field.GetDataAccessModel(subsetId).FullV2VarCode == resultsVarcode);
                        if (_entityRepository.TryGetInstance(subset, measure.EntityCombination.Single(e => !e.IsBrand).Identifier, dataRecord.GetInt32(1), out var otherEntityInstance))
                        {
                            var weightedDailyResult = new WeightedDailyResult(calculationPeriod.EndDate) { WeightedResult = dataRecord.GetDouble(3) };
                            var categoryResult = new CategoryResult(measure.Name, otherEntityInstance.Name, weightedDailyResult, dataRecord.GetDouble(4));
                            categoryResults.Add(categoryResult);
                        }
                    }
                );
            }

            return categoryResults.ToArray();
        }

        private static (ResponseFieldDescriptor BaseField, int[] BaseValues) GetBaseFieldValueKey(Measure measure)
        {
            int[] BaseValues = measure.LegacyBaseValues.IsList ? measure.LegacyBaseValues.Values :
                Enumerable.Range(measure.LegacyBaseValues.Minimum.GetValueOrDefault(), measure.LegacyBaseValues.Maximum.GetValueOrDefault() - measure.LegacyBaseValues.Minimum.GetValueOrDefault() + 1).ToArray();
            return (measure.BaseField, BaseValues);
        }

        private static bool YesNoWithEntityCombinationLengthTwo(Measure m) =>
            m.CalculationType == CalculationType.YesNo && m.EntityCombination.Any(c => c.IsBrand) &&
            m.EntityCombination.Count() == 2;

        private string BuildSql(Subset subset, FieldDefinitionModel baseDataAccessModel, int[] baseVals, IReadOnlyCollection<FieldDefinitionModel> trueDataAccessModels,
            int[] brandsToIncludeInAverage, int activeBrand)
        {
            var sb = new StringBuilder();

            var varCodeEntityColumnAndInstances = trueDataAccessModels.Select(m =>
            {
                var singleNonBrandEntityColumn = m.OrderedEntityColumns.Single(e => !e.EntityType.IsBrand);
                var instances = _entityRepository.GetInstancesOf(singleNonBrandEntityColumn.EntityType.Identifier, subset);
                return (Varcode: m.FullV2VarCode, EntityColumn: singleNonBrandEntityColumn, Instances: instances);
            }).ToArray();

            var varCodeEntityIdPairs = varCodeEntityColumnAndInstances
                    .SelectMany(v => v.Instances.Select(i => $"('{v.Varcode}', {i.Id})"));
            string varCodesAndInstances = $"(VALUES {string.Join(",", varCodeEntityIdPairs)}) AS V(VarCode, OtherEntityId)";
            string brandIds = $"(VALUES {string.Join(",", brandsToIncludeInAverage.Select(b => $"({b})"))}) AS Brands(Brand)";

            string[] extraAndConditions = varCodeEntityColumnAndInstances
                .Select(r => $"(trueQuestions.Varcode = '{r.Varcode}' AND trueAnswers.{r.EntityColumn.DbLocation.SafeSqlReference} IN ({r.Instances.Select(i => i.Id).CommaList()}))")
                .ToArray();
            string otherEntityFilter = extraAndConditions.Any()
                ? $"WHERE ({Environment.NewLine}{string.Join($" OR{Environment.NewLine}", extraAndConditions)}{Environment.NewLine})"
                : "";

            string baseBrandColumnLocation = baseDataAccessModel.OrderedEntityColumns.Single(e => e.EntityType.IsBrand).DbLocation.SafeSqlReference;
            string baseValueLocation = baseDataAccessModel.ValueDbLocation.SafeSqlReference;

            var otherEntityIdCaseSelector = varCodeEntityColumnAndInstances
                .GroupBy(v => v.EntityColumn.DbLocation.SafeSqlReference, v => v.Varcode)
                .Select(varCodes => $"trueQuestions.VarCode IN ({varCodes.JoinAsSingleQuotedList()}) THEN trueAnswers.{varCodes.Key}");
            string otherEntityIdCaseStatement = $"CASE WHEN {string.Join($"{Environment.NewLine}WHEN ", otherEntityIdCaseSelector)} END";

            var metaConnection = new SqlConnectionStringBuilder(_settings.AppSettings.MetaConnectionString);

            sb.AppendLine($@"
SELECT Varcode COLLATE DATABASE_DEFAULT AS Varcode, OtherEntityId, Brand
INTO #shapedResults
FROM
{varCodesAndInstances}
CROSS JOIN
{brandIds}

SELECT baseAnswers.*, qw.Weighting
INTO #baseAnswers
FROM Vue.Questions AS baseQuestion
INNER JOIN Vue.answers AS baseAnswers ON baseQuestion.QuestionId = baseAnswers.QuestionId
INNER JOIN surveyResponse AS sr ON baseAnswers.responseId = sr.responseId
INNER JOIN [{metaConnection.InitialCatalog}].[dbo].[{_productContext.ShortCode}-{subset.Id}-responseQuotas] AS rq ON baseAnswers.responseId = rq.responseId
INNER JOIN @quotaWeightings AS qw ON rq.QuotaCellId = qw.QuotaCellId and year(sr.lastChangeTime) = qw.YearNumber AND month(sr.lastChangeTime) = qw.MonthNumber
WHERE baseQuestion.VarCode = '{baseDataAccessModel.FullV2VarCode}'
AND baseAnswers.{baseValueLocation} IN ({baseVals.CommaList()})
AND sr.SurveyId IN ({subset.SurveyIdToSegmentNames.Keys.CommaList()})
AND sr.lastChangeTime BETWEEN @startDate AND CAST(@endDate AS datetime)
AND baseAnswers.{baseBrandColumnLocation} IN ({brandsToIncludeInAverage.CommaList()})


SELECT shaped.VarCode, shaped.OtherEntityId, shaped.Brand, coalesce(KeyBrand, 0) AS KeyBrand, coalesce(Average, 0) AS Average
FROM #shapedResults AS shaped
LEFT JOIN
(
    SELECT trueQuestions.VarCode, {otherEntityIdCaseStatement} AS OtherEntityId, baseAnswers.{baseBrandColumnLocation} AS Brand,
    SUM(CASE WHEN trueAnswers.AnswerValue = 1 THEN baseAnswers.weighting ELSE 0 END) / SUM(baseAnswers.weighting) AS KeyBrand,
    AVG(SUM(CASE WHEN trueAnswers.AnswerValue = 1 THEN baseAnswers.weighting ELSE 0 END) / SUM(baseAnswers.weighting))
		OVER (PARTITION BY trueQuestions.varcode, {otherEntityIdCaseStatement}) AS Average
    FROM #baseAnswers AS baseAnswers
    INNER JOIN vue.Answers AS trueAnswers ON baseAnswers.ResponseId = trueAnswers.ResponseId
    INNER JOIN vue.Questions AS trueQuestions ON trueAnswers.QuestionId = trueQuestions.QuestionId
    {otherEntityFilter}
    GROUP BY trueQuestions.VarCode, {otherEntityIdCaseStatement}, baseAnswers.{baseBrandColumnLocation}
) AS actuals
ON shaped.VarCode = actuals.VarCode
AND shaped.OtherEntityId = actuals.OtherEntityId AND shaped.Brand = actuals.Brand
WHERE shaped.Brand = {activeBrand}
");

            return sb.ToString();
        }

        private IEnumerable<(int QuotaCellId, int Year, int Month, double ScaleFactor)> GetScaleFactorPerQuotaYearAndMonth(Subset subset, AverageDescriptor average, DateTimeOffset endDate, string requestScopeOrganisation)
        {
            var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(subset);
            var monthlyAverage = _averageDescriptorRepository.Get("Monthly", requestScopeOrganisation);
            var respondentRepository = _respondentRepositorySource.GetForSubset(subset);
            var quotaCellGroup = respondentRepository.GetGroupedQuotaCells(average);
            for (int i = 0; i < average.NumberOfPeriodsInAverage; i++)
            {
                var multipliers = WeightGeneratorForRequestedPeriod.Generate(subset, profileResponseAccessor, _quotaCellReferenceWeightingRepository,
                    monthlyAverage, quotaCellGroup, endDate);
                foreach ((var key, double value) in multipliers)
                {
                    yield return (key.Id, endDate.Year, endDate.Month, value);
                }
                endDate = endDate.AddDays(-endDate.Day);
            }
        }

        private static IEnumerable<SqlDataRecord> GetSqlRecordSetFromQuotaCellWeightings(IEnumerable<(int QuotaCellId, int Year, int Month, double ScaleFactor)> quotaWeights)
        {
            return quotaWeights.Select(r =>
            {
                var responseWeighting = new SqlDataRecord(
                    new SqlMetaData("QuotaCellId", SqlDbType.Int),
                    new SqlMetaData("YearNumber", SqlDbType.Int),
                    new SqlMetaData("MonthNumber", SqlDbType.Int),
                    new SqlMetaData("Weighting", SqlDbType.Float)
                );

                (int quotaCellId, int year, int month, double scaleFactor) = r;
                responseWeighting.SetInt32(0, quotaCellId);
                responseWeighting.SetInt32(1, year);
                responseWeighting.SetInt32(2, month);
                responseWeighting.SetSqlDouble(3, scaleFactor);
                return responseWeighting;
            });
        }

        private class MetricBaseFieldComparer : IEqualityComparer<(ResponseFieldDescriptor BaseField, int[] BaseValues)>
        {
            public bool Equals((ResponseFieldDescriptor BaseField, int[] BaseValues) x, (ResponseFieldDescriptor BaseField, int[] BaseValues) y)
            {
                (var baseField, int[] baseValues) = x;
                (var otherBaseField, int[] otherBaseValues) = y;
                return baseField.Equals(otherBaseField) && baseValues.OrderBy(v => v).SequenceEqual(otherBaseValues.OrderBy(v => v));
            }

            public int GetHashCode((ResponseFieldDescriptor BaseField, int[] BaseValues) obj) =>
                HashCode.Combine(obj.BaseField, string.Join(",", obj.BaseValues.OrderBy(v => v)));
        }
    }
}
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Text;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Respondents.TextCoding;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;

namespace BrandVue.SourceData.LazyLoading
{
    public class AnswersTableLazyDataLoader : BaseLazyDataLoader
    {
        public AnswersTableLazyDataLoader(ISqlProvider sqlProvider, IDataLimiter dataLimiter) : base(sqlProvider, dataLimiter)
        {
        }

        protected override string BuildMeasureLoadingSql(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> fields, FieldDefinitionModel representativeFieldModel,
            IReadOnlyCollection<IDataTarget> targetInstances, Dictionary<string, object> sqlParameters,
            bool includeProfileData)
        {
            var safeSqlFieldParams = fields.Select((f, i) =>
            {
                var fieldDefinitionModel = f.GetDataAccessModel(subset.Id);
                string textLookupAlias = $"textLookup{i}";
                string fieldRef = fieldDefinitionModel.Lookup == null ? fieldDefinitionModel.ValueDbLocation.SafeSqlReference : $"{textLookupAlias}.MapToId";

                // When a floating point number is encountered, the AnswerValue is set to -993 and the value is stored in the text field - we need to bring this in. 
                if (fieldRef == "[AnswerValue]" && fieldDefinitionModel.ScaleFactor > 1)
                {
                    fieldRef = $"CASE WHEN [AnswerValue] = {SpecialResponseFieldValues.TextFieldIsNotANumber} THEN ISNULL(TRY_CAST([AnswerText] AS DECIMAL(18,9)), 0) ELSE [AnswerValue] END";
                }

                var selectRef = fieldRef;
                if (fieldDefinitionModel.ScaleFactor.HasValue)
                {
                    selectRef = fieldDefinitionModel.SqlRoundingType switch
                    {
                        SqlRoundingType.Ceiling => $"CEILING({fieldRef} * {fieldDefinitionModel.ScaleFactor.Value})",
                        SqlRoundingType.Floor => $"FLOOR({fieldRef} * {fieldDefinitionModel.ScaleFactor.Value})",
                        SqlRoundingType.Round => $"ROUND({fieldRef} * {fieldDefinitionModel.ScaleFactor.Value}, 0)",
                        _ => throw new ArgumentOutOfRangeException(),
                    };
                }

                return (IntegerIndex: i, VarCodeRef: $"@varCode{i}", ValueColumnId: $"Value_{i}",
                    DataAccessModel: fieldDefinitionModel, fieldDefinitionModel.Lookup, SelectRef: selectRef,
                    TextLookupRef: $"@textLookup{i}", TextLookupAlias: textLookupAlias);
            }).ToArray();

            var entityColumnGroups = representativeFieldModel.OrderedEntityColumns
                .Select(repCol => GetColumnGroups(safeSqlFieldParams, repCol)).ToArray();

            foreach (var f in safeSqlFieldParams)
            {
                string unsafeSqlVarCodeBase = f.DataAccessModel.UnsafeSqlVarCodeBase ?? throw new InvalidOperationException($"No varCode for {f.DataAccessModel.Name}");
                sqlParameters.Add(f.VarCodeRef.TrimStart('@'), new SqlParameter(f.VarCodeRef.TrimStart('@'), SqlDbType.NVarChar, 100){Value = unsafeSqlVarCodeBase});
                if (f.Lookup != null)
                {
                    sqlParameters.Add(f.TextLookupRef, new SqlParameter(f.TextLookupRef, SqlDbType.Structured)
                    {
                        Value = GetSqlRecordSetFromTextLookupData(f.Lookup.Data),
                        TypeName = "vue.TextLookup"
                    });
                }
            }

            return BuildSql(subset, targetInstances, includeProfileData, entityColumnGroups, safeSqlFieldParams);
        }

        private string BuildSql(Subset subset, IReadOnlyCollection<IDataTarget> targetInstances, bool includeProfileData,
            string[] entityColumnGroups,
            (int IntegerIndex, string VarCodeRef, string ValueColumnId, FieldDefinitionModel DataAccessModel, TextLookup Lookup,
                string SelectRef, string TextLookupRef, string TextLookupAlias)[] safeSqlFieldParams)
        {

            string surveyIdCommaList = subset.SurveyIdToSegmentNames.Keys.CommaList();
            var sb = new StringBuilder("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;");

            sb.Append(@"
SELECT sr.responseId responseId");
            if (includeProfileData) sb.Append(", MAX(sr.surveyId) surveyId, CAST(MAX(sr.lastChangeTime) AS Date) AS StartTime");

            if (!safeSqlFieldParams.Any())
            {
                // Exit early with no clever joining if no questions/answers needed
                sb.AppendLine($@"
FROM surveyResponse sr ");
                AppendLineWhereSurveyResponseIsRelevant();
                sb.AppendLine("GROUP BY sr.responseId");
                return sb.ToString();
            }

            sb.Append(entityColumnGroups.LeadingCommaList());

            foreach (var fieldParam in safeSqlFieldParams)
            {
                bool requiresNumericCast = fieldParam.DataAccessModel.ValueDbLocation != DbLocation.AnswerText;
                // Uses MAX here because it means we won't pick a value like -992 if there are multiple values.
                // Erroneous duplicates are removed in the data warehousing process but if a field definition omits one entity, there can be multiple results, e.g. For checking if a radio question was asked, you just want to know does it have ANY values
                sb.AppendLine().Append("    ,");
                if (requiresNumericCast) sb.Append("TRY_CAST(");
                sb.Append($"MAX(CASE WHEN VarCode = {fieldParam.VarCodeRef}");
                AppendFieldCondition(sb, fieldParam, targetInstances);
                sb.Append($@" THEN {fieldParam.SelectRef} END)");
                if (requiresNumericCast) sb.Append(@" AS INT)");
                sb.Append($@" {fieldParam.ValueColumnId}");
            }

            sb.AppendLine($@"
FROM");
            sb.Append(@"(
SELECT sr.responseId, sr.surveyId, sr.lastChangeTime, q.VarCode, q.QuestionId 
FROM surveyResponse sr 
    INNER JOIN vue.Questions q ON q.SurveyId = sr.surveyId");

            AppendLineWhereSurveyResponseIsRelevant();
            void AppendLineWhereSurveyResponseIsRelevant()
            {
                // EnsureDataLoadedForMeasure enforces a @startDate at the start of the day
                // Cast required so datetime roundtrips correctly https://app.clubhouse.io/mig-global/story/47536/show-numeric-results-again
                sb.AppendLine($@"
WHERE sr.lastChangeTime BETWEEN CAST(@startDate AS datetime) AND CAST(@endDate AS datetime)
    AND sr.status = 6  -- answers should only exists where status is 6, but specifying here helps use indexes which contain it
    AND sr.archived = 0
    AND sr.SurveyId IN ({surveyIdCommaList})");
                if (subset.SegmentIds != null)
                {
                    if (subset.SegmentIds.Count > 0)
                    {
                        sb.AppendLine($"    AND sr.SegmentId IN ({subset.SegmentIds.CommaList()})");
                    }
                }
            }

            if (safeSqlFieldParams.Any())
            {
                sb.AppendLine(" AND (");
                foreach (var fieldParam in safeSqlFieldParams)
                {
                    sb.Append(fieldParam.IntegerIndex == 0
                        ? "          "
                        : "        OR"
                    ).Append($" VarCode = {fieldParam.VarCodeRef}");
                    sb.AppendLine();
                }

                sb.Append(")");
            }

            sb.AppendLine(@") sr INNER JOIN Vue.answers a ON a.responseId = sr.responseId AND a.questionId = sr.questionId");

            foreach (var f in safeSqlFieldParams.Where(f => f.Lookup != null))
            {
                string joinOn = f.Lookup.BuildSqlJoinCondition("a.AnswerText", $"{f.TextLookupAlias}.LookupText");
                sb.Append($"LEFT OUTER JOIN {f.TextLookupRef} AS {f.TextLookupAlias} ON {joinOn}");
                AppendFieldCondition(sb, f, targetInstances);
                sb.AppendLine();
            }

            if (safeSqlFieldParams.Any())
            {
                sb.AppendLine(@"WHERE
    (");
                //Ensures we don't get extra NULL results returned. Especially relevant for spontaneous awareness, where the entity is sometimes null due to no match (the varcode bit also helps performance a lot)
                foreach (var fieldParam in safeSqlFieldParams)
                {
                    sb.Append(fieldParam.IntegerIndex == 0
                        ? "          "
                        : "        OR"
                    ).Append($" VarCode = {fieldParam.VarCodeRef}");
                    AppendFieldCondition(sb, fieldParam, targetInstances);
                    sb.AppendLine();
                }

                sb.AppendLine("    )");
            }

            sb.Append("GROUP BY sr.responseId");
            sb.AppendLine(entityColumnGroups.LeadingCommaList());
            // Note this is important for performance - if removing ensure the query plan is still good for retail in both loading profiles at startup and loading data for a measure
            // I think there is another good (possibly better) query plan for *some* situations (see file history), but there's also a terrible one that reads every row in the answers table we need to avoid
            sb.AppendLine("OPTION (FORCE ORDER)");
            return sb.ToString();

        }

        private static string GetColumnGroups((int IntegerIndex, string VarCodeRef, string ValueColumnId, FieldDefinitionModel DataAccessModel, TextLookup Lookup, string SelectRef, string TextLookupRef, string TextLookupAlias)[] safeSqlFieldParams, EntityFieldDefinitionModel repCol)
        {
            var groups = safeSqlFieldParams.Select(f => (Field: f,
                    Column: f.DataAccessModel.OrderedEntityColumns.Single(c => c.SafeSqlEntityIdentifier.Equals(repCol.SafeSqlEntityIdentifier))))
                .ToLookup(f => f.Column.DbLocation);

            if (groups.OnlyOrDefault() is {} onlyGroup) return onlyGroup.Key.SafeSqlReference;

            // Rare case when the entity is in a different column for different varcodes retrieved in the same query
            return
@$"CASE{string.Join(" ", groups.Select(g =>
                    $"\r\n    WHEN VarCode IN ({g.CommaList(f => f.Field.VarCodeRef)}) THEN {g.Key.SafeSqlReference}"))}
END";
        }

        private static void AppendFieldCondition(StringBuilder sb,
            (int IntegerIndex, string VarCodeRef, string ValueColumnId, FieldDefinitionModel DataAccessModel, TextLookup Lookup, string SelectRef, string TextLookupRef, string TextLookupAlias)
                fieldParam, IReadOnlyCollection<IDataTarget> targetInstances)
        {
            foreach (var filterCol in fieldParam.DataAccessModel.FilterColumns)
            {
                sb.Append($" AND {(filterCol.Location.SafeSqlReference)} = {filterCol.Value}");
            }

            foreach (var instance in targetInstances)
            {
                // ReSharper disable once SuggestVarOrType_Elsewhere - Making type clear so it's clear there's no sql injection
                ImmutableArray<int> safeSqlLongInstanceIds = instance.SortedEntityInstanceIds;
                sb.Append($" AND {fieldParam.DataAccessModel.GetSafeSqlColumnFor(instance.EntityType)} IN ({safeSqlLongInstanceIds.CommaList()})");
            }
        }

        /// <summary>
        /// https://github.com/MIG-Global/SurveyPlatform/tree/master/DatabaseSchema/vue/User%20Defined%20Types
        /// </summary>
        private static IEnumerable<SqlDataRecord> GetSqlRecordSetFromTextLookupData(IReadOnlyCollection<TextLookupData> textLookupData)
        {
            return textLookupData.Select(r =>
            {
                var textDataRecord = new SqlDataRecord(new SqlMetaData("LookupText", SqlDbType.NVarChar, 400),
                    new SqlMetaData("MapToId", SqlDbType.Int));
                textDataRecord.SetString(0, r.LookupText);
                textDataRecord.SetInt32(1, r.MapToId);
                return textDataRecord;
            });
        }
    }
}
using System.Threading;
using BrandVue.SourceData.CalculationPipeline;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.LazyLoading
{
    public class SyncedDataLimiter : IDataLimiter
    {
        private readonly ISqlProvider _sqlProvider;
        private readonly IProductContext _productContext;
        private readonly string _sqlSafeCommaSeparatedSurveyIds;
        private readonly DateTimeOffset? _lastSignOffDate;
        private readonly ILogger _logger;
        private readonly object _initialisationLock = new();
        private long _nextOutOfDateCheckTicks;
        private int _requestableResponseCount;
        private int _requestableResponseArchivedCount;
        private bool _requiresReload;
        private DateTimeOffset _latestDateToRequest;
        private DateTimeOffset _latestResponse;
        private DateTimeOffset _latestDataAppendTime;
        private bool _isResynching ;
        private DateTimeOffset ReloadedDate { get; }


        public SyncedDataLimiter(ISqlProvider sqlProvider, IProductContext productContext, int[] surveyIds,
            DateTimeOffset? lastSignOffDate,
            ILogger logger)
        {
            _sqlProvider = sqlProvider;
            _productContext = productContext;
            _sqlSafeCommaSeparatedSurveyIds = surveyIds.CommaList();
            _lastSignOffDate = lastSignOffDate;
            _logger = logger;
            ReloadedDate = DateTimeOffset.UtcNow;
        }

        internal int RecheckIntervalSeconds { get; set; } = 30;

        private bool AllowReloadFromCheckingArchiveOrCompletes => !_productContext.KeepInMemory && _productContext.AllowPartialDays;
        public DataLimiterStats Stats => new DataLimiterStats(AllowReloadFromCheckingArchiveOrCompletes, ReloadedDate, LatestDateToRequest, _requestableResponseCount, _requestableResponseArchivedCount, _isResynching, new DateTime(_nextOutOfDateCheckTicks).ToUtcDateOffset());
        public DateTimeOffset LatestDateToRequest
        {
            get
            {
                UpdateLimits();
                return _latestDateToRequest;
            }
        }

        public bool RequiresReload
        {
            get
            {
                UpdateLimits();
                return _requiresReload;
            }
        }

        /// <remarks>
        /// Do not use the above properties inside here or you'll cause infinite recursion!
        /// </remarks>
        private void UpdateLimits()
        {
            long recheckAfterTimeTicks = _nextOutOfDateCheckTicks;
            EnsureInitialised();

            // The check should be pretty cheap, but involves a db round trip and is expensive if it causes a reload, so ensure single threaded
            if (IsTimeToRecheck())
            {
                var (latestUsableDateForNewLoader, latestResponseInDb, latestDataAppendTime, isResynching) = GetLatestUsableDate();

                //For surveys with lots of responses (e.g. BrandVue), reload is slow, so we'll accept a discrepancy from the responses in the db until the reload the next day
                if (AllowReloadFromCheckingArchiveOrCompletes && ExistingCachedResponsesMayHaveChanged(latestResponseInDb, latestDataAppendTime, isResynching))
                {
                    _requiresReload = true;
                }
                else if (_latestDateToRequest < latestUsableDateForNewLoader && _latestDateToRequest < latestResponseInDb)
                {
                    // Future: Could do better than a full reload if we loaded missing profiles: https://github.com/MIG-Global/Vue/blob/555a06ca8849c77822004b52eb4b4bc2f59e8702/src/BrandVue.SourceData/CalculationPipeline/RespondentMeasureDataLoader.cs#L163
                    _requiresReload = true;
                }

                if (AllVueSurveyIdsHaveChanged())
                {
                    _requiresReload = true;
                }
                _isResynching = isResynching;
            }

            void EnsureInitialised()
            {
                if (_latestDateToRequest == DateTimeOffset.MinValue)
                {
                    lock (_initialisationLock)
                    {
                        if (TryIncrementTimeToCheck())
                        {
                            (_latestDateToRequest, _latestResponse, _latestDataAppendTime, _isResynching) = GetLatestUsableDate();
                            _requestableResponseCount = CountSurveyResponsesBefore(_latestDateToRequest);
                            _requestableResponseArchivedCount = CountSurveyResponsesArchived();
                        }
                    }
                }
            }

            bool ExistingCachedResponsesMayHaveChanged(DateTimeOffset latestResponse, DateTimeOffset latestDataAppendTime, bool isResynching) =>
                (latestResponse < _latestResponse)
                || (_latestDataAppendTime != latestDataAppendTime)
                || (CountSurveyResponsesBefore(_latestDateToRequest) != _requestableResponseCount)
                || (CountSurveyResponsesArchived() != _requestableResponseArchivedCount)
                || (_isResynching && !isResynching)
                ;

            bool IsTimeToRecheck() => recheckAfterTimeTicks < DateTime.UtcNow.Ticks && TryIncrementTimeToCheck();

            bool TryIncrementTimeToCheck() => Interlocked.Exchange(ref _nextOutOfDateCheckTicks, DateTime.UtcNow.AddSeconds(RecheckIntervalSeconds).Ticks) == recheckAfterTimeTicks;
        }

        private bool AllVueSurveyIdsHaveChanged() =>
            _productContext.IsAllVue && !_productContext.NonMapFileSurveyIds.IsEquivalent(GetLatestSurveyIds());

        private (DateTimeOffset LatestUsable, DateTimeOffset LatestResponseInDb, DateTimeOffset LatestDataAppendTimestamp, bool isResynching) GetLatestUsableDate()
        {

            if (_lastSignOffDate.HasValue) return (_lastSignOffDate.Value, _lastSignOffDate.Value, DateTimeOffset.MinValue, _isResynching);

            (DateTimeOffset LatestUsable, DateTimeOffset LatestResponseInDb, DateTimeOffset LatestDataAppendTime, bool isResynching) toReturn = default;

            const int lastDataSyncTimeColumnIndex = 0;
            const int mostRecentResponseColumnIndex = 1;
            const int lastDataAppendTimeColumnIndex = 2;
            const int resynchingColumnIndex = 3;

            string sqlQuery = $@"
SELECT MIN(LastDataSyncTime), MAX(MostRecentResponse), MAX(LastDataAppendTime),
MAX(CASE WHEN MostRecentResponse IS NOT NULL THEN 0 ELSE 1 END) FROM vue.SyncStates
WHERE SurveyId IN ({_sqlSafeCommaSeparatedSurveyIds})
";

            _sqlProvider.ExecuteReader(sqlQuery, null, reader =>
            {
                var latestUsableDate = reader.GetNullableDateTimeOffset(lastDataSyncTimeColumnIndex) ?? DateTime.UtcNow.ToUtcDateOffset().EndOfPreviousDay();
                var latestResponseInDb = reader.GetNullableDateTimeOffset(mostRecentResponseColumnIndex) ?? DateTime.UtcNow.ToUtcDateOffset().EndOfPreviousDay();
                var latestDataAppendTime = reader.GetNullableDateTimeOffset(lastDataAppendTimeColumnIndex) ?? DateTimeOffset.MinValue;
                var isReSynching = !reader.IsDBNull(resynchingColumnIndex) && reader.GetInt32(resynchingColumnIndex) == 1;

                if (latestUsableDate < DateTime.UtcNow.AddDays(-1))
                {
                    _logger.LogWarning($"#Vue_MissingData. #{_productContext.SubProductId ?? _productContext.ShortCode} Possible missing data. Please check following surveys sync status: {string.Join(", ", _sqlSafeCommaSeparatedSurveyIds)}");
                }

                // If it's a big project that had data coming in until recently, warn
                if (_productContext.KeepInMemory && latestUsableDate.AddDays(-3) < latestResponseInDb && latestResponseInDb < DateTime.UtcNow.AddDays(-1))
                {
                    _logger.LogWarning($"#Vue_MissingData. #{_productContext.SubProductId ?? _productContext.ShortCode} Possible missing data. No response received since {latestResponseInDb}. Please check sampling for surveys: {string.Join(", ", _sqlSafeCommaSeparatedSurveyIds)}");
                }

                latestUsableDate = _productContext.AllowPartialDays ? latestUsableDate : latestUsableDate.EndOfPreviousDay();
                toReturn = (latestUsableDate, latestResponseInDb, latestDataAppendTime, isReSynching);
            });

            return toReturn;
        }

        private IEnumerable<int> GetLatestSurveyIds()
        {
            if (int.TryParse(_productContext.SubProductId, out int surveyId))
            {
                return new List<int> { surveyId };
            }

            string sqlQuery = $@"
SELECT sg.SurveyId
FROM dbo.SurveyGroupSurveys sg
INNER JOIN dbo.SurveyGroups s
    ON s.SurveyGroupId = sg.SurveyGroupId
WHERE s.UrlSafeName = @subProductId";

            var toReturn = new List<int>();

            _sqlProvider.ExecuteReader(sqlQuery, new Dictionary<string, object> { { "subProductId", _productContext.SubProductId } } , reader =>
            {
                toReturn.Add(reader.GetInt32(0));
            });

            return toReturn;
        }

        private int CountSurveyResponsesArchived()
        {
            var sqlQuery = $@"
select count(*) from panelRespondents 
    WITH (INDEX(IX_panelRespondents_archived_panelRespondentId_surveyId)) 
where surveyId in ({_sqlSafeCommaSeparatedSurveyIds}) AND archived = 1
";
            int? numberOfResponses = null;
            _sqlProvider.ExecuteReader(sqlQuery, new Dictionary<string, object> { }, reader =>
            {
                numberOfResponses = reader.GetInt32(0);
            });

            return numberOfResponses ?? throw new InvalidOperationException("Query should not return null");
        }

        private int CountSurveyResponsesBefore(DateTimeOffset latestInclusive)
        {
            string timeField = _productContext.DayIsFromResponseEndTime ? "lastChangeTime" : "timestamp";
            string sqlQuery = $@"
SELECT COUNT(1) FROM [SurveyResponse] sr
WHERE sr.surveyid IN ({_sqlSafeCommaSeparatedSurveyIds})
AND sr.{timeField} <= @endDate AND status = 6
";
            int? numberOfResponses = null;
            _sqlProvider.ExecuteReader(sqlQuery, new Dictionary<string, object> { { "endDate", latestInclusive } }, reader =>
            {
                numberOfResponses = reader.GetInt32(0);
            });

            return numberOfResponses ?? throw new InvalidOperationException("Query should not return null");
        }
    }
}

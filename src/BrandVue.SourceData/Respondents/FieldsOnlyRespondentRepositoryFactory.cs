using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.QuotaCells;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Respondents
{
    public class FieldsOnlyRespondentRepositoryFactory : IRespondentRepositoryFactory
    {
        private readonly ILogger _logger;
        private readonly AppSettings _settings;
        private readonly IProductContext _productContext;
        private readonly IResponseFieldManager _responseFieldManager;
        private readonly ILazyDataLoader _lazyDataLoader;
        private readonly MapFileQuotaCellDescriptionProvider _mapperCollection;
        private readonly IReadOnlyDictionary<Subset, QuotaCellReferenceWeightings> _quotaCellReferenceWeightingsMap;

        public FieldsOnlyRespondentRepositoryFactory(
            IResponseFieldManager responseFieldManager,
            ILazyDataLoader lazyDataLoader,
            ILogger<FieldsOnlyRespondentRepositoryFactory> logger,
            AppSettings settings,
            IProductContext productContext,
            MapFileQuotaCellDescriptionProvider mapperCollection,
            IReadOnlyDictionary<Subset, QuotaCellReferenceWeightings> quotaCellReferenceWeightingsMap)
        {
            _responseFieldManager = responseFieldManager;
            _lazyDataLoader = lazyDataLoader;
            _logger = logger;
            _settings = settings;
            _productContext = productContext;
            _mapperCollection = mapperCollection;
            _quotaCellReferenceWeightingsMap = quotaCellReferenceWeightingsMap;
        }

        public List<QuotaCellAllocationReason> QuotaCellAllocationReason(Subset subset, IProfileResponseEntity profileResponseEntity)
        {
            var profileResponse = profileResponseEntity as ProfileResponseEntity;

            var fields = MapFileQuotaCellDescriptionProvider.GetFieldsForSubset(subset)
                .Select(s => _responseFieldManager.Get(s))
                .ToArray();
            var fieldValues = new Dictionary<string, int>();
            var results = new List<QuotaCellAllocationReason>();
            int numberOfFailures = 0;
            foreach (var field in fields)
            {
                var value = profileResponse.GetIntegerFieldValue(field, default);
                if (value != null)
                {
                    results.Add(new QuotaCellAllocationReason(field.Name, value, null));
                    fieldValues.Add(field.Name, (int)value);
                }
                else
                {
                    results.Add(new QuotaCellAllocationReason(field.Name, null, "Phase1: Failed to get value"));
                    numberOfFailures++;;
                }
            }
            if (numberOfFailures> 0)
            {
                return results;
            }
            return GetQuotaCellReason(subset, fieldValues);
        }

        private List<QuotaCellAllocationReason> GetQuotaCellReason(Subset subset, IReadOnlyDictionary<string, int> fieldValues)
        {
            var results = new List<QuotaCellAllocationReason>();
            try
            {
                foreach (var mapper in _mapperCollection.GetMappersForSubset(subset, null))
                {
                    var keyForField = mapper.GetCellKeyForProfile(fieldValues);
                    if (keyForField is null)
                    {
                        results.Add(new Respondents.QuotaCellAllocationReason(mapper.QuotaField, null, "Failed to find"));
                        return results;
                    }
                    results.Add(new QuotaCellAllocationReason(keyForField, fieldValues[keyForField], ""));
                }
            }
            catch (Exception ex)
            {
                results.Add(new Respondents.QuotaCellAllocationReason("Failure", null, ex.Message));
            }
            return results;
        }

        public async Task<IRespondentRepository> CreateRespondentRepository(Subset subset, DateTimeOffset? signOffDate,
            CancellationToken cancellationToken)
        {
            var respondentRepository = new RespondentRepository(subset, signOffDate);
            if (subset.Disabled)
            {
                _logger.LogWarning("Subset {SubsetId} is disabled and will not be loaded. Full subset information: {@Subset}",
                    subset.Id, subset);
            }

            try
            {
                LoadProfiles(subset, respondentRepository);
                if (!_productContext.HasSingleClient && _settings.UseDatabaseAssistedCalculationsForAudiences) CacheResponseQuotaCellInDb(subset, respondentRepository);
            }
            catch (DirectoryNotFoundException dnfe)
            {
                HandleLoadingException(subset, dnfe);
            }
            catch (FileNotFoundException fnfe)
            {
                HandleLoadingException(subset, fnfe);
            }
            catch (SqlException sqle)
            {
                _settings.UseDatabaseAssistedCalculationsForAudiences = false;
                _logger.LogError(sqle, $"Failed to cache response quota cells for product: '{_productContext.ShortCode}'. Disabling database optimized calculations.");
            }

            return respondentRepository;
        }

        /// <summary>
        /// TODO: This should really be only a temporary solution as it's quite clumsy. Ideally some kind of sync process could deal with assigning quota cells so BV doesn't have to do it.
        /// </summary>
        private void CacheResponseQuotaCellInDb(Subset subset, RespondentRepository respondents)
        {
            string tableName = $"[{_productContext.ShortCode}-{subset.Id}-responseQuotas]";
            var responseQuotaCells = new DataTable(tableName){ MinimumCapacity = respondents.Count };
            responseQuotaCells.Columns.Add("ResponseId", typeof(int));
            responseQuotaCells.Columns.Add("QuotaCellId", typeof(int));
            foreach (var response in respondents)
            {
                var dataRow = responseQuotaCells.NewRow();
                dataRow[0] = response.ProfileResponseEntity.Id;
                dataRow[1] = response.QuotaCell.Id;
                responseQuotaCells.Rows.Add(dataRow);
            }

            using var connection = new SqlConnection(_settings.MetaConnectionString);
            connection.Open();
            var recreateResponseQuotaTable = connection.CreateCommand();
            recreateResponseQuotaTable.CommandText = @$"
DROP TABLE IF EXISTS {tableName};
CREATE TABLE {tableName} (ResponseId INT PRIMARY KEY, QuotaCellId INT);";
            recreateResponseQuotaTable.ExecuteNonQuery();
            using var bulkCopy = new SqlBulkCopy(connection) { DestinationTableName = tableName };
            bulkCopy.WriteToServer(responseQuotaCells);
        }

        private void HandleLoadingException(Subset subset, IOException fnfe)
        {
            _logger.LogError(fnfe,
                "Error loading subset {@Subset} {ExceptionMessage} at {ExceptionStackTrace} Subset will be unavailable within dashboard.",
                subset, fnfe.Message, fnfe.StackTrace);

            subset.Disabled = true;
        }

        private void LoadProfiles(Subset subset, RespondentRepository respondents)
        {
            var fields = MapFileQuotaCellDescriptionProvider.GetFieldsForSubset(subset)
                .Select(s => _responseFieldManager.Get(s))
                .ToArray();

            if (fields.Any(f => f.EntityCombination.Count > 1))
            {
                throw new InvalidOperationException("Fields for quota cells can't be multi-entity");
            }

            var responsesWithNoQuotaCells = 0;
            var unweightedQuotaCell = QuotaCell.UnweightedQuotaCell(subset);
            var quotaCells = new Dictionary<string, QuotaCell> { { unweightedQuotaCell.ToString(), unweightedQuotaCell } };
            foreach (var responseFieldData in _lazyDataLoader.GetResponses(subset, fields).OrderBy(x=> x.ResponseId))
            {
                var profileResponse = new ProfileResponseEntity(responseFieldData.ResponseId, responseFieldData.Timestamp.ToDateInstance(), responseFieldData.SurveyId);

                foreach (var fieldValue in responseFieldData.FieldValues)
                {
                    var field = fieldValue.Key;
                    field.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
                    var value = fieldValue.Value;

                    var entityType = field.EntityCombination.OnlyOrDefault();
                    var entityIds = entityType != null ? EntityIds.FromIdsOrderedByEntityType(new int[] { value }) : EntityIds.FromIdsOrderedByEntityType(Array.Empty<int>());

                    profileResponse.AddFieldValue(field, entityIds, value, subset);
                }

                var fieldValues = responseFieldData.FieldValues.ToDictionary(f => f.Key.Name, f => f.Value);
                var quotaCell = GetQuotaCellFor(subset, responseFieldData.ResponseId, fieldValues, unweightedQuotaCell, quotaCells);

                if (quotaCell.IsUnweightedCell)
                {
                    responsesWithNoQuotaCells += 1;
                }

                respondents.Add(profileResponse, quotaCell);
            }
            if (responsesWithNoQuotaCells > 0)
            {
                _logger.LogWarning("{Product} ({Subset}): Placed {ResponsesWithNoQuotaCells} responses in unweighted cell due to errors assigning quota cell. Loaded {RespondentsCount} responses.", _productContext, subset.Id, responsesWithNoQuotaCells, respondents.Count);
            }
            if (respondents.Count == 0)
            {
                _logger.LogCritical("{Product} ({Subset}): No responses have been loaded for dataset. Check profile source has not been corrupted.", _productContext, subset.Id);
            }
            else if (responsesWithNoQuotaCells == 0)
            {
                _logger.LogInformation("{Product} ({Subset}): Loaded {RespondentsCount} responses.", _productContext, subset.Id, respondents.Count);
            }
            _logger.LogWarning("{@Subset}", subset);
        }

        private QuotaCell GetQuotaCellFor(Subset subset, int responseId, IReadOnlyDictionary<string, int> fieldValues, QuotaCell unweightedQuotaCell, IDictionary<string, QuotaCell> quotaCellsByStringKey)
        {
            try
            {
                var fieldToKey = new Dictionary<string, string>();

                //This method is only used by the FieldsOnlyRespondentRepositoryFactory. For AllVue we don't care about schemes yet so just pass null
                foreach (var mapper in _mapperCollection.GetMappersForSubset(subset, null))
                {
                    var keyForField = mapper.GetCellKeyForProfile(fieldValues);
                    if (keyForField is null)
                    {
                        return unweightedQuotaCell;
                    }
                    fieldToKey.Add(mapper.QuotaField, keyForField);
                }

                var newCell = new QuotaCell(quotaCellsByStringKey.Count - 1, subset, fieldToKey, null)
                {
                    Index = quotaCellsByStringKey.Count
                };
                return GetExistingOrAddNew(newCell, quotaCellsByStringKey, unweightedQuotaCell);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error assigning quota cell to ProfileResponseEntity {ProfileResponseId}. Respondent will be placed in the unweighted cell. Message: {ErrorMessage}", responseId, ex.Message);
            }
            return unweightedQuotaCell;
        }

        private QuotaCell GetExistingOrAddNew(QuotaCell newCell, IDictionary<string, QuotaCell> quotaCellsByStringKey,
            QuotaCell unweightedQuotaCell)
        {
            string newCellKey = newCell.ToString();
            if (quotaCellsByStringKey.TryGetValue(newCellKey, out var existingCell))
            {
                return existingCell;
            }

            if (IncludeCell(unweightedQuotaCell.Subset, newCell))
            {
                quotaCellsByStringKey.Add(newCellKey, newCell);
                return newCell;
            }

            return unweightedQuotaCell;
        }

        private bool IncludeCell(Subset subset, QuotaCell quotaCell) =>
            _quotaCellReferenceWeightingsMap.TryGetValue(subset, out var referenceWeightings) &&
            referenceWeightings.HasReferenceWeightingFor(quotaCell);
    }
}

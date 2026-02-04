using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlTypes;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Utils;
using Microsoft.EntityFrameworkCore;

namespace BrandVue.SourceData.LazyLoading
{
    public abstract class BaseLazyDataLoader : ILazyDataLoader
    {
        readonly ISqlProvider _sqlProvider;
        private readonly int _sizeOfChunk = 50;

        public IDataLimiter DataLimiter { get; }

        public BaseLazyDataLoader(ISqlProvider sqlProvider, IDataLimiter dataLimiter)
        {
            _sqlProvider = sqlProvider;
            DataLimiter = dataLimiter;
        }

        public IEnumerable<ResponseFieldData> GetResponses(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> quotaCellFields)
        {
            // We make sure that the TimeStamp property is set so we can rely on nullable Timestamp property below
            var measureData =  GetEntityMeasureDataWithTimeStamp(subset, quotaCellFields);

            if (quotaCellFields.All(f => f.EntityCombination.Count == 0)) // Avoid GroupBy when standard BrandVue profile fields
            {
                return measureData.Select(emd => new ResponseFieldData(emd.ResponseId, emd.Timestamp.Value, emd.SurveyId.Value, emd.Measures.ToDictionary(m => m.Field, m => m.Value)));
            }

            return measureData
                .GroupBy(d => d.ResponseId)
                .Select(g =>
                {
                    var firstMeasureDataForResponseId = g.First();
                    return new ResponseFieldData(
                        g.Key,
                        firstMeasureDataForResponseId.Timestamp.Value,
                        firstMeasureDataForResponseId.SurveyId.Value,
                        g.SelectMany(d => d.Measures).ToDictionary(m => m.Field, m => m.Value)
                    );
                });
        }

        private IEnumerable<EntityMetricData> GetEntityMeasureDataWithTimeStamp(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> quotaCellFields)
        {
            var results = new Dictionary<(int responseId, TypedEntityIds typedEntityIds), EntityMetricData>();

            var emptyModel = new FieldDefinitionModel(null, null, null, "AnswerValue", null, null,
                null, EntityInstanceColumnLocation.optValue, null, false, null, Enumerable.Empty<EntityFieldDefinitionModel>(), null) {ValueDbLocation = DbLocation.ConstantOne};
            var emptyGroup = new EntityCombinationFieldGroup(subset, emptyModel, Array.Empty<ResponseFieldDescriptor>());

            var fieldsByEntityCombo = quotaCellFields.Any() ? EntityCombinationFieldGroup.CreateGroups(subset, quotaCellFields) : emptyGroup.Yield();

            foreach (var entityCombinationFieldGroup in fieldsByEntityCombo.SelectMany(e => SelectFieldsEntityCombinationFieldGroups(e, _sizeOfChunk)))
            {
                PopulateResultsWithFields(results, null, Array.Empty<IDataTarget>(), entityCombinationFieldGroup, true);
            }

            return results.Values.ToArray();
        }

        public async Task<EntityMetricData[]> GetDataForFields(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> fields,
            (DateTime startDate, DateTime endDate)? timeRange, IReadOnlyCollection<IDataTarget> targetInstances,
            CancellationToken cancellationToken)
        {
            if (!fields.Any()) return Array.Empty<EntityMetricData>();
            if (!fields.All(f => 
                    f.EntityCombination.All(ec => 
                        targetInstances.Any(t => t.EntityType.Equals(ec)))))
            {
                throw new ArgumentException("Field missing entity type in target instances", nameof(targetInstances));
            }

            var fieldsByEntityCombo = EntityCombinationFieldGroup.CreateGroups(subset, fields);
            var results = new Dictionary<(int responseId, TypedEntityIds typedEntityIds), EntityMetricData>();

            foreach (var entityCombinationFieldGroup in fieldsByEntityCombo.SelectMany(e => SelectFieldsEntityCombinationFieldGroups(e, _sizeOfChunk)))
            {
                await PopulateResultsWithFieldsAsync(results, timeRange, targetInstances, entityCombinationFieldGroup, false, cancellationToken);
            }

            return results.Values.ToArray();
        }

        private static IEnumerable<EntityCombinationFieldGroup> SelectFieldsEntityCombinationFieldGroups(EntityCombinationFieldGroup e, int sizeOfChunk)
        {
            //We don't need to split fields and repopulate them if we don't have any fields
            if (!e.Fields.Any())
                return e.Yield();
            //Splitting Fields in chunks of 50 and then creating EntityCombinationFieldGroup
            return e.Fields.ToList().SplitList(sizeOfChunk).Select(f => new EntityCombinationFieldGroup(e.Subset, e.RepresentativeFieldModel, f.ToArray()));
        }

        private void PopulateResultsWithFields(Dictionary<(int responseId, TypedEntityIds typedEntityIds), EntityMetricData> results, (DateTime startDate, DateTime endDate)? timeRange,
            IReadOnlyCollection<IDataTarget> targetInstances, EntityCombinationFieldGroup entityCombinationFieldGroup, bool includeProfileData)
        {
            var targetInstancesForThisCombination = entityCombinationFieldGroup.GetRelevantTargetInstances(targetInstances);

            var limitedTimeRange = (
                startDate: DateTimeOffsetExtensions.Max(timeRange?.startDate.ToUtcDateOffset(),
                    SqlDateTime.MinValue.Value.ToUtcDateOffset()).NormalizeSqlDateTime(),
                endDate: DateTimeOffsetExtensions.Min(timeRange?.endDate.ToUtcDateOffset(),
                    DataLimiter.LatestDateToRequest).NormalizeSqlDateTime());

            var sqlParameters =
                new Dictionary<string, object>
                {
                    {"startDate", limitedTimeRange.startDate},
                    {"endDate", limitedTimeRange.endDate}
                };

            var sql = BuildMeasureLoadingSql(entityCombinationFieldGroup.Subset, entityCombinationFieldGroup.Fields,
                entityCombinationFieldGroup.RepresentativeFieldModel, targetInstancesForThisCombination, sqlParameters,
                includeProfileData);

            //PERF: Get models in advance (saved 25% of CPU Time) compared to doing per row inside ExecuteReader
            var fields = entityCombinationFieldGroup.Fields
                .Select(f => (Field: f, Model: f.GetDataAccessModel(entityCombinationFieldGroup.Subset.Id))).ToArray();
            _sqlProvider.ExecuteReader(sql, sqlParameters,
                dataRecord =>
                {
                    ReadResult(results, entityCombinationFieldGroup.Subset, fields, dataRecord,
                        entityCombinationFieldGroup.RepresentativeFieldModel, includeProfileData);
                });
        }


        private async Task PopulateResultsWithFieldsAsync(
            Dictionary<(int responseId, TypedEntityIds typedEntityIds), EntityMetricData> results,
            (DateTime startDate, DateTime endDate)? timeRange,
            IReadOnlyCollection<IDataTarget> targetInstances, EntityCombinationFieldGroup entityCombinationFieldGroup,
            bool includeProfileData, CancellationToken cancellationToken)
        {
            var targetInstancesForThisCombination = entityCombinationFieldGroup.GetRelevantTargetInstances(targetInstances);

            var limitedTimeRange = (
                startDate: DateTimeOffsetExtensions.Max(timeRange?.startDate.ToUtcDateOffset(),
                    SqlDateTime.MinValue.Value.ToUtcDateOffset()).NormalizeSqlDateTime(),
                endDate: DateTimeOffsetExtensions.Min(timeRange?.endDate.ToUtcDateOffset(),
                    DataLimiter.LatestDateToRequest).NormalizeSqlDateTime());

            var sqlParameters =
                new Dictionary<string, object>
                {
                    {"startDate", limitedTimeRange.startDate},
                    {"endDate", limitedTimeRange.endDate}
                };

            var sql = BuildMeasureLoadingSql(entityCombinationFieldGroup.Subset, entityCombinationFieldGroup.Fields,
                entityCombinationFieldGroup.RepresentativeFieldModel, targetInstancesForThisCombination, sqlParameters,
                includeProfileData);

            //PERF: Get models in advance (saved 25% of CPU Time) compared to doing per row inside ExecuteReader
            var fields = entityCombinationFieldGroup.Fields
                .Select(f => (Field: f, Model: f.GetDataAccessModel(entityCombinationFieldGroup.Subset.Id))).ToArray();
            await _sqlProvider.ExecuteReaderAsync(sql, sqlParameters,
                dataRecord =>
                {
                    ReadResult(results, entityCombinationFieldGroup.Subset, fields, dataRecord,
                        entityCombinationFieldGroup.RepresentativeFieldModel, includeProfileData);
                }, cancellationToken);
        }

        private void ReadResult(Dictionary<(int responseId, TypedEntityIds typedEntityIds), EntityMetricData> results, Subset subset,
            (ResponseFieldDescriptor Field, FieldDefinitionModel Model)[] fields, IDataRecord dataRecord,
            FieldDefinitionModel representativeFieldModel, bool hasProfileData)
        {
            int responseId;
            int? surveyId = null;
            DateTimeOffset? dateTimeOffset = null;
            int columnsBeforeEntityIds = 0;
            int currentColumn = 0;
            try
            {
                responseId = dataRecord.GetInt32(currentColumn++);
                columnsBeforeEntityIds++;
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error reading {nameof(responseId)} from column 0 for survey segment {subset.Id}", e);
            }


            if (hasProfileData)
            {
                try
                {
                    surveyId = dataRecord.GetInt32(currentColumn++);
                    columnsBeforeEntityIds++;
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Error reading {nameof(surveyId)} from column 1 for survey segment {subset.Id}", e);
                }
                try
                {
                    dateTimeOffset = dataRecord.GetDateTime(currentColumn++);
                    columnsBeforeEntityIds++;
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Error reading {nameof(EntityMetricData.Timestamp)} from column {currentColumn} for survey segment {subset.Id}", e);
                }
            }
            // ALLOC: Avoid allocating an object for an empty array for 0 entity fields
            var orderedEntityIds = representativeFieldModel.OrderedEntityColumns.Any() ? new int[representativeFieldModel.OrderedEntityColumns.Length] : Array.Empty<int>();

            foreach (var column in representativeFieldModel.OrderedEntityColumns)
            {
                try
                {
                    orderedEntityIds[currentColumn - columnsBeforeEntityIds] = dataRecord.GetInt32(currentColumn++);
                }
                catch (Exception e)
                {
                    throw new Exception(
                        $"Error reading id of entity type: {column.EntityType} for field: {currentColumn} for survey segment {subset.Id}",
                        e);
                }
            }

            var entityIds = EntityIds.FromIdsOrderedByEntityType(orderedEntityIds);
            var typedEntityIds = new TypedEntityIds(representativeFieldModel.OrderedEntityCombination, entityIds);

            if (!results.TryGetValue((responseId, typedEntityIds), out var entityMeasureData))
            {
                entityMeasureData = new EntityMetricData() {EntityIds = entityIds, ResponseId = responseId, SurveyId = surveyId, Timestamp = dateTimeOffset};
                results.Add((responseId, typedEntityIds), entityMeasureData);
            }

            for (var extraFieldIndex = 0; extraFieldIndex < fields.Length; extraFieldIndex++, currentColumn++)
            {
                if (dataRecord.IsDBNull(currentColumn))
                {
                    continue;
                }

                if (fields[extraFieldIndex].Model.ValueIsOpenText)
                    AddTextFieldValue(subset, dataRecord, currentColumn, extraFieldIndex, entityMeasureData, fields[extraFieldIndex].Field);
                else AddNumericFieldValue(subset, dataRecord, currentColumn, fields[extraFieldIndex].Field, extraFieldIndex, entityMeasureData);
            }
        }

        private static void AddNumericFieldValue(Subset subset, IDataRecord dataRecord, int currentField,
            ResponseFieldDescriptor field, int extraFieldIndex, EntityMetricData result)
        {
            int measureValue;
            try
            {
                measureValue = dataRecord.GetInt32(currentField);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error reading {field.Name} from column {currentField} for survey segment {subset.Id}",
                    e);
            }

            result.Measures.Add((field, measureValue));
        }

        private static void AddTextFieldValue(Subset subset, IDataRecord dataRecord, int currentField, int extraFieldIndex, EntityMetricData result, ResponseFieldDescriptor responseFieldDescriptor)
        {
            string measureValue;
            try
            {
                measureValue = dataRecord.GetString(currentField);
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Error reading {responseFieldDescriptor.Name} from column {extraFieldIndex + currentField} for survey segment {subset.Id}",
                    e);
            }

            result.TextFields.Add((responseFieldDescriptor, measureValue));
        }

        protected abstract string BuildMeasureLoadingSql(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> fields, FieldDefinitionModel representativeFieldModel,
            IReadOnlyCollection<IDataTarget> targetInstances, Dictionary<string, object> sqlParameters,
            bool includeProfileData);

        protected static string GetSqlColumn(string tableAlias, ResponseFieldDescriptor fieldDescriptor, Subset subset)
        {
            var fieldModel = fieldDescriptor.GetDataAccessModel(subset.Id);
            return $"{tableAlias}.{fieldModel.ValueDbLocation.SafeSqlReference}";
        }

        /// <summary>
        /// This contains the same info as EntityValueCombination, but is optimized for minimal allocations
        /// It may one day supersede EntityValueCombination so long as it doesn't leak too many optimization details higher into the web layer
        /// </summary>
        /// <remarks>
        /// Would probably be better as a record struct in C#10 to save some boilerplate
        /// </remarks>
        private readonly struct TypedEntityIds : IEquatable<TypedEntityIds>
        {
#nullable enable
            public ImmutableArray<EntityType> OrderedTypes { get; }
            public EntityIds Ids { get; }

            public TypedEntityIds(ImmutableArray<EntityType> orderedTypes, EntityIds entityIds)
            {
                OrderedTypes = orderedTypes;
                Ids = entityIds;
            }

            public override bool Equals(object? obj) => obj is EntityIds other && Equals(other);

            public bool Equals(TypedEntityIds other) => Ids.Equals(other.Ids) && OrderedTypes.SequenceEqual(other.OrderedTypes);

            public override int GetHashCode()
            {
                if (!OrderedTypes.Any()) return 0;

                int hashcode = Ids.GetHashCode();
                for (int i = 0, loopTo = OrderedTypes.Length; i < loopTo; i++)
                {
                    hashcode = HashCode.Combine(hashcode, OrderedTypes[i], Ids[i]);
                }

                return hashcode;
            }
        }
    }
}
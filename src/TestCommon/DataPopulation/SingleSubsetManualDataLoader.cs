using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace TestCommon.DataPopulation
{
    /// <summary>
    /// Only handles a single field per entity measure data
    /// </summary>
    public class SingleSubsetManualDataLoader : ILazyDataLoader
    {
        private readonly ILookup<string, EntityMetricData> _fieldNameToEntityMeasureData;

        public SingleSubsetManualDataLoader(IReadOnlyCollection<EntityMetricData> subsetEntityMeasureData)
        {
            _fieldNameToEntityMeasureData = subsetEntityMeasureData.ToLookup(emd => emd.Measures.Single().Field.Name);
        }

        public async Task<EntityMetricData[]> GetDataForFields(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> fields,
            (DateTime startDate, DateTime endDate)? timeRange, IReadOnlyCollection<IDataTarget> targetInstances,
            CancellationToken cancellationToken)
        {
            var orderedDataTargets = targetInstances.OrderBy(t => t.EntityType).ToArray();

            var forField = FilterByField(fields);
            var forFieldAndTime = FilterByTime(forField, timeRange);
            forFieldAndTime = FilterByEntityIds(orderedDataTargets, forFieldAndTime);
            return GetFieldResponses(forFieldAndTime, fields);
        }

        public IEnumerable<ResponseFieldData> GetResponses(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> quotaCellFields)
        {
            var entityMeasureData = _fieldNameToEntityMeasureData.SelectMany(f => f);
            var measureData = GetFieldResponses(entityMeasureData, quotaCellFields);
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

        public IDataLimiter DataLimiter { get; } = new NullDataLimiter();

        private static EntityMetricData[] GetFieldResponses(IEnumerable<EntityMetricData> entityMeasureData,
            IEnumerable<ResponseFieldDescriptor> fields)
        {
            var fieldNamesToSelect = fields.ToDictionary(x => x.Name);
            return entityMeasureData.Select(emd => CreateEntityMeasureData(emd, fieldNamesToSelect)).ToArray();
        }

        private IEnumerable<EntityMetricData> FilterByField(IReadOnlyCollection<ResponseFieldDescriptor> fields)
        {
            var subsetData = _fieldNameToEntityMeasureData;

            var forField = fields.SelectMany(f => subsetData[f.Name]);
            return forField;
        }

        private static IEnumerable<EntityMetricData> FilterByEntityIds(IDataTarget[] orderedDataTargets, IEnumerable<EntityMetricData> entityMeasureData)
        {
            return entityMeasureData.Where(emd =>
                orderedDataTargets.Select((t, index) => t.SortedEntityInstanceIds.Contains(emd.EntityIds[index])).All(x => x));
        }

        private static IEnumerable<EntityMetricData> FilterByTime(IEnumerable<EntityMetricData> subsetData, (DateTime startTime, DateTime endDate)? timeRange)
        {
            if (timeRange.HasValue)
            {
                subsetData = subsetData.Where(emd => timeRange.Value.startTime <= emd.Timestamp && emd.Timestamp <= timeRange.Value.endDate).ToArray();
            }

            return subsetData;
        }

        private static EntityMetricData CreateEntityMeasureData(EntityMetricData emd,
            Dictionary<string, ResponseFieldDescriptor> fieldNamesToSelect)
        {
            var entityMeasureData = new EntityMetricData()
            {
                EntityIds = emd.EntityIds,
                ResponseId = emd.ResponseId,
                Timestamp = emd.Timestamp,
                SurveyId = emd.SurveyId,
                // Must switch to the field from the requesting loader since its InMemoryIndex can be different
                Measures = emd.Measures.Where(m => fieldNamesToSelect.ContainsKey(m.Field.Name))
                    .Select(m => (fieldNamesToSelect[m.Field.Name], m.Value)).ToList()
            };
            return entityMeasureData;
        }
    }
}
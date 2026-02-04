using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using TestCommon.Extensions;

namespace TestCommon.DataPopulation
{
    public class FileLazyDataLoader : ILazyDataLoader
    {
        private readonly IBrandVueDataLoaderSettings _settings;

        private readonly Dictionary<string, (List<int>[] Keys, Dictionary<string, List<string>> Values)> _data =
            new Dictionary<string, (List<int>[], Dictionary<string, List<string>>)>();


        public FileLazyDataLoader(IBrandVueDataLoaderSettings settings)
        {
            _settings = settings;
        }

        private (List<int>[] Keys, Dictionary<string, List<string>> Values) GetCsvLookup(string filename, int keyCols)
        {
            if (!_data.ContainsKey(filename))
            {
                var key = new List<int>[keyCols];
                var values = new Dictionary<string, List<string>>();
                using (var rdr = new StreamReader(filename))
                {
                    var header = rdr.ReadLine().Split(',');

                    for (var headerIndex = 0; headerIndex < header.Length; headerIndex++)
                    {
                        if (headerIndex < keyCols)
                        {
                            key[headerIndex] = new List<int>();
                        }
                        else
                        {
                            values[header[headerIndex]] = new List<string>();
                        }
                    }

                    var currentRow = 0;
                    while (!rdr.EndOfStream)
                    {
                        var data = rdr.ReadLine().Split(',');
                        for (var headerIndex = 0; headerIndex < header.Length; headerIndex++)
                        {
                            if (headerIndex < keyCols)
                            {
                                key[headerIndex].Add(int.Parse(data[headerIndex]));
                            }
                            else
                            {
                                values[header[headerIndex]].Add(data[headerIndex]);
                            }
                        }

                        currentRow++;
                    }
                }

                _data[filename] = (key, values);
            }

            return _data[filename];
        }

        public async Task<EntityMetricData[]> GetDataForFields(Subset subset,
            IReadOnlyCollection<ResponseFieldDescriptor> fields,
            (DateTime startDate, DateTime endDate)? timeRange, IReadOnlyCollection<IDataTarget> targetInstances,
            CancellationToken cancellationToken)
        {
            var instancesToLoad = targetInstances.Where(t => t.EntityType == TestEntityTypeRepository.Brand).SelectMany(t => t.SortedEntityInstanceIds).ToHashSet();

            if (!_ticksToProfileLookup.Any())
            {
                throw new Exception("Must load profiles first!");
            }

            var dataFile = _settings.BrandResponseDataFilepath(subset);

            var results = new List<EntityMetricData>();

            var values = GetCsvLookup(dataFile, 2);

            int rows = values.Keys[0].Count;

            for (var i = 0; i < rows; i++)
            {
                var entityInstanceId = values.Keys[1][i];
                if (!instancesToLoad.Contains(entityInstanceId))
                {
                    continue;
                }
                var profileId = values.Keys[0][i];
                if (_ticksToProfileLookup[profileId] >= timeRange.Value.startDate.Ticks && _ticksToProfileLookup[profileId] <= timeRange.Value.endDate.Ticks)
                {
                    var result = new EntityMetricData
                    {
                        EntityIds = new EntityValueCombination(new EntityValue(TestEntityTypeRepository.Brand, entityInstanceId)).EntityIds,
                        ResponseId = profileId,
                    };

                    foreach (var field in fields)
                    {
                        if (!int.TryParse(values.Values[field.Name][i], out var value))
                        {
                            continue;
                        }
                        result.Measures.Add((field, value));
                    }

                    results.Add(result);
                }
            }

            return results.ToArray();
        }

        public IDataLimiter DataLimiter { get; } = new NullDataLimiter();

        private readonly ConcurrentDictionary<int, long> _ticksToProfileLookup = new ConcurrentDictionary<int, long>();

        public IEnumerable<ResponseFieldData> GetResponses(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> quotaCellFields)
        {
            var profileFile = _settings.RespondentProfileDataFilepath(subset);

            var results = new List<ResponseFieldData>();

            var values = GetCsvLookup(profileFile, 1);

            for (var i = 0; i < values.Keys[0].Count; i++)
            {
                var fieldValues = new Dictionary<ResponseFieldDescriptor, int>();

                foreach (var field in quotaCellFields)
                {
                    if (!int.TryParse(values.Values[field.Name][i], out var value))
                    {
                        continue;
                    }
                    var fieldModel = field.GetDataAccessModel(subset.Id);
                    var scaledValue = fieldModel.ScaleFactor.HasValue
                        ? (int)Math.Round(value * fieldModel.ScaleFactor.Value)
                        : value;
                    fieldValues.Add(field, scaledValue);
                }

                var result = new ResponseFieldData(values.Keys[0][i], DateTimeOffset.Parse(values.Values[RespondentFields.StartTime][i]), -1, fieldValues);

                _ticksToProfileLookup[result.ResponseId] = result.Timestamp.Ticks;
                results.Add(result);
            }

            return results;
        }
    }
}

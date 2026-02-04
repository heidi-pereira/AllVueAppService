using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Utils;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class RespondentDataLoader : IRespondentDataLoader
    {
        private const int BatchSizeForEfficientDbAccess = 15;
        private readonly ILazyDataLoader _lazyDataLoader;
        private readonly SemaphoreSlim _dataLoadLockQueueSemaphore;
        private readonly int _maxCartesianProductSize;

        private readonly ConcurrentDictionary<(string subsetId, int fieldIndex, HashSet<EntityValue> entityValues), TimeRangesManager> _loadedData = new(new RespondentDataLoader.LoadedDataKeyComparer());
        private readonly IEntityRepository _entityRepository;
        private static readonly IEqualityComparer<HashSet<EntityValue>> SetComparer = HashSet<EntityValue>.CreateSetComparer();
        private readonly ResourceLockQueue _resourceLockQueue = new();

        public RespondentDataLoader(ILazyDataLoader lazyDataLoader, IEntityRepository entityRepository, IBrandVueDataLoaderSettings settings)
        {
            _lazyDataLoader = lazyDataLoader;
            _entityRepository = entityRepository;
            _dataLoadLockQueueSemaphore = new SemaphoreSlim(settings.MaxConcurrentDataLoaders, settings.MaxConcurrentDataLoaders);
            _maxCartesianProductSize = settings.AppSettings.MaxCartesianProductSize;
        }

        public async Task PossiblyLoadMeasures(IRespondentRepository respondentRepository, Subset subset,
            FieldsAndDataTargets targets, long startTicks, long endTicks,
            CancellationToken cancellationToken)
        {
            var groups = EntityCombinationFieldGroup.CreateGroups(subset, targets.Fields);
            foreach (var group in groups)
            {
                var groupTargetInstances = group.GetRelevantTargetInstances(targets.DataTargets);
                await PossiblyLoadGroupData(respondentRepository, @group, groupTargetInstances, startTicks, endTicks, cancellationToken);
            }
        }
        
        protected void PopulateResponsesFromData(IRespondentRepository respondentRepository,
            EntityMetricData[] entityMeasureData, Subset subset)
        {
            foreach (var entityMeasure in entityMeasureData)
            {
                if (!respondentRepository.TryGet(entityMeasure.ResponseId, out var cellResponse))
                {
                    continue;
                }

                foreach (var measureFieldValue in entityMeasure.Measures)
                {
                    cellResponse.ProfileResponseEntity.AddFieldValue(measureFieldValue.Field, entityMeasure.EntityIds, measureFieldValue.Value, subset);
                }
            }
        }


        private async Task PossiblyLoadGroupData(IRespondentRepository respondentRepository,
            EntityCombinationFieldGroup group,
            IReadOnlyCollection<IDataTarget> groupTargetInstances, long startTicks, long endTicks,
            CancellationToken cancellationToken)
        {
            var instanceCombinations = groupTargetInstances.GetEntityValueCombination(_maxCartesianProductSize).ToList();

            var (fieldsToLoad, entityCombinationsToLoad) = GetMeasuresAndEntitiesToLoad(group.Subset, group.Fields,
                instanceCombinations, startTicks, endTicks);

            if (!entityCombinationsToLoad.Any())
            {
                return;
            }

            var targetsWithAdditional = groupTargetInstances.Select(dt => GetWithExtraInstancesToLoad(group.Subset, dt)).ToArray();
            (fieldsToLoad, entityCombinationsToLoad) = GetMeasuresAndEntitiesToLoad(group.Subset, fieldsToLoad,
                targetsWithAdditional.GetEntityValueCombination(_maxCartesianProductSize), startTicks, endTicks);

            bool enteredLock = await _dataLoadLockQueueSemaphore.WaitAsync(TimeSpan.Zero, cancellationToken);

            if (!enteredLock)
            {
                // Stop IIS running out of threads waiting for SQL server
                // This shouldn't be necessary if data loading is faster or if we use async/await
                throw new TooBusyException("Too many requests to load data ");
            }

            try
            {
                // Lock at a sufficiently granular level to avoid unnecessary loading data multiple times whilst not blocking other threads from loading data that should be loaded concurrently
                using var disposableFieldsLockScope = await _resourceLockQueue.WaitForDisposableLocksAsync(group.Fields.Select(f => f.InMemoryIndex), cancellationToken);
                if (ShouldStillLoadData(group.Subset, group.Fields, entityCombinationsToLoad, startTicks, endTicks))
                {
                    var entityMeasureData = await _lazyDataLoader.GetDataForFields(group.Subset, fieldsToLoad,
                        (new DateTime(startTicks), new DateTime(endTicks)),
                        targetsWithAdditional.Select(dt =>
                            CreateDataTargetFromEntityCombinations(dt, entityCombinationsToLoad)).ToArray(), cancellationToken);

                    PopulateResponsesFromData_ThreadUnsafe(respondentRepository, group, entityMeasureData);
                    MarkDataAsLoaded(group.Subset.Id, fieldsToLoad, entityCombinationsToLoad, startTicks, endTicks);
                }
            }
            finally
            {
                _dataLoadLockQueueSemaphore.Release();
            }
        }

        /// <summary>
        /// Private - only marked internal for benchmarks to use
        /// </summary>
        internal void PopulateResponsesFromData_ThreadUnsafe(IRespondentRepository respondentRepository, EntityCombinationFieldGroup group,
            EntityMetricData[] entityMeasureData)
        {
            foreach (var f in group.Fields)
            {
                f.EnsureLoadOrderIndexInitialized_ThreadUnsafe();
            }

            PopulateResponsesFromData(respondentRepository, entityMeasureData, group.Subset);
        }

        private bool ShouldStillLoadData(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> fields, IEnumerable<HashSet<EntityValue>> valueCombinations, long startTicks, long endTicks)
        {
            foreach (var entityValueCombination in valueCombinations)
            {
                if (fields.Any(field => !IsDataLoaded(subset.Id, field.InMemoryIndex, entityValueCombination, startTicks, endTicks)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Because the engine doesn't support calculating 2D result sets, for audiences there's a string of single-entity requests which is very inefficient
        /// PERF: Pull in a few nearby entities at the same time.
        /// Note:  If someone requests in reverse order, the perf-benefit will be lost, but it added too much complexity and non-determinism to check the cache before
        /// We could have hedged our bets by picking some before and some after each id: Again, added complexity
        /// </summary>
        private IDataTarget GetWithExtraInstancesToLoad(Subset subset, IDataTarget instances)
        {
            var entityType = instances.EntityType;
            var requiredIds = instances.SortedEntityInstanceIds;

            // Loading extras would mean BrandVue will generally start using a bit more memory by pulling in brands and products no one ever looks at.
            // I think some views would load marginally slower from a cold start. I felt like it'd need more careful testing for those cases, so I left it out for now. Happy for someone else to enable it if you see good results.
            var adjacentEntitiesRarelyRequestedForType = entityType.IsProduct || entityType.IsBrand;
            if (requiredIds.Length > BatchSizeForEfficientDbAccess || adjacentEntitiesRarelyRequestedForType) return instances;

            var allInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset);
            long lastId = requiredIds.Last();
            int extraToTake = BatchSizeForEfficientDbAccess - requiredIds.Length;
            // Ensure none of the existing ids are returned:
            var extraIds = allInstances
                .Select(i => i.Id).OrderBy(i => i)
                .SkipWhile(i => i <= lastId).Take(extraToTake)
                .ToArray();
            var originalPlusExtraIds = requiredIds.Concat(extraIds);
            return new DataTarget(entityType, originalPlusExtraIds);
        }

        private IDataTarget CreateDataTargetFromEntityCombinations(IDataTarget instances, List<HashSet<EntityValue>> entityCombinations)
        {
            var filteredEntityInstanceIds = instances.SortedEntityInstanceIds.Where(ei=>entityCombinations.Any(x=>x.Any(y=>y.EntityType == instances.EntityType && y.Value == ei)));
            return new DataTarget(instances.EntityType, filteredEntityInstanceIds);
        }

        private (List<ResponseFieldDescriptor> Fields, List<HashSet<EntityValue>> EntityCombinations)
            GetMeasuresAndEntitiesToLoad(Subset subset, IReadOnlyCollection<ResponseFieldDescriptor> measureFields,
                IEnumerable<HashSet<EntityValue>> valueCombinations, long startTicks, long endTicks)
        {
            var measuresToLoad = new HashSet<ResponseFieldDescriptor>();
            var entityCombinationsToLoad = new HashSet<HashSet<EntityValue>>(SetComparer);
            foreach (var entityValueCombination in valueCombinations)
            {
                foreach (var measureField in measureFields)
                {
                    if (IsDataLoaded(subset.Id, measureField.InMemoryIndex, entityValueCombination, startTicks, endTicks))
                    {
                        continue;
                    }

                    entityCombinationsToLoad.Add(entityValueCombination);

                    if (!measuresToLoad.Contains(measureField) && measureField.GetDataAccessModelOrNull(subset.Id)?.ValueIsOpenText != true)
                    {
                        measuresToLoad.Add(measureField);
                    }
                }
            }

            return (measuresToLoad.ToList(), entityCombinationsToLoad.ToList());
        }


        private bool IsDataLoaded(string subsetId, int measureId, HashSet<EntityValue> entityValues, long startTicks, long endTicks)
        {
            return _loadedData.TryGetValue((subsetId, measureId, entityValues), out var rangesManager) && rangesManager.IsRangeEntirelyIncluded(startTicks, endTicks);
        }

        private void MarkDataAsLoaded(string subsetId, IReadOnlyCollection<ResponseFieldDescriptor> fields, IEnumerable<HashSet<EntityValue>> instanceCombinations, long startTicks, long endTicks)
        {
            foreach (var entityValueCombination in instanceCombinations)
            {
                foreach (var field in fields)
                {
                    var rangesManager = _loadedData.GetOrAdd((subsetId, field.InMemoryIndex, entityValueCombination), new TimeRangesManager(startTicks, endTicks));
                    rangesManager.AddRange(startTicks, endTicks);
                }
            }
        }
        private class LoadedDataKeyComparer : IEqualityComparer<(string subsetId, int measureId, HashSet<EntityValue> entityValues)>
        {
            public bool Equals((string subsetId, int measureId, HashSet<EntityValue> entityValues) x, (string subsetId, int measureId, HashSet<EntityValue> entityValues) y)
            {
                return x.subsetId.Equals(y.subsetId) &&
                       x.measureId.Equals(y.measureId) &&
                       x.entityValues.SetEquals(y.entityValues);
            }

            public int GetHashCode((string subsetId, int measureId, HashSet<EntityValue> entityValues) obj)
            {
                return (obj.subsetId, obj.measureId, RespondentDataLoader.SetComparer.GetHashCode(obj.entityValues)).GetHashCode();
            }
        }
    }
}
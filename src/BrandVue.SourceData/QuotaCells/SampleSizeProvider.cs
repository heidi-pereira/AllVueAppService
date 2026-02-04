using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrandVue.SourceData.QuotaCells
{
    public class SampleSizeProvider : ISampleSizeProvider
    {
        private readonly IAverageDescriptorRepository _averageDescriptorRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IFieldExpressionParser _fieldExpressionParser;
        private readonly IDataPresenceGuarantor _dataPresenceGuarantor;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;
        private readonly IProductContext _productContext;

        public SampleSizeProvider(IAverageDescriptorRepository averageDescriptorRepository,
            IEntityRepository entityRepository, IRespondentRepositorySource respondentRepositorySource,
            IFieldExpressionParser fieldExpressionParser, IDataPresenceGuarantor dataPresenceGuarantor,
            IBaseExpressionGenerator baseExpressionGenerator,
            IProductContext productContext)
        {
            _averageDescriptorRepository = averageDescriptorRepository;
            _entityRepository = entityRepository;
            _respondentRepositorySource = respondentRepositorySource;
            _fieldExpressionParser = fieldExpressionParser;
            _dataPresenceGuarantor = dataPresenceGuarantor;
            _baseExpressionGenerator = baseExpressionGenerator;
            _productContext = productContext;
        }

        public Task<double> GetTotalSampleSize(Subset subset, IFilter filter, WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken)
        {
            return GetSampleSize(subset, filter, weightingMetrics, cancellationToken);
        }

        private IDictionary<EntityInstance, IGroupedQuotaCells> GroupedQuotaCellsFilteredForMeasure(Subset subset, IFilter filter, Measure metric, IRespondentRepository respondentRepository)
        {
            var filterToPartialKey = FilterToPartialQuotaCellKeyForMeasure(filter);
            if (filterToPartialKey != null)
            {
                var entityType = metric.EntityCombination.Single();
                var allCategoryInstances = _entityRepository.GetInstancesOf(entityType.Identifier, subset).OrderBy(i => i.Id);
                var result = new Dictionary<EntityInstance, IGroupedQuotaCells>();

                foreach (var categoryInstance in allCategoryInstances)
                {
                    var myList = new List<(string, string)>(filterToPartialKey);
                    myList.Add((metric.Name, categoryInstance.Id.ToString()));

                    var quotaCells = respondentRepository.WeightedCellsGroup.Where(x =>
                    {
                        var match = true;
                        for (int index = 0; match && index < myList.Count; index++)
                        {
                            match = (x.FieldGroupToKeyPart.ContainsKey(myList[index].Item1)) &&
                            x.FieldGroupToKeyPart[myList[index].Item1] == myList[index].Item2;
                        }
                        return match;
                    });
                    result.Add(categoryInstance, quotaCells.Any() ? quotaCells: null);
                }
                return result;
            }
            return null;
        }
        private IList<(string, string)> FilterToPartialQuotaCellKeyForMeasure(IFilter filter)
        {
            if (filter is AlwaysIncludeFilter)
            {
                return (new List<(string, string)>());
            }
            else
            {
                return FilterToPartialQuotaCellKey(filter);
            }
        }

        public IReadOnlyDictionary<EntityInstance, double> GetSampleSizeByEntityUsingCurrentWeighting(Subset subset, Measure metric, IFilter filter, WeightingMetrics weightingMetrics)
        {
            var inMemoryRespondents = _respondentRepositorySource.GetForSubset(subset);
            var groupedQuotaCells = GroupedQuotaCellsFilteredForMeasure(subset, filter, metric, inMemoryRespondents);

            if (groupedQuotaCells == null || (!inMemoryRespondents.WeightedCellsGroup.Any()))
            {
                return new Dictionary<EntityInstance, double>();
            }
            else
            {
                var profileResponseAccessor = new ProfileResponseAccessor(inMemoryRespondents, subset);
                return groupedQuotaCells.ToDictionary(r => r.Key, r => r.Value == null ? 0.0
                : profileResponseAccessor.GetResponses(r.Value).Sum(x =>
                    {
                        return x.Profiles.Length;
                    }
                 ));
            }
        }

        public async Task<ResultSampleSizePair> GetUnweightedProfileResultAndSample(Subset subset, Measure measure,
            IFilter filter, WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            return (await GetUnweightedResults(subset, measure, weightingMetrics, filter, _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriodNotWeighted), cancellationToken))
                .UnweightedResults.Single().CellsTotalsSeries.Single().SingleTotalForAverage();
        }

        public async Task<IEnumerable<(EntityInstance Instance, ResultSampleSizePair Result)>>
            GetUnweightedEntityResultAndSample(Subset subset,
            Measure measure,
            IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken)
        {
            var results = await GetUnweightedResults(subset,
                measure,
                weightingMetrics,
                filter,
                _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriodNotWeighted), cancellationToken);
            return results.UnweightedResults.Select(r =>
            {
                return (r.EntityInstance, r.CellsTotalsSeries.Single().SingleTotalForAverage());
            });
        }

        public async Task<IEnumerable<(EntityInstance Instance, ResultSampleSizePair Result)>>
            GetUnweightedEntityResultAndSampleMultiEntityEstimate(Subset subset,
            Measure measure, IFilter filter,
            WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken)
        {
            var results = await GetUnweightedResultsMultiEntityEstimate(subset,
                measure,
                weightingMetrics,
                filter,
                _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriodNotWeighted),
                cancellationToken);
            return results.UnweightedResults.Select(r =>
            {
                return (r.EntityInstance, r.CellsTotalsSeries.Single().SingleTotalForAverage());
            });
        }

        public async Task<IReadOnlyDictionary<EntityInstance, double>> GetSampleSizeByEntity(Subset subset,
            Measure metric, IFilter filter, WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            //Multiple instance, single period, single quota cell
            return (await GetUnweightedResults(subset, metric, weightingMetrics, filter, _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriodNotWeighted), cancellationToken))
                .UnweightedResults
                .ToDictionary(r => r.EntityInstance, r => r.CellsTotalsSeries.Single().SingleAverageResult());
        }

        public Task<IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)>> GetSampleSizeByQuotaCell(Subset subset,
            WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            return GetSampleSizeByQuotaCell(subset, new AlwaysIncludeFilter(), weightingMetrics, cancellationToken);
        }

        public async Task<IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)>> GetSampleSizeByQuotaCell(
            Subset subset, IFilter filter, WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            var populationMeasure = _fieldExpressionParser.CreatePopulationMeasure();
            var results = await GetUnweightedResults(subset, populationMeasure, weightingMetrics, filter, _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriod), cancellationToken);

            //Single instance, single period
            var sampleCountsForSinglePeriod = results.UnweightedResults.Single().CellsTotalsSeries.Single();
            return results.WeightedCells.Select(quotaCell => (quotaCell, sampleCountsForSinglePeriod[quotaCell]?.TotalForAverage.Result ?? 0.0)).ToList();
        }

        private IGroupedQuotaCells GroupedQuotaCellsFiltered(IFilter filter, IRespondentRepository respondentRepository)
        {
            var filterToPartialKey = FilterToPartialQuotaCellKey(filter);
            if (filterToPartialKey != null && filterToPartialKey.Any())
            {
                var quotaCells = respondentRepository.WeightedCellsGroup.Where(x =>
                {
                    var match = true;
                    for (int index = 0; match && index < filterToPartialKey.Count; index++)
                    {
                        match = (x.FieldGroupToKeyPart.ContainsKey(filterToPartialKey[index].Item1)) &&
                        x.FieldGroupToKeyPart[filterToPartialKey[index].Item1] == filterToPartialKey[index].Item2;
                    }
                    return match;
                });
                return quotaCells.Any()?quotaCells:null;
            }
            return null;
        }
        private IList<(string, string)> FilterToPartialQuotaCellKey(IFilter filter)
        {
            var partialQuotaCellKey = new List<(string, string)>();
            if ( (filter is MetricFilter measureFilter) && (measureFilter.OnlyValueOrDefault.HasValue) )
            {
                partialQuotaCellKey.Add((measureFilter.Metric.Name, measureFilter.OnlyValueOrDefault.ToString()));
            }
            else if (filter is AndFilter andFilter)
            {
                foreach(var filt in andFilter.Filters)
                {
                    if ( (filt is MetricFilter measure) && (measure.OnlyValueOrDefault.HasValue) )
                    {
                        partialQuotaCellKey.Add((measure.Metric.Name, measure.OnlyValueOrDefault.ToString()));
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                return null;
            }
            return partialQuotaCellKey;
        }

        private IGroupedQuotaCells GetGroupedQuotaCells(string entityId, IRespondentRepository respondentRepository)
        {
            var quotaCells =
                respondentRepository.WeightedCellsGroup.Where(x =>
                    x.FieldGroupToKeyPart.Values.First() == entityId);
            return quotaCells.Any() ? quotaCells : null;
        }

        public async Task<IList<SampleSize>> GetSampleSizeByWeightingForTopLevel(Subset subset,
            WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            var inMemoryRespondents = _respondentRepositorySource.GetForSubset(subset);
            var profileResponseAccessor = new ProfileResponseAccessor(inMemoryRespondents, subset);

            var entityIds = inMemoryRespondents.WeightedCellsGroup.Cells
                .GroupBy(x => x.FieldGroupToKeyPart.Values.First()).Select(x => x.Key);

            return entityIds.Select(x =>
                    {
                        var entityId = int.Parse(x);
                        var respondents = profileResponseAccessor.GetResponses(GetGroupedQuotaCells(x, inMemoryRespondents)) ;
                        var total = respondents.Sum(x => x.Profiles.Length);
                        return new SampleSize(entityId, total);
                    }
                ).ToList();
        }

        private async Task<double> GetSampleSize(Subset subset, IFilter filter, WeightingMetrics weightingMetrics,
            CancellationToken cancellationToken)
        {
            var inMemoryRespondents = _respondentRepositorySource.GetForSubset(subset);
            if (filter is AlwaysIncludeFilter)
            {
                return inMemoryRespondents.Count;
            }
            if (inMemoryRespondents.WeightedCellsGroup.Any())
            {
                var groupedQuotaCells = GroupedQuotaCellsFiltered(filter, inMemoryRespondents);

                if (groupedQuotaCells != null)
                {
                    var profileResponseAccessor = new ProfileResponseAccessor(inMemoryRespondents, subset);

                    return profileResponseAccessor.GetResponses(groupedQuotaCells).Sum(x =>
                    {
                        return x.Profiles.Length;
                    });
                }
            }

            var populationMeasure = _fieldExpressionParser.CreatePopulationMeasure();
            var unweightedTotalisationPeriodResultsByQuotaCell = (await GetUnweightedResults(subset, populationMeasure, weightingMetrics, filter, _averageDescriptorRepository.GetCustom(AverageIds.CustomPeriodNotWeighted), cancellationToken))
                            .UnweightedResults
                            .Single().CellsTotalsSeries.Single();
            return unweightedTotalisationPeriodResultsByQuotaCell.SingleAverageResult();
        }

        public async Task<IEnumerable<int>> GetRespondents(Subset subset, IFilter filter,
            WeightingMetrics weightingMetrics, CancellationToken cancellationToken)
        {
            var inMemoryRespondents = _respondentRepositorySource.GetForSubset(subset);
            if (filter is AlwaysIncludeFilter)
            {
                return inMemoryRespondents.Select( x=> x.ProfileResponseEntity.Id);
            }

            if (inMemoryRespondents.WeightedCellsGroup.Any())
            {
                var groupedQuotaCells = GroupedQuotaCellsFiltered(filter, inMemoryRespondents);
                if (groupedQuotaCells != null)
                {
                    var profileResponseAccessor = new ProfileResponseAccessor(inMemoryRespondents, subset);

                    var result = profileResponseAccessor.GetResponses(groupedQuotaCells).SelectMany(populatedQuotaCell=> populatedQuotaCell.Profiles.ToArray().Select(profile=> profile.Id));
                    return result;
                }
            }

            var subsetWeightingMeasures = new Dictionary<Subset, WeightingMetrics> { { subset, weightingMetrics } };

            var measureBasedRespondentRepositoryFactory = new MetricBasedRespondentRepositoryFactory(_averageDescriptorRepository,
                _dataPresenceGuarantor, subsetWeightingMeasures, NullLogger<MetricBasedRespondentRepositoryFactory>.Instance, _productContext);

            var weightableRespondents = await measureBasedRespondentRepositoryFactory.WithWeightsFrom(inMemoryRespondents, weightingMetrics, cancellationToken);

            return weightableRespondents.Where(x => !x.QuotaCell.IsUnweightedCell)
                .Select(x => x.ProfileResponseEntity.Id);
        }

        private Task<(IEnumerable<QuotaCell> WeightedCells, EntityTotalsSeries[] UnweightedResults)>
            GetUnweightedResults(Subset subset, Measure requestMeasure,
                WeightingMetrics weightingMetrics, IFilter filter, AverageDescriptor averageDescriptor,
                CancellationToken cancellationToken)
        {
            var requestedInstances = _entityRepository.CreateTargetInstances(subset, requestMeasure);
            var filterInstances = Array.Empty<TargetInstances>();
            return DoGetUnweightedResults(subset, requestMeasure, weightingMetrics, filter, averageDescriptor, requestedInstances, filterInstances, cancellationToken);
        }

        private async Task<(IEnumerable<QuotaCell> WeightedCells, EntityTotalsSeries[] UnweightedResults)>
            GetUnweightedResultsMultiEntityEstimate(Subset subset, Measure requestMeasure,
                WeightingMetrics weightingMetrics, IFilter filter, AverageDescriptor averageDescriptor,
                CancellationToken cancellationToken)
        {
            //This picks the first entity instance of all unspecified types to get an estimate of the results rather than a result per multi-entity instance
            var primaryType = requestMeasure.EntityCombination.First();
            var filterTypes = requestMeasure.EntityCombination.Skip(1);
            var requestedInstances = new TargetInstances(primaryType, _entityRepository.GetInstancesOf(primaryType.Identifier, subset));
            var filterInstances = filterTypes.Select(type => new TargetInstances(type, _entityRepository.GetInstancesOf(type.Identifier, subset).OrderBy(i => i.Id).Take(1))).ToArray();
            return await DoGetUnweightedResults(subset, requestMeasure, weightingMetrics, filter, averageDescriptor, requestedInstances, filterInstances, cancellationToken);
        }

        private async Task<(IEnumerable<QuotaCell> WeightedCells, EntityTotalsSeries[] UnweightedResults)>
            DoGetUnweightedResults(Subset subset, Measure requestMeasure,
                WeightingMetrics weightingMetrics, IFilter filter, AverageDescriptor averageDescriptor,
                TargetInstances requestedInstances, TargetInstances[] filterInstances,
                CancellationToken cancellationToken)
        {
            if (requestMeasure.IsVariableWithoutBaseExpression())
            {
                requestMeasure = _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(requestMeasure,
                    new BaseExpressionDefinition
                    {
                        BaseType = BaseDefinitionType.SawThisQuestion,
                        BaseMeasureName = requestMeasure.Name,
                        BaseVariableId = null
                    });
            }

            var inMemoryRespondents = _respondentRepositorySource.GetForSubset(subset);

            var subsetWeightingMeasures = new Dictionary<Subset, WeightingMetrics> { { subset, weightingMetrics } };
            var measureBasedRespondentRepositoryFactory = new MetricBasedRespondentRepositoryFactory(_averageDescriptorRepository,
                _dataPresenceGuarantor, subsetWeightingMeasures, NullLogger<MetricBasedRespondentRepositoryFactory>.Instance, _productContext);

            var weightableRespondents = await measureBasedRespondentRepositoryFactory.WithWeightsFrom(inMemoryRespondents, weightingMetrics, cancellationToken);
            var profileResponseAccessor = new ProfileResponseAccessor(weightableRespondents, subset);

            var unweightedTotaliser = TotaliserFactory.Create(averageDescriptor);
            var weightedCells = weightableRespondents.GetGroupedQuotaCells(averageDescriptor);

            var requestStartDate = weightableRespondents.EarliestResponseDate;
            var requestEndDate = weightableRespondents.LatestResponseDate;
            var calculationPeriod = new CalculationPeriod(requestStartDate, requestEndDate);
            await _dataPresenceGuarantor.EnsureDataIsLoaded(inMemoryRespondents, subset, requestMeasure, calculationPeriod, averageDescriptor, filter, filterInstances.Prepend(requestedInstances).ToArray(), Array.Empty<Break>(), cancellationToken);
            var filteredMeasure = FilteredMetric.Create(requestMeasure, filterInstances, subset, filter);
            var unweightedResults = unweightedTotaliser.TotalisePerCell(profileResponseAccessor, filteredMeasure, calculationPeriod, averageDescriptor, requestedInstances, weightedCells, null, weightableRespondents.AllCellsGroup, requestStartDate, requestEndDate, cancellationToken);
            return (weightableRespondents.WeightedCellsGroup.Cells, unweightedResults);
        }
    }

}
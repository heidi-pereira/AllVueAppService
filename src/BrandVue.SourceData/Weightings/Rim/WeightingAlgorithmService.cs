using BrandVue.SourceData.QuotaCells;
using System.Globalization;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.SourceData.Import;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.SourceData.Weightings.Rim
{
    public class WeightingAlgorithmService
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly IRimWeightingCalculator _rimWeightingCalculator;
        private readonly ISampleSizeProvider _sampleSizeProvider;
        private readonly IRespondentRepositorySource _respondentRepositorySource;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;
        private readonly IEntityRepository _entityRepository;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;

        public WeightingAlgorithmService(ISubsetRepository subsetRepository, IEntityRepository entityRepository,
            IMeasureRepository measureRepository,
            IRimWeightingCalculator rimWeightingCalculator, ISampleSizeProvider sampleSizeProvider,
            IRespondentRepositorySource respondentRepositorySource,
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IBaseExpressionGenerator baseExpressionGenerator)
        {
            _subsetRepository = subsetRepository;
            _measureRepository = measureRepository;
            _rimWeightingCalculator = rimWeightingCalculator;
            _sampleSizeProvider = sampleSizeProvider;
            _respondentRepositorySource = respondentRepositorySource;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _entityRepository = entityRepository;
            _baseExpressionGenerator = baseExpressionGenerator;
        }

        public async Task<double> GetRimTotalSampleSize(string subsetId, string filterMetricName, int? filterInstanceId,
            CancellationToken cancellationToken)
        {
            if (!TryGetSubset(subsetId, out var subset))
            {
                return 0.0f;
            }
            var filter = CreateFilterForInstance(subset, filterMetricName, filterInstanceId);
            return await _sampleSizeProvider.GetTotalSampleSize(subset, filter, new WeightingMetrics(_measureRepository, _entityRepository, subset, null), cancellationToken);
        }

        public async Task<IList<SampleSize>> GetSampleSizeByWeightingForTopLevel(string subsetId, CancellationToken cancellationToken)
        {
            if (!TryGetSubset(subsetId, out var subset))
            {
                return null;
            }
            
            return await _sampleSizeProvider.GetSampleSizeByWeightingForTopLevel(subset, new WeightingMetrics(_measureRepository, _entityRepository, subset, null), cancellationToken);
        }

        public async Task<double> GetRimTotalSampleSize(string subsetId, List<WeightingFilterInstance> filterInstances,
            CancellationToken cancellationToken)
        {
            if (!TryGetSubset(subsetId, out var subset))
            {
                return 0.0f;
            }
            var filter = CreateFilterForInstance(subset, filterInstances);
            return await _sampleSizeProvider.GetTotalSampleSize(subset, filter, new WeightingMetrics(_measureRepository, _entityRepository, subset, null), cancellationToken);
        }

        public async Task<IReadOnlyDictionary<EntityInstance, double>> GetRimDimensionSampleSizes(string selectedSubset,
            string metricName, List<WeightingFilterInstance> filterInstances, CancellationToken cancellationToken)
        {
            if (!TryGetSubset(selectedSubset, out var subset))
            {
                return new Dictionary<EntityInstance, double>();
            }

            var measure = _measureRepository.Get(metricName);
            var filter = CreateFilterForInstance(subset, filterInstances);
            return await _sampleSizeProvider
                .GetSampleSizeByEntity(subset, measure, filter, new WeightingMetrics(_measureRepository, _entityRepository, subset, null), cancellationToken);
        }

        public async
            Task<IEnumerable<(string metricName, int instanceId, string name, double? rawSampleSize, double?
                sampleSizeByWeighting)>> GetReport(string selectedSubset, string metricName,
                CancellationToken cancellationToken)
        {
            IEnumerable<(string metricName, int instanceId, string name, double? rawSampleSize, double? sampleSizeByWeighting)> GetError(string name, string message)
            {
                return new List<(string metricName, int instanceId, string name, double? rawSampleSize, double? sampleSizeByWeighting)> 
                { (name, -1, message, null, null) };
            }

            double ? PossibleValue(EntityInstance key, IReadOnlyDictionary<EntityInstance, double> lookup)
            {
                var found = lookup.SingleOrDefault(x => x.Key.Id == key.Id);
                if (found.Key != default(EntityInstance))
                {
                    return found.Value;
                }
                return (double?)null;
            }

            if (!TryGetSubset(selectedSubset, out var subset))
            {
                return GetError(metricName, $"{selectedSubset} does not exist");
            }
            if (!_measureRepository.TryGet(metricName, out var measure))
            {
                return GetError(metricName, $"Metric {metricName} does not exist");
            }

            IFilter filter = new AlwaysIncludeFilter();
            var defaultWeightingMeseaure = new WeightingMetrics(_measureRepository, _entityRepository, subset, null);
            var raw = await _sampleSizeProvider.GetSampleSizeByEntity(subset, measure, filter, defaultWeightingMeseaure, cancellationToken);
            var weighting = _sampleSizeProvider.GetSampleSizeByEntityUsingCurrentWeighting(subset, measure, filter, defaultWeightingMeseaure);

            var keys = weighting.Select(x => x.Key).Union(raw.Select(x => x.Key)).Distinct();
            return keys.Select(key => (metricName, key.Id, key.Name, PossibleValue(key, raw), PossibleValue(key, weighting)));

        }

        public async Task<IReadOnlyDictionary<EntityInstance, double>> GetRimDimensionSampleSizes(string selectedSubset,
            string metricName, string filterMetricName,
            int? filterInstanceId, CancellationToken cancellationToken)
        {
            if (!TryGetSubset(selectedSubset, out var subset))
            {
                return new Dictionary<EntityInstance, double>();
            }
            var measure = _measureRepository.Get(metricName);

            var filter = CreateFilterForInstance(subset, filterMetricName, filterInstanceId);
            return await _sampleSizeProvider
                .GetSampleSizeByEntity(subset, measure, filter, new WeightingMetrics(_measureRepository, _entityRepository, subset, null), cancellationToken);
        }

        public async Task<RimWeightingCalculationResult> ValidateRimWeightingScheme(string selectedSubset,
            IReadOnlyCollection<WeightingPlan> plans, List<WeightingFilterInstance> weightingFilterInstances,
            CancellationToken cancellationToken)
        {
            if (!TryGetSubset(selectedSubset, out var subset))
            {
                return null;
            }
            return await GetRimWeightingCalculationResult(subset, plans, weightingFilterInstances, false, cancellationToken);
        }


        private IFilter CreateFilterForInstance(Subset subset, IList<WeightingFilterInstance> instances)
        {
            IFilter filter = new AlwaysIncludeFilter();

            var filters = instances.Where(instance => instance.FilterMetricName is not null && instance.FilterInstanceId is not null).Select(instance =>
            {
                return CreateFilter(subset, instance.FilterMetricName, instance.FilterInstanceId.Value);
            }).ToList();
            if (filters.Any())
            {
                if (filters.Count() == 1)
                {
                    return filters.First();
                }
                return new AndFilter(filters);
            }
            return filter;
        }

        private IFilter CreateFilterForInstance(Subset subset, string filterMetricName, int? filterInstanceId)
        {
            IFilter filter = new AlwaysIncludeFilter();
            if (filterMetricName is not null && filterInstanceId is not null)
            {
                filter = CreateFilter(subset, filterMetricName, filterInstanceId.Value);
            }
            return filter;
        }

        private IFilter CreateFilter(Subset subset, string filterMetricName, int filterInstanceId)
        {
            var requestMeasure = _measureRepository.Get(filterMetricName);

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
            var entityValues =
                new EntityValue(requestMeasure.EntityCombination.Single(), filterInstanceId);
            var entityValueCombination = new EntityValueCombination(entityValues);
            return new MetricFilter(subset, requestMeasure, entityValueCombination, new[] { filterInstanceId });
        }

        private async Task<RimWeightingCalculationResult> GetRimWeightingCalculationResult(Subset subset,
            IReadOnlyCollection<WeightingPlan> partialPlans, List<WeightingFilterInstance> weightingFilterInstances,
            bool includeDetails, CancellationToken cancellationToken)
        {
            var validWeightingPlans = ValidRimPlansOrThrow(partialPlans);
            var filter = CreateFilterForInstance(subset, weightingFilterInstances);

            var quotaCellMeasures = WeightingMetrics.CreateForPartialPlans(_measureRepository, _entityRepository, subset, weightingFilterInstances, partialPlans);
            var quotaCellToSampleSize = await _sampleSizeProvider.GetSampleSizeByQuotaCell(subset, filter, quotaCellMeasures, cancellationToken);
            var cellToSampleSize = quotaCellToSampleSize.Select(q => (QuotaCell: q.QuotaCell, SampleSize: q.SampleSize)).ToList();
            var totalSampleSizeForQuotaCells = cellToSampleSize.Sum(q => q.SampleSize);

            var quotaSampleSizesForNullTargetsByVariable = validWeightingPlans.Select( plan => (plan.FilterMetricName, SampleSize: plan.Targets
                .Where(target => !target.Target.HasValue)
                .Select(target => quotaCellToSampleSize
                    .Where(qs => int.Parse(qs.QuotaCell.GetKeyPartForFieldGroup(plan.FilterMetricName)) == target.FilterMetricEntityId)
                    .Sum(q=>q.SampleSize))
                    .Sum())
                ).ToDictionary(x=> x.FilterMetricName, x=>x.SampleSize);

            var rimDimensions = validWeightingPlans.ToDictionary(
                plan => plan.FilterMetricName,
                plan => plan.Targets.ToDictionary(
                    target => target.FilterMetricEntityId,
                    target => target.Target.HasValue
                        ? decimal.ToDouble(target.Target.Value) * (totalSampleSizeForQuotaCells -
                            (quotaSampleSizesForNullTargetsByVariable.ContainsKey(plan.FilterMetricName)
                                ? quotaSampleSizesForNullTargetsByVariable[plan.FilterMetricName]
                                : 0.0))
                        : cellToSampleSize
                            .Where(qs => int.Parse(qs.QuotaCell.GetKeyPartForFieldGroup(plan.FilterMetricName)) == target.FilterMetricEntityId)
                            .Sum(q => q.SampleSize)
                )
            );

            return _rimWeightingCalculator.Calculate(cellToSampleSize, rimDimensions, includeDetails);
        }

        private static IReadOnlyCollection<WeightingPlan> ValidRimPlansOrThrow(IReadOnlyCollection<WeightingPlan> plans)
        {
            if (!plans.AreAllPlansRimWeighted())
            {
                throw new NotImplementedException("Only Rim weighting schemes are supported in this method");
            }
            return plans;
        }

        private bool TryGetSubset(string subsetId, out Subset subset)
        {
            subset = null;
            if (_subsetRepository.TryGet(subsetId, out subset) && !subset.Disabled)
            {
                return true;
            }
            return false;
        }

        public (IEnumerable<string> headers, List<Dictionary<string, string>> rows) ResponsesWithCell()
        {
            var subsets = _subsetRepository.Where(s => s.Id != BrandVueDataLoader.All);
            var headers = new List<string> { "SubsetId", "ResponseId", "Date" };
            var responsesAndCells = subsets.SelectMany(subset =>
            {
                var profileResponseAccessor = _profileResponseAccessorFactory.GetOrCreate(subset);
                var quotaCells = _respondentRepositorySource.GetForSubset(subset).WeightedCellsGroup;
                var responses = profileResponseAccessor.GetResponses(quotaCells);
                var responsesAndCellsForSubset = responses
                    .SelectMany(q => q.Profiles.ToArray().Select(p => (Response: p, q.QuotaCell)));
                var subsetResponseAndCell = responsesAndCellsForSubset.Select(r => (Subset: subset, r.Response, r.QuotaCell));
                if (quotaCells.Any()) headers.AddRange(quotaCells.Cells.First().FieldGroupToKeyPart.Keys);
                return subsetResponseAndCell;
            }).ToArray();
            headers = headers.Distinct().ToList();
            var rows = responsesAndCells.Select(rc =>
            {
                var dict = new Dictionary<string, string>
                {
                    {"SubsetId", rc.Subset.Id},
                    {"ResponseId", rc.Response.Id.ToString()},
                    {"Date", rc.Response.Timestamp.ToString()}
                };
                foreach ((string dimension, string value) in rc.QuotaCell.FieldGroupToKeyPart)
                {
                    dict.Add(dimension, value);
                }

                return dict;
            }).ToList();
            return (headers, rows);
        }
    }
}
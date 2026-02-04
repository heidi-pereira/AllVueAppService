using System.Threading;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.PublicApi.Models;
using BrandVue.SourceData;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;

namespace BrandVue.PublicApi.Services
{
    public class MetricResultCalculationProxy : IMetricResultCalculationProxy
    {
        private readonly DemographicFilterToQuotaCellMapper _demographicFilterToQuotaCellMapper;
        private readonly IFilterRepository _filter;
        private readonly IMetricCalculationOrchestrator _calculator;

        public MetricResultCalculationProxy(IFilterRepository filter, IRespondentRepositorySource respondentRepositorySource, IMetricCalculationOrchestrator calculator)
        {
            _filter = filter;
            _calculator = calculator;
            _demographicFilterToQuotaCellMapper = new DemographicFilterToQuotaCellMapper(respondentRepositorySource);
        }

        public async Task<IEnumerable<MetricCalculationResult>> Calculate(MetricCalculationRequestInternal request,
            CancellationToken cancellationToken)
        {
            if (!request.IsValid) throw new ArgumentException("Request passed to proxy contains validation errors.");

            var quotaCells = _demographicFilterToQuotaCellMapper.MapQuotaCellsFor(request.Surveyset, new DemographicFilter(_filter), request.Average);

            IEnumerable<MetricCalculationResult> metricCalculationResults;
            if (request.FilterEntityInstancesCollection.Any())
            {
                var entityInstanceCombinations = request.FilterEntityInstancesCollection
                    .Select(ti => ti.OrderedInstances.AsEnumerable().Select(entityInstance => (ti.EntityType, entityInstance)))
                    .CartesianProduct();

                metricCalculationResults = await entityInstanceCombinations
                    .Select(combination =>
                        combination.Select(e => new TargetInstances(e.EntityType, new[] { e.entityInstance })).ToArray())
                    .ToAsyncEnumerable().SelectAwait(async filterTargetInstances =>
                    {
                        var calculationPeriod = new CalculationPeriod(request.StartDate, request.EndDate);
                        Subset subset = request.Surveyset;
                        Measure measure = request.MetricDescriptor;
                        IFilter filter = new AlwaysIncludeFilter();
                        var entityWeightedDailyResults =
                            await _calculator.Calculate(
                                FilteredMetric.Create(measure, filterTargetInstances, subset, filter),
                                calculationPeriod, request.Average, request.PrimaryEntityInstances, quotaCells, false, cancellationToken);
                        return new MetricCalculationResult(request.PrimaryEntityInstances, filterTargetInstances, entityWeightedDailyResults);
                    }).ToArrayAsync(cancellationToken);
            }
            else
            {
                var calculationPeriod = new CalculationPeriod(request.StartDate, request.EndDate);
                Subset subset = request.Surveyset;
                Measure measure = request.MetricDescriptor;
                IFilter filter = new AlwaysIncludeFilter();
                TargetInstances[] filterInstances = request.FilterEntityInstancesCollection;
                var entityWeightedDailyResults =
                    await _calculator.Calculate(FilteredMetric.Create(measure, filterInstances, subset, filter),
                        calculationPeriod, request.Average, request.PrimaryEntityInstances, quotaCells, false, cancellationToken);
                metricCalculationResults = new[]
                {
                    new MetricCalculationResult(request.PrimaryEntityInstances, request.FilterEntityInstancesCollection, entityWeightedDailyResults)
                };
            }

            return metricCalculationResults;
        }
    }
}
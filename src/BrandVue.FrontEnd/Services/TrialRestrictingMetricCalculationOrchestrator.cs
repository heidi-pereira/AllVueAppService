using System.Threading;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.Filters;
using BrandVue.SourceData;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using Microsoft.AspNetCore.Http;

namespace BrandVue.Services
{
    public class TrialRestrictingMetricCalculationOrchestrator : IMetricCalculationOrchestrator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserContext _userContext;
        private readonly IMetricCalculationOrchestrator _metricCalculationOrchestrator;
        private readonly IProfileResponseAccessorFactory _profileResponseAccessorFactory;

        public TrialRestrictingMetricCalculationOrchestrator(
            IHttpContextAccessor httpContextAccessor,
            IMetricCalculationOrchestrator metricCalculationOrchestrator,
            IProfileResponseAccessorFactory profileResponseAccessorFactory,
            IUserContext userContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _metricCalculationOrchestrator = metricCalculationOrchestrator;
            _profileResponseAccessorFactory = profileResponseAccessorFactory;
            _userContext = userContext;
        }

        private void ClearRestrictedData(IEnumerable<EntityWeightedDailyResults> entityWeightedDailyResults, Subset subset)
        {
            var weightedDailyResults = entityWeightedDailyResults.SelectMany(x => x.WeightedDailyResults);
            ClearRestrictedData(weightedDailyResults, subset);
        }

        private void ClearRestrictedData(IEnumerable<WeightedDailyResult> weightedDailyResults, Subset subset)
        {
            if (_userContext.IsTrialUser)
            {
                var maxDateAllowedForTrial = _userContext.GetTrialDataRestrictedDate(_profileResponseAccessorFactory.GetOrCreate(subset).EndDate);

                var resultsToClear = weightedDailyResults.Where(weightedDailyResult => weightedDailyResult.Date > maxDateAllowedForTrial).ToArray();
                foreach (var weightedDailyResult in resultsToClear)
                {
                    weightedDailyResult.WeightedResult = 0.0;
                }

                var httpContext = _httpContextAccessor.HttpContext ;

                if (resultsToClear.Any())
                {
                    httpContext.Items[TrialDateRestrictionWarner.ItemKey] = true;
                }
            }
        }

        public async Task<EntityWeightedDailyResults[]> Calculate(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average, TargetInstances requestedInstances, IGroupedQuotaCells quotaCells,
            bool calculateSignificance, CancellationToken cancellationToken)
        {
            var entityWeightedDailyResults = await _metricCalculationOrchestrator.Calculate(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, calculateSignificance, cancellationToken);
            ClearRestrictedData(entityWeightedDailyResults, filteredMetric.Subset);
            return entityWeightedDailyResults;
        }

        public async Task<WeightedDailyResult> CalculateAverageMentions(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            CancellationToken cancellationToken)
        {
            var averageMentionsResult = await _metricCalculationOrchestrator.CalculateAverageMentions(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, cancellationToken);
            ClearRestrictedData(new[] { averageMentionsResult }, filteredMetric.Subset);
            return averageMentionsResult;
        }

        public async Task<WeightedDailyResult[]> CalculateNumericResponseAverage(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            AverageType averageType,
            ResponseFieldDescriptor field,
            CancellationToken cancellationToken)
        {
            var results = await _metricCalculationOrchestrator.CalculateNumericResponseAverage(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, averageType, field, cancellationToken);
            ClearRestrictedData(results, filteredMetric.Subset);
            return results;
        }

        public Task<UnweightedTotals> CalculateUnweightedTotals(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average, TargetInstances requestedInstances, IGroupedQuotaCells quotaCells,
            CancellationToken cancellationToken,
            EntityWeightedDailyResults[] weightedAverages = null)
        {
            var unweightedResults = _metricCalculationOrchestrator.CalculateUnweightedTotals(filteredMetric, calculationPeriod, average, requestedInstances, quotaCells, cancellationToken, weightedAverages);
            return unweightedResults;
        }

        public async Task<EntityWeightedDailyResults[]> CalculateWeightedFromUnweighted(
            UnweightedTotals unweighted, bool calculateSignificance,
            CancellationToken cancellationToken,
            IGroupedQuotaCells filteredCells = null)
        {
            var entityWeightedDailyResults = await _metricCalculationOrchestrator.CalculateWeightedFromUnweighted(unweighted, calculateSignificance, cancellationToken, filteredCells);
            ClearRestrictedData(entityWeightedDailyResults, unweighted.Subset);
            return entityWeightedDailyResults;
        }

        public IList<WeightedDailyResult> CalculateMarketAverage(EntityWeightedDailyResults[] measureResults,
            Subset subset,
            ushort minimumSamplePerPoint,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps,
            EntityWeightedDailyResults[] relativeSizes = null)
        {
            var marketAverageResults = _metricCalculationOrchestrator.CalculateMarketAverage(measureResults,
                subset,
                minimumSamplePerPoint,
                averageType,
                questionType,
                entityMeanMaps,
                relativeSizes);
            ClearRestrictedData(marketAverageResults, subset);
            return marketAverageResults;
        }
    }
}
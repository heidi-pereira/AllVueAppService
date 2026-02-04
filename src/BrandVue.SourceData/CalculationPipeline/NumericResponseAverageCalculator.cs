using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.CalculationPipeline
{
    public class NumericResponseAverageCalculator
    {
        private readonly IQuotaCellReferenceWeightingRepository _quotaCellReferenceWeightingRepository;
        private readonly IProfileResponseAccessor _profileResponseAccessor;

        public NumericResponseAverageCalculator(
            IQuotaCellReferenceWeightingRepository quotaCellReferenceWeightingRepository,
            IProfileResponseAccessor profileResponseAccessor)
        {
            _quotaCellReferenceWeightingRepository = quotaCellReferenceWeightingRepository;
            _profileResponseAccessor = profileResponseAccessor;
        }

        public WeightedDailyResult[] CalculateNumericResponseAverage(FilteredMetric filteredMetric,
            CalculationPeriod calculationPeriod,
            AverageDescriptor average,
            TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCells,
            ResponseFieldDescriptor field,
            AverageType averageType)
        {
            MetricResultEntityInformationCache[] entityCaches = new MetricResultEntityInformationCache[requestedInstances.OrderedInstances.Length];
            for (var i = 0; i < requestedInstances.OrderedInstances.Length; i++)
            {
                entityCaches[i] = new MetricResultEntityInformationCache(requestedInstances.EntityType, requestedInstances.OrderedInstances[i], filteredMetric);
            }

            var results = new WeightedDailyResult[calculationPeriod.Periods.Length];
            for (int periodIndex = 0; periodIndex < calculationPeriod.Periods.Length; periodIndex++)
            {
                var period = calculationPeriod.Periods[periodIndex];
                var quotaCellWeights = WeightGeneratorForRequestedPeriod.Generate(filteredMetric.Subset, _profileResponseAccessor, _quotaCellReferenceWeightingRepository, average, quotaCells, period.EndDate);
                if (AverageHelper.IsTypeOfMean(averageType))
                {
                    results[periodIndex] = CalculateMeanForProfileField(field, filteredMetric.Metric, period, quotaCells, quotaCellWeights, entityCaches);
                }
                else if (averageType == AverageType.Median)
                {
                    results[periodIndex] = CalculateMedianForProfileField(field, filteredMetric.Metric, period, quotaCells, quotaCellWeights, entityCaches);
                }
            }
            return results;
        }

        private WeightedDailyResult CalculateMeanForProfileField(ResponseFieldDescriptor field,
            Measure metric,
            CalculationPeriodSpan period,
            IGroupedQuotaCells quotaCells,
            Dictionary<QuotaCell, double> quotaCellWeights,
            MetricResultEntityInformationCache[] entityCaches)
        {
            var result = new WeightedDailyResult(period.EndDate)
            {
                Text = AverageHelper.GetAverageDisplayText(AverageType.Mean)
            };

            Action<int, double> addResponseValueAndWeight = (numericValue, weight) =>
            {
                result.UnweightedSampleSize += 1;
                result.WeightedSampleSize += weight;
                result.UnweightedValueTotal += numericValue;
                result.WeightedValueTotal += numericValue * weight;
            };
            AddResponsesForProfileField(field, metric, period, quotaCells, quotaCellWeights, entityCaches, addResponseValueAndWeight);
            result.WeightedResult = result.WeightedSampleSize != 0.0 ? result.WeightedValueTotal / result.WeightedSampleSize : 0.0;
            return result;
        }

        private WeightedDailyResult CalculateMedianForProfileField(ResponseFieldDescriptor field,
            Measure measure,
            CalculationPeriodSpan period,
            IGroupedQuotaCells quotaCells,
            Dictionary<QuotaCell, double> quotaCellWeights,
            MetricResultEntityInformationCache[] entityCaches)
        {
            var result = new WeightedDailyResult(period.EndDate)
            {
                Text = AverageHelper.GetAverageDisplayText(AverageType.Median),
                WeightedResult = 0.0
            };
            var responseValues = new List<(double Value, double Weight)>();

            Action<int, double> addResponseValueAndWeight = (numericValue, weight) =>
            {
                result.UnweightedSampleSize += 1;
                result.WeightedSampleSize += weight;
                responseValues.Add((numericValue, weight));
            };
            AddResponsesForProfileField(field, measure, period, quotaCells, quotaCellWeights, entityCaches, addResponseValueAndWeight);

            if (responseValues.Count > 0)
            {
                var ordered = responseValues.OrderBy(v => v.Value).ToArray();
                var totalWeight = ordered.Sum(v => v.Weight);
                var middle = totalWeight / 2;
                var index = 0;
                var sum = 0.0;
                while (sum < middle)
                {
                    sum += ordered[index++].Weight;
                }
                if (ordered.Length % 2 != 0)
                {
                    result.WeightedResult = ordered[index - 1].Value;
                }
                else
                {
                    var safeIndex = Math.Min(index, ordered.Length - 1);
                    result.WeightedResult = (ordered[index - 1].Value + ordered[safeIndex].Value) / 2f;
                }
            }
            return result;
        }

        private void AddResponsesForProfileField(ResponseFieldDescriptor field,
            Measure measure,
            CalculationPeriodSpan period,
            IGroupedQuotaCells quotaCells,
            Dictionary<QuotaCell, double> quotaCellWeights,
            MetricResultEntityInformationCache[] entityCaches,
            Action<int, double> addResponseValueAndWeight)
        {
            foreach (var (quotaCell, responses) in _profileResponseAccessor.GetResponses(period.StartDate, period.EndDate, quotaCells))
            {
                var weight = quotaCellWeights[quotaCell];
                var responseSpan = responses.Span;
                for (int responseIndex = 0; responseIndex < responseSpan.Length; responseIndex++)
                {
                    var response = responseSpan[responseIndex];
                    if (entityCaches.Any(c => IncludeResponseInNumericAverage(response, c, measure)))
                    {
                        var numericValue = response.GetIntegerFieldValue(field, default);
                        if (numericValue.HasValue)
                        {
                            addResponseValueAndWeight(numericValue.Value, weight);
                        }
                    }
                }
            }
        }

        private bool IncludeResponseInNumericAverage(IProfileResponseEntity response, MetricResultEntityInformationCache entityCache, Measure measure)
        {
            if (entityCache.CheckShouldIncludeInBase(response) && entityCache.CheckShouldIncludeInFilter(response))
            {
                var responseForMeasure = entityCache.CalculateMetricValue(response);
                if (measure.IsBasedOnCustomVariable)
                {
                    return responseForMeasure.HasValue && responseForMeasure.Value != 0;
                }
                return measure.IsValidPrimaryValue(responseForMeasure);
            }
            return false;
        }
    }
}

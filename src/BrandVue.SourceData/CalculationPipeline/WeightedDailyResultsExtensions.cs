using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Models;

public class WeightedDailyResultWithEntityInstanceId
{
    public int EntityInstanceId { get; set; }
    public WeightedDailyResult Result { get; set; }
    public double Weight { get; set; }

    public WeightedDailyResultWithEntityInstanceId(WeightedDailyResult result, int entityInstanceId, double weight)
    {
        Result = result;
        EntityInstanceId = entityInstanceId;
        Weight = weight;
    }
}

namespace BrandVue.SourceData.CalculationPipeline
{
    internal static class WeightedDailyResultsExtensions
    {
        /// <param name="entityRequestedMeasureResults"></param>
        /// <param name="minimumSamplePerPoint"></param>
        /// <param name="entityWeightingMeasureResults">If null, all brands are weighted equally</param>
        /// <returns></returns>
        public static IList<WeightedDailyResult> CalculateMarketAverage(this EntityWeightedDailyResults[] entityRequestedMeasureResults,
            ushort minimumSamplePerPoint,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps,
            EntityWeightedDailyResults[] entityWeightingMeasureResults = null)
        {
            entityWeightingMeasureResults?.AssertEntityInstancesAreAligned(entityRequestedMeasureResults);

            var pivotedRequestedMeasureResults = entityRequestedMeasureResults.PivotToPeriodWeightedDailyResults();
            var pivotedWeightingMeasureResults = entityWeightingMeasureResults?.PivotToPeriodWeightedDailyResults();

            var resultsAndWeightings = pivotedWeightingMeasureResults == null 
                ? pivotedRequestedMeasureResults.AddEqualWeightings()
                : pivotedRequestedMeasureResults.AddWeightingsFromMeasureResults(pivotedWeightingMeasureResults);

            if (pivotedWeightingMeasureResults != null)
            {
                var downweightedEntityIndices = new HashSet<int>();
                resultsAndWeightings = TrimWeightsForLowSample(resultsAndWeightings, minimumSamplePerPoint, downweightedEntityIndices);
                //Will later return this through the API to go in the low sample box
                var downWeightedEntityInstances = downweightedEntityIndices
                    .Select(instanceIndex => entityRequestedMeasureResults[instanceIndex].EntityInstance).ToArray();
            }

            return WeightedAverage(resultsAndWeightings, averageType, questionType, entityMeanMaps);
        }

        /// <summary>
        /// We usually warn about inaccuracy if a data point has a sample size below a minimumAllowedSampleTotal.
        /// This method attempts to form a reliable average based on a similar principle.
        /// </summary>
        /// <example>Let minimumAllowedSampleTotal = 75
        /// Saying sample under 75 is unreliable, is the same as saying each 1/75th of the result must represent at least one person
        /// This method caps the weighting for any brand that would violate this rule.
        /// </example>
        /// <remarks>
        /// A simpler version of this would be just to cut out brands with sample under 75. I didn't do that because:
        /// * The discontinuity would cause the average to jump around on the over time view for a brand going between 74 and 75 sample.
        /// * It would mean no average exists for an average of 100 brands each with 60 sample. Surely 6000 responses is enough to show something...
        /// The MRS suggests a maximum weighting of 4. i.e. No respondent should be weighted more than 4 times the size of any other.
        /// We could incorporate this by grouping together brands with smaller weightings into an "other" category that's weighted together.
        /// This would reduce the noise introduced by amplifying small sample.
        /// It might also be reasonable to report an "effective sample size" (i.e. weighted sample size)
        /// Note: weightings do not sum to 1, they are just relative to each other initially</remarks>
        private static WeightedDailyResultWithEntityInstanceId[][] TrimWeightsForLowSample(
            WeightedDailyResultWithEntityInstanceId[][] resultsAndWeightings, ushort minimumAllowedSampleTotal, ISet<int> downweightedInstanceIndices)
        {
            return resultsAndWeightings.Select(perEntityInstanceResultsForPeriod => TrimWeightsForLowSample(perEntityInstanceResultsForPeriod, minimumAllowedSampleTotal, downweightedInstanceIndices)).ToArray();
        }

        private static WeightedDailyResultWithEntityInstanceId[] TrimWeightsForLowSample(
            WeightedDailyResultWithEntityInstanceId[] perEntityInstanceResultsForPeriod,
            ushort minimumAllowedSampleTotal,
            ISet<int> downweightedInstanceIndices)
        {
            const double weightIsZero = 1E-10;
            var totalWeight = perEntityInstanceResultsForPeriod.Sum(t => t.Weight);
            if (Math.Abs(totalWeight) < weightIsZero || minimumAllowedSampleTotal == 0)
            {
                return perEntityInstanceResultsForPeriod;
            }

            var leastUpweightableFirst = perEntityInstanceResultsForPeriod.Select((entityInstanceResult, originalInstanceIndex) =>
            {
                var originalNormalizedWeight = entityInstanceResult.Weight / totalWeight;
                double maxAllowedNormalizedWeight = (double)entityInstanceResult.Result.UnweightedSampleSize / minimumAllowedSampleTotal;
                return (Result: entityInstanceResult.Result, RelativeWeight: entityInstanceResult.Weight, OriginalInstanceIndex: originalInstanceIndex,
                    MaxAllowedNormalizedWeight: maxAllowedNormalizedWeight, OriginalNormalizedWeight: originalNormalizedWeight,
                    EntityInstanceId: entityInstanceResult.EntityInstanceId);
            }).OrderBy(t => t.MaxAllowedNormalizedWeight / t.OriginalNormalizedWeight);

            var currentUpweightFactor = 1d;
            var remainingOriginalNormalizedWeight = 1d;
            return leastUpweightableFirst.Select(entityInstanceResult =>
            {
                remainingOriginalNormalizedWeight -= entityInstanceResult.OriginalNormalizedWeight;
                var newNormalizedWeight = entityInstanceResult.OriginalNormalizedWeight * currentUpweightFactor;
                if (newNormalizedWeight > entityInstanceResult.MaxAllowedNormalizedWeight)
                {
                    var upweightFactor =
                        CalculateUpweightingForOthers(newNormalizedWeight, remainingOriginalNormalizedWeight, entityInstanceResult.MaxAllowedNormalizedWeight);
                    currentUpweightFactor *= upweightFactor;
                    newNormalizedWeight = entityInstanceResult.MaxAllowedNormalizedWeight;
                    downweightedInstanceIndices.Add(entityInstanceResult.OriginalInstanceIndex);
                }
                return new WeightedDailyResultWithEntityInstanceId(entityInstanceResult.Result, entityInstanceResult.EntityInstanceId, newNormalizedWeight);
            }).ToArray();
        }

        private static double CalculateUpweightingForOthers(double currentNormalizedWeight,
            double remainingOriginalNormalizedWeight,
            double maxAllowedNormalizedWeight)
        {
            var spareNormalizedWeight = currentNormalizedWeight - maxAllowedNormalizedWeight;
            return 1d + spareNormalizedWeight / remainingOriginalNormalizedWeight;
        }

        private static void AssertEntityInstancesAreAligned(this EntityWeightedDailyResults[] requestedMeasureResults,
            EntityWeightedDailyResults[] weightingMeasureResults)
        {
            var misalignedInstancePairs = requestedMeasureResults.Zip(weightingMeasureResults, (r, w) => (Result: r, Weight: w))
                .Where(t => t.Result.EntityInstance != t.Weight.EntityInstance).ToArray();
            if (misalignedInstancePairs.Any())
                throw new InvalidOperationException($"Entity instance alignment issue for: {string.Join(",", misalignedInstancePairs)}");
        }

        private static (WeightedDailyResult results, int entityInstanceId)[][] PivotToPeriodWeightedDailyResults(this EntityWeightedDailyResults[] entityWeightedDailyResults)
        {
            var periodCount = (Min: entityWeightedDailyResults.Min(r => r.WeightedDailyResults.Count), Max: entityWeightedDailyResults.Max(r => r.WeightedDailyResults.Count));
            if (periodCount.Min != periodCount.Max)
            {
                throw new Exception("Number of daily weighted results for each entity instance is not the same.");
            }

            var periodWeightedDailyResults = entityWeightedDailyResults[0].WeightedDailyResults.Select((t, periodIndex) =>
                entityWeightedDailyResults.Select(entityResults => (entityResults.WeightedDailyResults[periodIndex], entityResults.EntityInstance?.Id ?? -1)).ToArray()
            ).ToArray();

            return periodWeightedDailyResults;
        }

        private static WeightedDailyResultWithEntityInstanceId[][] AddEqualWeightings(this (WeightedDailyResult results, int entityId)[][] perPeriodPerEntityInstanceResults)
        {
            return perPeriodPerEntityInstanceResults.Select(periodResults =>
            {
                return periodResults.Select(r => new WeightedDailyResultWithEntityInstanceId(r.results, r.entityId, 1f)).ToArray();
            }).ToArray();
        }

        /// <summary>
        /// We bias which brands we ask questions about (and other things using survey quotas).
        /// Therefore we shouldn't use WeightedSampleCount for brand weighting - we have more accurate brand sizing information from another measure.
        /// </summary>
        private static WeightedDailyResultWithEntityInstanceId[][] AddWeightingsFromMeasureResults(
            this (WeightedDailyResult results, int entityId)[][] perPeriodPerEntityInstanceResults,
            (WeightedDailyResult results, int entityId)[][] perPeriodPerEntityInstanceWeightings)
        {
            var weightingsByDate = perPeriodPerEntityInstanceWeightings.ToDictionary(weightingsForPeriod => weightingsForPeriod[0].results.Date);

            return perPeriodPerEntityInstanceResults
                .Select(resultsForPeriod => ZipResultAndWeight(resultsForPeriod, weightingsByDate)).ToArray();
        }

        private static WeightedDailyResultWithEntityInstanceId[] ZipResultAndWeight(
            (WeightedDailyResult results, int entityId)[] entityResultsForPeriod, IReadOnlyDictionary<DateTimeOffset, (WeightedDailyResult results, int entityId)[]> entityWeightingsByDate)
        {
            var allEntityInstanceWeightings = entityWeightingsByDate.TryGetValue(entityResultsForPeriod[0].results.Date, out var perEntityInstanceResults)
                                ? perEntityInstanceResults
                                : throw new ArgumentException($"No weighting defined for {entityResultsForPeriod[0].results.Date}");
            return entityResultsForPeriod
                .Zip(allEntityInstanceWeightings, (r, w) => new WeightedDailyResultWithEntityInstanceId(r.results, r.entityId, w.results.WeightedResult))
                .ToArray();
        }

        private static IList<WeightedDailyResult> WeightedAverage(WeightedDailyResultWithEntityInstanceId[][] perPeriodResultsAndSizes,
            AverageType averageType,
            MainQuestionType questionType,
            EntityMeanMap entityMeanMaps)
        {
            var averageResults = new List<WeightedDailyResult>(perPeriodResultsAndSizes.Length);

            Func<WeightedDailyResultWithEntityInstanceId[], WeightedDailyResult> calculateAverage = averageType switch
            {
                AverageType.ResultMean or AverageType.Mean => results => GetNonSingleChoiceMean(results, questionType),
                AverageType.EntityIdMean => results => GetAverageByEntityInstanceId(results, entityMeanMaps),
                AverageType.Median => GetMedianEntityInstanceId,
                _ => throw new ArgumentOutOfRangeException(nameof(averageType), averageType, $"Average type `{averageType}` not supported"),
            };

            foreach (var perEntityInstanceResultsForPeriod in perPeriodResultsAndSizes)
            {
                var averageResult = calculateAverage(perEntityInstanceResultsForPeriod);
                averageResults.Add(averageResult);
            }

            return averageResults;
        }

        private static WeightedDailyResult GetMedianEntityInstanceId(WeightedDailyResultWithEntityInstanceId[] perEntityInstanceResultsForPeriod)
        {
            var orderedResults = perEntityInstanceResultsForPeriod.OrderBy(r => r.EntityInstanceId).ToArray();
            var weightedSampleSize = (uint)perEntityInstanceResultsForPeriod.Sum(p => p.Result.WeightedValueTotal);
            var averageResult = new WeightedDailyResult(perEntityInstanceResultsForPeriod[0].Result.Date);

            //Where there is an even number of respondents, we take the upper median as we cannot divide an entity instance
            var medianRespondentIndex = (int)Math.Ceiling(weightedSampleSize / 2.0);
            var entityContainingMedianRespondentIndex = GetEntityContainingMedianRespondent(orderedResults, medianRespondentIndex);
            averageResult.WeightedResult = entityContainingMedianRespondentIndex.EntityInstanceId;
            averageResult.UnweightedSampleSize = (uint)perEntityInstanceResultsForPeriod.Sum(p => p.Result.UnweightedSampleSize);
            averageResult.WeightedSampleSize = weightedSampleSize;

            averageResult.Text = AverageHelper.GetAverageDisplayText(AverageType.Median);
            return averageResult;
        }

        private static WeightedDailyResultWithEntityInstanceId GetEntityContainingMedianRespondent(
            WeightedDailyResultWithEntityInstanceId[] perEntityInstanceResultsForPeriod,
            int medianIndex)
        {
            double runningTotal = 0;
            foreach (var result in perEntityInstanceResultsForPeriod)
            {
                runningTotal += result.Result.WeightedValueTotal;
                if (runningTotal >= medianIndex)
                {
                    return result;
                }
            }
            throw new Exception("Unable to calculate median");
        }

        private static WeightedDailyResult GetAverageByEntityInstanceId(WeightedDailyResultWithEntityInstanceId[] perEntityInstanceResultsForPeriod,
            EntityMeanMap entityMeanMaps)
        {
            var entityIdsToExclude = Array.Empty<int>();
            if(entityMeanMaps != null)
            {
                entityIdsToExclude = entityMeanMaps.Mapping
                    .Where(e => !e.IncludeInCalculation)
                    .Select(x => x.EntityId)
                    .ToArray();
            }

            var resultsToIncludeInCalculation = perEntityInstanceResultsForPeriod
                .Where(p => !entityIdsToExclude.Contains(p.EntityInstanceId))
                .ToArray();

            var averageResult = new WeightedDailyResult(resultsToIncludeInCalculation[0].Result.Date);
            var totalResult = 0d;
            var weightedValues = new List<(double value, double weight)>();

            foreach (var weightedDailyResult in resultsToIncludeInCalculation)
            {
                if (weightedDailyResult.Result.UnweightedSampleSize > 0)
                {
                    var multiplier = weightedDailyResult.EntityInstanceId;
                    if(entityMeanMaps != null && entityMeanMaps.Mapping.Count() > 0)
                    {
                        var meanMap = entityMeanMaps.Mapping.First(x => x.EntityId == weightedDailyResult.EntityInstanceId);
                        multiplier = meanMap.MeanCalculationValue;
                    }

                    totalResult += multiplier * weightedDailyResult.Result.WeightedValueTotal;
                    weightedValues.Add((multiplier, weightedDailyResult.Result.WeightedValueTotal));
                    averageResult.ResponseIdsForDay.AddRange(weightedDailyResult.Result.ResponseIdsForDay);
                }
            }

            //this is misleading, as it is always the result
            var totalWeight = resultsToIncludeInCalculation.Sum(instance => instance.Result.WeightedValueTotal);
            averageResult.WeightedResult = SafeDivide(totalResult, totalWeight);
            averageResult.UnweightedSampleSize = resultsToIncludeInCalculation[0].Result.UnweightedSampleSize;
            averageResult.WeightedSampleSize = resultsToIncludeInCalculation[0].Result.WeightedSampleSize;
            averageResult.UnweightedValueTotal = resultsToIncludeInCalculation.Count();
            averageResult.Text = AverageHelper.GetAverageDisplayText(AverageType.ResultMean);
            
            (averageResult.StandardDeviation, averageResult.Variance) = CalculateStandardDeviation(weightedValues, totalWeight, averageResult.WeightedResult);
            
            return averageResult;
        }

        private static WeightedDailyResult GetNonSingleChoiceMean(WeightedDailyResultWithEntityInstanceId[] perEntityInstanceResultsForPeriod, MainQuestionType questionType)
        {
            var averageResult = new WeightedDailyResult(perEntityInstanceResultsForPeriod[0].Result.Date);

            var totalResult = 0d;
            var totalWeight = 0d;
            var weightedValues = new List<(double value, double weight)>();

            foreach (var weightedDailyResult in perEntityInstanceResultsForPeriod)
            {
                if (weightedDailyResult.Result.UnweightedSampleSize > 0 && weightedDailyResult.Weight > 0)
                {
                    averageResult.UnweightedSampleSize += weightedDailyResult.Result.UnweightedSampleSize;
                    averageResult.WeightedSampleSize += weightedDailyResult.Result.WeightedSampleSize;
                    averageResult.ResponseIdsForDay.AddRange(weightedDailyResult.Result.ResponseIdsForDay);
                    totalResult += weightedDailyResult.Result.WeightedResult * weightedDailyResult.Weight;
                    totalWeight += weightedDailyResult.Weight;
                    weightedValues.Add((weightedDailyResult.Result.WeightedResult, weightedDailyResult.Weight));
                }
            }

            averageResult.WeightedResult = SafeDivide(totalResult, totalWeight);
            averageResult.UnweightedValueTotal = perEntityInstanceResultsForPeriod.Count();
            averageResult.Text = AverageHelper.GetAverageDisplayText(AverageType.ResultMean);
            
            (averageResult.StandardDeviation, averageResult.Variance) = CalculateStandardDeviation(weightedValues, totalWeight, averageResult.WeightedResult);
            
            return averageResult;
        }

        private static (double? standardDeviation, double? variance) CalculateStandardDeviation(List<(double value, double weight)> weightedValues, double totalWeight, double mean)
        {
            if (weightedValues.Count <= 1 || totalWeight <= 0)
            {
                return (null, null);
            }

            var variance = 0d;
            foreach (var (value, weight) in weightedValues)
            {
                variance += weight * Math.Pow(value - mean, 2);
            }
            variance /= totalWeight;
            return (Math.Sqrt(variance), variance);
        }

        private static double SafeDivide(double numerator, double denominator)
        {
            return denominator > 0 ? numerator / denominator : 0f;
        }
    }
}
using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Weightings.Rim
{
    public class RimWeightingCalculator : IRimWeightingCalculator
    {
        public const double PointTolerance = 0.00005;
        private const uint MaxIterations = 50;

        /// <summary>
        /// Uses RIM algorithm to calculate the weights for all quota cells based on the configured rim category targets.
        /// </summary>
        /// <param name="quotaCellSampleSizesInIndexOrder">Quota cells with their respective sample size.
        ///     There should be a quota cell for each combination of rim dimension categories, i.e. possible answers to questions we want to weight on.</param>
        /// <param name="rimDimensions">Use this model to tell the calculator what the desired target for each dimension category is.
        ///     RimDimension is the survey question used for weighting and it has a collection of RimCategories
        ///     (i.e. possible answers to this question) with their respective targets.</param>
        /// <param name="includeQuotaDetails">Whether or not to include calculation details per quota cell.</param>
        public RimWeightingCalculationResult Calculate(
            IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellSampleSizesInIndexOrder,
            Dictionary<string, Dictionary<int, double>> rimDimensions, bool includeQuotaDetails)
        {
            if (!quotaCellSampleSizesInIndexOrder.Any()) return new RimWeightingCalculationResult(0, 0, 0, false, 0);
            var quotaCellsInIndexOrder = quotaCellSampleSizesInIndexOrder.Select((q, index) => (q.QuotaCell, InternalIndex: index)).ToArray();
            var sampleSizesInQuotaIndexOrder = quotaCellSampleSizesInIndexOrder.Select(q => q.SampleSize).ToArray();

            const double defaultWeight = 1.0;
            var weightsInQuotaIndexOrder = new double[quotaCellsInIndexOrder.Length];
            Array.Fill(weightsInQuotaIndexOrder, defaultWeight);
            bool noConvergence = true;
            uint iterationsPerformed = 0;

            var quotaCellsWithTargets = rimDimensions.SelectMany(dimensionToCategoryTargets => GetQuotaCellsWithTarget(quotaCellsInIndexOrder, dimensionToCategoryTargets)).ToArray();
            while (noConvergence && iterationsPerformed <= MaxIterations)
            {
                foreach (var (quotaCells, target) in quotaCellsWithTargets)
                {
                    var totalSampleInCategory = quotaCells.Sum(c => sampleSizesInQuotaIndexOrder[c.InternalIndex]);
                    if (totalSampleInCategory == 0)
                    {
                        // No one gave this answer to this quota cell question. We leave all the target sizes as 0.
                        continue;
                    }

                    var factor = target / totalSampleInCategory;
                    foreach (var categoryCell in quotaCells)
                    {
                        sampleSizesInQuotaIndexOrder[categoryCell.InternalIndex] *= factor;
                    }
                }

                // If a cell has 0 people in it, then we leave it at 0 so the weight is 1.
                var newWeightsInQuotaIndexOrder = quotaCellsInIndexOrder
                    .Select(qs => quotaCellSampleSizesInIndexOrder[qs.InternalIndex].SampleSize == 0 ? 1 : sampleSizesInQuotaIndexOrder[qs.InternalIndex] / quotaCellSampleSizesInIndexOrder[qs.InternalIndex].SampleSize)
                    .ToArray();

                noConvergence = newWeightsInQuotaIndexOrder.Select((w, index) => Math.Abs(w - weightsInQuotaIndexOrder[index]) <= PointTolerance).Any(c => !c);
                weightsInQuotaIndexOrder = newWeightsInQuotaIndexOrder;
                iterationsPerformed++;
            }

            var minWeight = weightsInQuotaIndexOrder.Min(w => w);
            var maxWeight = weightsInQuotaIndexOrder.Max(w => w);
            var totalSampleSize = quotaCellSampleSizesInIndexOrder.Sum(q => q.SampleSize);
            var efficiency = CalculateEfficiency(quotaCellsInIndexOrder, quotaCellSampleSizesInIndexOrder, weightsInQuotaIndexOrder, totalSampleSize);

            if (includeQuotaDetails)
            {
                var quotaCellToWeight = quotaCellsInIndexOrder.ToDictionary(q => q, q => weightsInQuotaIndexOrder[q.InternalIndex]);
                var quotaDetails = GetQuotaDetails(quotaCellSampleSizesInIndexOrder, quotaCellToWeight, totalSampleSize);
                return new RimWeightingCalculationResult(minWeight, maxWeight, efficiency, !noConvergence, iterationsPerformed, quotaDetails);
            }

            var distribution = GenerateWeightingDistribution(quotaCellSampleSizesInIndexOrder, quotaCellsInIndexOrder, weightsInQuotaIndexOrder);

            return new RimWeightingCalculationResult(minWeight, maxWeight, efficiency, !noConvergence, iterationsPerformed, null, distribution);
        }

        private static WeightsDistribution GenerateWeightingDistribution(IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellSampleSizesInIndexOrder, (QuotaCell QuotaCell, int InternalIndex)[] quotaCellsInIndexOrder, double[] weightsInQuotaIndexOrder)
        {
            var distribution = new WeightsDistribution();
            for (int index = 0; index < quotaCellsInIndexOrder.Length; index++)
            {
                var qs = quotaCellsInIndexOrder[index];
                var sampleSize = quotaCellSampleSizesInIndexOrder[qs.InternalIndex].SampleSize;
                var weight = weightsInQuotaIndexOrder[index];
                int bucketIndex = (int)(weight / distribution.BucketFactor);
                if (bucketIndex < 0)
                {
                    bucketIndex = 0;
                }
                if (bucketIndex >= distribution.NumberOfBuckets)
                {
                    bucketIndex = distribution.NumberOfBuckets - 1;
                }
                distribution.Buckets[bucketIndex] += (int)sampleSize;
            }

            return distribution;
        }

        private static IEnumerable<(IReadOnlyCollection<(QuotaCell QuotaCell, int InternalIndex)> QuotaCells, double Target)> GetQuotaCellsWithTarget((QuotaCell QuotaCell, int InternalIndex)[] quotaCellsInIndexOrder, KeyValuePair<string, Dictionary<int, double>> dimensionToCategoryTargets)
        {
            (string dimension, var categoryTargets) = dimensionToCategoryTargets;
            var cellsByDimensionKey = quotaCellsInIndexOrder.ToLookup(qc => int.Parse(qc.QuotaCell.GetKeyPartForFieldGroup(dimension)));
            return categoryTargets.Select(category => (QuotaCells: (IReadOnlyCollection<(QuotaCell QuotaCell, int index)>)cellsByDimensionKey[category.Key].ToList(), Target: category.Value));
        }

        private static QuotaWeightingDetails[] GetQuotaDetails(IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellSampleSizesInIndexOrder, Dictionary<(QuotaCell QuotaCell, int InternalIndex), double> quotaCellToScaleFactor, double totalSampleSize)
        {
            var quotaCellKeyToScaleFactor = quotaCellToScaleFactor.ToDictionary(q => q.Key.QuotaCell.ToString(), q => q.Value);
            var quotaKeyToTarget = WeightingHelper.ConvertScaleFactorsToTargetWeights(quotaCellSampleSizesInIndexOrder, quotaCellKeyToScaleFactor, totalSampleSize);
            return quotaCellSampleSizesInIndexOrder.Select(quotaSampleSize => ToQuotaWeightingDetails(quotaSampleSize, quotaCellKeyToScaleFactor, quotaKeyToTarget)).ToArray();
        }

        private static QuotaWeightingDetails ToQuotaWeightingDetails((QuotaCell QuotaCell, double SampleSize) quotaSampleSize,
            IReadOnlyDictionary<string, double> quotaCellKeyToScaleFactor, IReadOnlyDictionary<string, double> quotaKeyToTarget)
        {
            (var quotaCell, double sampleSize) = quotaSampleSize;
            var quotaCellKey = quotaCell.ToString();
            return new QuotaWeightingDetails(quotaCell, sampleSize, quotaCellKeyToScaleFactor[quotaCellKey],
                quotaKeyToTarget[quotaCellKey]);
        }

        /// <summary>
        /// This implementation is based off of https://quantipy.readthedocs.io/en/staging-develop/sites/lib_doc/weights/03_diags.html
        /// We have data at the quota cell level instead of the respondent level, but we just multiply through by the sample size for that quota cell
        /// </summary>
        private static double CalculateEfficiency((QuotaCell QuotaCell, int InternalIndex)[] quotaCellsInIndexOrder,
            IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellToSampleSize,
            double[] finalQuotaCellToWeights, double totalNumberOfRespondents)
        {
            var sumOfWeightsPerRespondent = quotaCellsInIndexOrder.Sum(w => quotaCellToSampleSize[w.InternalIndex].SampleSize * finalQuotaCellToWeights[w.InternalIndex]);
            var sumOfWeightsPerRespondentSquared = Math.Pow(sumOfWeightsPerRespondent, 2);
            var sumOfSquaredWeightsPerRespondent = quotaCellsInIndexOrder.Sum(w => quotaCellToSampleSize[w.InternalIndex].SampleSize * Math.Pow(finalQuotaCellToWeights[w.InternalIndex], 2));
            return sumOfWeightsPerRespondentSquared / totalNumberOfRespondents / sumOfSquaredWeightsPerRespondent;
        }
    }
}

using BrandVue.SourceData.Weightings.Rim;

namespace BrandVue.SourceData.QuotaCells
{
    public record WeightingValue(double? Weight, bool IsResponseLevelWeighting, double? ExpansionFactor = null)
    {
        public static WeightingValue StandardWeighting(double referenceWeighting)
        {
            return new WeightingValue(referenceWeighting, false);
        }
        public static WeightingValue ResponseLevelWeighting(double referenceWeighting)
        {
            return new WeightingValue(referenceWeighting, true);
        }
    }
    /// <summary>
    /// Null means "unweighted". i.e. don't do any weighting adjustment related to this quota cell, just always weight quota cell as 1
    /// </summary>
    public class QuotaCellReferenceWeightings : IReferenceWeightingChecker
    {
        public const double StaticWeightingValue = -1.0;
        private readonly IDictionary<string, WeightingValue> _referenceWeightingsByQuotaCellString
            = new Dictionary<string, WeightingValue>();
        public double TotalWeighting { get; set; }

        /// <summary>
        /// Weight of -1 means "weight statically as 1"
        /// </summary>
        internal QuotaCellReferenceWeightings(IDictionary<string, WeightingValue> weightings)
        {
            ConstructAndValidateDictionary(weightings);
        }

        private void ConstructAndValidateDictionary(IDictionary<string, WeightingValue> weightings)
        {
            foreach (var weighting in weightings)
            {
                var weightingValue = weighting.Value.Weight;
                if (WeightStaticallyAsOne(weightingValue.Value))
                {
                    weightingValue = null;
                }
                else if (weightingValue >= 0)
                {
                    TotalWeighting += weightingValue.Value;
                }
                else
                {
                    // This has been here ever since negative weights were supported, so if we want to use -0.5 to mean always weight statically as 0.5 in future, we could safely do so.
                    throw new ArgumentOutOfRangeException(weighting.Key, weightingValue.Value.ToString(),
                        $"Weighting must be positive, or -1 to indicate static weighting of 1, but is `{weightingValue.Value}` for `{weighting.Key}`");
                }
                _referenceWeightingsByQuotaCellString.Add(weighting.Key, weighting.Value with { Weight = weightingValue });
            }
        }

        /// <remarks>
        ///  If weighting is -1, weight statically as 1
        /// </remarks>
        private static bool WeightStaticallyAsOne(double weighting)
        {
            return Math.Abs(weighting + 1) < RimWeightingCalculator.PointTolerance;
        }
        
        /// <summary>
        /// Null means "unweighted". i.e. don't do any weighting adjustment related to this quota cell, just always weight quota cell as 1
        /// </summary>
        public WeightingValue GetReferenceWeightingFor(QuotaCell quotaCell)
        {
            if (quotaCell == null)
            {
                throw new ArgumentNullException(
                    nameof(quotaCell),
                    $"Cannot retrieve reference weighting for null quota cell");
            }
            return GetReferenceWeightingFor(quotaCell.ToString());
        }

        public WeightingValue GetReferenceWeightingFor(string quotaCellString)
        {
            if (quotaCellString == null)
            {
                throw new ArgumentNullException(
                    nameof(quotaCellString),
                    $"Cannot retrieve reference weighting for null quota cell string.");
            }

            try
            {
                return _referenceWeightingsByQuotaCellString[quotaCellString];
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException(
                    $"No reference weighting has been defined for quota cell '{quotaCellString}'. Check reference weighting configuration file.");
            }
        }

        public bool HasReferenceWeightingFor(QuotaCell quotaCell)
        {
            return quotaCell == null
                ? false
                : HasReferenceWeightingFor(quotaCell.ToString());
        }

        private bool HasReferenceWeightingFor(string quotaCellString)
        {
            return quotaCellString == null
                ? false
                : _referenceWeightingsByQuotaCellString.ContainsKey(quotaCellString);
        }
    }
}

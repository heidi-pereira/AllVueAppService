using System.Linq;

namespace BrandVue.EntityFramework.MetaData.Weightings
{
    public class WeightingSchemeDetails
    {
        private const decimal ErrorTolerance = 0.00005M;
        private const int MAX_NUMBER_OF_QUOTA_CELLS = 2000 * 1000;
        public List<Dimension> Dimensions { get; set; }

        public bool IsValid(out string invalidReason)
        {
            invalidReason = default;
            int numberOfQuotaCells = Dimensions.Aggregate(1, (current, dimension) => current * dimension.CellKeyToTarget.Values.Count());
            //greater than or equal to accounts for the extra 1 unweighted cell
            if (numberOfQuotaCells >= MAX_NUMBER_OF_QUOTA_CELLS) invalidReason = $"This scheme contains too many categories. It would yield more than the maximum {MAX_NUMBER_OF_QUOTA_CELLS:N0} quota cells.";
            else if (Dimensions.Any(d => !SumToApproximatelyOne(d))) invalidReason = "At least one dimension in the scheme does not add up to 100%";
            else if (Dimensions.Count <= 0) invalidReason = "Schemes should contain at least one dimension";
            return invalidReason is null;
        }

        private static bool SumToApproximatelyOne(Dimension d)
        {
            return Math.Abs(d.CellKeyToTarget.Sum(c => c.Value) -1) < ErrorTolerance;
        }
    }

    public class Dimension
    {
        public string[] InterlockedVariableIdentifiers { get; set; }
        public IReadOnlyDictionary<string, decimal> CellKeyToTarget { get; set; }
    }
}
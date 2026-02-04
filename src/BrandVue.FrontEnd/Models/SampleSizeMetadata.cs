using NJsonSchema.Annotations;

namespace BrandVue.Models
{
    public class SampleSizeMetadata
    {
        public UnweightedAndWeightedSample SampleSize { get; set; }
        public IReadOnlyDictionary<string, UnweightedAndWeightedSample> SampleSizeByMetric { get; set; }
        public IReadOnlyDictionary<string, UnweightedAndWeightedSample> SampleSizeByEntity { get; set; }
        
        public DateTimeOffset? CurrentDate { get; set; }
        [CanBeNull]
        public string SampleSizeEntityInstanceName { get; set; }
    }

    public readonly record struct UnweightedAndWeightedSample : IComparable<UnweightedAndWeightedSample>
    {
        public double Unweighted { get; init; }
        public double Weighted { get; init; }

        public bool HasDifferentWeightedSample => Math.Abs(Weighted - Unweighted) >= 1.0;

        public int CompareTo(UnweightedAndWeightedSample other)
        {
            int result = Unweighted.CompareTo(other.Unweighted);
            return result != 0 ? result : Weighted.CompareTo(other.Weighted);
        }
    }
}

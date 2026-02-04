using BrandVue.SourceData.QuotaCells;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Weightings.Rim
{
    public class WeightsDistribution
    {
        public WeightsDistribution()
        {
            BucketFactor = 0.1;
            NumberOfBuckets = 50;
            Buckets = new int[50];
        }

        public double BucketFactor { get; }
        public int NumberOfBuckets { get; }
        public int[] Buckets { get; }
    }
    public class RimWeightingCalculationResult
    {
        [JsonIgnore]
        public IReadOnlyCollection<QuotaWeightingDetails> QuotaDetails { get; }
        public double MinWeight { get; }
        public double MaxWeight { get; }
        public double EfficiencyScore { get; }
        public bool Converged { get; }
        public uint IterationsRequired { get; }
        public WeightsDistribution WeightsDistribution { get;}

        public RimWeightingCalculationResult(double minWeight, double maxWeight, double efficiencyScore, bool converged,
            uint iterationsRequired, IReadOnlyCollection<QuotaWeightingDetails> quotaDetails = null, WeightsDistribution distribution = null)
        {
            MinWeight = minWeight;
            MaxWeight = maxWeight;
            EfficiencyScore = efficiencyScore;
            Converged = converged;
            IterationsRequired = iterationsRequired;
            QuotaDetails = quotaDetails;
            WeightsDistribution = distribution;
        }
    }
    
    public class QuotaWeightingDetails
    {
        public QuotaWeightingDetails(QuotaCell quotaCell, double sampleSize, double scaleFactor, double target) : this(quotaCell.ToString(), sampleSize, scaleFactor, target)
        {
            QuotaCell = quotaCell;
        }
        
        [JsonConstructor]
        public QuotaWeightingDetails(string quotaCellKey, double sampleSize, double scaleFactor, double target)
        {
            QuotaCellKey = quotaCellKey;
            SampleSize = sampleSize;
            ScaleFactor = scaleFactor;
            Target = target;
        }
        public QuotaCell QuotaCell { get; }
        public string QuotaCellKey { get; }
        public double SampleSize { get; }
        public double ScaleFactor { get; }
        public double Target { get; }
    }
}
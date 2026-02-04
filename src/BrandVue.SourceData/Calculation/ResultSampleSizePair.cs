using Newtonsoft.Json;
using System.Numerics;
using BrandVue.SourceData.Filters;

namespace BrandVue.SourceData.Calculation
{
    public class ResultSampleSizePair : IAdditionOperators<ResultSampleSizePair, ResultSampleSizePair, ResultSampleSizePair>, ISubtractionOperators<ResultSampleSizePair, ResultSampleSizePair, ResultSampleSizePair>
    {
        public double Result { get; set; }
        public uint SampleSize;
        public double Variance;

        public ResultSampleSizePair[] ChildResults { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);

        public static ResultSampleSizePair operator +(ResultSampleSizePair left, ResultSampleSizePair right) => new()
        {
            Result = left.Result + right.Result,
            SampleSize = left.SampleSize + right.SampleSize,
            Variance = left.Variance + right.Variance,
            // Note: Not a deep copy - do not mutate
            ChildResults = left.ChildResults is null && right.ChildResults is not null ? right.ChildResults : left.ChildResults.SafeZipAdd(right.ChildResults)
        };

        public static ResultSampleSizePair operator -(ResultSampleSizePair left, ResultSampleSizePair right) => new()
        {
            Result = left.Result - right.Result,
            SampleSize = left.SampleSize - right.SampleSize,
            Variance = left.Variance - right.Variance,
            ChildResults = left.ChildResults.SafeZipSubtract(right.ChildResults)
        };

        internal void Add(int responseValue, double? weightedAverage)
        {
            SampleSize++;
            Result += responseValue;

            if (weightedAverage != null)
            {
                Variance += Math.Pow(responseValue - weightedAverage.Value, 2);
            }
        }

        public void AddToBreakResults(IProfileResponseEntity profileResponse, int responseValue, double? weightedAverage, Break[] breaks)
        {
            Add(responseValue, weightedAverage);

            if (breaks is null) return;

            foreach (var b in breaks)
            {
                var childIndexes = b.GetInstanceIndexes(profileResponse);
                foreach (int childIndex in childIndexes)
                {
                    var breakInstanceResult = ChildResults[childIndex];
                    //Future: Make weightedAverage work for breaks - currently it's using the top level average, not the one for the break
                    breakInstanceResult.AddToBreakResults(profileResponse, responseValue, weightedAverage, b.ChildBreak);
                }
            }
        }

        public static ResultSampleSizePair[] EmptyWithChildResults(Break[] breaks)
        {
            return breaks is { Length: > 0 }
                ? breaks.SelectMany(b => b.Instances, (b, i) =>
                    new ResultSampleSizePair { ChildResults = EmptyWithChildResults(b.ChildBreak) }
                ).ToArray()
                : null;
        }
    }
}

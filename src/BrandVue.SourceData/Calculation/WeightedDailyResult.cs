using Newtonsoft.Json;

namespace BrandVue.SourceData.Calculation
{
    /// <summary>
    /// Should be renamed to just WeightedResult (there isn't necessarily one of these per day, though each one is attached to a day)
    /// 
    /// Weighted result for a specific time period. The date is the last day of the period being represented.
    /// </summary>
    public class WeightedDailyResult
    {
        public WeightedDailyResult(DateTimeOffset date)
        {
            Date = date;
            ResponseIdsForDay = new List<int>();
        }

        public DateTimeOffset Date { get; set; }
        public double WeightedResult { get; set; }
        public double WeightedValueTotal { get; set; }
        public double UnweightedValueTotal { get; set; }
        public uint UnweightedSampleSize { get; set; }
        public double WeightedSampleSize { get; set; }
        public List<int> ResponseIdsForDay { get; set; }
        public string Text { get; set; }
        public double? StandardDeviation { get; set; }
        public double? Variance { get; set; }
        public double? Tscore { get; set; }
        public Significance? Significance { get; set; }
        public string? SigificanceHelpText { get; set; }
        public WeightedDailyResult[] ChildResults { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }

        public IEnumerable<WeightedDailyResult> RootAndLeaves()
        {
            var weightedDailyResults = this.Yield();
            if (ChildResults?.Length >= 1) weightedDailyResults = weightedDailyResults.Concat(Leaves(this));
            return weightedDailyResults;

            IEnumerable<WeightedDailyResult> Leaves(WeightedDailyResult current)
            {
                if (current.ChildResults?.Length >= 1)
                {
                    return current.ChildResults.SelectMany(Leaves);
                }
                else
                {
                    return current.Yield();
                }
            }
        }
    }
}

using Newtonsoft.Json;

namespace BrandVue.SourceData.Calculation
{
    public class WeightedTotal
    {
        public WeightedTotal(DateTimeOffset date)
        {
            Date = date;
            ResponseIdsForDay = new List<int>();
        }

        public DateTimeOffset Date { get; set; }
        public double WeightedValueTotal;
        public double UnweightedValueTotal;
        public uint UnweightedSampleCount;
        public double WeightedSampleCount;

        public List<int> ResponseIdsForDay { get; }
        public WeightedTotal[] ChildResults { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.None);
        }
    }
}

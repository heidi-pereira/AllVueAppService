using Newtonsoft.Json;

namespace BrandVue.SourceData.Calculation
{
    public class CalculationPeriod
    {
        public CalculationPeriod()
        {
        }

        public CalculationPeriod(DateTimeOffset startDate, DateTimeOffset endDate) : this()
        {
            Periods = new[] { new CalculationPeriodSpan { StartDate = startDate, EndDate = endDate } };
        }

        public CalculationPeriodSpan[] Periods { get; set; }

        public DateTimeOffset EndDate => Periods.Last().EndDate;

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static CalculationPeriod Parse(string startDateYyyyMMdd, string endDateYyyyMMdd)
        {
            return new CalculationPeriod(
                DateTimeOffsetExtensions.ParseDate(startDateYyyyMMdd),
                DateTimeOffsetExtensions.ParseDate(endDateYyyyMMdd));
        }

    }
}

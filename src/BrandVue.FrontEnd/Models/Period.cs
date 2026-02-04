using BrandVue.SourceData.Calculation;

namespace BrandVue.Models
{
    public class Period
    {
        public string Average { get; set; }

        public CalculationPeriodSpan[] ComparisonDates { get; set; }
    }
}
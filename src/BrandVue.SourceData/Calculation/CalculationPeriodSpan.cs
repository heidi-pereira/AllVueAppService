using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using NJsonSchema.Annotations;

namespace BrandVue.SourceData.Calculation
{
    public class CalculationPeriodSpan
    {
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        [CanBeNull] public string Name { get; set; }

        public override string ToString()
        {
            return StartDate.ToString("yyyy-MM-dd");
        }
    }
}
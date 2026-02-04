using BrandVue.Services;

namespace BrandVue.Models
{
    public class CrosstabCalculationParameters
    {
        public string CalculationId { get; set; }
        public string DisplayName { get; set; }
        public ResultsProviderParameters LegacyCalculationParameters { get; set; }
    }
}

using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.Calculation
{
    public interface IProfileResultsCalculator
    {
        IEnumerable<CategoryResult> GetResults(IReadOnlyCollection<Measure> measures, string subsetId,
            CalculationPeriodSpan[] comparisonDates,
            string averageName, int[] brandsToIncludeInAverage, int activeBrand, string requestScopeAuthCompany);
    }
}
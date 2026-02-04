using BrandVue.SourceData.QuotaCells;

namespace BrandVue.SourceData.Weightings.Rim
{
    public interface IRimWeightingCalculator
    {
        RimWeightingCalculationResult Calculate(
            IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellToSampleSize,
            Dictionary<string, Dictionary<int, double>> rimDimensions, bool includeQuotaDetails);
    }
}
namespace BrandVue.SourceData.QuotaCells
{
    public static class WeightingHelper
    {
        public static IReadOnlyDictionary<string, double> ConvertScaleFactorsToTargetWeights(
            IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellSampleSizes, IReadOnlyDictionary<string, double> scaleFactors,
            double totalSampleSize)
        {
            var cellSampleSizes = Join(quotaCellSampleSizes, scaleFactors).ToList();
            return ConvertScaleFactorsToTargetWeights(cellSampleSizes, totalSampleSize);
        }

        public static IReadOnlyDictionary<string, double> ConvertScaleFactorsToTargetWeights(
            IReadOnlyList<(QuotaCell QuotaCell, double SampleSize, double ScaleFactor)> cellSampleSizes,
            double totalSampleSize)
        {
            var results = new Dictionary<string, double>(cellSampleSizes.Count);
            foreach (var quotaCellSampleSize in cellSampleSizes)
            {
                var quotaKey = quotaCellSampleSize.QuotaCell.ToString();
                results.Add(quotaKey, quotaCellSampleSize.ScaleFactor * quotaCellSampleSize.SampleSize / totalSampleSize);
            }

            return results;
        }

        private static IEnumerable<(QuotaCell QuotaCell, double SampleSize, double ScaleFactor)> Join(IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellSampleSizes, IReadOnlyDictionary<string, double> scaleFactors)
        {
            return quotaCellSampleSizes.Select(c =>
                (c.QuotaCell, c.SampleSize, ScaleFactor: scaleFactors[c.QuotaCell.ToString()]));
        }
    }
}
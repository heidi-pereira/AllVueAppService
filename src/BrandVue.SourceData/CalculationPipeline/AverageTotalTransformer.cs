using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class AverageTotalTransformer : ICalcTypeResponseValueTransformer
    {
        private readonly Measure _measure;
        public AverageTotalTransformer(Measure measure) => _measure = measure;

        public int? Transform(int? primaryFieldValue,
            int? secondaryFieldValue)
        {
            int? ret = null;

            if (_measure.HasCustomFieldExpression)
            {
                if (primaryFieldValue.HasValue)
                {
                    ret = primaryFieldValue;
                }
            }
            else if (_measure.IsValidPrimaryValue(primaryFieldValue))
            {
                ret =  primaryFieldValue;
            }

            if (_measure.FieldOperation is FieldOperation.Minus or FieldOperation.Plus)
            {
                int multiplier = _measure.FieldOperation == FieldOperation.Minus ? -1 : 1;
                bool secondaryTrueValuesContains = secondaryFieldValue.HasValue &&
                                                   _measure.LegacySecondaryTrueValues.Contains(secondaryFieldValue);
                if (secondaryTrueValuesContains)
                {
                    ret = (ret ?? 0) + secondaryFieldValue.Value * multiplier;
                }
            }
            return ret;
        }
    }
}

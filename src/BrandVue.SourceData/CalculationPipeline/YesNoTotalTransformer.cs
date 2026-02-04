using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class YesNoTotalTransformer : ICalcTypeResponseValueTransformer
    {
        private readonly Measure _measure;
        public YesNoTotalTransformer(Measure measure) => _measure = measure;

        public int? Transform(int? primaryFieldValue,
            int? secondaryFieldValue)
        {
            int ret = 0;
            bool primaryIsTrue = PrimaryIsTrue(primaryFieldValue);
            
            if (primaryIsTrue)
            {
                ret++;
            }

            switch (_measure.FieldOperation)
            {
                case FieldOperation.Or:
                case FieldOperation.Plus:
                    if (!primaryIsTrue &&
                        _measure.LegacySecondaryTrueValues.Contains(secondaryFieldValue))
                    {
                        ++ret;
                    }

                    break;

                case FieldOperation.Minus:
                    if (_measure.LegacySecondaryTrueValues.Contains(secondaryFieldValue))
                    {
                        --ret;
                    }

                    break;
            }

            return ret;
        }

        private bool PrimaryIsTrue(int? primaryFieldValue)
        {
            if (_measure.HasCustomFieldExpression)
            {
                return primaryFieldValue.HasValue && primaryFieldValue != 0;
            }
            return _measure.IsValidPrimaryValue(primaryFieldValue);
        }
    }
}
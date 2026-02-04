using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Measures;

namespace BrandVue.SourceData.CalculationPipeline
{
    internal class NetPromoterScoreTotalTransformer : ICalcTypeResponseValueTransformer
    {
        private const int MinimumNetPromotorResponse = 0;
        private const int MaximumNetPromotorResponse = 10;
        private const int MaximumDetractorScore = 6;
        private const int MinimumPromotorScore = 9;

        private readonly Measure _measure;
        public NetPromoterScoreTotalTransformer(Measure measure) => _measure = measure;

        public int? Transform(int? primaryFieldValue,
            int? secondaryFieldValue)
        {
            if (IsValidNetPromotorInput(primaryFieldValue))
            {
                if (IsDetractor(primaryFieldValue))
                {
                    return -1;
                }
                else if (IsPromotor(primaryFieldValue))
                {
                    return 1;
                }

                return 0;
            }
            return null;
        }

        private bool IsValidNetPromotorInput(int? putativeNetPromoterInput)
        {
            return putativeNetPromoterInput >= MinimumNetPromotorResponse
                   && putativeNetPromoterInput <= MaximumNetPromotorResponse;
        }

        private bool IsDetractor(int? netPromotorInput)
        {
            return netPromotorInput >= MinimumNetPromotorResponse
                   && netPromotorInput <= MaximumDetractorScore;
        }

        private bool IsPromotor(int? netPromotorInput)
        {
            return netPromotorInput >= MinimumPromotorScore
                   && netPromotorInput <= MaximumNetPromotorResponse;
        }
    }
}

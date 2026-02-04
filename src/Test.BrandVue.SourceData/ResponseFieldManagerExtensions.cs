using System;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;

namespace Test.BrandVue.SourceData
{
    public static class ResponseFieldManagerExtensions
    {
        public static Measure CreateYesNoMeasure(this ResponseFieldManager responseFieldManager,
            string unbiasedRelativeSizesMeasureName, int[] baseVals = null, int[] vals = null,
            DateTimeOffset? startDate = null)
        {
            var field = responseFieldManager.Get(unbiasedRelativeSizesMeasureName);
            baseVals = baseVals ?? new[] { 5, 6 };
            vals = vals ?? new[] { 6 };
            return new Measure
            {
                Name = unbiasedRelativeSizesMeasureName,
                BaseField = field,
                LegacyBaseValues = { Values = baseVals },
                Field = field,
                LegacyPrimaryTrueValues = { Values = vals },
                StartDate = startDate,
                CalculationType = CalculationType.YesNo
            };
        }
    }
}
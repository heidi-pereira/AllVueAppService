using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Weightings.Rim;

namespace TestCommon.Weighting
{
    public class RimTestData
    {
        public IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> QuotaCellSampleSizesInIndexOrder { get; }
        public Dictionary<string, Dictionary<int, double>> RimDimensions { get; }
        public RimWeightingCalculationResult RimWeightingCalculationResult { get; }

        public RimTestData(IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> quotaCellSampleSizesInIndexOrder, Dictionary<string, Dictionary<int, double>> rimDimensions, RimWeightingCalculationResult rimWeightingCalculationResult)
        {
            QuotaCellSampleSizesInIndexOrder = quotaCellSampleSizesInIndexOrder;
            RimDimensions = rimDimensions;
            RimWeightingCalculationResult = rimWeightingCalculationResult;
        }

        public override string ToString() => 
            $"{QuotaCellSampleSizesInIndexOrder.Count} quota cells";
    }
}
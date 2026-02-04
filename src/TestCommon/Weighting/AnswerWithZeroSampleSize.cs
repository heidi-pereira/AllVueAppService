using System.Collections.Generic;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings.Rim;

namespace TestCommon.Weighting
{
    internal class AnswerWithZeroSampleSize
    {
        internal static RimTestData CreateTestData() => new(QuotaCellsToSampleSizes(), RimTargets(), ExpectedReport());

        private static IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> QuotaCellsToSampleSizes()
        {
            var ukSubset = new Subset { Id = "UK" };
            // There are no people who answered A1 to question A
            return new List<(QuotaCell QuotaCell, double SampleSize)>
            {
                ( new QuotaCell(0, ukSubset, new Dictionary<string, int> { { "A", 1 }, { "B", 1 } }) { Index = 0 }, 0 ),
                ( new QuotaCell(1, ukSubset, new Dictionary<string, int> { { "A", 1 }, { "B", 2 } }) { Index = 1 }, 0 ),
                ( new QuotaCell(2, ukSubset, new Dictionary<string, int> { { "A", 2 }, { "B", 1 } }) { Index = 2 }, 45 ),
                ( new QuotaCell(3, ukSubset, new Dictionary<string, int> { { "A", 2 }, { "B", 2 } }) { Index = 3 }, 57 )
            };
        }
        
        private static Dictionary<string, Dictionary<int, double>> RimTargets() =>
            new()
            {
                {"A", new Dictionary<int, double> {{1, 40}, {2, 60}}},
                {"B", new Dictionary<int, double> {{1, 50}, {2, 50}}}
            };
        
        private static RimWeightingCalculationResult ExpectedReport() =>
            new(0.877193, 1.1111112, 0.98615915, true, 2, new List<QuotaWeightingDetails>
            {
                new("1:1", 0, 1, 0),
                new("1:2", 0, 1, 0),
                new("2:1", 45, 1.1111112, 0.4901961),
                new("2:2", 57, 0.877193, 0.49019608)
            });
    }
}

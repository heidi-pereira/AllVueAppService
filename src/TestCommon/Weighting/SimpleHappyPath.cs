using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings.Rim;

namespace TestCommon.Weighting
{
    internal class SimpleHappyPath
    {
        internal static RimTestData CreateTestData() => new(QuotaCellsToSampleSizes(), RimTargets(), ExpectedReport());

        private static IReadOnlyList<(QuotaCell QuotaCell, double SampleSize)> QuotaCellsToSampleSizes()
        {
            var ukSubset = new Subset { Id = "UK" };

            return new List<(QuotaCell QuotaCell, double SampleSize)>
            {
                ( new QuotaCell(0, ukSubset, new Dictionary<string, int> { { "A", 1 }, { "B", 1 } }) { Index = 0 }, 20 ),
                ( new QuotaCell(1, ukSubset, new Dictionary<string, int> { { "A", 2 }, { "B", 1 } }) { Index = 1 }, 50 ),
                ( new QuotaCell(2, ukSubset, new Dictionary<string, int> { { "A", 3 }, { "B", 1 } }) { Index = 2 }, 100 ),
                ( new QuotaCell(3, ukSubset, new Dictionary<string, int> { { "A", 4 }, { "B", 1 } }) { Index = 3 }, 30 ),
                ( new QuotaCell(4, ukSubset, new Dictionary<string, int> { { "A", 1 }, { "B", 2 } }) { Index = 4 }, 40 ),
                ( new QuotaCell(5, ukSubset, new Dictionary<string, int> { { "A", 2 }, { "B", 2 } }) { Index = 5 }, 140 ),
                ( new QuotaCell(6, ukSubset, new Dictionary<string, int> { { "A", 3 }, { "B", 2 } }) { Index = 6 }, 50 ),
                ( new QuotaCell(7, ukSubset, new Dictionary<string, int> { { "A", 4 }, { "B", 2 } }) { Index = 7 }, 100 ),
                ( new QuotaCell(8, ukSubset, new Dictionary<string, int> { { "A", 1 }, { "B", 3 } }) { Index = 8 }, 40 ),
                ( new QuotaCell(9, ukSubset, new Dictionary<string, int> { { "A", 2 }, { "B", 3 } }) { Index = 9 }, 310 ),
                ( new QuotaCell(10, ukSubset, new Dictionary<string, int> { { "A", 3 }, { "B", 3 } }) { Index = 10 }, 50 ),
                ( new QuotaCell(11, ukSubset, new Dictionary<string, int> { { "A", 4 }, { "B", 3 } }) { Index = 11 }, 70 )
            };
        }
        
        private static Dictionary<string, Dictionary<int, double>> RimTargets() =>
            new()
            {
                {"A", new Dictionary<int, double> {{1, 175}, {2, 550}, {3, 430}, {4, 345}}},
                {"B", new Dictionary<int, double> {{1, 365}, {2, 415}, {3, 720}}}
          };
        
        private static RimWeightingCalculationResult ExpectedReport() =>
            new(0.86936, 2.44585, 0.90936, true, 6, new List<QuotaWeightingDetails>
            {
                new("1:1", 20, 1.81082, 0.036216144),
                new("2:1", 50, 1.08358, 0.054178346),
                new("3:1", 100, 2.19607, 0.21960866),
                new("4:1", 30, 1.83324, 0.054996833),
                new("1:2", 40, 1.45282, 0.05811288),
                new("2:2", 140, 0.86936, 0.12170937),
                new("3:2", 50, 1.76192, 0.08809671),
                new("4:2", 100, 1.47081, 0.14708103),
                new("1:3", 40, 2.01677, 0.080671),
                new("2:3", 310, 1.20682, 0.37411287),
                new("3:3", 50, 2.44585, 0.12229389),
                new("4:3", 70, 2.04175, 0.14292224)
            });
    }
}

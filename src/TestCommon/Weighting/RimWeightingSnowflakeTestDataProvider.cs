using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace TestCommon.Weighting
{
    public static class RimWeightingSnowflakeTestDataProvider
    {
        public static IEnumerable<object[]> GetTestCaseData()
        {
            yield return new object[] { SimpleHappyPath() };
            yield return new object[] { AnswerWithZeroSampleSize() };
            yield return new object[] { SlowManyDimensionalPath() };
        }

        public static object SimpleHappyPath()
        {
            // Quota cells and sample sizes
            var quotaCells = new[]
            {
                new { quota_cell_key = "A_1_B_1", sample_size = 20 },
                new { quota_cell_key = "A_2_B_1", sample_size = 50 },
                new { quota_cell_key = "A_3_B_1", sample_size = 100 },
                new { quota_cell_key = "A_4_B_1", sample_size = 30 },
                new { quota_cell_key = "A_1_B_2", sample_size = 40 },
                new { quota_cell_key = "A_2_B_2", sample_size = 140 },
                new { quota_cell_key = "A_3_B_2", sample_size = 50 },
                new { quota_cell_key = "A_4_B_2", sample_size = 100 },
                new { quota_cell_key = "A_1_B_3", sample_size = 40 },
                new { quota_cell_key = "A_2_B_3", sample_size = 310 },
                new { quota_cell_key = "A_3_B_3", sample_size = 50 },
                new { quota_cell_key = "A_4_B_3", sample_size = 70 }
            };
            // Rim targets
            var rimDimensions = new Dictionary<string, Dictionary<string, double>>
            {
                {"A", new Dictionary<string, double> {{"1", 175}, {"2", 550}, {"3", 430}, {"4", 345}}},
                {"B", new Dictionary<string, double> {{"1", 365}, {"2", 415}, {"3", 720}}}
            };
            // Expected result (as returned by Snowflake procedure)
            var expected = new {
                min_weight = 0.86936,
                max_weight = 2.44585,
                efficiency_score = 0.90936,
                converged = true,
                iterations_required = 6,
                quota_details = new[]
                {
                    new { quota_cell_key = "A_1_B_1", sample_size = 20.0, scale_factor = 1.81082, target = 0.036216144 },
                    new { quota_cell_key = "A_2_B_1", sample_size = 50.0, scale_factor = 1.08358, target = 0.054178346 },
                    new { quota_cell_key = "A_3_B_1", sample_size = 100.0, scale_factor = 2.19607, target = 0.21960866 },
                    new { quota_cell_key = "A_4_B_1", sample_size = 30.0, scale_factor = 1.83324, target = 0.054996833 },
                    new { quota_cell_key = "A_1_B_2", sample_size = 40.0, scale_factor = 1.45282, target = 0.05811288 },
                    new { quota_cell_key = "A_2_B_2", sample_size = 140.0, scale_factor = 0.86936, target = 0.12170937 },
                    new { quota_cell_key = "A_3_B_2", sample_size = 50.0, scale_factor = 1.76192, target = 0.08809671 },
                    new { quota_cell_key = "A_4_B_2", sample_size = 100.0, scale_factor = 1.47081, target = 0.14708103 },
                    new { quota_cell_key = "A_1_B_3", sample_size = 40.0, scale_factor = 2.01677, target = 0.080671 },
                    new { quota_cell_key = "A_2_B_3", sample_size = 310.0, scale_factor = 1.20682, target = 0.37411287 },
                    new { quota_cell_key = "A_3_B_3", sample_size = 50.0, scale_factor = 2.44585, target = 0.12229389 },
                    new { quota_cell_key = "A_4_B_3", sample_size = 70.0, scale_factor = 2.04175, target = 0.14292224 }
                }
            };
            return new { quota_cells = quotaCells, rim_dimensions = rimDimensions, expected };
        }

        public static object AnswerWithZeroSampleSize()
        {
            var quotaCells = new[]
            {
                new { quota_cell_key = "A_1_B_1", sample_size = 0 },
                new { quota_cell_key = "A_1_B_2", sample_size = 0 },
                new { quota_cell_key = "A_2_B_1", sample_size = 45 },
                new { quota_cell_key = "A_2_B_2", sample_size = 57 }
            };
            var rimDimensions = new Dictionary<string, Dictionary<string, double>>
            {
                {"A", new Dictionary<string, double> {{"1", 40}, {"2", 60}}},
                {"B", new Dictionary<string, double> {{"1", 50}, {"2", 50}}}
            };
            var expected = new {
                min_weight = 0.877193,
                max_weight = 1.1111112,
                efficiency_score = 0.98615915,
                converged = true,
                iterations_required = 2,
                quota_details = new[]
                {
                    new { quota_cell_key = "A_1_B_1", sample_size = 0.0, scale_factor = 1.0, target = 0.0 },
                    new { quota_cell_key = "A_1_B_2", sample_size = 0.0, scale_factor = 1.0, target = 0.0 },
                    new { quota_cell_key = "A_2_B_1", sample_size = 45.0, scale_factor = 1.1111112, target = 0.4901961 },
                    new { quota_cell_key = "A_2_B_2", sample_size = 57.0, scale_factor = 0.877193, target = 0.49019608 }
                }
            };
            return new { quota_cells = quotaCells, rim_dimensions = rimDimensions, expected };
        }

        public static object SlowManyDimensionalPath()
        {
            var baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Weighting", "test-weighting-data");
            var quotaCellSamples = JsonConvert.DeserializeObject<Dictionary<string, double>>(File.ReadAllText(Path.Combine(baseDir, "cell-sample-sizes.json")));
            var rimDimensions = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(File.ReadAllText(Path.Combine(baseDir, "rim-weighting-scheme.json")));
            var expected = JsonConvert.DeserializeObject<object>(File.ReadAllText(Path.Combine(baseDir, "weighting-results.json")));
            var quotaCells = quotaCellSamples.Select(kvp => new { quota_cell_key = kvp.Key, sample_size = kvp.Value }).ToArray();
            return new { quota_cells = quotaCells, rim_dimensions = rimDimensions, expected };
        }
    }
}


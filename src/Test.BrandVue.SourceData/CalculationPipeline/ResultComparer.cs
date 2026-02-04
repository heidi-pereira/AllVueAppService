using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.ResponseRepository;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Helper methods for formatting and comparing test results
    /// </summary>
    public static class ResultComparer
    {
        public static string ConvertToCsv(WeightedWordCount[] results)
        {
            if (!results.Any())
            {
                return "Text,Result,UnweightedResult\n";
            }

            var lines = new List<string> { "Text,Result,UnweightedResult" };
            foreach (var result in results)
            {
                // Escape text if it contains commas or quotes
                var escapedText = result.Text.Contains(",") || result.Text.Contains("\"")
                    ? $"\"{result.Text.Replace("\"", "\"\"")}\""
                    : result.Text;
                lines.Add($"{escapedText},{result.Result},{result.UnweightedResult}");
            }

            return string.Join("\n", lines);
        }

        public static ComparisonResult CompareResults(
            WeightedWordCount[] snowflakeResults,
            WeightedWordCount[] sqlServerResults,
            string testDataName)
        {
            // Handle duplicates by grouping and summing
            var snowflakeLookup = snowflakeResults
                .GroupBy(r => r.Text)
                .ToDictionary(
                    g => g.Key,
                    g => new WeightedWordCount
                    {
                        Text = g.Key,
                        Result = g.Sum(r => r.Result),
                        UnweightedResult = g.Sum(r => r.UnweightedResult)
                    });

            var sqlServerLookup = sqlServerResults
                .GroupBy(r => r.Text)
                .ToDictionary(
                    g => g.Key,
                    g => new WeightedWordCount
                    {
                        Text = g.Key,
                        Result = g.Sum(r => r.Result),
                        UnweightedResult = g.Sum(r => r.UnweightedResult)
                    });

            var differences = new List<string>();
            var tolerance = 0.001; // Allow small floating point differences

            // Report if duplicates were found
            var snowflakeDuplicates = snowflakeResults.GroupBy(r => r.Text).Where(g => g.Count() > 1).ToList();
            var sqlServerDuplicates = sqlServerResults.GroupBy(r => r.Text).Where(g => g.Count() > 1).ToList();
            
            if (snowflakeDuplicates.Any())
            {
                differences.Add($"WARNING: Snowflake has duplicate text entries: {string.Join(", ", snowflakeDuplicates.Select(g => $"'{g.Key}' ({g.Count()}x)"))}");
            }
            
            if (sqlServerDuplicates.Any())
            {
                differences.Add($"WARNING: SQL Server has duplicate text entries: {string.Join(", ", sqlServerDuplicates.Select(g => $"'{g.Key}' ({g.Count()}x)"))}");
            }

            // Check for items in Snowflake not in SQL Server
            foreach (var text in snowflakeLookup.Keys.Except(sqlServerLookup.Keys))
            {
                var item = snowflakeLookup[text];
                differences.Add($"Only in Snowflake: '{text}' (Result: {item.Result}, Unweighted: {item.UnweightedResult})");
            }

            // Check for items in SQL Server not in Snowflake
            foreach (var text in sqlServerLookup.Keys.Except(snowflakeLookup.Keys))
            {
                var item = sqlServerLookup[text];
                differences.Add($"Only in SQL Server: '{text}' (Result: {item.Result}, Unweighted: {item.UnweightedResult})");
            }

            // Check for differences in common items
            foreach (var text in snowflakeLookup.Keys.Intersect(sqlServerLookup.Keys))
            {
                var snowflake = snowflakeLookup[text];
                var sqlServer = sqlServerLookup[text];

                if (Math.Abs(snowflake.Result - sqlServer.Result) > tolerance)
                {
                    differences.Add($"Result mismatch for '{text}': Snowflake={snowflake.Result}, SqlServer={sqlServer.Result}");
                }

                if (snowflake.UnweightedResult != sqlServer.UnweightedResult)
                {
                    differences.Add($"UnweightedResult mismatch for '{text}': Snowflake={snowflake.UnweightedResult}, SqlServer={sqlServer.UnweightedResult}");
                }
            }

            return new ComparisonResult
            {
                TestDataName = testDataName,
                IsMatch = !differences.Any(),
                SnowflakeCount = snowflakeResults.Length,
                SqlServerCount = sqlServerResults.Length,
                Differences = differences
            };
        }

        public static string GenerateComparisonReport(ComparisonResult comparison)
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine($"Test: {comparison.TestDataName}");
            report.AppendLine($"Snowflake results: {comparison.SnowflakeCount}");
            report.AppendLine($"SQL Server results: {comparison.SqlServerCount}");
            report.AppendLine($"Differences found: {comparison.Differences.Count}");
            report.AppendLine();

            foreach (var diff in comparison.Differences.Take(50)) // Limit to first 50 differences
            {
                report.AppendLine($"  - {diff}");
            }

            if (comparison.Differences.Count > 50)
            {
                report.AppendLine($"  ... and {comparison.Differences.Count - 50} more differences");
            }

            return report.ToString();
        }
    }
}

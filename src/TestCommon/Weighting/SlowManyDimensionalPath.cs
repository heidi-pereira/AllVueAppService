using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Utils;
using BrandVue.SourceData.Weightings.Rim;

namespace TestCommon.Weighting
{
    internal class SlowManyDimensionalPath
    {
        private static QuotaCell ToQuotaCell(Dictionary<string, byte> sharedKeys, int id, Subset subset,
            IEnumerable<int> entityIdValues, int? filterInstanceId, WeightingScheme weightingScheme = null)
        {
            var namesWithCategoryInstances = ((int?)filterInstanceId).YieldNonNull().Concat(entityIdValues).ToArray();
            var fieldGroupToKeyPart = new FixedSharedKeysDictionary<string, byte, string>(sharedKeys, i => namesWithCategoryInstances[i].ToString());
            return new QuotaCell(id, subset, fieldGroupToKeyPart, weightingScheme?.FilterMetricEntityId);
        }
        private static IEnumerable<QuotaCell> CreateTargetQuotaCellsForSubset(WeightingScheme weightingScheme, Subset subset, string filterMetricName)
        {
            if (weightingScheme is null) return Enumerable.Empty<QuotaCell>();
            var dimensionVariables = weightingScheme.WeightingSchemeDetails.Dimensions.SelectMany(c => c.InterlockedVariableIdentifiers);
            var sharedKeys = filterMetricName.YieldNonNull().Concat(dimensionVariables).Select((k, i) => (k, i))
                .ToDictionary(kvp => kvp.k, kvp => (byte)kvp.i);
            return weightingScheme.WeightingSchemeDetails
                .Dimensions
                .Select(WeightingModelsExtensions.CategoriesForDimension)
                .CartesianProduct()
                .Select((categoryCombination, cellIndex) =>
                    ToQuotaCell(sharedKeys, cellIndex, subset, categoryCombination.SelectMany(x => x),
                        weightingScheme.FilterMetricEntityId, weightingScheme));
        }

        internal static RimTestData CreateTestData()
        {
            var outputDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var serializer = new JsonSerializer();
            var testDataDirectory = Path.Combine(outputDirectory, @"Weighting\test-weighting-data");
            using var quotaStringToSamplesFile = File.OpenText(Path.Combine(testDataDirectory, "cell-sample-sizes.json"));
            var quotaStringToSamples = (Dictionary<string, double>)serializer.Deserialize(quotaStringToSamplesFile, typeof(Dictionary<string, double>));
            var totalSampleSize = quotaStringToSamples.Sum(s => s.Value);
            using var dimensionTargetsFile = File.OpenText(Path.Combine(testDataDirectory, "rim-weighting-scheme.json"));
            var weightingScheme = (WeightingScheme)serializer.Deserialize(dimensionTargetsFile, typeof(WeightingScheme));
            var cells = Indexed(CreateTargetQuotaCellsForSubset(weightingScheme, null, null));
            var quotaCellToSampleSize = cells.Cells.Select(c => (c, quotaStringToSamples[c.ToString()])).ToList();
            var targets = weightingScheme?.WeightingSchemeDetails.Dimensions.ToDictionary(d => d.InterlockedVariableIdentifiers.Single(),
                d => d.CellKeyToTarget.ToDictionary(c => int.Parse(c.Key), c => decimal.ToDouble(c.Value) * totalSampleSize));
            using var calculationResultFile = File.OpenText(Path.Combine(testDataDirectory, "weighting-results.json")); 
            var expectedReport = (RimWeightingCalculationResult)serializer.Deserialize(calculationResultFile, typeof(RimWeightingCalculationResult));
            return new RimTestData(quotaCellToSampleSize, targets, expectedReport);
        }

        private static IGroupedQuotaCells Indexed(IEnumerable<QuotaCell> quotaCells)
        {
            var cells = quotaCells.ToArray();
            for (int index = 0; index < cells.Length; index++)
            {
                cells[index].Index = index;
            }

            return GroupedQuotaCells.CreateUnfiltered(cells);
        }
    }
}

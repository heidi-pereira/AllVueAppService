using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.ResponseRepository;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.CalculationPipeline
{
    /// <summary>
    /// Helper methods for loading and parsing test data from JSON files
    /// </summary>
    public static class TestDataLoader
    {
        public static IEnumerable<TestCaseData> GetTestDataFromJsonFiles()
        {
            var inputsFolder = Path.Combine(TestContext.CurrentContext.TestDirectory, "CalculationPipeline", "Inputs");
            
            if (!Directory.Exists(inputsFolder))
            {
                yield break; // No inputs folder, no test cases
            }

            var jsonFiles = Directory.GetFiles(inputsFolder, "*.json");
            
            foreach (var jsonFile in jsonFiles)
            {
                var jsonContent = File.ReadAllText(jsonFile);
                var testData = JsonSerializer.Deserialize<TestDataModel>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (testData == null)
                {
                    continue;
                }

                // Convert JSON models to actual types
                var responseWeights = testData.ResponseWeights
                    .Select(rw => new ResponseWeight(rw.ResponseId, rw.Weight))
                    .ToArray();

                var filters = testData.Filters
                    .Select(f => (ParseDbLocation(f.Location), f.Id))
                    .ToArray();

                var testName = Path.GetFileNameWithoutExtension(jsonFile);
                
                yield return new TestCaseData(
                    testData.TestDataName,
                    responseWeights,
                    testData.VarCodeBase,
                    filters
                ).SetName(testName);
            }
        }

        private static DbLocation ParseDbLocation(string location)
        {
            return location switch
            {
                "SectionEntity" => DbLocation.SectionEntity,
                "PageEntity" => DbLocation.PageEntity,
                "QuestionEntity" => DbLocation.QuestionEntity,
                _ => throw new ArgumentException($"Unknown DbLocation: {location}")
            };
        }
    }
}

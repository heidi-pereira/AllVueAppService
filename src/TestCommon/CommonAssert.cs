using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Weightings.Rim;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace TestCommon
{
    public static class CommonAssert
    {
        public static void DoesNotReferenceOtherTestProjects()
        {
            var referencedTestAssemblies = Assembly.GetCallingAssembly().GetReferencedAssemblies().Where(a => a.Name.StartsWith("Test."));
            Assert.That(referencedTestAssemblies.Select(a => a.Name), Is.Empty, "Referencing from test assembly to test assembly causes TeamCity to run all tests twice since the referenced assembly gets copied. Please put common code in TestCommon.");
        }

        public static TargetInstances AssertEntityFoundInRepository(IEntityRepository entityRepository, Subset subset, EntityType entityType, string entityName)
        {
            EntityInstance brand = entityRepository
                .GetInstancesOf(entityType.Identifier, subset)
                .FirstOrDefault(instance => instance.Name.Equals(entityName));
            Assert.That(brand != null && !brand.Equals(default) && brand.Name.Equals(entityName), Is.True, $"Brand '{entityName}' is missing");
            return new TargetInstances(entityType, new[] { brand });
        }

        public static void AssertDoubleEqualWithinErrorMargin(double expected, double actual,
            double errorMargin = RimWeightingCalculator.PointTolerance)
        {
            Assert.That(Math.Abs(expected - actual), Is.LessThan(errorMargin), $"Actual value {actual} was different from value {expected} by more than accepted margin.");
        }

        public static void AssertResult(IEnumerable<WeightedDailyResult> weightedDailyResults, IResolveConstraint resultConstraint, int expectedSample, string messageContext)
        {
            var responseIdToSeenCount = new Dictionary<int, int>();
            foreach (var result in weightedDailyResults)
            {
                Assert.That(result.WeightedResult, resultConstraint, messageContext);

                Assert.That(result.UnweightedSampleSize, Is.EqualTo(expectedSample),
                    $"Calculated unweighted sample size mismatch {messageContext}.");

                Assert.That(result.WeightedSampleSize, Is.EqualTo(expectedSample),
                    $"Calculated weighted sample size mismatch {messageContext}.");

                Assert.That(result.ResponseIdsForDay.Count, Is.EqualTo(expectedSample),
                    $"Calculated response id count mismatch {messageContext}.");

                foreach (var responseId in result.ResponseIdsForDay)
                {
                    var currentCount = responseIdToSeenCount.TryGetValue(responseId, out var count) ? count : 0;
                    var newCount = responseIdToSeenCount[responseId] = currentCount + 1;
                    Assert.That(newCount, Is.LessThanOrEqualTo(expectedSample));
                }
            }
        }


        public static void NoDuplicatedTestNames(IEnumerable<TestCaseData> testCases)
        {
            var duplicatedNames = testCases.ToLookup(t => t.TestName).Where(g => g.Count() > 1)
                .Select(t => new { TestName = t.Key, Count = t.Count() })
                .ToArray();
            Assert.That(duplicatedNames, Is.Empty);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NUnit.Framework;
using TestCommon;
using TestCommon.Weighting;

namespace Test.BrandVue.SourceData.Weightings
{
    [TestFixture]
    public class WeightingPlanRepositoryTests
    {        
        private void WriteToDatabase(IDbContextFactory<MetaDataContext> dbContextFactory, string productCode, string subProductId, IEnumerable<WeightingPlanConfiguration> weightingPlanConfigurations)
        {
            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(dbContextFactory);
            foreach (var plan in weightingPlanConfigurations)
            {
                weightingPlanRepository.CreateWeightingPlan(productCode, subProductId, plan);
            }
        }

        [Test]
        public void TestDatabaseDeleteBySubset()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan();
            var firstPlan = weightingPlansForDatabase.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabase);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);

            weightingPlanRepository.DeleteWeightingPlanForSubset(firstPlan.ProductShortCode, firstPlan.SubProductId, firstPlan.SubsetId);


            Assert.That(weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId).Count, Is.EqualTo(0));
        }

        [Test]
        public void TestDatabaseDeleteBySubsetWithMultipleSubsets()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var weightingPlansForDatabaseSubset1 = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan();
            var weightingPlansForDatabaseSubset2 = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan(subsetId: "Subset2");
            var firstPlan = weightingPlansForDatabaseSubset1.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabaseSubset1);
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabaseSubset2);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);

            weightingPlanRepository.DeleteWeightingPlanForSubset(firstPlan.ProductShortCode, firstPlan.SubProductId, firstPlan.SubsetId);

            var plansLeft = weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId);
            Assert.That(plansLeft.Count, Is.EqualTo(2));
            Assert.That(plansLeft.Select(x => x.SubsetId).Where(x => x.Equals("Subset2")).Count, Is.EqualTo(2));
        }

        [Test]
        public void TestDatabaseGetBySubsetWithMultipleSubsets()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var weightingPlansForDatabaseSubset1 = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan(subsetId: "subset1");
            var weightingPlansForDatabaseSubset2 = WeightingPlanConfigurationsTestObjects.FiveWeightingPlans(subsetId: "subset2");
            var firstPlan = weightingPlansForDatabaseSubset1.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabaseSubset1);
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabaseSubset2);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);

            var result = weightingPlanRepository.GetWeightingPlansBySubsetId(firstPlan.ProductShortCode, firstPlan.SubProductId);

            Assert.That(result.Count(), Is.EqualTo(2), "Wrong number of results");
            Assert.That(result.First().plans.Count(), Is.EqualTo(2));
            Assert.That(result.Last().plans.Count(), Is.EqualTo(1));

            var jsonOriginal = JsonConvert.SerializeObject(UiWeightingConfigurationRoot.ToUIWeightingRoots(weightingPlansForDatabaseSubset2.ToList()));
            var jsonFoundByDatabase = JsonConvert.SerializeObject(UiWeightingConfigurationRoot.ToUIWeightingRoots(result.Last().plans));
            Assert.That(jsonFoundByDatabase, Is.EqualTo(jsonOriginal));
        }

        [Test]
        public void TestDatabaseDeleteById()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan();
            var firstPlan = weightingPlansForDatabase.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabase);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);


            var plansFromDatabase = weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId);

            var planToDelete = plansFromDatabase.First().Id;

            weightingPlanRepository.DeleteWeightingPlan(firstPlan.ProductShortCode, firstPlan.SubProductId, planToDelete);

            var plansAfterDelete = weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId);
            Assert.That(plansAfterDelete.Count, Is.EqualTo(1));
            Assert.That(plansAfterDelete.First().Id, Is.Not.EqualTo(planToDelete));
        }

        static IEnumerable<object[]> AllPlans()
        {
            yield return new object[]
            { nameof(WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan),
                WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan() };

            yield return new object[]
            { nameof(WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy), WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy() };

            yield return new object[]
            { nameof(WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy), WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy() };

            yield return new object[]
            { nameof(WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategy), WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategy()};

            yield return new object[]
{ nameof(WeightingPlanConfigurationsTestObjects.FiveWeightingPlans), WeightingPlanConfigurationsTestObjects.FiveWeightingPlans()};

        }


        [TestCaseSource(nameof(AllPlans))]
        public void TestCloning(string planName, IEnumerable<WeightingPlanConfiguration> plan)
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);

            var weightingPlansForDatabase = plan; // ;
            var expected = UiWeightingConfigurationRoot.ToUIWeightingRoots(weightingPlansForDatabase.ToArray()).First();

            var firstPlan = weightingPlansForDatabase.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabase);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);
            var plansFromDatabase = weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId);

            var root = UiWeightingConfigurationRoot.ToUIWeightingRoots(plansFromDatabase).FirstOrDefault();

            var cloned = root.CloneTreeFor("MySubset");

            var jsonClone = JsonConvert.SerializeObject(cloned.UiWeightingPlans);
            var jsonOriginal = JsonConvert.SerializeObject(expected.UiWeightingPlans);
            Assert.That(cloned.SubsetId, Is.EqualTo("MySubset"));
            Assert.That(jsonClone, Is.EqualTo(jsonOriginal));
        }

        [TestCase("DecemberWeeksWaveVariable", 1, "Gender")]
        public void UpdateRim_DeleteVariableFromFirstWave(string waveVariable, int instanceId, string variableToDelete)
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy();
            var firstPlan = weightingPlansForDatabase.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabase);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);
            var plansFromDatabase = weightingPlanRepository.GetWeightingPlansForSubset(firstPlan.ProductShortCode, firstPlan.SubProductId, firstPlan.SubsetId);
            var wavePlan = plansFromDatabase.Single(p => p.VariableIdentifier == waveVariable);
            var waveInstance = wavePlan.ChildTargets.First( x=>x.EntityInstanceId == instanceId);
            var rimPlansForWave = waveInstance.ChildPlans;
            var rimPlanToRemove = rimPlansForWave.First(x=> x.VariableIdentifier == variableToDelete);
            rimPlansForWave.RemoveAll( x=> x.Id == rimPlanToRemove.Id);
            weightingPlanRepository.UpdateAllWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId, new[] { wavePlan });

            var plansAfterDelete = weightingPlanRepository.GetWeightingPlansForSubset(firstPlan.ProductShortCode, firstPlan.SubProductId, firstPlan.SubsetId);

            var wavePlan1 = plansAfterDelete.Single(p => p.VariableIdentifier == waveVariable);
            var waveInstance1 = wavePlan1.ChildTargets.First(x => x.EntityInstanceId == instanceId);
            var rimPlansForWave1 = waveInstance1.ChildPlans;

            Assert.That(rimPlansForWave1.Count(), Is.EqualTo(1));

            Assert.That(rimPlansForWave1.Any(x=> x.VariableIdentifier == variableToDelete), Is.False, $"Failed to delete plan for variable {variableToDelete}");
        }

        [TestCase("DecemberWeeksWaveVariable", 1, "Gender2")]
        public void UpdateRim_AddGenderFromFirstWave(string waveVariable, int instanceId, string variableAdd)
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var weightingPlansForDatabase = WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy();
            var firstPlan = weightingPlansForDatabase.First();
            WriteToDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabase);

            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);
            var plansFromDatabase = weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId);
            var wavePlan = plansFromDatabase.Single(p => p.VariableIdentifier == waveVariable);
            var waveInstance = wavePlan.ChildTargets.First(x => x.EntityInstanceId == instanceId);
            var rimPlansForWave = waveInstance.ChildPlans;
            var planToCopy = rimPlansForWave.First();
            var rimPlanToAdd = new WeightingPlanConfiguration
            {
                SubProductId = planToCopy.SubProductId,
                SubsetId = planToCopy.SubsetId,
                ParentTarget = planToCopy.ParentTarget,
                ParentWeightingTargetId = planToCopy.ParentWeightingTargetId,
                ProductShortCode = planToCopy.ProductShortCode,
                VariableIdentifier = variableAdd,
                ChildTargets = new List<WeightingTargetConfiguration> (planToCopy.ChildTargets),
            };
            rimPlansForWave.Add(rimPlanToAdd);


            weightingPlanRepository.UpdateWeightingPlan(firstPlan.ProductShortCode, firstPlan.SubProductId, wavePlan);

            var plansAfterUpdate = weightingPlanRepository.GetWeightingPlans(firstPlan.ProductShortCode, firstPlan.SubProductId);

            var wavePlan1 = plansFromDatabase.Single(p => p.VariableIdentifier == waveVariable);
            var waveInstance1 = wavePlan.ChildTargets.First(x => x.EntityInstanceId == instanceId);

            var rimPlansForWave1 = waveInstance1.ChildPlans;

            Assert.That(rimPlansForWave1.Count(), Is.EqualTo(3));
        }
    }
}

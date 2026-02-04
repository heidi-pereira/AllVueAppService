using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Weightings;
using BrandVue.Models;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Weightings;
using Newtonsoft.Json;
using NUnit.Framework;
using TestCommon;
using TestCommon.Weighting;
using VerifyNUnit;


namespace Test.BrandVue.SourceData.Weightings
{
    [TestFixture]
    public class UIWeightingPlanConfigurationsToWeightingPlans
    {
        private static readonly JsonSerializerSettings Settings = new() { NullValueHandling = NullValueHandling.Ignore, ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
       
        [Test]
        public void NonFilteredRimOnlyStrategy()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var result = ConvertPlanInAndOutofUIModel(testMetadataContextFactory, WeightingPlanConfigurationsTestObjects.GetSimpleRimWeightingPlan());
            Assert.That(result.originalWeightingPlanForDatabase, Is.EqualTo(result.convertedViaUIWeightingPlanForDatabase));

        }

        private (string originalWeightingPlanForDatabase, string convertedViaUIWeightingPlanForDatabase) ConvertPlanInAndOutofUIModel(TestMetadataContextFactoryInMemory testMetadataContextFactory, IEnumerable<WeightingPlanConfiguration> weightingPlansForDatabase)
        {
            var firstPlan = weightingPlansForDatabase.First();
            var roundTrippedPlansForDatabase = WriteThenReadFromDatabase(testMetadataContextFactory, firstPlan.ProductShortCode, firstPlan.SubProductId, weightingPlansForDatabase);
            var uiWeightingPlan = UiWeightingConfigurationRoot.ToUIWeightingRoots(roundTrippedPlansForDatabase.ToList()).First();
            var resultWeightingPlanForDatabase = uiWeightingPlan.ToWeightingPlanConfiguration(firstPlan.ProductShortCode, firstPlan.SubProductId);

            var originalWeightingPlanForDatabase = JsonConvert.SerializeObject(roundTrippedPlansForDatabase, Settings);
            var convertedViaUIWeightingPlanForDatabase = JsonConvert.SerializeObject(resultWeightingPlanForDatabase, Settings);

            CleanUp(testMetadataContextFactory, roundTrippedPlansForDatabase);

            return (originalWeightingPlanForDatabase, convertedViaUIWeightingPlanForDatabase);
        }

        [Test]
        public void FilteredRimOnlyStrategy()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);

            var result = ConvertPlanInAndOutofUIModel(testMetadataContextFactory,WeightingPlanConfigurationsTestObjects.FilteredRimOnlyStrategy());
            Assert.That(result.originalWeightingPlanForDatabase, Is.EqualTo(result.convertedViaUIWeightingPlanForDatabase));
        }

        [Test]
        public void NonFilteredTargetOnlyStrategy()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var result = ConvertPlanInAndOutofUIModel(testMetadataContextFactory, WeightingPlanConfigurationsTestObjects.NonFilteredTargetOnlyStrategy());
            Assert.That(result.originalWeightingPlanForDatabase, Is.EqualTo(result.convertedViaUIWeightingPlanForDatabase));
        }


        [Test]
        public void FilteredTargetOnlyStrategy()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var result = ConvertPlanInAndOutofUIModel(testMetadataContextFactory, WeightingPlanConfigurationsTestObjects.FilteredTargetOnlyStrategy());
            Assert.That(result.originalWeightingPlanForDatabase, Is.EqualTo(result.convertedViaUIWeightingPlanForDatabase));
        }

        [Test]
        public async Task NonFilteredRimToTargetStyleCharacterization()
        {
            var weightingPlanConfigurations = WeightingPlanConfigurationsTestObjects.NonFilteredRimToTargetStyleCharacterization();
            var weightingPlans = weightingPlanConfigurations.ToAppModel();

            var output = weightingPlans.ToQuotaCellTree();
            await Verifier.Verify(output);
        }

        [Test]
        public void FiveWeightingPlans()
        {
            TestMetadataContextFactoryInMemory testMetadataContextFactory = (TestMetadataContextFactoryInMemory)ITestMetadataContextFactory.Create(StorageType.InMemory);
            var result = ConvertPlanInAndOutofUIModel(testMetadataContextFactory, WeightingPlanConfigurationsTestObjects.FiveWeightingPlans());
            Assert.That(result.originalWeightingPlanForDatabase, Is.EqualTo(result.convertedViaUIWeightingPlanForDatabase));
        }


        private void CleanUp(TestMetadataContextFactoryInMemory testMetadataContextFactory, IEnumerable<WeightingPlanConfiguration> plans)
        {
            if (plans == null) return;
            
            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);
            foreach (var plan in plans)
            {
                weightingPlanRepository.DeleteWeightingPlan(plan.ProductShortCode, plan.SubProductId, plan.Id);
            }
        }

        private IEnumerable<WeightingPlanConfiguration> WriteThenReadFromDatabase(TestMetadataContextFactoryInMemory testMetadataContextFactory, string productCode, string subProductId, IEnumerable<WeightingPlanConfiguration> weightingPlanConfigurations)
        {
            WeightingPlanRepository weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);
            foreach (var plan in weightingPlanConfigurations)
            {
                weightingPlanRepository.CreateWeightingPlan(productCode, subProductId, plan);
            }
            return weightingPlanRepository.GetWeightingPlans(productCode, subProductId).Where( x=> !x.ParentWeightingTargetId.HasValue);
        }
    }
}

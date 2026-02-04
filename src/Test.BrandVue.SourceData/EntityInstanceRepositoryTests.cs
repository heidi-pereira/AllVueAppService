using System.Linq;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using Test.BrandVue.SourceData.Extensions;

namespace Test.BrandVue.SourceData 
{
    public class EntityInstanceRepositoryTests
    {
        [Test]
        public void ShouldGetInstanceById()
        {
            var testSubset = new Subset()
            {
                Id = "All"
            };
            
            var expectedDavid = new EntityInstance {Id = 1, Name = "Attenborough", Subsets = new [] {testSubset}};
            var expectedJames = new EntityInstance {Id = 4, Name = "Rodden", Subsets = new [] {testSubset}};

            var repository = new EntityInstanceRepository();
            repository.AddInstances("David", expectedDavid);
            repository.AddInstances("James", expectedJames);

            repository.TryGetInstance(testSubset, "David", 1, out var retrievedDavid);
            repository.TryGetInstance(testSubset, "James", 4, out var retrievedJames);

            Assert.That(retrievedDavid, Is.EqualTo(expectedDavid));
            Assert.That(retrievedJames, Is.EqualTo(expectedJames));
        }

        [Test]
        public void ShouldGetAllInstancesOfEntity()
        {
            var testSubset = new Subset()
            {
                Id = "All"
            };

            var expectedDavids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough", Subsets = new [] {testSubset}},
                new EntityInstance {Id = 2, Name = "Connell", Subsets = new [] {testSubset}},
            };

            var expectedJameseses = new[]
            {
                new EntityInstance {Id = 1, Name = "Rodden", Subsets = new [] {testSubset}},
                new EntityInstance {Id = 2, Name = "Hand", Subsets = new [] {testSubset}},
                new EntityInstance {Id = 3, Name = "Foster", Subsets = new [] {testSubset}},
            };

            var repository = new EntityInstanceRepository();
            repository.AddInstances("David", expectedDavids);
            repository.AddInstances("James", expectedJameseses);

            var retrievedDavids = repository.GetInstancesOf("David", testSubset);
            var retrievedJameses = repository.GetInstancesOf("James", testSubset);

            Assert.That(retrievedDavids, Is.EquivalentTo(expectedDavids));
            Assert.That(retrievedJameses, Is.EquivalentTo(expectedJameseses));
        }

        [Test]
        public void ShouldGetInstancesForSpecificSubset()
        {
            var ukSubset = new Subset
            {
                Id = "UK"
            };
            
            var ausSubset = new Subset
            {
                Id = "AUS"
            };
            
            var ukJameseses = new[]
            {
                new EntityInstance {Id = 1, Name = "Rodden", Subsets = new [] {ukSubset}},
                new EntityInstance {Id = 3, Name = "Foster", Subsets = new [] {ukSubset}},
            };
            
            var foreignJameseses = new[]
            {
                new EntityInstance {Id = 2, Name = "Hand", Subsets = new [] {ausSubset}},
            };

            var repository = new EntityInstanceRepository();
            repository.AddInstances("James", ukJameseses);
            repository.AddInstances("James", foreignJameseses);

            var retrievedJameses = repository.GetInstancesOf("James", ukSubset);

            Assert.That(retrievedJameses, Is.EquivalentTo(ukJameseses));
        }

        [Test]
        public void ShouldAlwaysGetInstancesWithNoSubsets()
        {
            var testSubset = new Subset()
            {
                Id = "All"
            };
            
            var ausSubset = new Subset
            {
                Id = "AUS"
            };

            var expectedJameseses = new[]
            {
                new EntityInstance {Id = 1, Name = "Rodden", Subsets = new [] {testSubset}},
                new EntityInstance {Id = 3, Name = "Foster"},
            };

            var foreignJameseses = new[]
            {
                new EntityInstance {Id = 2, Name = "Hand", Subsets = new [] {ausSubset}},
            };
            
            var repository = new EntityInstanceRepository();
            repository.AddInstances("James", expectedJameseses);
            repository.AddInstances("James", foreignJameseses);

            var retrievedJameses = repository.GetInstancesOf("James", testSubset);

            Assert.That(retrievedJameses, Is.EquivalentTo(expectedJameseses));
        }
    }
}
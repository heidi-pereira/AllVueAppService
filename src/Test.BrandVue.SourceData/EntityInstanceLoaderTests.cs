using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue;
using BrandVue.SourceData.CommonMetadata;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Subsets;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    public class EntityInstanceLoaderTests
    {
        private static readonly ConfigurationSourcedLoaderSettings ConfigurationSourcedLoaderSettings = TestLoaderSettings.WithProduct("Test.EntityInstance");

        [Test]
        public void LoaderShouldLoadInstancesFromAllEntityCsvFiles()
        {
            var expectedDavids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
            };

            var expectedJameseses = new[]
            {
                new EntityInstance {Id = 1, Name = "Rodden"},
                new EntityInstance {Id = 2, Name = "Hand"},
                new EntityInstance {Id = 3, Name = "Foster"},
            };

            var loader = TestDataLoader.Create(ConfigurationSourcedLoaderSettings);

            loader.LoadBrandVueMetadata();
            var subset = loader.SubsetRepository.Get("All");

            var entityRepository = loader.EntityInstanceRepository;

            var allDavids = entityRepository.GetInstancesOf("David", subset);
            var allJameseses = entityRepository.GetInstancesOf("James", subset);

            Assert.That(allDavids, Is.SupersetOf(expectedDavids));
            Assert.That(allJameseses, Is.SupersetOf(expectedJameseses));
        }

        [Test]
        public void LoaderShouldLoadSubsetsAsArray()
        {
            var expectedSubsets = new[]
            {
                new Subset()
                {
                    Id = "Film"
                },
                new Subset()
                {
                    Id = "TV"
                }
            };

            var loader = TestDataLoader.Create(ConfigurationSourcedLoaderSettings);

            loader.LoadBrandVueMetadata();

            var entityRepository = loader.EntityInstanceRepository;

            var found0 = entityRepository.TryGetInstance(expectedSubsets[0], "James", 10, out var testJames0);
            Assert.That(testJames0.Subsets, Is.EqualTo(expectedSubsets));
            var found1 = entityRepository.TryGetInstance(expectedSubsets[1], "James", 10, out var testJames1);
            Assert.That(testJames1.Subsets, Is.EqualTo(expectedSubsets));
        }

        /// <summary>
        /// Regression sc60018
        /// </summary>
        [Test]
        public void LoadingReturnsNullForMissingEntitySubset()
        {
            var loader = TestDataLoader.Create(ConfigurationSourcedLoaderSettings);

            loader.LoadBrandVueMetadata();

            var entityRepository = loader.EntityInstanceRepository;

            var found = entityRepository.TryGetInstance(loader.SubsetRepository.First(), "James", 1234, out var testJames);
            Assert.That(found, Is.False);
            Assert.That(testJames, Is.Null);
        }

        /// <summary>
        /// Regression sc60018
        /// </summary>
        [Test]
        public void LoadingEntityListWillNotContainNulls()
        {
            var loader = TestDataLoader.Create(ConfigurationSourcedLoaderSettings);

            loader.LoadBrandVueMetadata();

            var entityRepository = loader.EntityInstanceRepository;

            var results = entityRepository.GetInstances("James", new[] {10,1234}, loader.SubsetRepository.First(s => s.Id == "Film"));

            Assert.That(results.Count() == 1, Is.True);
            Assert.That(results.Any(x=>x==null), Is.False);
        }
    }
}
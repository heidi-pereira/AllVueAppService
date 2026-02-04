using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData
{
    public class EntitySetLoaderTests
    {
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
        private readonly IProductContext _productContext = Substitute.For<IProductContext>();
        private ISubsetRepository _subsetRepository;
        private EntitySetRepository _entitySetRepository;
        private IEntitySetConfigurationRepository _entitySetConfigurationRepository;
        private EntityTypeRepository _entityTypeRepository;
        private EntityInstanceRepository _entityInstanceRepository;
        private Subset _AllSubset = null;
        private const string ENTITY_TYPE = "James";

        [SetUp]
        public void SetUp()
        {
            _entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var subsetRepository = new SubsetRepository();
            _AllSubset = new Subset() { Id = "All" };
            subsetRepository.Add(_AllSubset);
            subsetRepository.Add(new Subset{Id = "AnotherSubset"});
            _subsetRepository = subsetRepository;
    
            _entityTypeRepository = new EntityTypeRepository();
            var jamesEntityType = new EntityType(ENTITY_TYPE, ENTITY_TYPE, ENTITY_TYPE);
            _entityTypeRepository.TryAdd(ENTITY_TYPE, jamesEntityType);

            _entityInstanceRepository = new EntityInstanceRepository();
            _entityInstanceRepository.Add(jamesEntityType, new EntityInstance { Id = 1, Name = "Rodden", Subsets = [_AllSubset]});
            _entityInstanceRepository.Add(jamesEntityType, new EntityInstance { Id = 2, Name = "Hand", Subsets = [_AllSubset] });
            _entityInstanceRepository.Add(jamesEntityType, new EntityInstance { Id = 3, Name = "Foster", Subsets = [_AllSubset] });
            _entityInstanceRepository.Add(jamesEntityType, new EntityInstance { Id = 7, Name = "Bond", Subsets = [_AllSubset] });

            _entitySetConfigurationRepository = Substitute.For<IEntitySetConfigurationRepository>();
            var entitySetConfigurations = new List<EntitySetConfiguration>
            {
                new() {Id = 1, Name = "single", EntityType = "James", Subset = _AllSubset.Id, Instances = "1", Organisation = ""},
                new() {Id = 2, Name = "multiple", EntityType = "James", Subset = _AllSubset.Id, Instances = "1|2|3", Organisation = ""},
                new() {Id = 3, Name = "range", EntityType = "James", Subset = _AllSubset.Id, Instances = "01:03", Organisation = ""},
                new() {Id = 4, Name = "combo", EntityType = "James", Subset = _AllSubset.Id, Instances = "1:3|7", Organisation = ""},
                new() {Id = 5, Name = "empty key instances", EntityType = "James", Subset = _AllSubset.Id, Instances = "", Organisation = ""},
                new() {Id = 6, Name = "include key instances", EntityType = "James", Subset = _AllSubset.Id, Instances = "01|07", Organisation = ""},
                new() {Id = 7, Name = "with organisation", EntityType = "James", Subset = _AllSubset.Id, Instances = "1", Organisation = "Framistan"},
                new() {Id = 8, Name = "with single average", EntityType = "James", Subset = _AllSubset.Id, Instances = "1", Organisation = "Framistan", },
                new() {Id = 9, Name = "with multiple averages", EntityType = "James", Subset = _AllSubset.Id, Instances = "1", Organisation = "Framistan"},
                new() {Id = 9, Name = "Failure", EntityType = "James", Subset = "NotValid", Instances = "1", Organisation = "Framistan"},
            };
            _entitySetConfigurationRepository.GetEntitySetConfigurations().Returns(entitySetConfigurations);
        }

        [Test]
        public void ShouldLoadIndividualInstances()
        {
            var expectedJames = new EntityInstance
            {
                Id = 1,
                Name = "Rodden",
            };

            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);

            entitySetLoader.AddOrUpdateAll();

            var basicSet = GetEntitySetByName("single", ENTITY_TYPE, _AllSubset, "");
            
            Assert.That(basicSet.Name, Is.EqualTo("single"));
            Assert.That(basicSet.Instances, Has.One.Items);
            Assert.That(basicSet.Instances.Single(), Is.EqualTo(expectedJames));
        }


        [Test]
        public void ShouldLoadMultipleInstances()
        {
            var expectedJameses = new []
            {
                new EntityInstance { Id = 1, Name = "Rodden" },
                new EntityInstance { Id = 2, Name = "Hand" }, 
                new EntityInstance { Id = 3, Name = "Foster" }, 
            };

            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);

            entitySetLoader.AddOrUpdateAll();

            var basicSet = GetEntitySetByName("multiple", ENTITY_TYPE, _AllSubset, "");
            
            Assert.That(basicSet.Name, Is.EqualTo("multiple"));
            Assert.That(basicSet.Instances, Has.Exactly(3).Items);
            Assert.That(basicSet.Instances, Is.EquivalentTo(expectedJameses));
        }

        [Test]
        public void ShouldLoadRangeOfInstances()
        {
            var expectedJameses = new []
            {
                new EntityInstance { Id = 1, Name = "Rodden" },
                new EntityInstance { Id = 2, Name = "Hand" }, 
                new EntityInstance { Id = 3, Name = "Foster" }, 
            };

            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);

            entitySetLoader.AddOrUpdateAll();

            var basicSet = GetEntitySetByName("range", ENTITY_TYPE, _AllSubset, "");
            
            Assert.That(basicSet.Name, Is.EqualTo("range"));
            Assert.That(basicSet.Instances, Has.Exactly(3).Items);
            Assert.That(basicSet.Instances, Is.EquivalentTo(expectedJameses));
        }

        [Test]
        public void ShouldLoadCombinedRangeAndIndividualInstances()
        {
            var expectedJameses = new []
            {
                new EntityInstance { Id = 1, Name = "Rodden" },
                new EntityInstance { Id = 2, Name = "Hand" },
                new EntityInstance { Id = 3, Name = "Foster" },
                new EntityInstance { Id = 7, Name = "Bond" }, 
            };

            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);

            entitySetLoader.AddOrUpdateAll();

            var basicSet = GetEntitySetByName("combo", ENTITY_TYPE, _AllSubset, "");
            
            Assert.That(basicSet.Name, Is.EqualTo("combo"));
            Assert.That(basicSet.Instances, Has.Exactly(4).Items);
            Assert.That(basicSet.Instances, Is.EquivalentTo(expectedJameses));
        }

        [Test]
        public void InstancesCountAsKeyWhenKeyInstancesIsEmpty()
        {
            var expectedJameses = new EntityInstance []
            {
            };

            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);

            entitySetLoader.AddOrUpdateAll();

            var entitySet = GetEntitySetByName("empty key instances", ENTITY_TYPE, _AllSubset, "");
            
            Assert.That(entitySet.Instances, Is.EquivalentTo(expectedJameses));
        }

        [Test]
        public void InstancesShouldIncludeAllKeyInstances()
        {
            var allTheJameses = new []
            {
                new EntityInstance { Id = 1, Name = "Rodden" },
                new EntityInstance { Id = 7, Name = "Bond" },
            };

            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);

            entitySetLoader.AddOrUpdateAll();

            var entitySet = GetEntitySetByName("include key instances", ENTITY_TYPE, _AllSubset, "");
            
            Assert.That(entitySet.Instances, Is.EquivalentTo(allTheJameses));
        }

        [Test]
        public void ShouldLoadOrganisation()
        {
            var entitySetLoader = new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory);
            entitySetLoader.AddOrUpdateAll();

            var entitySets = _entitySetRepository.GetAllFor(ENTITY_TYPE, _AllSubset, "Framistan");
            Assert.That(entitySets.Count(), Is.EqualTo(10));

            var entitySetNames = entitySets.Select(e => e.Name);
            Assert.That(entitySetNames, Does.Contain("with organisation"));
        }


        [Test]
        public void ShouldLoadInstancesCorrectlyIfThereExistsATypeWithoutInstances()
        {
            const string ENTITY_TYPE_NO_INSTANCES = "NoInstances";
            var noInstancesType = new EntityType(ENTITY_TYPE_NO_INSTANCES, ENTITY_TYPE_NO_INSTANCES, ENTITY_TYPE_NO_INSTANCES);
            _entityTypeRepository.TryAdd(ENTITY_TYPE_NO_INSTANCES, noInstancesType);

            var expectedJameses = new[]
            {
                new EntityInstance { Id = 1, Name = "Rodden" },
                new EntityInstance { Id = 2, Name = "Hand" },
                new EntityInstance { Id = 3, Name = "Foster" },
            };

            new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory)
                .AddOrUpdateAll();

            var generatedSet = GetEntitySetByName("multiple", ENTITY_TYPE, _AllSubset, "");

            Assert.That(_entityTypeRepository.TryGet(ENTITY_TYPE_NO_INSTANCES, out _), Is.False);

            Assert.That(_entityTypeRepository.TryGet(ENTITY_TYPE, out _), Is.True);
            Assert.That(generatedSet.Name, Is.EqualTo("multiple"));
            Assert.That(generatedSet.Instances, Has.Exactly(3).Items);
            Assert.That(generatedSet.Instances, Is.EquivalentTo(expectedJameses));
        }

        [Test]
        public void ShouldLoadGeneratedSetFromEntityTypeWithCorrectInstances()
        {
            const string ENTITY_TYPE_NO_SET = "Enigma";
            var enigmaEntityType = new EntityType(ENTITY_TYPE_NO_SET, ENTITY_TYPE_NO_SET, ENTITY_TYPE_NO_SET);
            _entityTypeRepository.TryAdd(ENTITY_TYPE_NO_SET, enigmaEntityType);
            _entityInstanceRepository.Add(enigmaEntityType, new EntityInstance { Id = 1, Name = "Adam" });
            _entityInstanceRepository.Add(enigmaEntityType, new EntityInstance { Id = 2, Name = "David" });
            _entityInstanceRepository.Add(enigmaEntityType, new EntityInstance { Id = 3, Name = "Christian" });

            var expectedInstances = new[]
            {
                new EntityInstance { Id = 1, Name = "Adam" },
                new EntityInstance { Id = 2, Name = "David" },
                new EntityInstance { Id = 3, Name = "Christian" }
            };

            new EntitySetConfigurationLoader(_entitySetConfigurationRepository, _entitySetRepository, _subsetRepository, _entityTypeRepository, _entityInstanceRepository, _productContext, _loggerFactory)
                .AddOrUpdateAll();

            var generatedSet = GetEntitySetByName(BrandVueDataLoader.All, ENTITY_TYPE_NO_SET, _AllSubset, "");

            Assert.That(generatedSet.Name, Is.EqualTo(BrandVueDataLoader.All));
            Assert.That(generatedSet.Instances, Has.Exactly(3).Items);
            Assert.That(generatedSet.Instances, Is.EquivalentTo(expectedInstances));
        }

        private EntitySet GetEntitySetByName(string name, string entityType, Subset subset, string organisation)
        {
            var entitySets = _entitySetRepository.GetAllFor(entityType, subset, organisation);
            return entitySets.SingleOrDefault(es => es.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}

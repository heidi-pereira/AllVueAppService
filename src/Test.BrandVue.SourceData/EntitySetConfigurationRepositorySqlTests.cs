using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Dashboard;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData
{
    public class EntitySetConfigurationRepositorySqlTests
    {
        private const string ShortCode = "retail";
        private const string SubProductId = null;
        private readonly ProductContext _productContext = new ProductContext(ShortCode, SubProductId, false, "Test survey");

        private ITestMetadataContextFactory _testMetadataContextFactory;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_testMetadataContextFactory != null)
                await _testMetadataContextFactory.Dispose();
        }

        [SetUp]
        public async Task SetUp()
        {
            _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemory);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _testMetadataContextFactory.RevertDatabase();
        }

        [Test]
        public void GetEntitySetConfigurationShouldIncludeAverageMappings()
        {
            using var context = _testMetadataContextFactory.CreateDbContext();

            var newSet1 = CreateEntitySetConfiguration(1, []);
            
            var newSet2 = CreateEntitySetConfiguration(2, []);

            context.Add(newSet1);
            context.Add(newSet2);
            context.SaveChanges();

            var newAverage = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = newSet1.Id,
                ChildEntitySetId = newSet2.Id
            };

            context.Add(newAverage);
            context.SaveChanges();
            
            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);

            var retrievedConfigurations = repository.GetEntitySetConfigurations();
            Assert.That(retrievedConfigurations, Has.Count.EqualTo(2));

            var retrievedSet1 = retrievedConfigurations.First();
            Assert.That(retrievedSet1.Name, Is.EqualTo(newSet1.Name));
            Assert.That(retrievedSet1.ChildAverageMappings, Is.Not.Empty);
            Assert.That(retrievedSet1.ChildAverageMappings.Any(mapping => mapping.ParentEntitySetId == newSet1.Id && mapping.ChildEntitySetId == newSet2.Id), Is.True);
        }

        [Test]
        public void CreateShouldSaveAverageMappings()
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);

            var newSet2 = CreateEntitySetConfiguration(2, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet2);

            var newAverage = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = newSet1.Id,
                ChildEntitySetId = newSet2.Id
            };
            
            newSet1.ChildAverageMappings.Add(newAverage);
            repository.Create(newSet1);

            var allSets = repository.GetEntitySetConfigurations();
            var retrievedSet1 = allSets.First(set => set.Name == newSet1.Name);
            
            Assert.That(retrievedSet1.ChildAverageMappings, Is.Not.Empty);
            Assert.That(retrievedSet1.ChildAverageMappings.First().ParentEntitySetId, Is.Not.EqualTo(0));
            Assert.That(retrievedSet1.ChildAverageMappings.First().ParentEntitySetId, Is.EqualTo(newSet1.Id));
            Assert.That(retrievedSet1.ChildAverageMappings.First().ChildEntitySetId, Is.Not.EqualTo(0));
            Assert.That(retrievedSet1.ChildAverageMappings.First().ChildEntitySetId, Is.EqualTo(newSet2.Id));
        }

        [Test]
        public void UpdateShouldAddAverages()
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);
            
            var newSet2 = CreateEntitySetConfiguration(2, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet1);
            repository.Create(newSet2);
            
            var newAverage = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = newSet1.Id,
                ChildEntitySetId = newSet2.Id
            };
            
            newSet1.ChildAverageMappings.Add(newAverage);

            repository.Update(newSet1);
            
            var allSets = repository.GetEntitySetConfigurations();
            var retrievedSet1 = allSets.First(set => set.Name == newSet1.Name);
            
            Assert.That(retrievedSet1.ChildAverageMappings, Is.Not.Empty);
            var entitySetAverageMappingConfiguration = retrievedSet1.ChildAverageMappings.First(x => x.ChildEntitySetId != retrievedSet1.Id);
            Assert.That(entitySetAverageMappingConfiguration.ParentEntitySetId, Is.Not.EqualTo(0));
            Assert.That(entitySetAverageMappingConfiguration.ParentEntitySetId, Is.EqualTo(newSet1.Id));
            Assert.That(entitySetAverageMappingConfiguration.ChildEntitySetId, Is.Not.EqualTo(0));
            Assert.That(entitySetAverageMappingConfiguration.ChildEntitySetId, Is.EqualTo(newSet2.Id));
        }

        [Theory]
        public void UpdateShouldRemoveAverages(bool useClear)
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);

            var newSet2 = CreateEntitySetConfiguration(2, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet1);
            repository.Create(newSet2);
            
            var newAverage = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = newSet1.Id,
                ChildEntitySetId = newSet2.Id
            };
            
            newSet1.ChildAverageMappings.Add(newAverage);

            repository.Update(newSet1);
            if (useClear)
            {
                newSet1.ChildAverageMappings.Clear();
            }
            else
            {
                foreach (var mapping in newSet1.ChildAverageMappings.ToArray())
                {
                    newSet1.ChildAverageMappings.Remove(mapping);
                }
            }

            repository.Update(newSet1);

            var allSets = repository.GetEntitySetConfigurations();
            var retrievedSet1 = allSets.First(set => set.Name == newSet1.Name);
            
            Assert.That(retrievedSet1.ChildAverageMappings, Is.Empty);
        }

        [Test]
        public void DeleteShouldRemoveAllAssociatedAverages()
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);

            var newSet2 = CreateEntitySetConfiguration(2, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet1);
            repository.Create(newSet2);
            
            var newAverage = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = newSet1.Id,
                ChildEntitySetId = newSet2.Id
            };
            
            newSet1.ChildAverageMappings.Add(newAverage);

            repository.Update(newSet1);
            repository.Delete(newSet1);
            
            var allSets = repository.GetEntitySetConfigurations();
            Assert.That(allSets, Has.Count.EqualTo(1));
            
            var retrievedSet1 = allSets.FirstOrDefault(set => set.Name == newSet1.Name);
            Assert.That(retrievedSet1, Is.Null);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var allAverageMappings = dbContext.EntitySetAverageMappingConfigurations;
            
            Assert.That(allAverageMappings, Is.EquivalentTo(new[] {new EntitySetAverageMappingConfiguration {ParentEntitySetId = newSet2.Id, ChildEntitySetId = newSet2.Id}}).Using<EntitySetAverageMappingConfiguration>(HaveEqualParentAndChildIds));
        }

        [Test]
        public void DeleteShouldRemoveOnlyAssociatedAverages()
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);

            var newSet2 = CreateEntitySetConfiguration(2, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet1);
            repository.Create(newSet2);

            var averageMapping = new EntitySetAverageMappingConfiguration()
            {
                ParentEntitySetId = newSet1.Id,
                ParentEntitySetConfiguration = newSet1,
                ChildEntitySetId = newSet2.Id,
                ChildEntitySetConfiguration = newSet2
            };

            newSet1.ChildAverageMappings.Add(averageMapping);
            repository.Update(newSet1);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var allAverageMappings = dbContext.EntitySetAverageMappingConfigurations.ToList();
            var newSet1AverageMappings = allAverageMappings.Where(a => a.ParentEntitySetId == newSet1.Id);
            var newSet2AverageMappings = allAverageMappings.Where(a => a.ParentEntitySetId == newSet2.Id);

            // First, assert that the new average mapping has been added
            Assert.Multiple(() =>
            {
                Assert.That(allAverageMappings, Has.Count.EqualTo(3),
                    "Expected total of 3 average mappings");

                Assert.That(newSet1AverageMappings.Select(n => n.ChildEntitySetId),
                    Is.EquivalentTo(new[] { newSet1.Id, newSet2.Id }),
                    "Set 1 should have mappings to both Set 1 and Set 2");

                Assert.That(newSet2AverageMappings.Select(n => n.ChildEntitySetId),
                    Is.EquivalentTo(new[] { newSet2.Id }),
                    "Set 2 should only have mapping to itself");
            });

            newSet1.ChildAverageMappings.Remove(averageMapping);
            repository.Update(newSet1);

            allAverageMappings = dbContext.EntitySetAverageMappingConfigurations.ToList();
            newSet1AverageMappings = allAverageMappings.Where(a => a.ParentEntitySetId == newSet1.Id);
            newSet2AverageMappings = allAverageMappings.Where(a => a.ParentEntitySetId == newSet2.Id);

            // Then, assert that the average mapping has been removed
            Assert.Multiple(() =>
            {
                Assert.That(allAverageMappings, Has.Count.EqualTo(2),
                    "Expected total of 2 average mappings");

                Assert.That(newSet1AverageMappings.Select(n => n.ChildEntitySetId),
                    Is.EquivalentTo(new[] { newSet1.Id }),
                    "Set 1 should only have mappings to itself");

                Assert.That(newSet2AverageMappings.Select(n => n.ChildEntitySetId),
                    Is.EquivalentTo(new[] { newSet2.Id }),
                    "Set 2 should only have mapping to itself");
            });
        }

        public bool HaveEqualParentAndChildIds(EntitySetAverageMappingConfiguration mapping1,
            EntitySetAverageMappingConfiguration mapping2)
        {
            return mapping1.ChildEntitySetId == mapping2.ChildEntitySetId &&
                   mapping1.ParentEntitySetId == mapping2.ParentEntitySetId;

        }

        [Test]
        public void DeleteEntitySetShouldDeleteAverageMappings()
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);

            var newSet2 = CreateEntitySetConfiguration(2, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet1);
            repository.Create(newSet2);
            
            var newAverage = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = newSet1.Id,
                ChildEntitySetId = newSet2.Id
            };
            
            newSet1.ChildAverageMappings.Add(newAverage);

            repository.Update(newSet1);
            
            var allSetsBeforeDelete = repository.GetEntitySetConfigurations();
            var retrievedSet1 = allSetsBeforeDelete.First(set => set.Name == newSet1.Name);
            var averageMappingsBeforeDelete = GetAverageMappings(_testMetadataContextFactory);

            repository.Delete(retrievedSet1);

            var allSetsAfterDelete = repository.GetEntitySetConfigurations();
            var retrievedSet2 = allSetsAfterDelete.First(set => set.Name == newSet2.Name);
            var averageMappingsAfterDelete = GetAverageMappings(_testMetadataContextFactory);

            Assert.Multiple(() =>
            {
                Assert.That(averageMappingsBeforeDelete.Count(), Is.EqualTo(3));
                Assert.That(allSetsAfterDelete.Count(), Is.EqualTo(1));
                Assert.That(retrievedSet2, Is.Not.Null);
                Assert.That(averageMappingsAfterDelete, Has.One.Items);
            });
        }

        private List<EntitySetAverageMappingConfiguration> GetAverageMappings(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            using var dbContext = dbContextFactory.CreateDbContext();
            return [..dbContext.EntitySetAverageMappingConfigurations];
        }

        [Test]
        public void TestPersistingAverageMappings()
        {
            var newSet1 = CreateEntitySetConfiguration(1, []);
            var newSet2 = CreateEntitySetConfiguration(2, []);
            var newSet3 = CreateEntitySetConfiguration(3, []);
            var newSet4 = CreateEntitySetConfiguration(4, []);
            var newSet5 = CreateEntitySetConfiguration(5, []);

            var repository = new EntitySetConfigurationRepositorySql(_testMetadataContextFactory, _productContext);
            repository.Create(newSet1);
            repository.Create(newSet2);
            repository.Create(newSet3);
            repository.Create(newSet4);
            repository.Create(newSet5);

            var newAverage1 = CreateEntitySetAverageMappingConfiguration(newSet1, newSet2);
            var newAverage2 = CreateEntitySetAverageMappingConfiguration(newSet1, newSet3);

            newSet1.ChildAverageMappings.Add(newAverage1);
            newSet1.ChildAverageMappings.Add(newAverage2);

            repository.Update(newSet1);

            newSet1.ChildAverageMappings.Single(m => m.ChildEntitySetId == newSet2.Id).ChildEntitySetId = newSet4.Id;
            newSet1.ChildAverageMappings.Single(m => m.ChildEntitySetId == newSet3.Id).ChildEntitySetId = newSet5.Id;
            // Add an average that's the same as newAverage1 so that we get a match between and existing and new
            // average in the method MergeEntitySetMapping.
            newSet1.ChildAverageMappings.Add(CreateEntitySetAverageMappingConfiguration(newSet1, newSet2));

            repository.Update(newSet1);

            Assert.That(newSet1.ChildAverageMappings.Count(), Is.EqualTo(4));
        }

        private static EntitySetAverageMappingConfiguration CreateEntitySetAverageMappingConfiguration(EntitySetConfiguration parentSet, EntitySetConfiguration childSet)
        {
            return new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = parentSet.Id,
                ChildEntitySetId = childSet.Id
            };
        }

        private static EntitySetConfiguration CreateEntitySetConfiguration(int id, List<EntitySetAverageMappingConfiguration> mappings)
        {
            var selfReferencingMapping = new EntitySetAverageMappingConfiguration
            {
                ParentEntitySetId = id,
                ChildEntitySetId = id
            };

            mappings.Add(selfReferencingMapping);

            return new EntitySetConfiguration
            {
                Name = "Test set " + id,
                ProductShortCode = ShortCode,
                SubProductId = SubProductId,
                EntityType = "TestType",
                Organisation = "TestOrg",
                Instances = $"{id*3-2}|{id*3-1}|{id*3}",
                MainInstance = 1,
                LastUpdatedUserId = "testUser",
                ChildAverageMappings = mappings,
            };
        }
    }
}

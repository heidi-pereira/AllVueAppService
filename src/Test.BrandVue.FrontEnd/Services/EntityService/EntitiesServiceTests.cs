using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Subsets;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BrandVue.EntityFramework.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;
using Vue.Common.Auth;

namespace Test.BrandVue.FrontEnd.Services.EntityService
{
    [TestFixture]
    public class EntitiesServiceTests
    {
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
            _userContext.UserId.Returns("testuser123");
            _userContext.AuthCompany.Returns("authcomp888");
            _userContext.IsAdministrator.Returns(true);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _testMetadataContextFactory.RevertDatabase();
        }

        private readonly IProductContext _productContext = new ProductContext("test", "12345", true, "surveyName");
        private readonly IProductContext _secondaryProductContext = new ProductContext("othertest", "67890", true, "othersurvey");
        private readonly IUserContext _userContext = Substitute.For<IUserContext>();

        private Subset _subset;
        private readonly EntityType _entityType = new() { Identifier = "entityTypeIdentifier" };
        private readonly EntityType _anotherEntityType = new EntityType { Identifier = "type", DisplayNameSingular = "type", DisplayNamePlural = "types" };

        private (IEntitiesService EntitiesService,
            EntityInstanceRepository EntityRepository,
            IEntityInstanceConfigurationRepository EntityInstanceConfigurationRepository,
            EntityTypeRepository EntityTypeRepository,
            IEntityTypeConfigurationRepository EntityTypeConfigurationRepository,
            IEntitySetConfigurationRepository EntitySetConfigurationRepository,
            ISubsetRepository SubsetRepository)
            GetEntityManagers(IDbContextFactory<MetaDataContext> dbContextFactory)
        {
            var subsetRepo = new FallbackSubsetRepository();
            _subset = subsetRepo.First();
            var dataLoader = Substitute.For<IBrandVueDataLoader>();
            var entityRepository = new EntityInstanceRepository();
            var entityInstanceConfigRepo = new EntityInstanceRepositorySql(_productContext, dbContextFactory, entityRepository);
            var entityTypeRepo = new TestEntityTypeRepository(_entityType, _anotherEntityType);
            var entityTypeConfigRepo = new EntityTypeRepositorySql(_productContext, dbContextFactory, entityTypeRepo);
            IEntitySetConfigurationRepository entitySetConfigRepo = new EntitySetConfigurationRepositorySql(dbContextFactory, _productContext);
            var entitySetConfigurationLoader = new EntitySetConfigurationLoader(entitySetConfigRepo, new EntitySetRepository(NullLoggerFactory.Instance, _productContext), subsetRepo, entityTypeRepo, entityRepository, _productContext, NullLoggerFactory.Instance);
            entitySetConfigRepo = new InMemoryRepositoryUpdatingEntitySetConfigurationRepository(entitySetConfigRepo, entitySetConfigurationLoader);
            var entitiesService = new EntitiesService(
                entityRepository,
                entityTypeRepo,
                entityTypeConfigRepo,
                entityInstanceConfigRepo,
                entitySetConfigRepo,
                _productContext,
                subsetRepo,
                dataLoader,
                _userContext);
            
            return (entitiesService, entityRepository, entityInstanceConfigRepo, entityTypeRepo, entityTypeConfigRepo, entitySetConfigRepo, subsetRepo);
        }

        private EntitySetModel BuildDefaultEntitySetModel(string name) => new EntitySetModel
        {
            Id = 0,
            Name = name,
            InstanceIds = new[] { 1, 2, 3 },
            MainInstanceId = 1,
            EntityType = _anotherEntityType,
            Organisation = _userContext.UserOrganisation,
            IsSectorSet = false,
            IsDefault = false,
            IsFallback = false,
            AverageMappings = Array.Empty<EntitySetAverageMappingModel>(),
        };

        [Test]
        public void ShouldGetDbAndDynamicEntityTypeConfigurations()
        {
            var managers = GetEntityManagers(_testMetadataContextFactory);
            var initialTypeIdentifiers = managers.EntitiesService.GetEntityTypeConfigurations().Select(t => t.Identifier);
            var dbEntityType = "dbtype";
            var dynamicType = "dynamictype";
            managers.EntityTypeConfigurationRepository.Save(dbEntityType, dbEntityType, $"{dbEntityType}s", Array.Empty<string>(), null);
            managers.EntityTypeRepository.TryAdd(dynamicType, new EntityType(dynamicType, dynamicType, $"{dynamicType}s"));

            var addedConfigurationIdentifiers = managers.EntitiesService.GetEntityTypeConfigurations().Select(t => t.Identifier).Except(initialTypeIdentifiers);
            Assert.That(addedConfigurationIdentifiers, Is.EquivalentTo(new[] { dbEntityType, dynamicType }));
        }

        [Test]
        public void ShouldNotGetEntityTypeConfigurationsFromOtherSubProduct()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var initialTypeIdentifiers = tools.EntitiesService.GetEntityTypeConfigurations().Select(t => t.Identifier); ;
            var otherSubProductEntityTypeConfigRepo = new EntityTypeRepositorySql(_secondaryProductContext, _testMetadataContextFactory, tools.EntityTypeRepository);
            var currentSubProductEntityType = "primaryType";
            var otherSubProductEntityType = "otherType";
            tools.EntityTypeConfigurationRepository.Save(currentSubProductEntityType, currentSubProductEntityType, $"{currentSubProductEntityType}s", Array.Empty<string>(), null);
            otherSubProductEntityTypeConfigRepo.Save(otherSubProductEntityType, otherSubProductEntityType, $"{otherSubProductEntityType}s", Array.Empty<string>(), null);

            var configurations = tools.EntitiesService.GetEntityTypeConfigurations();
            Assert.That(configurations.Select(c => c.Identifier).Except(initialTypeIdentifiers), Is.EquivalentTo(new[]{currentSubProductEntityType}));
        }

        [Test]
        public void ShouldGetEntityInstanceConfigurations()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            string name = "instance";
            string imageUrl = "imageUrl";
            int choiceId = 2;
            tools.EntityInstanceConfigurationRepository.Save(_subset, _entityType.Identifier, choiceId, name, true, null, imageUrl);
            tools.EntityRepository.Add(new EntityType { Identifier = _entityType.Identifier }, new EntityInstance { Name = name, Id = choiceId });

            var configurations = tools.EntitiesService.GetEntityInstanceConfigurations(_subset, _entityType.Identifier);
            Assert.That(configurations.Count, Is.EqualTo(1));
            Assert.That(configurations.Single().DisplayName, Is.EqualTo(name));
            Assert.That(configurations.Single().SurveyChoiceId, Is.EqualTo(choiceId));
        }

        [Test]
        public void ShouldNotAllowDuplicateEntityInstanceNames()
        {
            //var dbContextFactory = GetDbContextFactory();
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityTypeIdentifier = "entityTypeIdentifier";
            var displayName = "displayName";
            string imageUrl = "imageUrl";
            tools.EntityRepository.Add(_entityType, new EntityInstance { Name = displayName, Id = 1 });

            Assert.Multiple(() =>
            {
                Assert.Throws<BadRequestException>(() => tools.EntityInstanceConfigurationRepository.Save(_subset, entityTypeIdentifier, 2, displayName, true, null, imageUrl));
                Assert.DoesNotThrow(() => tools.EntityInstanceConfigurationRepository.Save(_subset, "differentEntityIdentifier", 2, displayName, true, null, imageUrl));
            });
        }

        [Test]
        public void ShouldNotGetEntityInstanceConfigurationsFromOtherSubProduct()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var otherSubProductEntityInstanceRepo = new EntityInstanceRepository();
            string entityType = "entityType";
            string instanceName = "instance";
            tools.EntityRepository.Add(new EntityType { Identifier = entityType }, new EntityInstance { Name = instanceName, Id = 1 });
            otherSubProductEntityInstanceRepo.Add(new EntityType { Identifier = entityType }, new EntityInstance { Name = "other", Id = 2 });
            var configurations = tools.EntitiesService.GetEntityInstanceConfigurations(_subset, entityType);
            Assert.That(configurations.Count, Is.EqualTo(1));
            Assert.That(configurations.Single().DisplayName, Is.EqualTo(instanceName));
        }

        [Test]
        public void ShouldGetDynamicEntityInstancesFromAllSubsets()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = tools.EntityTypeRepository.DefaultEntityType;
            var subsetUk = new Subset { Id = "1", DisplayName = "UK" };
            var subsetUs = new Subset { Id = "2", DisplayName = "US" };
            var instanceAName = "Tesco";
            var instanceBName = "Sainsbury's";

            int sharedId = 1;
            tools.EntityRepository.Add(entityType, new EntityInstance { Name = instanceAName, Id = sharedId, Subsets = new[] { subsetUk } });
            tools.EntityRepository.Add(entityType, new EntityInstance { Name = instanceBName, Id = sharedId, Subsets = new[] { subsetUs } });

            var configurations = tools.EntitiesService.GetEntityInstanceConfigurations(subsetUk, entityType.Identifier);
            string[] expectedInstances = [instanceAName];
            Assert.That(expectedInstances, Is.EquivalentTo(configurations.Select(c => c.DisplayName).ToArray()));
        }

        [Test]
        public void ShouldSaveNewEntityTypeConfigurations()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = new EntityType("type", "type", $"types");
            var newDisplayName = "newDisplayNameSingular";
            var newDisplayNamePlural = newDisplayName + "s";
            tools.EntityTypeRepository.TryAdd(entityType.Identifier, entityType);
            tools.EntitiesService.SaveEntityType(entityType.Identifier, newDisplayName, newDisplayNamePlural);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var configurations = dbContext.EntityTypeConfigurations.ToArray();
            Assert.That(configurations.Length, Is.EqualTo(1));
            var config = configurations.Single();
            Assert.That(config.Identifier, Is.EqualTo(entityType.Identifier));
            Assert.That(config.DisplayNameSingular, Is.EqualTo(newDisplayName));
            Assert.That(config.DisplayNamePlural, Is.EqualTo(newDisplayNamePlural));
        }

        [Test]
        public void ShouldNotSaveEntityTypeConfigurationIfNoMatchingType()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = new EntityType("non-existent-type", "type", $"types");
            var newDisplayName = "newDisplayNameSingular";
            var newDisplayNamePlural = newDisplayName + "s";
            Assert.Throws<InvalidOperationException>(() =>
                tools.EntitiesService.SaveEntityType(entityType.Identifier, newDisplayName, newDisplayNamePlural));
        }

        [Test]
        public void SavingEntityTypeShouldUpdateRepository()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = new EntityType("type", "type", $"types");
            var newDisplayName = "newDisplayNameSingular";
            var newDisplayNamePlural = newDisplayName + "s";
            tools.EntityTypeRepository.TryAdd(entityType.Identifier, entityType);
            tools.EntitiesService.SaveEntityType(entityType.Identifier, newDisplayName, newDisplayNamePlural);

            var typeFromRepository = tools.EntityTypeRepository.Get(entityType.Identifier);
            Assert.That(typeFromRepository.DisplayNameSingular, Is.EqualTo(newDisplayName));
            Assert.That(typeFromRepository.DisplayNamePlural, Is.EqualTo(newDisplayNamePlural));
        }

        [Test]
        public void ShouldSaveNewEntityInstanceConfigurations()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = new EntityType("type", "type", $"types");
            var choiceId = 3;
            var newName = "newName";
            var entityInstanceConfigurationModel = new EntityInstanceConfigurationModel
            {
                EntityTypeIdentifier = entityType.Identifier,
                SurveyChoiceId = choiceId,
                DisplayName = newName,
                Enabled = true,
                ImageUrl = entityType.Identifier,
            };
            tools.EntityRepository.Add(entityType, new EntityInstance { Name = "instance", Id = choiceId });
            tools.EntitiesService.SaveEntityInstance(_subset, entityInstanceConfigurationModel);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var configurations = dbContext.EntityInstanceConfigurations.ToArray();
            Assert.That(configurations.Length, Is.EqualTo(1));
            var config = configurations.Single();
            Assert.That(config.DisplayNameOverrideBySubset[_subset.Id], Is.EqualTo(newName));
            Assert.That(config.SurveyChoiceId, Is.EqualTo(choiceId));
        }

        [Test]
        public void ShouldNotSaveEntityInstanceConfigurationIfNoMatchingChoiceId()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = new EntityType("type", "type", $"types");
            var choiceId = 3;
            var otherChoiceId = 5;
            var newName = "newName";
            var entityInstanceConfigurationModel = new EntityInstanceConfigurationModel
            {
                EntityTypeIdentifier = entityType.Identifier,
                SurveyChoiceId = otherChoiceId,
                DisplayName = newName,
                Enabled = true
            };
            tools.EntityRepository.Add(entityType, new EntityInstance { Name = "instance", Id = choiceId });
            Assert.Throws<InvalidOperationException>(() =>
                tools.EntitiesService.SaveEntityInstance(_subset, entityInstanceConfigurationModel));
        }

        [Test]
        public void SavingEntityInstanceShouldUpdateRepository()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var entityType = new EntityType("type", "type", $"types");
            var choiceId = 3;
            var newName = "newName";
            var entityInstanceConfigurationModel = new EntityInstanceConfigurationModel
            {
                EntityTypeIdentifier = entityType.Identifier,
                SurveyChoiceId = choiceId,
                DisplayName = newName,
                Enabled = true
            };
            tools.EntityRepository.Add(entityType, new EntityInstance { Name = "instance", Id = choiceId });
            tools.EntitiesService.SaveEntityInstance(_subset, entityInstanceConfigurationModel);

            var found = tools.EntityRepository.TryGetInstance(_subset, entityType.Identifier, choiceId, out var instanceFromRepository);
            Assert.That(found, Is.True);
            Assert.That(instanceFromRepository.Name, Is.EqualTo(newName));
        }

        [Test]
        public void ShouldCreateNewEntitySet()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var model = BuildDefaultEntitySetModel("set");
            tools.EntitiesService.CreateEntitySet(_subset.Id, model);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var configurations = dbContext.EntitySetConfigurations.ToArray();
            Assert.That(configurations.Length, Is.EqualTo(1));
            var config = configurations.Single();
            Assert.That(config.Name, Is.EqualTo(model.Name));
            Assert.That(config.Instances, Is.EqualTo(string.Join("|", model.InstanceIds)));
            Assert.That(config.MainInstance, Is.EqualTo(model.MainInstanceId));
            Assert.That(config.IsSectorSet, Is.EqualTo(model.IsSectorSet));
            Assert.That(config.IsDefault, Is.EqualTo(model.IsDefault));
            Assert.That(config.IsFallback, Is.EqualTo(model.IsFallback));
            Assert.That(config.EntityType, Is.EqualTo(model.EntityType.Identifier));
        }

        [Test]
        public void ShouldRemoveDefaultWhenCreatingNewDefaultEntitySet()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);

            var oldDefault = BuildDefaultEntitySetModel("set1");
            oldDefault.IsDefault = true;
            tools.EntitiesService.CreateEntitySet(_subset.Id, oldDefault);

            var newDefault = BuildDefaultEntitySetModel("set2");
            newDefault.IsDefault = true;
            tools.EntitiesService.CreateEntitySet(_subset.Id, newDefault);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var allConfigurations = dbContext.EntitySetConfigurations.ToArray();
            var defaultConfigurations = allConfigurations.Where(c => c.IsDefault).ToArray();
            Assert.That(allConfigurations.Length, Is.EqualTo(2));
            Assert.That(defaultConfigurations.Length, Is.EqualTo(1));
            Assert.That(defaultConfigurations.Single().Name, Is.EqualTo(newDefault.Name));
        }

        [Test]
        public void ShouldNotCreateEntitySetIfAlreadyExists()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var model = BuildDefaultEntitySetModel("set");

            tools.EntitiesService.CreateEntitySet(_subset.Id, model);
            Assert.Throws<InvalidOperationException>(() => tools.EntitiesService.CreateEntitySet(_subset.Id, model));
        }

        [Test]
        public void ShouldCreateAverageMappingForEntitySetConfigIfNotSpecified()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var model = BuildDefaultEntitySetModel("set");

            model.AverageMappings = Array.Empty<EntitySetAverageMappingModel>();
            var createdSet = tools.EntitiesService.CreateEntitySet(_subset.Id, model);
            var entitySetId = createdSet.Id ?? 0;

            // Check db entitySetId is being passed back correctly
            Assert.That(entitySetId, Is.Not.EqualTo(0));

            Assert.That(createdSet.AverageMappings.Count, Is.EqualTo(1));
            var average = createdSet.AverageMappings[0];
            Assert.That(average.ParentEntitySetId, Is.EqualTo(entitySetId));
            Assert.That(average.ChildEntitySetId, Is.EqualTo(entitySetId));
            Assert.That(average.ExcludeMainInstance, Is.EqualTo(false));

            // Also check the db
            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var averages = dbContext.EntitySetAverageMappingConfigurations
                .Include(entitySetAverageMappingConfiguration =>
                    entitySetAverageMappingConfiguration.ChildEntitySetConfiguration)
                .Include(entitySetAverageMappingConfiguration =>
                    entitySetAverageMappingConfiguration.ParentEntitySetConfiguration).ToArray();
            Assert.That(averages.Length, Is.EqualTo(1));
            var dbAverage = averages.First();
            Assert.That(dbAverage.ParentEntitySetId, Is.EqualTo(entitySetId));
            Assert.That(dbAverage.ChildEntitySetId, Is.EqualTo(entitySetId));
            Assert.That(dbAverage.ExcludeMainInstance, Is.EqualTo(false));
            Assert.That(dbAverage.ChildEntitySetConfiguration.Id, Is.EqualTo(entitySetId));
            Assert.That(dbAverage.ParentEntitySetConfiguration.Id, Is.EqualTo(entitySetId));
        }

        [Test]
        public void ShouldCreateAverageMappingForEntitySetConfigWithZeroAverageMappingsIds()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var model = BuildDefaultEntitySetModel("set");

            model.AverageMappings = new List<EntitySetAverageMappingModel>
            {
                new() { ParentEntitySetId = 0, ChildEntitySetId = 0}
            };
            var createdSet = tools.EntitiesService.CreateEntitySet(_subset.Id, model);
            var entitySetId = createdSet.Id ?? 0;

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var averages = dbContext.EntitySetAverageMappingConfigurations.ToArray();
            Assert.That(averages.Length, Is.EqualTo(1));
            var dbAverage = averages.First();
            Assert.That(dbAverage.ParentEntitySetId, Is.EqualTo(entitySetId));
            Assert.That(dbAverage.ChildEntitySetId, Is.EqualTo(entitySetId));
        }

        [Test]
        public void ShouldUpdateExistingEntitySetConfiguration()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);
            var model = BuildDefaultEntitySetModel("set");
            var createdSet = tools.EntitiesService.CreateEntitySet(_subset.Id, model);
            createdSet = JsonConvert.DeserializeObject<EntitySetModel>(JsonConvert.SerializeObject(createdSet));
            createdSet.InstanceIds = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            tools.EntitiesService.SaveEntitySet(_subset.Id, createdSet);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var configurations = dbContext.EntitySetConfigurations.ToArray();
            Assert.That(configurations.Length, Is.EqualTo(1));
            Assert.That(configurations.Single().Instances, Is.EqualTo(string.Join("|", createdSet.InstanceIds)));
        }

        [Test]
        public void ShouldUpdateExistingEntitySetConfigurationWithChildAverages()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);

            var averageModel = BuildDefaultEntitySetModel("average");
            var createdAverage = tools.EntitiesService.CreateEntitySet(_subset.Id, averageModel);

            var model = BuildDefaultEntitySetModel("set");
            model.AverageMappings = model.AverageMappings.Append(new(){ChildEntitySetId = createdAverage.Id.Value}).ToList();
            var createdSet = tools.EntitiesService.CreateEntitySet(_subset.Id, model);

            createdSet = JsonConvert.DeserializeObject<EntitySetModel>(JsonConvert.SerializeObject(createdSet));
            createdSet.InstanceIds = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            tools.EntitiesService.SaveEntitySet(_subset.Id, createdSet);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var configurations = dbContext.EntitySetConfigurations.ToArray();
            Assert.That(configurations.Select(c => c.Name), Is.EquivalentTo(new[]{"average", "set"}));

            var configurationSetInstanceIds = configurations.Single(x => x.Name == "set").Instances.Split("|").Select(int.Parse);
            Assert.That(configurationSetInstanceIds, Is.EquivalentTo(createdSet.InstanceIds));
        }

        [Test]
        public void ShouldThrowIfNoMatchingEntitySetToUpdate()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);

            var model = BuildDefaultEntitySetModel("set");
            Assert.Throws<InvalidOperationException>(() => tools.EntitiesService.SaveEntitySet(_subset.Id, model));
        }

        [Test]
        public void ShouldRemoveDefaultWhenUpdatingSetToDefault()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);

            var setA = BuildDefaultEntitySetModel("setA");
            setA.IsDefault = true;
            var setB = BuildDefaultEntitySetModel("setB");
            setB.IsDefault = false;
            tools.EntitiesService.CreateEntitySet(_subset.Id, setA);
            setB = tools.EntitiesService.CreateEntitySet(_subset.Id, setB);
            setB = JsonConvert.DeserializeObject<EntitySetModel>(JsonConvert.SerializeObject(setB));

            setB.IsDefault = true;
            tools.EntitiesService.SaveEntitySet(_subset.Id, setB);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var allConfigurations = dbContext.EntitySetConfigurations.ToArray();
            var defaultConfigurations = allConfigurations.Where(c => c.IsDefault).ToArray();
            Assert.That(allConfigurations.Length, Is.EqualTo(2));
            Assert.That(defaultConfigurations.Length, Is.EqualTo(1));
            Assert.That(defaultConfigurations.Single().Name, Is.EqualTo(setB.Name));
        }

        [Test]
        public void ShouldBeAbleToDeleteEntitySet()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);

            var model = BuildDefaultEntitySetModel("set");
            model = tools.EntitiesService.CreateEntitySet(_subset.Id, model);
            tools.EntitiesService.DeleteEntitySet(model.Id.Value);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var configurations = dbContext.EntitySetConfigurations.ToArray();
            Assert.That(configurations, Is.Empty);
        }

        [Test]
        public void ShouldNotDeleteFallbackEntitySets()
        {
            var tools = GetEntityManagers(_testMetadataContextFactory);

            var model = BuildDefaultEntitySetModel("set");
            model = tools.EntitiesService.CreateEntitySet(_subset.Id, model);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var config = dbContext.EntitySetConfigurations.Single();
            config.IsFallback = true;
            dbContext.SaveChanges();

            Assert.Throws<InvalidOperationException>(() => tools.EntitiesService.DeleteEntitySet(model.Id.Value));
        }
    }
}

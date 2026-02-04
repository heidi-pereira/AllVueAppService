using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;


namespace Test.BrandVue.SourceData
{
    public class EntitySetRepositoryTests
    {
        private readonly ILoggerFactory _loggerFactory = Substitute.For<ILoggerFactory>();
        private readonly IProductContext _productContext = Substitute.For<IProductContext>();
        private static readonly string DefaultEntityType = "brand";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TestContext.AddFormatter<EntitySet>(obj =>
            {
                if (obj == null) return "null";
                if (!(obj is EntitySet entitySet)) return obj.ToString();

                return $"Name: {entitySet.Name}, Organisation: {entitySet.Organisation}, Instances: {string.Join(",", entitySet.Instances.Select(i => i.Name))}";
            });
        }

        [Test]
        public void BrandEntityGetAllShouldNotThrowWhenNoFallback()
        {
            const string brandEntityType = "Brand";
            const string organisation = "TestOrganisation";
            
            var ukSubset = new Subset {Id = "UK"};
            var brandInstances = new[] {new EntityInstance {Id = 1, Name = "McDonalds"}};
            var allBrandSet = new EntitySet(null, "All Brands", brandInstances, organisation, false, false);

            var entityRepository = Substitute.For<IEntityRepository>();
            entityRepository.GetInstancesOf(Arg.Any<string>(), Arg.Any<Subset>()).Returns(brandInstances);
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            entitySetRepository.Add(allBrandSet, brandEntityType, ukSubset);

            var notUkSubset = new Subset {Id = "NotUK"};
            var actual = entitySetRepository.GetAllFor(brandEntityType, notUkSubset, organisation);
            Assert.That(actual, Is.Empty);
        }

        [Test]
        public void NonBrandEntitiesGetAllShouldNotThrowWhenNoEntitySetsAndNoFallback()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            const string organisation = "TestOrganisation";

            var notUkSubset = new Subset {Id = "NotUK"};

            Assert.That(entitySetRepository.GetAllFor(davidEntityType, notUkSubset, organisation), Is.Empty);
        }

        [Test]
        public void GetAllShouldReturnAllGlobalSetsForSubsetAndEntity()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            const string organisation = "TestOrganisation";
            
            var ukSubset = new Subset {Id = "UK"};
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
                new EntityInstance {Id = 3, Name = "Beckham"}
            };
            var allDavids = new EntitySet(null, "All Davids", davids, organisation, false, false);
            var footballDavids = new EntitySet(null, "Football Davids", new[] {davids[2]}, organisation, false, false);

            entitySetRepository.Add(allDavids, davidEntityType, ukSubset);
            entitySetRepository.Add(footballDavids, davidEntityType, ukSubset);

            var davidEntitySets = entitySetRepository.GetAllFor(davidEntityType, ukSubset, organisation);

            Assert.That(davidEntitySets, Is.EquivalentTo(new[] {allDavids, footballDavids}).Using<EntitySet>(EntitySetEquals));
        }

        [Test]
        public void GetAllShouldReturnAllOrganisationSetsWhenOrganisationNotSpecified()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            var ukSubset = new Subset {Id = "UK"};
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
                new EntityInstance {Id = 3, Name = "Beckham"}
            };
            var allDavids = new EntitySet(null, "All Davids", davids, null, false, false);
            var footballDavids = new EntitySet(null, "Football Davids", new[] {davids[2]}, null, false, false);

            entitySetRepository.Add(allDavids, davidEntityType, ukSubset);
            entitySetRepository.Add(footballDavids, davidEntityType, ukSubset);

            var davidEntitySets = entitySetRepository.GetOrganisationAgnostic(davidEntityType, ukSubset);

            Assert.That(davidEntitySets, Is.EquivalentTo(new[] {allDavids, footballDavids}).Using<EntitySet>(EntitySetEquals));
        }

        [Test]
        [Description("Querying any other entity type will return entity sets with no org specified. Brand is special and will only return entity sets explicitly assigned to the org.")]
        public void GetAllBrandSetsForOrgShouldNotReturnBrandSetsWithoutOrg()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string brandEntityType = "Brand";
            var ukSubset = new Subset {Id = "UK"};
            var brands = new[]
            {
                new EntityInstance {Id = 1, Name = "Brand1"},
                new EntityInstance {Id = 2, Name = "Brand2"},
                new EntityInstance {Id = 3, Name = "Brand3"}
            };
            var brandSetForOrg1 = new EntitySet(null, "All Brands", brands, "org1", false, false);
            var brandsSetOnlySeenByAdminsInBv = new EntitySet(null, "All Brands", new[] {brands[1], brands[2]}, "", false, false);

            entitySetRepository.Add(brandSetForOrg1, brandEntityType, ukSubset);
            entitySetRepository.Add(brandsSetOnlySeenByAdminsInBv, brandEntityType, ukSubset);

            var returnedBrandSetsForOrg1 = entitySetRepository.GetAllFor(brandEntityType, ukSubset, "org1");
            Assert.That(returnedBrandSetsForOrg1, Is.EquivalentTo(new[] {brandSetForOrg1}).Using<EntitySet>(EntitySetEquals));
        }

        [Test]
        public void ShouldGetAllForOrganisation()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            var ukSubset = new Subset {Id = "UK"};
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
                new EntityInstance {Id = 3, Name = "Beckham"}
            };
            var allDavids = new EntitySet(null, "All Davids", davids, "People", false, false);
            var footballDavids = new EntitySet(null, "Football Davids", new[] {davids[2]}, "Football", false, false);

            entitySetRepository.Add(allDavids, davidEntityType, ukSubset);
            entitySetRepository.Add(footballDavids, davidEntityType, ukSubset);

            var people = entitySetRepository.GetAllFor(davidEntityType, ukSubset, "People");

            Assert.That(people, Is.EquivalentTo(new[] {allDavids}).Using<EntitySet>(EntitySetEquals));
        }

        [Test]
        public void ShouldExcludeDuplicates()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            var ukSubset = new Subset { Id = "UK" };
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"}
            };
            var allDavids = new EntitySet(1, "All Davids", davids, "People", false, false);
            var allDavidsDuplicate = new EntitySet(1, "All Davids", davids, "People", false, false);

            entitySetRepository.Add(allDavids, davidEntityType, ukSubset);
            entitySetRepository.Add(allDavidsDuplicate, davidEntityType, ukSubset);

            var people = entitySetRepository.GetAllFor(davidEntityType, ukSubset, "People");

            Assert.That(people.Count, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetAllGlobalSetsForOrganisation()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            var ukSubset = new Subset {Id = "UK"};
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
                new EntityInstance {Id = 3, Name = "Beckham"}
            };

            var allDavids = new EntitySet(null, "All Davids", davids, null, false, false);
            var footballDavids = new EntitySet(null, "Football Davids", new[] {davids[2]}, "Football", false, false);

            entitySetRepository.Add(allDavids, davidEntityType, ukSubset);
            entitySetRepository.Add(footballDavids, davidEntityType, ukSubset);

            var fallbackSet = new EntitySet(null, "Fallback", new []{new EntityInstance{Id=1000,Name="Fallback"} }, "demo", false, false) { IsFallback = true };
            entitySetRepository.Add(fallbackSet, davidEntityType, ukSubset);

            var retrievedFootballDavids = entitySetRepository.GetAllFor(davidEntityType, ukSubset, "Football");
            var otherDavids = entitySetRepository.GetAllFor(davidEntityType, ukSubset, "OtherOrg");

            Assert.Multiple(() =>
            {
                Assert.That(retrievedFootballDavids, Is.EquivalentTo(new[] { allDavids, footballDavids}).Using<EntitySet>(EntitySetEquals));
                Assert.That(otherDavids, Is.EquivalentTo(new [] { allDavids }).Using<EntitySet>(EntitySetEquals));
            });
        }

        [Test]
        public void ShouldGetAllSetsForOrganisationWhenSubsetNotSpecified()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            var ukSubset = new Subset {Id = "UK"};
            var usSubset = new Subset {Id = "US"};
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
                new EntityInstance {Id = 3, Name = "Beckham"}
            };
            var allDavids = new EntitySet(null, "All Davids", davids, "Football", false, false);
            var footballDavids = new EntitySet(null, "Football Davids", new[] {davids[2]}, "Football", false, false);

            entitySetRepository.Add(allDavids, davidEntityType, null);
            entitySetRepository.Add(footballDavids, davidEntityType, ukSubset);

            var ukDavids = entitySetRepository.GetAllFor(davidEntityType, ukSubset, "Football");
            var usDavids = entitySetRepository.GetAllFor(davidEntityType, usSubset, "Football");

            Assert.Multiple(() =>
            {
                Assert.That(ukDavids, Is.EquivalentTo(new[] {allDavids, footballDavids}).Using<EntitySet>(EntitySetEquals));
                Assert.That(usDavids, Is.EquivalentTo(new[] {allDavids}).Using<EntitySet>(EntitySetEquals));
            });
        }

        [Test]
        public void ShouldGetAllSetsForOrganisationWhenSubsetAndOrganisationNotSpecified()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string davidEntityType = "David";
            var ukSubset = new Subset {Id = "UK"};
            var davids = new[]
            {
                new EntityInstance {Id = 1, Name = "Attenborough"},
                new EntityInstance {Id = 2, Name = "Connell"},
                new EntityInstance {Id = 3, Name = "Beckham"}
            };
            var allDavids = new EntitySet(null, "All Davids", davids, null, false, false);
            var footballDavids = new EntitySet(null, "Football Davids", new[] {davids[2]}, "Football", false, false);

            entitySetRepository.Add(allDavids, davidEntityType, null);
            entitySetRepository.Add(footballDavids, davidEntityType, ukSubset);

            var ukDavids = entitySetRepository.GetAllFor(davidEntityType, ukSubset, "Football");
            Assert.That(ukDavids, Is.EquivalentTo(new[] {allDavids, footballDavids}).Using<EntitySet>(EntitySetEquals));
        }

        [Test]
        public void ShouldGetFallbackSetsForSpecificSubset()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string brandEntityType = "Brand";
            var ukSubset = new Subset {Id = "UK"};
            var usSubset = new Subset {Id = "US"};
            var frSubset = new Subset {Id = "FR"};

            var brands = new[]
            {
                new EntityInstance {Id = 1, Name = "British brand"},
                new EntityInstance {Id = 2, Name = "American brand"},
                new EntityInstance {Id = 3, Name = "French brand"}
            };

            var britishFallback = new EntitySet(null, "BritishFallback", new[] {brands[0]}, "demo", false, false) { IsFallback = true };
            var americanFallback = new EntitySet(null, "AmericanFallback", new[] {brands[1]}, "demo", false, false) { IsFallback = true };
            var frenchFallback = new EntitySet(null, "FrenchFallback", new[] {brands[2]}, "demo", false, false) { IsFallback = true };

            entitySetRepository.Add(britishFallback, brandEntityType, ukSubset);
            entitySetRepository.Add(americanFallback, brandEntityType, usSubset);
            entitySetRepository.Add(frenchFallback, brandEntityType, frSubset);

            var americanBrandSets = entitySetRepository.GetAllFor(brandEntityType, usSubset, "savanta");
            var defaultAmericanBrandSet = entitySetRepository.GetDefaultSetForOrganisation(brandEntityType, usSubset, "savanta");

            Assert.Multiple(() =>
            {
                Assert.That(americanBrandSets, Is.EquivalentTo(new[] {americanFallback}));
                Assert.That(defaultAmericanBrandSet, Is.EqualTo(americanFallback));
            });
        }

        [Test]
        public void ShouldFallBackOnSectorSubsetLast()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string brandEntityType = "Brand";
            var ukSubset = new Subset {Id = "UK"};
            var usSubset = new Subset {Id = "US"};
            var frSubset = new Subset {Id = "FR"};

            var brands = new[]
            {
                new EntityInstance {Id = 1, Name = "British brand"},
                new EntityInstance {Id = 2, Name = "American brand"},
                new EntityInstance {Id = 3, Name = "French brand"}
            };

            var britishSector = new EntitySet(null, "BritishFallback", new[] {brands[0]}, "demo", true, false) { IsFallback = true };
            var american = new EntitySet(null, "American", new[] {brands[1]}, "demo", false, false) { IsFallback = true };
            var americanSector = new EntitySet(null, "AmericanSector", new[] {brands[1]}, "demo", true, false) { IsFallback = true };
            var frenchFallback = new EntitySet(null, "FrenchFallback", new[] {brands[2]}, "demo", false, false) { IsFallback = true };

            entitySetRepository.Add(britishSector, brandEntityType, ukSubset);
            entitySetRepository.Add(american, brandEntityType, usSubset);
            entitySetRepository.Add(americanSector, brandEntityType, usSubset);
            entitySetRepository.Add(frenchFallback, brandEntityType, frSubset);

            var defaultAmericanBrandSet = entitySetRepository.GetDefaultSetForOrganisation(brandEntityType, usSubset, "savanta");
            var defaultBritishBrandSet = entitySetRepository.GetDefaultSetForOrganisation(brandEntityType, ukSubset, "savanta");

            Assert.Multiple(() =>
            {
                Assert.That(defaultAmericanBrandSet, Is.EqualTo(american));
                Assert.That(defaultBritishBrandSet, Is.EqualTo(britishSector));
            });
        }

        [Test]
        public void ShouldAlwaysGetSectorSets()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string testEntityType = "Test";
            var ukSubset = new Subset {Id = "UK"};
            var testInstances = new[]
            {
                new EntityInstance {Id = 1, Name = "One"},
                new EntityInstance {Id = 2, Name = "Two"},
                new EntityInstance {Id = 3, Name = "Three"}
            };

            var sectorSet = new EntitySet(null, "Sector set", testInstances, null, true, false);
            var organisationSet = new EntitySet(null, "Organisation set", new[] {testInstances[2]}, "testOrg", false, false);

            entitySetRepository.Add(sectorSet, testEntityType, ukSubset);
            entitySetRepository.Add(organisationSet, testEntityType, ukSubset);

            var fallbackSet = new EntitySet(null, "Fallback", new[] {new EntityInstance {Id = 1000, Name = "Fallback"}}, "demo", false, false) { IsFallback = true };
            entitySetRepository.Add(fallbackSet, testEntityType, ukSubset);

            var retrievedFootballDavids = entitySetRepository.GetAllFor(testEntityType, ukSubset, "testOrg");
            var otherDavids = entitySetRepository.GetAllFor(testEntityType, ukSubset, "OtherOrg");

            Assert.Multiple(() =>
            {
                Assert.That(retrievedFootballDavids, Is.EquivalentTo(new[] {sectorSet, organisationSet}).Using<EntitySet>(EntitySetEquals));
                Assert.That(otherDavids, Is.EquivalentTo(new[] { sectorSet}).Using<EntitySet>(EntitySetEquals));
            });
        }

        [Test]
        public void ShouldGetDefaultSet()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string testEntityType = "Test";
            const string myOrganisation = "MyOrganisation";
            var ukSubset = new Subset {Id = "UK"};
            var testInstances = new[]
            {
                new EntityInstance {Id = 1, Name = "One"},
                new EntityInstance {Id = 2, Name = "Two"},
                new EntityInstance {Id = 3, Name = "Three"}
            };
            
            var setOne = new EntitySet(null, "Set One", testInstances, myOrganisation, false, false);
            var setTwo = new EntitySet(null, "Set Two", new[] {testInstances[2]}, myOrganisation, false, false);
            var defaultSet = new EntitySet(null, "Default set", new[] {testInstances[1]}, myOrganisation, false, true);
            
            entitySetRepository.Add(setOne, testEntityType, ukSubset);
            entitySetRepository.Add(setTwo, testEntityType, ukSubset);
            entitySetRepository.Add(defaultSet, testEntityType, ukSubset);

            var retrievedDefaultSet = entitySetRepository.GetDefaultSetForOrganisation(testEntityType, ukSubset, myOrganisation);
            Assert.That(retrievedDefaultSet.Name, Is.EqualTo(defaultSet.Name));
        }
        
        [Test]
        public void ShouldGetFirstSetWhenNoDefaultConfigured()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string testEntityType = "Test";
            const string myOrganisation = "MyOrganisation";
            var ukSubset = new Subset {Id = "UK"};
            var testInstances = new[]
            {
                new EntityInstance {Id = 1, Name = "One"},
                new EntityInstance {Id = 2, Name = "Two"},
                new EntityInstance {Id = 3, Name = "Three"}
            };
            
            var setOne = new EntitySet(null, "Set One", testInstances, myOrganisation, false, false);
            var setTwo = new EntitySet(null, "Set Two", new[] {testInstances[2]}, myOrganisation, false, false);
            var setThree = new EntitySet(null, "Not the default set", new[] {testInstances[1]}, myOrganisation, false, false);
            
            entitySetRepository.Add(setOne, testEntityType, ukSubset);
            entitySetRepository.Add(setTwo, testEntityType, ukSubset);
            entitySetRepository.Add(setThree, testEntityType, ukSubset);

            var retrievedDefaultSet = entitySetRepository.GetDefaultSetForOrganisation(testEntityType, ukSubset, myOrganisation);
            Assert.That(retrievedDefaultSet.Name, Is.EqualTo(setOne.Name));
        }

        [Test]
        public void ShouldNotHaveAverageWhenAverageIsNotLoaded()
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            const string brandEntityType = "Brand";
            var ukSubset = new Subset { Id = "UK" };
            var brands = new[]
            {
                new EntityInstance {Id = 1, Name = "Brand1"},
                new EntityInstance {Id = 2, Name = "Brand2"},
                new EntityInstance {Id = 3, Name = "Brand3"}
            };
            var brandSet1ForOrg1 = new EntitySet(1, "All Brands", brands, "org1", false, false);
            var brandSet2ForOrg1 = new EntitySet(2, "Other Brands", brands, "org1", false, false);
            var brandSetOnlySeenByAdminsInBv = new EntitySet(3, "All Brands", new[] { brands[1], brands[2] }, "AnotherOrg", false, false);
            brandSet1ForOrg1.Averages = new[] {
                new EntitySetAverageMappingConfiguration() {
                    ParentEntitySetId = 1,
                    ChildEntitySetId = 2,
                    ChildEntitySetConfiguration = new EntitySetConfiguration() {Id = 2, Organisation = "org1", Name = "Other Brands"},
                },
                new EntitySetAverageMappingConfiguration() {
                    ParentEntitySetId = 1,
                    ChildEntitySetId = 3,
                    ChildEntitySetConfiguration = new EntitySetConfiguration() {Id = 3, Organisation = "AnotherOrg", Name = "All Brands"},
                }
            };

            entitySetRepository.Add(brandSet1ForOrg1, brandEntityType, ukSubset);
            entitySetRepository.Add(brandSet2ForOrg1, brandEntityType, ukSubset);
            entitySetRepository.Add(brandSetOnlySeenByAdminsInBv, brandEntityType, ukSubset);

            var returnedBrandSetsForOrg1 = entitySetRepository.GetAllFor(brandEntityType, ukSubset, "org1");
            var actualChildEntitySetIds = returnedBrandSetsForOrg1.First().Averages.Select(a => a.ChildEntitySetId);
            Assert.That(actualChildEntitySetIds, Is.EquivalentTo(2.Yield()));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, and the first has a null Id, that one of the others gets picked.")]
        public void CheckAllEntitySetsDeDupeProperly_ByNullId(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(null, "All", null,      "TestOrg",    false,         false),
                    new EntitySet(1439, "All", null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(1439));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, and one has an organisation, that one gets picked.")]
        public void CheckAllEntitySetsDeDupeProperly_ByOrganisation(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "All", null,      "TestOrg",    false,         false),
                    new EntitySet(1439, "All", null,      null,         false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(1));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, that pick the first one with a non-null subset, ordered ascending.")]
        public void CheckAllEntitySetsDeDupeProperly_BySubset(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "All", null,      "TestOrg",    false,         false),
                    new EntitySet(6442, "All", null,      "TestOrg",    false,         false)
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();
            // Specifically, for this test, we need to change the subset from null to "UK" for the first tuple.
            entitySetTuples[0].Subset = "UK";

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(1));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, and at least one has IsDefault set to true, that we pick one that is a default.")]
        public void CheckAllEntitySetsDeDupeProperly_ByIsDefaultFlag(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "All", null,      "TestOrg",    false,         true),
                    new EntitySet(6442, "All", null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(1));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, and at least one has IsSectorSet set to true, that we pick one that is a sector set.")]
        public void CheckAllEntitySetsDeDupeProperly_ByIsSectorSetFlag(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "All", null,      "TestOrg",    true,          false),
                    new EntitySet(6442, "All", null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(1));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, and at least one has IsFallback set to true, that we pick one that is a fallback.")]
        public void CheckAllEntitySetsDeDupeProperly_ByIsFallbackFlag(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "All", null,      "TestOrg",    false,         false) {IsFallback = true},
                    new EntitySet(6442, "All", null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(1));
        }

        [Theory]
        [Description("Check that, if there are multiple 'All' entity sets, and more than one has a non-null Id, that we pick the one with the highest Id.")]
        public void CheckAllEntitySetsDeDupeProperly_ByHighestId(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,  Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "All", null,      "TestOrg",    false,         false),
                    new EntitySet(6442, "All", null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var allEntitySet = actualEntitySets.Single(es => es.Name == "All");
            Assert.That(allEntitySet.Id, Is.EqualTo(6442));
        }

        [Theory]
        [Description("Check that that GetForAll only returns entity sets that have the correct subset or a null subset.")]
        public void Check_GetForAll_ReturnsForCorrectSubsetOrNull(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,            Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "cc",            null,      "TestOrg",    false,         false),
                    new EntitySet(1439, "HSBC Products", null,      "TestOrg",    false,         false),
                    new EntitySet(6442, "All",           null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();
            // Specifically, for this test, we need to have one entity set have a null subset, and the other two have valid but different subsets.
            entitySetTuples[0].Subset = "UK";
            entitySetTuples[2].Subset = "HS";

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            Assert.Multiple(() =>
            {
                Assert.That(actualEntitySets.Count, Is.EqualTo(2));
                Assert.That(actualEntitySets.Count(es => es.Id == 1), Is.EqualTo(1)); // Subset == UK
                Assert.That(actualEntitySets.Count(es => es.Id == 1439), Is.EqualTo(1)); // Subset == null
            });
        }

        [Theory]
        [Description("Check that that GetForAll only returns one entity set per unique entity set name.")]
        public void Check_GetForAll_DedupesAllEntitySetsGroupedByName(bool reverseInput)
        {
            var entitySets =
                new[]
                {
                    //            Id,   Name,            Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,    "cc",            null,      "TestOrg",    false,         false),
                    new EntitySet(null, "cc",            null,      "TestOrg",    false,         false),
                    new EntitySet(12,   "cc",            null,      "TestOrg",    false,         false),
                    new EntitySet(null, "HSBC Products", null,      "TestOrg",    false,         false),
                    new EntitySet(1440, "HSBC Products", null,      "TestOrg",    false,         false),
                    new EntitySet(1441, "HSBC Products", null,      "TestOrg",    false,         false),
                    new EntitySet(6442, "All",           null,      "TestOrg",    false,         false),
                    new EntitySet(null, "All",           null,      "TestOrg",    false,         false),
                    new EntitySet(6444, "All",           null,      "TestOrg",    false,         false),
                    new EntitySet(6445, "Other1",        null,      "TestOrg",    false,         false),
                    new EntitySet(6446, "Other2",        null,      "OtherOrg",    false,         false),
                    new EntitySet(6447, "Other3",        null,      "TestOrg",    false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();
            // Specifically, for this test, we want to have the same, valid subset for (12, "cc") and (null, "cc").
            for (int i = 0; i < entitySetTuples.Length; i++)
            {
                if (entitySetTuples[i].EntitySet.Id is 12 or null && entitySetTuples[i].EntitySet.Name == "cc")
                    entitySetTuples[i].Subset = "UK";
                else if (entitySetTuples[i].EntitySet.Id is 1441 or null && entitySetTuples[i].EntitySet.Name == "HSBC Products")
                    entitySetTuples[i].Subset = "UK";
                else if (entitySetTuples[i].EntitySet.Id is 6444 or null && entitySetTuples[i].EntitySet.Name == "All")
                    entitySetTuples[i].Subset = "UK";
            }

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, reverseInput);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");

            Assert.Multiple(() =>
            {
                Assert.That(actualEntitySets.Count, Is.EqualTo(5));
                Assert.That(actualEntitySets.Count(es => es.Id == 12), Is.EqualTo(1));
                Assert.That(actualEntitySets.Count(es => es.Id == 1441), Is.EqualTo(1));
                Assert.That(actualEntitySets.Count(es => es.Id == 6444), Is.EqualTo(1));
                Assert.That(actualEntitySets.Count(es => es.Id == 6445), Is.EqualTo(1));
                Assert.That(actualEntitySets.Count(es => es.Id == 6447), Is.EqualTo(1));
            });
        }

        [Test]
        [Description("Check that that GetForAll only returns one entity set per unique entity set name.")]
        public void Check_EntityAveragesAreCorrect()
        {
            var entitySets =
                new[]
                {
                    //            Id, Name, Instances, Organisation, Is Sector Set, Is Default
                    new EntitySet(1,  "cc", null,      "TestOrg",    false,         false),
                    new EntitySet(2,  "cc", null,      "TestOrg",    false,         false),
                    new EntitySet(3,  "cc", null,      null,         false,         false),
                    new EntitySet(4,  "cc", null,      "OtherOrg",   false,         false),
                };

            var entitySetTuples = WrapEntitySetsInNullSubsetTuples(entitySets).ToArray();

            var childEntitySetConfiguration1 = new EntitySetConfiguration() {Id = 1, Organisation = "TestOrg", Name = "cc"};
            var childEntitySetConfiguration2 = new EntitySetConfiguration() {Id = 2, Organisation = "TestOrg", Name = "cc"};
            var childEntitySetConfiguration3 = new EntitySetConfiguration() {Id = 3, Organisation = null, Name = "cc"};
            var childEntitySetConfiguration4 = new EntitySetConfiguration() {Id = 4, Organisation = "OtherOrg", Name = "cc"};

            entitySetTuples[1].EntitySet.Averages =
            [
                new () { Id = 1, ParentEntitySetId = 2, ChildEntitySetId = 2, ChildEntitySetConfiguration = childEntitySetConfiguration2 },
                new () { Id = 2, ParentEntitySetId = 2, ChildEntitySetId = 3, ChildEntitySetConfiguration = childEntitySetConfiguration3 },
                new () { Id = 3, ParentEntitySetId = 2, ChildEntitySetId = 4, ChildEntitySetConfiguration = childEntitySetConfiguration4 },
            ];

            var entitySetRepository = CreateEntitySetRepository(entitySetTuples, false);
            var actualEntitySets = entitySetRepository.GetAllFor(DefaultEntityType, new Subset() { Id = "UK" }, "TestOrg");
            var entitySet = actualEntitySets.First();

            Assert.Multiple(() =>
            {
                Assert.That(entitySet.Id, Is.EqualTo(2));
                Assert.That(entitySet.Averages.Length, Is.EqualTo(2));
                Assert.That(entitySet.Averages[0].ParentEntitySetId, Is.EqualTo(2));
                Assert.That(entitySet.Averages[0].ChildEntitySetId, Is.EqualTo(2));
                Assert.That(entitySet.Averages[1].ParentEntitySetId, Is.EqualTo(2));
                Assert.That(entitySet.Averages[1].ChildEntitySetId, Is.EqualTo(3));
            });
        }

        private EntitySetRepository CreateEntitySetRepository((string Subset, EntitySet EntitySet)[] entitySets,
            bool reverseInput)
        {
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);

            if (reverseInput)
                entitySets = entitySets.AsEnumerable().Reverse().ToArray();

            foreach (var entitySet in entitySets)
                entitySetRepository.Add(entitySet.EntitySet, DefaultEntityType, entitySet.Subset ==  null ? null : new Subset() {Id = entitySet.Subset});

            return entitySetRepository;
        }

        private IEnumerable<(string Subset, EntitySet EntitySet)> WrapEntitySetsInNullSubsetTuples(IEnumerable<EntitySet> entitySets)
        {
            foreach (var entitySet in entitySets)
            {
                yield return (null, entitySet);
            }
        }

        [Test]
        public void Test_AddEntitySets_DiffTypes()
        {
            // Arrange
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var ukSubset = new Subset { Id = "UK" };
            const string entityType1 = "EntitySetType1";
            const string entityType2 = "EntitySetType2";
            const string organisation = "TestOrg";

            // Act
            entitySetRepository.Add(new EntitySet(1, "Entity Set Name 1", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(2, "Entity Set Name 2", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(3, "Entity Set Name 3", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(4, "Entity Set Name 4", null, organisation, false, false), entityType2, ukSubset);
            entitySetRepository.Add(new EntitySet(5, "Entity Set Name 5", null, organisation, false, false), entityType2, ukSubset);

            // Assert
            int?[] expected1 = [1, 2, 3];
            int?[] expected2 = [4, 5];

            Assert.That(expected1, Is.EquivalentTo(entitySetRepository!
                .GetAllFor(entityType1, ukSubset, organisation)
                .Select(s => s.Id)
                .ToArray()));

            Assert.That(expected2, Is.EquivalentTo(entitySetRepository!
                .GetAllFor(entityType2, ukSubset, organisation)
                .Select(s => s.Id)
                .ToArray()));

        }

        [Test]
        public void Test_AddEntitySets_DiffOrgs()
        {
            // Arrange
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var ukSubset = new Subset { Id = "UK" };
            const string entityType1 = "EntitySetType1";
            const string organisation1 = "TestOrg1";
            const string organisation2 = "TestOrg2";

            // Act
            entitySetRepository.Add(new EntitySet(1, "Entity Set Name 1", null, organisation1, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(2, "Entity Set Name 2", null, organisation1, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(3, "Entity Set Name 3", null, organisation1, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(4, "Entity Set Name 4", null, organisation2, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(5, "Entity Set Name 5", null, organisation2, false, false), entityType1, ukSubset);

            // Assert
            int?[] expected1 = [ 1, 2, 3 ];
            int?[] expected2 = [ 4, 5];
            
            Assert.That(expected1,Is.EquivalentTo(entitySetRepository!
                .GetAllFor(entityType1, ukSubset, organisation1)
                .Select(s => s.Id)
                .ToArray()));

            Assert.That(expected2, Is.EquivalentTo(entitySetRepository!
                .GetAllFor(entityType1, ukSubset, organisation2)
                .Select(s => s.Id)
                .ToArray()));
            
        }

        [Test]
        public void Test_AddEntitySets()
        {
            // Arrange
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var ukSubset = new Subset { Id = "UK" };
            const string entityType1 = "EntitySetType1";
            const string organisation = "TestOrg";

            // Act
            entitySetRepository.Add(new EntitySet(1, "Entity Set Name 1", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(2, "Entity Set Name 2", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(3, "Entity Set Name 3", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(4, "Entity Set Name 4", null, organisation, false, false), entityType1, ukSubset);
            entitySetRepository.Add(new EntitySet(5, "Entity Set Name 5", null, organisation, false, false), entityType1, ukSubset);



            //Assert
            int?[] expected = [1, 2, 3, 4, 5];
            int?[] res = entitySetRepository!
                .GetAllFor(entityType1, ukSubset, organisation)
                .Select(s => s.Id)
                .ToArray();

            Assert.That(res,Is.EquivalentTo(expected));
            
        }

        [Test]
        public void Test_RemoveEntitySets()
        {
            // Arrange
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var ukSubset = new Subset { Id = "UK" };
            const string entityType = "EntitySetType1";
            const string organisation = "TestOrg";

            entitySetRepository.Add(new EntitySet(1, "Entity Set Name 1", null, organisation, false, false), entityType, ukSubset);
            entitySetRepository.Add(new EntitySet(2, "Entity Set Name 2", null, organisation, false, false), entityType, ukSubset);
            entitySetRepository.Add(new EntitySet(3, "Entity Set Name 3", null, organisation, false, false), entityType, ukSubset);
            entitySetRepository.Add(new EntitySet(4, "Entity Set Name 4", null, organisation, false, false), entityType, ukSubset);
            entitySetRepository.Add(new EntitySet(5, "Entity Set Name 5", null, organisation, false, false), entityType, ukSubset);

            var entitySetToRemove1 = new EntitySet(1, "Entity Set Name 1", null, organisation, false, false);
            var entitySetToRemove2 = new EntitySet(4, "Entity Set Name 4", null, organisation, false, false);
            
            // Act
            entitySetRepository.Remove(entitySetToRemove1, entityType, ukSubset);
            entitySetRepository.Remove(entitySetToRemove2, entityType, ukSubset);

            //Assert
            int?[] expected = [ 2, 3 ,5];
            int?[] res = entitySetRepository!
                .GetAllFor(entityType, ukSubset, organisation)
                .Select(s => s.Id)
                .ToArray();

            Assert.That(res, Is.EquivalentTo(expected));
        }

        [Test]
        public void Test_RemoveEntitySets_DuplicateNameDiffOrg()
        {
            // Arrange
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var ukSubset = new Subset { Id = "UK" };
            const string entityType = "EntitySetType1";
            const string entitySetName = "Entity Set Name 1";
            const string org1Name = "Org1";
            const string org2Name = "Org2";
            var entitySetType1Id1Org1 = new EntitySet(1, entitySetName, null, org1Name, false, false);
            var entitySetType1Id2Org2 = new EntitySet(2, entitySetName, null, org2Name, false, false);
            entitySetRepository.Add(entitySetType1Id1Org1, entityType, ukSubset);
            entitySetRepository.Add(entitySetType1Id2Org2, entityType, ukSubset);

            // Act
            entitySetRepository.Remove(entitySetType1Id2Org2, entityType, ukSubset);

            // Assert
            int?[] expected = [1];
            int?[] res1 = entitySetRepository!
                .GetAllFor(entityType, ukSubset, org1Name)
                .Select(s => s.Id)
                .ToArray();

            var res2 = entitySetRepository!
                .GetAllFor(entityType, ukSubset, org2Name);

            Assert.That(res1, Is.EquivalentTo(expected));

            Assert.That(res2, Is.Empty);

        }

        [Test]
        public void Test_RemoveEntitySets_DuplicateNameNullOrg()
        {
            // Arrange
            var entitySetRepository = new EntitySetRepository(_loggerFactory, _productContext);
            var ukSubset = new Subset { Id = "UK" };
            const string entityType = "EntitySetType1";
            const string entitySetName = "Entity Set Name 1";
            const string org1Name = "Org1";
            var entitySetType1Id1Org1 = new EntitySet(1, entitySetName, null, org1Name, false, false);
            var entitySetType1Id2NullOrg = new EntitySet(2, entitySetName, null, null, false, false);
            entitySetRepository.Add(entitySetType1Id1Org1, entityType, ukSubset);
            entitySetRepository.Add(entitySetType1Id2NullOrg, entityType, ukSubset);

            // Act
            entitySetRepository.Remove(entitySetType1Id2NullOrg, entityType, ukSubset);

            // Assert
            int?[] expected = [1];
            int?[] res1 = entitySetRepository!
                .GetAllFor(entityType, ukSubset, org1Name)
                .Select(s => s.Id)
                .ToArray();

            var res2 = entitySetRepository!
                .GetAllFor(entityType, ukSubset, null);

            Assert.That(res1, Is.EquivalentTo(expected));

            Assert.That(res2, Is.Empty);

        }



        private bool EntitySetEquals(EntitySet x, EntitySet y)
        {
            return x.Name == y.Name
                   && x.Instances.Length == y.Instances.Length;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Subsets;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData.Entity;

[TestFixture]
public class EntityRepositoryConfiguratorTests
{
    private EntityInstanceRepository _entityInstanceRepository;
    private EntityTypeRepository _entityTypeRepository;
    private EntityRepositoryConfigurator _configurator;
    private FallbackSubsetRepository _subsetRepository;

    [SetUp]
    public void Setup()
    {
        _subsetRepository = new FallbackSubsetRepository();
        _entityInstanceRepository = new EntityInstanceRepository();
        _entityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
        _configurator = new EntityRepositoryConfigurator(_entityInstanceRepository, _entityTypeRepository, _subsetRepository);
    }

    [Test]
    public void ApplyConfiguredEntityInstances_ValidInput_AppliesConfigurations()
    {
        // Arrange
        var entityTypeIdentifier = "Brand";
        var surveyChoiceId = 1;
        var subsetId = _subsetRepository.First().Id;
        var entityInstanceConfigurations = new List<EntityInstanceConfiguration>
        {
            new()
            {
                EntityTypeIdentifier = entityTypeIdentifier,
                SurveyChoiceId = surveyChoiceId,
                DisplayNameOverrideBySubset = new Dictionary<string, string> { { subsetId, "OverriddenName" } },
                EnabledBySubset = new Dictionary<string, bool> { { subsetId, true } },
                StartDateBySubset = new Dictionary<string, DateTimeOffset> { { subsetId, DateTime.Now } }
            }
        };

        var expectedInstance = new EntityInstance
        {
            Id = surveyChoiceId,
            Name = "OverriddenName",
            DefaultColor = "Blue",
            Identifier = "Identifier1",
            Subsets = new[] { new Subset { Id = subsetId } },
            EnabledBySubset = new Dictionary<string, bool> { { subsetId, true } },
            StartDateBySubset = new Dictionary<string, DateTimeOffset> { { subsetId, entityInstanceConfigurations[0].StartDateBySubset[subsetId] } }
        };

        var existingInstance = new EntityInstance
        {
            Id = surveyChoiceId,
            Name = "OriginalName",
            DefaultColor = "Blue",
            Identifier = "Identifier1",
            Subsets = [new Subset { Id = subsetId }]
        };

        // Add the existing instance and entity type to our repositories
        _entityInstanceRepository.Add(_entityTypeRepository.DefaultEntityType, existingInstance);

        // Act
        _configurator.ApplyConfiguredEntityInstances(entityInstanceConfigurations);

        // Assert
        var updatedInstances = _entityInstanceRepository.GetInstancesAnySubset(entityTypeIdentifier).ToList();

        Assert.That(updatedInstances, Has.Count.EqualTo(1));
        Assert.That(updatedInstances[0], Is.EqualTo(expectedInstance).Using(EntityInstance.ExactlyEquivalentEqualityComparer.Instance));
    }

    [Test]
    public void ApplyConfiguredEntityInstances_DifferentSubset_DoesNotOverrideDisplayName()
    {
        // Arrange
        var entityTypeIdentifier = "Brand";
        var surveyChoiceId = 1;
        var configuredSubsetId = _subsetRepository.First().Id;
        var differentSubsetId = _subsetRepository.Skip(1).First().Id;

        var entityInstanceConfigurations = new List<EntityInstanceConfiguration>
        {
            new()
            {
                EntityTypeIdentifier = entityTypeIdentifier,
                SurveyChoiceId = surveyChoiceId,
                DisplayNameOverrideBySubset = new Dictionary<string, string> { { configuredSubsetId, "OverriddenName" } },
                EnabledBySubset = new Dictionary<string, bool> { { configuredSubsetId, true }, { differentSubsetId, true } },
                StartDateBySubset = new Dictionary<string, DateTimeOffset>
                {
                    { configuredSubsetId, DateTime.Now },
                    { differentSubsetId, DateTime.Now }
                }
            }
        };

        var existingInstance = new EntityInstance
        {
            Id = surveyChoiceId,
            Name = "OriginalName",
            DefaultColor = "Blue",
            Identifier = "Identifier1",
            Subsets = new[] { new Subset { Id = configuredSubsetId }, new Subset { Id = differentSubsetId } }
        };

        // Add the existing instance and entity type to our repositories
        _entityInstanceRepository.Add(_entityTypeRepository.DefaultEntityType, existingInstance);

        // Act
        _configurator.ApplyConfiguredEntityInstances(entityInstanceConfigurations);

        // Assert
        var updatedInstances = _entityInstanceRepository.GetInstancesAnySubset(entityTypeIdentifier).ToList();
        Assert.That(updatedInstances, Has.Count.EqualTo(2));

        var configuredSubsetInstance = updatedInstances.FirstOrDefault(i => i.Subsets.Any(s => s.Id == configuredSubsetId));
        var differentSubsetInstance = updatedInstances.FirstOrDefault(i => i.Subsets.Any(s => s.Id == differentSubsetId));

        Assert.That(configuredSubsetInstance.Name, Is.EqualTo("OverriddenName"));
        Assert.That(differentSubsetInstance.Name, Is.EqualTo("OriginalName"));
    }

    [Test]
    public void GetInstancesAnySubset_EntityWithMultipleSubsets_ReturnsAllInstancesAndSubsetsForInstances()
    {
        // Arrange;
        var entityTypeIdentifier = "Brand";
        var surveyChoiceId = 1;
        var subsets = _subsetRepository.Take(2).ToArray();

        var existingInstance = new EntityInstance
        {
            Id = surveyChoiceId,
            Name = "OriginalName",
            DefaultColor = "Blue",
            Identifier = "Identifier1",
            Subsets = subsets
        };

        var entityInstanceConfigurations = new List<EntityInstanceConfiguration>
        {
            new()
            {
                EntityTypeIdentifier = entityTypeIdentifier,
                SurveyChoiceId = surveyChoiceId,
                DisplayNameOverrideBySubset = subsets.ToDictionary(subset => subset.Id, _ => "OverriddenName"),
                EnabledBySubset = subsets.ToDictionary(subset => subset.Id, _ => true),
                StartDateBySubset = subsets.ToDictionary(subset => subset.Id, _ => DateTimeOffset.Now)
            }
        };

        _entityInstanceRepository.Add(_entityTypeRepository.DefaultEntityType, existingInstance);

        // Act
        _configurator.ApplyConfiguredEntityInstances(entityInstanceConfigurations);

        // Assert
        var result = _entityInstanceRepository.GetInstancesAnySubset(entityTypeIdentifier);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result.Single().Subsets, Is.EqualTo(subsets));
    }
    
    [Test]
    public void GetInstancesAnySubset_EntityWithMultipleSubsets_ReturnsAllInstancesWithDifferentNamesForDifferentSubsets()
    {
        // Arrange;
        var entityTypeIdentifier = "Brand";
        var surveyChoiceId = 1;
        var subsets = _subsetRepository.Take(2).ToArray();
        var subset1 = subsets.First();
        var subset2 = subsets.Skip(1).First();

        var existingInstance = new EntityInstance
        {
            Id = surveyChoiceId,
            Name = "OriginalName",
            DefaultColor = "Blue",
            Identifier = "Identifier1",
            Subsets = subsets
        };

        var entityInstanceConfigurations = new List<EntityInstanceConfiguration>
        {
            new()
            {
                EntityTypeIdentifier = entityTypeIdentifier,
                SurveyChoiceId = surveyChoiceId,
                DisplayNameOverrideBySubset = subsets.ToDictionary(subset => subset.Id, subset => $"OverriddenName_{subset}"),
                EnabledBySubset = subsets.ToDictionary(subset => subset.Id, _ => true),
                StartDateBySubset = subsets.ToDictionary(subset => subset.Id, _ => DateTimeOffset.Now)
            }
        };

        _entityInstanceRepository.Add(_entityTypeRepository.DefaultEntityType, existingInstance);

        // Act
        _configurator.ApplyConfiguredEntityInstances(entityInstanceConfigurations);

        // Assert
        var result = _entityInstanceRepository.GetInstancesAnySubset(entityTypeIdentifier);

        Assert.That(result, Has.Count.EqualTo(2));

        var instance1 = result.First();
        var instance2 = result.Skip(1).First();

        Assert.That(instance1.Name, Is.EqualTo($"OverriddenName_{subset1}"));
        Assert.That(instance1.Subsets, Is.EqualTo(subset1.Yield()));

        Assert.That(instance2.Name, Is.EqualTo($"OverriddenName_{subset2}"));
        Assert.That(instance2.Subsets, Is.EqualTo(subset2.Yield()));
    }

    [Test]
    public void ApplyConfiguredEntityInstances_NoSubsetsConfigured_AppliesToAllSubsets()
    {
        // Arrange
        var entityTypeIdentifier = "Brand";
        var surveyChoiceId = 1;
        var subsets = _subsetRepository.Take(2).ToArray();

        var entityInstanceConfigurations = new List<EntityInstanceConfiguration>
        {
            new()
            {
                EntityTypeIdentifier = entityTypeIdentifier,
                SurveyChoiceId = surveyChoiceId,
                DisplayNameOverrideBySubset = new Dictionary<string, string>(),
                EnabledBySubset = new Dictionary<string, bool>(),
                StartDateBySubset = new Dictionary<string, DateTimeOffset>()
            }
        };

        var existingInstance = new EntityInstance
        {
            Id = surveyChoiceId,
            Name = "OriginalName",
            DefaultColor = "Blue",
            Identifier = "Identifier1",
            Subsets = Array.Empty<Subset>()
        };

        _entityInstanceRepository.Add(_entityTypeRepository.DefaultEntityType, existingInstance);

        // Act
        _configurator.ApplyConfiguredEntityInstances(entityInstanceConfigurations);

        // Assert
        var updatedInstances = _entityInstanceRepository.GetInstancesAnySubset(entityTypeIdentifier).ToList();

        Assert.That(updatedInstances, Has.Count.EqualTo(1));
        Assert.That(updatedInstances[0].Subsets, Is.EqualTo(subsets));
        Assert.That(updatedInstances[0].Name, Is.EqualTo("OriginalName"));
    }
}
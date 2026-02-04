using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData.Configuration;

internal class InMemoryRepositoryUpdatingEntitySetConfigurationRepositoryTests
{
    /// <remarks>
    /// These cases are bundled together because the test uses an in memory database which is quite slow to initialize
    /// </remarks>
    [Test]
    public void CreateUpdateAndDeleteEntitySetReflectedInRepository()
    {
        var answerChoiceSet = new ChoiceSet{Name = "AnEntityType", Choices = new[]{new Choice{ChoiceId = 1, Name = "London"}, new Choice{ChoiceId = 2, Name = "Not London"}}};
        var calculatorBuilder = new ProductionCalculatorBuilder().IncludeQuestions(new Question(){VarCode = "region", QuestionText = "", AnswerChoiceSet = answerChoiceSet}).BuildRealCalculatorWithInMemoryDb();
        var loader = calculatorBuilder.DataLoader;
        var productContext = calculatorBuilder.ProductContext;
        var entitySetConfigurationRepository = new EntitySetConfigurationRepositorySql(loader.MetaDataContextFactory, productContext);
        var repoToTest = new InMemoryRepositoryUpdatingEntitySetConfigurationRepository(entitySetConfigurationRepository, new EntitySetConfigurationLoader(entitySetConfigurationRepository, loader.EntitySetRepository, loader.SubsetRepository, loader.EntityTypeRepository, loader.EntityInstanceRepository, productContext, NullLoggerFactory.Instance));
        var subset = loader.SubsetRepository.First();
        string entityType = loader.EntityTypeRepository.First(x => !x.IsProfile && x.Identifier == answerChoiceSet.Name).Identifier;

        AssertCreateUpdateDeleteAffectRepo(productContext, repoToTest, loader, entityType, "AnEntitySet", subset);
        AssertCreateUpdateDeleteAffectRepo(productContext, repoToTest, loader, entityType, "AnEntitySetForAllSubsets", null);
    }

    private static void AssertCreateUpdateDeleteAffectRepo(IProductContext productContext,
        IEntitySetConfigurationRepository repoToTest,
        TestDataLoader loader, string entityType, string originalIdentifier, Subset subset)
    {
        var created = CreateEntitySet(originalIdentifier, subset?.Id);
        repoToTest.Create(created);
        Assert.That(GetNonDefaultEntitySetNamesForType(loader, entityType, subset), Is.EquivalentTo(new[] { originalIdentifier }), originalIdentifier + "AfterCreate");

        string newIdentifier = originalIdentifier + "WithANewIdentifier";
        created.Name = newIdentifier;
        repoToTest.Update(created);
        Assert.That(GetNonDefaultEntitySetNamesForType(loader, entityType, subset), Is.EquivalentTo(new[]{newIdentifier}), originalIdentifier + "AfterUpdate");

        repoToTest.Delete(created);
        Assert.That(GetNonDefaultEntitySetNamesForType(loader, entityType, subset), Is.Empty, originalIdentifier + "AfterDelete");

        EntitySetConfiguration CreateEntitySet(string identifier, string subsetId)
        {
            return new()
            {
                Name = identifier, EntityType = entityType, LastUpdatedUserId = "", Instances = "",
                Subset = subsetId,
                ChildAverageMappings = new List<EntitySetAverageMappingConfiguration>(),
                ProductShortCode = productContext.ShortCode, SubProductId = productContext.SubProductId
            };
        }
    }

    private static string[] GetNonDefaultEntitySetNamesForType(TestDataLoader loader, string entityType, Subset subset)
    {
        return loader.EntitySetRepository.GetOrganisationAgnostic(entityType, subset).Select(x => x.Name).Where(s => s != BrandVueDataLoader.All).ToArray();
    }
}
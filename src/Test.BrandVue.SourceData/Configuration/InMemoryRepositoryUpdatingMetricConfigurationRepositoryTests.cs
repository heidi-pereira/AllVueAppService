using System;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData.Configuration;

internal class InMemoryRepositoryUpdatingMetricConfigurationRepositoryTests
{
    /// <remarks>
    /// These three cases are bundled together because the test uses an in memory database which is quite slow to initialize
    /// </remarks>
    [Test]
    public void CreateUpdateAndDeleteVariableReflectedInRepository()
    {
        var calculatorBuilder = new ProductionCalculatorBuilder().IncludeQuestions(new Question(){VarCode = "age", QuestionText = ""}).BuildRealCalculatorWithInMemoryDb();
        var groupedVariableDefinition = new GroupedVariableDefinitionBuilder("dynamicEntity").WithGreaterThanGroup("Only", 1, "age").Build();
        var loader = calculatorBuilder.DataLoader;
        var repoToTest = loader.MetricConfigurationRepository;
        string originalIdentifier = "AMetric";
        var created = CreateMetric(originalIdentifier);
        repoToTest.Create(created);
        Assert.That(loader.MeasureRepository.Get(originalIdentifier), Is.Not.Null, "Should exist after create");

        string newIdentifier = originalIdentifier + "WithANewIdentifier";
        created.Name = newIdentifier;
        repoToTest.Update(created);
        Assert.That(loader.MeasureRepository.TryGet(originalIdentifier, out _), Is.False, "Old name should not exist after update");
        Assert.That(loader.MeasureRepository.Get(newIdentifier), Is.Not.Null, "New name should exist after update");

        repoToTest.Delete(created);
        Assert.That(loader.MeasureRepository.TryGet(newIdentifier, out _), Is.False, "New name should not exist after delete");

        MetricConfiguration CreateMetric(string identifier) => new(){Name = identifier, CalcType = "yn", FieldExpression="1", ProductShortCode = "survey", SubProductId = ""};
    }
}
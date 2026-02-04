using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData.Configuration;

internal class InMemoryRepositoryUpdatingVariableConfigurationRepositoryTests
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
        var variableRepoToTest = loader.VariableConfigurationRepository;
        string originalIdentifier = "AVariable";
        string newIdentifier = "AVariableWithANewIdentifier";
        var created = variableRepoToTest.Create(CreateVariable(originalIdentifier), Array.Empty<string>()) with{ Identifier = newIdentifier};
        Assert.That(loader.FieldExpressionParser.GetDeclaredVariableOrNull(originalIdentifier), Is.Not.Null);
        variableRepoToTest.Update(created);
        Assert.That(loader.FieldExpressionParser.GetDeclaredVariableOrNull(originalIdentifier), Is.Null);
        Assert.That(loader.FieldExpressionParser.GetDeclaredVariableOrNull(newIdentifier), Is.Not.Null);

        variableRepoToTest.Delete(created);
        Assert.That(loader.FieldExpressionParser.GetDeclaredVariableOrNull(newIdentifier), Is.Null);

        VariableConfiguration CreateVariable(string identifier) => new(){Identifier = identifier, Definition = groupedVariableDefinition, ProductShortCode = "survey", DisplayName = "A Variable", SubProductId = ""};
    }
}
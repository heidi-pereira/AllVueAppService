using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData.Configuration
{
    internal class InMemoryRepositoryUpdatingAverageConfigurationRepositoryTests
    {
        /// <remarks>
        /// These three cases are bundled together because the test uses an in memory database which is quite slow to initialize
        /// </remarks>
        [Test]
        public void CreateUpdateAndDeleteAverageReflectedInRepository()
        {
            var calculatorBuilder = new ProductionCalculatorBuilder().IncludeQuestions(new Question(){VarCode = "region", QuestionText = ""}).BuildRealCalculatorWithInMemoryDb();
            var loader = calculatorBuilder.DataLoader;
            var productContext = calculatorBuilder.ProductContext;
            var averageConfigurationRepository = new AverageConfigurationRepository(loader.MetaDataContextFactory, productContext);
            var averageRepoToTest = new InMemoryRepositoryUpdatingAverageConfigurationRepository(averageConfigurationRepository, new AverageDescriptorSqlLoader((AverageDescriptorRepository)loader.AverageDescriptorRepository, productContext, averageConfigurationRepository, loader.SubsetRepository, loader.Settings.AppSettings));
            string originalIdentifier = "AnAverage";
            var created = CreateAverage(originalIdentifier);
            averageRepoToTest.Create(created);
            Assert.That(loader.AverageDescriptorRepository.Get(originalIdentifier, productContext.ShortCode), Is.Not.Null);

            string newIdentifier = "AnAverageWithANewIdentifier";
            created.AverageId = newIdentifier;
            averageRepoToTest.Update(created);
            Assert.That(loader.AverageDescriptorRepository.TryGet(originalIdentifier, out _), Is.False);
            Assert.That(loader.AverageDescriptorRepository.Get(newIdentifier, productContext.ShortCode), Is.Not.Null);

            averageRepoToTest.Delete(created.Id);
            Assert.That(loader.AverageDescriptorRepository.TryGet(newIdentifier, out _), Is.False);

            AverageConfiguration CreateAverage(string identifier) => new(){AverageId = identifier, DisplayName = "An average", SubsetIds = Array.Empty<string>(), ProductShortCode = productContext.ShortCode, SubProductId = productContext.SubProductId};
        }
    }
}

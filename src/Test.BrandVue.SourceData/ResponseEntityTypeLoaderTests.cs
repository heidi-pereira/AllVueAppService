using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;

namespace Test.BrandVue.SourceData
{
    public class ResponseEntityTypeLoaderTests
    {
        [Test]
        public void ShouldLoadEntityDisplayNameFromFile()
        {
            var repository = new EntityTypeRepository();
            var substituteLogger = Substitute.For<ILogger<ResponseEntityTypeInformationLoader>>();
            var loader = new ResponseEntityTypeInformationLoader(repository, substituteLogger);

            string settings = TestLoaderSettings.EatingOut.ResponseEntityTypesMetadataFilepath;

            loader.Load(settings);

            var tvEntity = repository.Get("tvchannel");
            Assert.That(tvEntity.DisplayNameSingular, Is.EqualTo("TV Channel"));
            Assert.That(tvEntity.DisplayNamePlural, Is.EqualTo("TV Channels"));
        }
    }
}
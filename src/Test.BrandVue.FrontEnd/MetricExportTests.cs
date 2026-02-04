using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using VerifyNUnit;

namespace Test.BrandVue.FrontEnd
{
    [TestFixture]
    public class MetricExportTests
    {
        [Test]
        public void ExportMetricsToCsv_Returns_File()
        {
            MetricsController controller = new MetricsController(Substitute.For<IMeasureRepository>(),
                Substitute.For<IMetricConfigurationRepository>(),
                Substitute.For<IMetricAboutRepository>(), Substitute.For<ISubsetRepository>(),
                Substitute.For<IUserContext>(),
                Substitute.For<ILinkedMetricRepository>(), Substitute.For<IMeasureBaseDescriptionGenerator>(),
                Substitute.For<IVariableConfigurationRepository>(),
                Substitute.For<IVariableConfigurationFactory>(),
                Substitute.For<IVariableValidator>());

            var result = controller.ExportMetricsToCsv();

            Assert.That(result, Is.TypeOf(typeof(FileStreamResult)));
        }

        [Test]
        public async Task ExportMetricsToCsv_Contents_Returns_Metrics()
        {
            IMetricConfigurationRepository _metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            _metricConfigurationRepository.GetAll().Returns(new List<MetricConfiguration>
            {
                new MetricConfiguration { Id = 1, Name = "Test Metric 1" },
                new MetricConfiguration { Id = 2, Name = "Test Metric 2" }
            });

            MetricsController controller = new MetricsController(Substitute.For<IMeasureRepository>(),
                _metricConfigurationRepository,
                Substitute.For<IMetricAboutRepository>(), Substitute.For<ISubsetRepository>(),
                Substitute.For<IUserContext>(),
                Substitute.For<ILinkedMetricRepository>(), Substitute.For<IMeasureBaseDescriptionGenerator>(),
                Substitute.For<IVariableConfigurationRepository>(),
                Substitute.For<IVariableConfigurationFactory>(),
                Substitute.For<IVariableValidator>());

            var result = controller.ExportMetricsToCsv();
            var stream = (MemoryStream)result.FileStream;
            var reader = new StreamReader(result.FileStream);

            stream.Position = 0;

            string readToEnd = await reader.ReadToEndAsync();
            await Verifier.Verify(readToEnd, "csv");
        }
    }
}
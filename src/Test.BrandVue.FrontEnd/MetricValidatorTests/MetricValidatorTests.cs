using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Metrics;
using NSubstitute;
using NUnit.Framework;
using static BrandVue.Services.MetricValidator.MetricValidationErrors;

namespace Test.BrandVue.FrontEnd.MetricValidatorTests
{
    [TestFixture]
    public class MetricValidatorTests
    {
        private const string ExistingName = "existingName";
        private IMetricConfigurationRepository _metricConfigurationRepository;
        private MetricValidator _metricValidator;

        [SetUp]
        public void SetUp()
        {
            _metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            _metricValidator = new MetricValidator(_metricConfigurationRepository);
        }

        [Test]
        public void ValidateMetricDisplayName_ShouldThrowBadRequestException_WhenDisplayNameIsEmpty()
        {
            // Arrange
            string newDisplayName = null;
            _metricConfigurationRepository.GetAll().Returns([new MetricConfiguration(){ DisplayName = ExistingName }]);

            // Act & Assert
            var ex = Assert.Throws<BadRequestException>(() => _metricValidator.ValidateMetricDisplayName(newDisplayName));
            Assert.That(ex.Message, Is.EqualTo(MetricNameMustBeAtLeast3Characters));
        }

        [Test]
        public void ValidateMetricDisplayName_ShouldThrowBadRequestException_WhenDisplayNameIsTooShort()
        {
            // Arrange
            string newDisplayName = "ab";
            _metricConfigurationRepository.GetAll().Returns([new MetricConfiguration()]);

            // Act & Assert
            var ex = Assert.Throws<BadRequestException>(() => _metricValidator.ValidateMetricDisplayName(newDisplayName));
            Assert.That(ex.Message, Is.EqualTo(MetricNameMustBeAtLeast3Characters));
        }

        [Test]
        public void ValidateMetricDisplayName_ShouldThrowBadRequestException_WhenDisplayNameAlreadyExists()
        {
            // Arrange
            string newDisplayName = ExistingName;
            var existingMetric = new MetricConfiguration { DisplayName = ExistingName };

            _metricConfigurationRepository.GetAll().Returns([existingMetric]);

            // Act & Assert
            var ex = Assert.Throws<BadRequestException>(() => _metricValidator.ValidateMetricDisplayName(newDisplayName));
            Assert.That(ex.Message, Is.EqualTo(MetricNameAlreadyExists));
        }

        [Test]
        public void ValidateMetricDisplayName_ShouldThrowBadRequestException_WhenMultipleRulesFail()
        {
            // Arrange
            string newDisplayName = "";
            var existingMetric = new MetricConfiguration { DisplayName = "" };

            _metricConfigurationRepository.GetAll().Returns([existingMetric]);

            // Act & Assert
            var ex = Assert.Throws<BadRequestException>(() => _metricValidator.ValidateMetricDisplayName(newDisplayName));
            Assert.That(ex.Message, Is.EqualTo($"{MetricNameMustBeAtLeast3Characters}, {MetricNameAlreadyExists}"));
        }

        [Test]
        public void ValidateMetricDisplayName_ShouldNotThrowException_WhenAllRulesPass()
        {
            // Arrange
            string newDisplayName = "validName";
            var existingMetric = new MetricConfiguration { DisplayName = "differentName" };

            _metricConfigurationRepository.GetAll().Returns([existingMetric]);

            // Act & Assert
            Assert.DoesNotThrow(() => _metricValidator.ValidateMetricDisplayName(newDisplayName));
        }
    }
}
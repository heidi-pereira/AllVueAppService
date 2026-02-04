using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.EntityFramework.MetaData.Interfaces;
using BrandVue.Services;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class FeaturesServiceTests
    {
        private IFeaturesRepository _mockFeatureRepository;
        private FeaturesService _featuresService;

        [SetUp]
        public void SetUp()
        {
            _mockFeatureRepository = Substitute.For<IFeaturesRepository>();
            _featuresService = new FeaturesService(_mockFeatureRepository);
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenFeatureRepositoryIsNull()
        {
            // Arrange
            IFeaturesRepository nullFeatureRepository = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new FeaturesService(nullFeatureRepository));
            Assert.That("featureRepository", Is.EqualTo(ex.ParamName));
        }

        [Test]
        public void Constructor_ShouldNotThrowException_WhenFeatureRepositoryIsNotNull()
        {
            // Arrange
            var mockFeatureRepository = Substitute.For<IFeaturesRepository>();

            // Act & Assert
            Assert.DoesNotThrow(() => new FeaturesService(mockFeatureRepository));
        }

        [Test]
        public async Task GetFeaturesAsync_ShouldReturnFeatures_WhenFeaturesExist()
        {
            // Arrange
            var featureCodesFromEnum = Enum.GetValues<FeatureCode>().Where(code => code != FeatureCode.unknown).ToList();
            var features = new List<Feature>();
            _mockFeatureRepository.GetFeaturesAsync(CancellationToken.None).Returns(features);

            // Act
            var result = (await _featuresService.GetFeaturesAsync(CancellationToken.None)).ToList();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(featureCodesFromEnum.Count));
            foreach (var code in featureCodesFromEnum)
            {
                Assert.That(result.Any(f => f.FeatureCode == code), Is.True, $"Missing FeatureCode: {code}");
            }
        }
    }
}
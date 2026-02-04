using BrandVue.EntityFramework.MetaData.FeatureToggle;
using BrandVue.Services;
using BrandVue.Services.Interfaces;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vue.Common.FeatureFlags;
using ZiggyCreatures.Caching.Fusion;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class FeatureToggleServiceDecoratorTests
    {
        private IFusionCacheProvider _fusionCacheProvider;
        private IUserContext _userContext;
        private IFeatureToggleService _featureToggleService;
        private FeatureToggleServiceDecorator _service;

        private readonly List<Feature> _features =
        [
            new Feature { Id = 1, Name = "Feature1", DocumentationUrl = "http://savanta.com", FeatureCode = (FeatureCode)0, IsActive = true },
            new Feature { Id = 2, Name = "Feature2", DocumentationUrl = "http://savanta.com", FeatureCode = (FeatureCode)1, IsActive = true },
            new Feature { Id = 3, Name = "Feature3", DocumentationUrl = "http://example.com", FeatureCode = (FeatureCode)2, IsActive = false},
        ];
        private const string UserId = "user1";
        private const string ExpectedCacheKey = "user_features_user1";
        private const string CacheTag = "user_features";
        private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(8);

        [SetUp]
        public void SetUp()
        {
            _fusionCacheProvider = Substitute.For<IFusionCacheProvider>();
            _fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName)
                .Returns(new FusionCache(new FusionCacheOptions() { CacheName = FeatureToggleServiceDecorator.CacheName }));
            _userContext = Substitute.For<IUserContext>();
            _userContext.UserId.Returns(UserId);
            _featureToggleService = Substitute.For<IFeatureToggleService>();
            _service = new FeatureToggleServiceDecorator(_userContext, _fusionCacheProvider, _featureToggleService);
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenCacheProviderIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new FeatureToggleServiceDecorator(
                _userContext,
                null,
                _featureToggleService));
            Assert.That(ex!.ParamName, Is.EqualTo("cacheProvider"));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenCurrentUserInformationIsNull()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new FeatureToggleServiceDecorator(
                null,
                _fusionCacheProvider,
                _featureToggleService));
            Assert.That(ex!.ParamName, Is.EqualTo("userInformationProvider"));
        }

        [Test]
        public async Task GetFeaturesForUser_ShouldThrowArgumentNullException_WhenUserIdIsNullOrEmpty()
        {
            // Arrange
            var userInformationProvider = Substitute.For<IUserContext>();
            userInformationProvider.UserId.Returns(string.Empty);
            var service = new FeatureToggleServiceDecorator(
                userInformationProvider, _fusionCacheProvider, _featureToggleService
                );

            // Act
            var ex = Assert.ThrowsAsync<ArgumentNullException>(() => service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None));

            // Assert
            Assert.That(ex!.ParamName, Is.EqualTo("UserId"));
        }

        [Test]
        public async Task GetFeaturesForUser_CacheMiss_ShouldFetchFromRepository()
        {
            // Arrange
            var enabledFeatures = _features.Where(feature => feature.IsActive);
            var fusionCacheProvider = Substitute.For<IFusionCacheProvider>();
            fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName)
                .Returns(new FusionCache(new FusionCacheOptions() { CacheName = FeatureToggleServiceDecorator.CacheName }));

            var featureToggleService = Substitute.For<IFeatureToggleService>();
            featureToggleService.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None).Returns(enabledFeatures);

            FeatureToggleServiceDecorator service = new FeatureToggleServiceDecorator(_userContext,
                fusionCacheProvider, featureToggleService);

            // Act
            var result = await service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(enabledFeatures));
       
        }

        [Test]
        public async Task GetFeaturesForUser_CacheHit_ShouldReturnFromCache()
        {
            // Arrange
            var enabledFeatures = _features.Where(feature => feature.IsActive).ToList();
            var fusionCacheProvider = Substitute.For<IFusionCacheProvider>();
            fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName)
                .Returns(new FusionCache(new FusionCacheOptions() { CacheName = FeatureToggleServiceDecorator.CacheName }));

            // populate cache directly
            var fusionCache = fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName);
            await fusionCache.SetAsync<IEnumerable<Feature>>(ExpectedCacheKey, enabledFeatures, token: CancellationToken.None);

            // we force the underlying service to return nothing this way we can test if we get a hit from the cache or not
            var featureToggleService = Substitute.For<IFeatureToggleService>();
            featureToggleService.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None)
                .Returns(new List<Feature>());

            FeatureToggleServiceDecorator service = new FeatureToggleServiceDecorator(_userContext,
                fusionCacheProvider, featureToggleService);

            // Act
            var result = await service.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None);

            // Assert
            Assert.That(result, Is.EqualTo(enabledFeatures));
          
        }

        [Test]
        public async Task InvalidCacheForUser_ShouldRemoveCacheEntry()
        {
            // Arrange
            var enabledFeatures = _features.Where(feature => feature.IsActive).ToList();
            var fusionCacheProvider = Substitute.For<IFusionCacheProvider>();
            fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName)
                .Returns(new FusionCache(new FusionCacheOptions() { CacheName = FeatureToggleServiceDecorator.CacheName }));

            var featureToggleService = Substitute.For<IFeatureToggleService>();
            featureToggleService.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None).Returns(enabledFeatures);

            FeatureToggleServiceDecorator service = new FeatureToggleServiceDecorator(_userContext,
                fusionCacheProvider, featureToggleService);

            // populate cache directly
            var fusionCache = fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName);
            await fusionCache.SetAsync<IEnumerable<Feature>>(
                ExpectedCacheKey, enabledFeatures,
                options => options.SetDuration(_cacheDuration),
                tags: [CacheTag],
                token: CancellationToken.None);

            // Act
            await service.InvalidateCacheAsync(CancellationToken.None);

            var cachedItem = await fusionCache.TryGetAsync<IEnumerable<Feature>>(ExpectedCacheKey, token: CancellationToken.None);

            // Assert
            Assert.That(cachedItem.HasValue, Is.False);
        }

        [Test]
        public async Task SaveUserFeatures_WhenSaved_ShouldInvalidateCache()
        {
            // Arrange
            int featureId = 4;
            var enabledFeatures = _features.Where(feature => feature.IsActive).ToList();
            var fusionCacheProvider = Substitute.For<IFusionCacheProvider>();
            fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName)
                .Returns(new FusionCache(new FusionCacheOptions() { CacheName = FeatureToggleServiceDecorator.CacheName }));

            var featureToggleService = Substitute.For<IFeatureToggleService>();
            featureToggleService.SaveUserFeaturesAsync(UserId, featureId,CancellationToken.None).Returns(new UserFeature { FeatureId = featureId, UserId = UserId });

            FeatureToggleServiceDecorator service = new FeatureToggleServiceDecorator(_userContext,
                fusionCacheProvider, featureToggleService);

            // populate cache directly
            var fusionCache = fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName);
            await fusionCache.SetAsync<IEnumerable<Feature>>(
                ExpectedCacheKey, enabledFeatures,
                options => options.SetDuration(_cacheDuration),
                tags: [CacheTag],
                token: CancellationToken.None);

            // Act
            await service.SaveUserFeaturesAsync(UserId, featureId, CancellationToken.None);

            var cachedItem = await fusionCache.TryGetAsync<IEnumerable<Feature>>(ExpectedCacheKey, token: CancellationToken.None);

            // Assert
            Assert.That(cachedItem.HasValue, Is.False);

        }

        [Test]
        public async Task SaveUserFeatures_WhenNotSaved_ShouldNotInvalidateCache()
        {
            // Arrange
            var enabledFeatures = _features.Where(feature => feature.IsActive).ToList();
            var fusionCacheProvider = Substitute.For<IFusionCacheProvider>();
            fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName)
                .Returns(new FusionCache(new FusionCacheOptions() { CacheName = FeatureToggleServiceDecorator.CacheName }));

            var featureToggleService = Substitute.For<IFeatureToggleService>();
            featureToggleService.GetEnabledFeaturesForCurrentUserAsync(CancellationToken.None).Returns(enabledFeatures);
            featureToggleService.SaveUserFeaturesAsync(UserId,4,CancellationToken.None).Returns(new Task<UserFeature>(() => null));

            FeatureToggleServiceDecorator service = new FeatureToggleServiceDecorator(_userContext,
                fusionCacheProvider, featureToggleService);

            // populate cache directly
            var fusionCache = fusionCacheProvider.GetCache(FeatureToggleServiceDecorator.CacheName);
            await fusionCache.SetAsync<IEnumerable<Feature>>(
                ExpectedCacheKey, enabledFeatures,
                options => options.SetDuration(_cacheDuration),
                tags: [CacheTag],
                token: CancellationToken.None);

            // Act
            var result = await _service.SaveUserFeaturesAsync(UserId, 4, CancellationToken.None);

            var cachedItem = await fusionCache.TryGetAsync<IEnumerable<Feature>>(ExpectedCacheKey, token: CancellationToken.None);

            // Assert
            Assert.That(result, Is.Null);
            Assert.That(cachedItem.HasValue, Is.True);
            Assert.That(cachedItem.Value, Is.EqualTo(enabledFeatures));
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Subsets;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData.Configuration
{
    internal class AverageConfigurationRepositoryTests
    {
        private const string RequestScopeProductName = "brandvue";
        private static readonly ProductContext ProductContext = new(RequestScopeProductName);

        private ITestMetadataContextFactory _testMetadataContextFactory;

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_testMetadataContextFactory != null)
                await _testMetadataContextFactory.Dispose();
        }

        [SetUp]
        public async Task SetUp()
        {
            _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemory);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _testMetadataContextFactory.RevertDatabase();
        }

        [Test]
        public void ConvertingAverageRetainsAuthCompanyInformation()
        {
            string idAndName = "First";
            string shortcode = "savanta";
            var expectedAverage = new AverageConfiguration() { DisplayName = idAndName, ProductShortCode = RequestScopeProductName, AverageId = idAndName, SubsetIds = Array.Empty<string>(), AuthCompanyShortCode = shortcode };
            var actualAverage = AverageDescriptorSqlLoader.AverageConfigurationFrom(new AverageDescriptor { DisplayName = idAndName, AverageId = idAndName, Subset = null, AuthCompanyShortCode = shortcode }, ProductContext);
            Assert.That(Serialize(actualAverage), Is.EqualTo(Serialize(expectedAverage)));
        }

        [Test]
        public void AllClientAverageIsReturnedForClient()
        {
            string idAndName = "First";
            var averageDescriptorRepo = LoadRepository(new AverageConfiguration()
            {
                DisplayName = idAndName, ProductShortCode = RequestScopeProductName, AverageId = idAndName,
                SubsetIds = Array.Empty<string>()
            });
            var actualAverages = averageDescriptorRepo.GetAllForClient("1");
            var expectedAverages = new[]
            {
                new AverageDescriptor { DisplayName = idAndName, AverageId = idAndName, Subset = null }
            };
            Assert.That(actualAverages.Select(Serialize), Is.EqualTo(expectedAverages.Select(Serialize)));
        }

        [Test]
        public void SingleClientAverageIsReturnedForClient()
        {
            string idAndName = "First";
            string shortcode = "savanta";
            var averageDescriptorRepo = LoadRepository(new AverageConfiguration()
            {
                DisplayName = idAndName, ProductShortCode = RequestScopeProductName, AverageId = idAndName,
                SubsetIds = Array.Empty<string>(), AuthCompanyShortCode = shortcode
            });
            var actualAverages = averageDescriptorRepo.GetAllForClient(shortcode);
            var expectedAverages = new[]
            {
                new AverageDescriptor { DisplayName = idAndName, AverageId = idAndName, Subset = null, AuthCompanyShortCode = shortcode }
            };
            Assert.That(actualAverages.Select(Serialize), Is.EqualTo(expectedAverages.Select(Serialize)));
        }

        [Test]
        public void SingleClientAverageIsNotReturnedForOtherClient()
        {
            string idAndName = "First";
            string clientA = "kfc";
            string clientB = "maccas";
            var averageDescriptorRepo = LoadRepository(new AverageConfiguration()
            {
                DisplayName = idAndName, ProductShortCode = RequestScopeProductName, AverageId = idAndName,
                SubsetIds = Array.Empty<string>(), AuthCompanyShortCode = clientA
            });
            var actualAverages = averageDescriptorRepo.GetAllForClient(clientB);
            Assert.That(actualAverages.Select(Serialize), Is.Empty);
        }

        [Test]
        public void DisabledAverageIsNotReturned()
        {
            string idAndName = "First";
            var averageDescriptorRepo = LoadRepository(new AverageConfiguration
            {
                DisplayName = idAndName, 
                ProductShortCode = RequestScopeProductName, 
                AverageId = idAndName,
                SubsetIds = Array.Empty<string>(),
                Disabled = true
            });
            var actualAverages = averageDescriptorRepo.GetAllForClient("1").ToList();
            Assert.Multiple(() =>
            {
                Assert.That(actualAverages.Count, Is.GreaterThan(0)); // Make sure we return the fallback averages
                Assert.That(actualAverages.FirstOrDefault(a => a.AverageId == idAndName), Is.Null);
            });
        }

        [Test]
        public void CustomAverageOverwritesFallback()
        {
            string fallbackName = "Monthly";

            // Assert we're checking a fallback average
            var averageDescriptorRepo = LoadRepository();
            var fallbackAverage = averageDescriptorRepo.GetAllForClient(null)
                .FirstOrDefault(a => a.AverageId == fallbackName);
            Assert.That(fallbackAverage?.DisplayName, Is.EqualTo(fallbackName));

            // Assert custom average overwrites fallback average
            averageDescriptorRepo = LoadRepository(new AverageConfiguration
            {
                DisplayName = "New average",
                AverageId = fallbackName,
                ProductShortCode = RequestScopeProductName,
                SubsetIds = Array.Empty<string>(),
            }); 
            var customAverage = averageDescriptorRepo.GetAllForClient(null)
                .FirstOrDefault(a => a.AverageId == fallbackName);
            Assert.That(customAverage?.DisplayName, Is.EqualTo("New average"));
        }

        [Test]
        public void AveragesShouldBeSortedByOrder([Random(5)] int seed)
        {
            var sortedAverages = new[]
            {
                new AverageConfiguration
                {
                    DisplayName = "second",
                    AverageId = "1",
                    ProductShortCode = RequestScopeProductName,
                    SubsetIds = Array.Empty<string>(),
                    Order = 100
                },
                new AverageConfiguration
                {
                    DisplayName = "A third",
                    AverageId = "2",
                    ProductShortCode = RequestScopeProductName,
                    SubsetIds = Array.Empty<string>(),
                    Order = 200
                },
                new AverageConfiguration
                {
                    DisplayName = "B third",
                    AverageId = "3",
                    ProductShortCode = RequestScopeProductName,
                    SubsetIds = Array.Empty<string>(),
                    Order = 200
                },
                new AverageConfiguration
                {
                    DisplayName = "C third",
                    AverageId = "4",
                    ProductShortCode = RequestScopeProductName,
                    SubsetIds = Array.Empty<string>(),
                    Order = 200
                },
                new AverageConfiguration
                {
                    DisplayName = "first",
                    AverageId = "5",
                    ProductShortCode = RequestScopeProductName,
                    SubsetIds = Array.Empty<string>(),
                    Order = 300
                },
            };
            var random = new Random(seed);

            var averageDescriptorRepo = LoadRepository(sortedAverages.OrderBy(a=> random.Next()).ToArray());
            var actualAverages = averageDescriptorRepo.GetAllForClient(null);
            Assert.That(actualAverages.Select(a => a.AverageId), Is.EqualTo(sortedAverages.Select(a => a.AverageId)));
        }

        private AverageDescriptorRepository LoadRepository(params AverageConfiguration[] averageConfigurations)
        {
            var averageDescriptorRepo = new AverageDescriptorRepository();
            var averageConfigurationRepository = new AverageConfigurationRepository(_testMetadataContextFactory, ProductContext);

            foreach (var average in averageConfigurations)
            {
                averageConfigurationRepository.Create(average);
            }
            var averageLoader = new AverageDescriptorSqlLoader(averageDescriptorRepo, ProductContext,
                averageConfigurationRepository, new SubsetRepository(), new AppSettings());
            averageLoader.Load(null, null);
            return averageDescriptorRepo;
        }

        private string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);
    }
}

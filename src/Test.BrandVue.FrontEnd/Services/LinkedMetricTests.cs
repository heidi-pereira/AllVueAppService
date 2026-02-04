using System.Collections.Generic;
using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System.Threading.Tasks;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using TestCommon;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class LinkedMetricTests
    {
        private IDbContextFactory<MetaDataContext> GetDbContextFactory() => ITestMetadataContextFactory.Create(StorageType.InMemory);
        private LinkedMetricRepository _linkedMetricRepository;
        private IProductContext _productContext;

        [SetUp]
        public void Setup()
        {
            var dbContextFactory = GetDbContextFactory();
            var dbContext = dbContextFactory.CreateDbContext();
            var linkedMetric = new LinkedMetric
            {
                ProductShortCode = "test",
                SubProductId = "subtest",
                MetricName = "testname",
                LinkedMetricNames = new[] {"a","b","c"}
            };
            dbContext.Add(linkedMetric);
            var noSubProduct = new LinkedMetric
            {
                ProductShortCode = "test",
                MetricName = "testname",
                LinkedMetricNames = new []{ "d", "e", "f" }
            };
            dbContext.Add(noSubProduct);
            dbContext.SaveChanges();

            _productContext = Substitute.For<IProductContext>();

            _linkedMetricRepository = new LinkedMetricRepository(dbContextFactory, _productContext);
        }

        [Test]
        public async Task LinkedMetric_That_Exists_Is_Returned()
        {
            _productContext.ShortCode.Returns("test");
            _productContext.SubProductId.Returns("subtest");

            var result = await _linkedMetricRepository.GetLinkedMetricsForMetric("testname");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<LinkedMetric>());
                Assert.That(result.LinkedMetricNames, Is.EqualTo(new[]{"a", "b", "c"}));
            });
        }

        [Test]
        public async Task LinkedMetric_That_Does_Not_Exist_Returns_Null()
        {
            _productContext.ShortCode.Returns("badtest");
            _productContext.SubProductId.Returns("badsubtest");

            var result = await _linkedMetricRepository.GetLinkedMetricsForMetric("badtestname");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task Linked_Metric_With_No_SubProduct_Is_Returned()
        {
            _productContext.ShortCode.Returns("test");
            _productContext.SubProductId.Returns(string.Empty);

            var result = await _linkedMetricRepository.GetLinkedMetricsForMetric("testname");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<LinkedMetric>());
                Assert.That(result.LinkedMetricNames, Is.EqualTo(new[] { "d", "e", "f" }));
            });

        }
    }

    [TestFixture]
    public class LinkedMetricControllerTests
    {
        private IDbContextFactory<MetaDataContext> GetDbContextFactory() => ITestMetadataContextFactory.Create(StorageType.InMemory);
        private MetricsController _controller;
        private IProductContext _productContext;

        [SetUp]
        public void Setup()
        {
            var dbContextFactory = GetDbContextFactory();
            var dbContext = dbContextFactory.CreateDbContext();
            var linkedMetric = new LinkedMetric
            {
                ProductShortCode = "test",
                SubProductId = "subtest",
                MetricName = "testname",
                LinkedMetricNames = new[] { "a", "b", "c" }
            };
            dbContext.Add(linkedMetric);
            var noSubProduct = new LinkedMetric
            {
                ProductShortCode = "test",
                MetricName = "testname",
                LinkedMetricNames = new [] { "d", "e", "f" }
            };
            dbContext.Add(noSubProduct);
            dbContext.SaveChanges();

            _productContext = Substitute.For<IProductContext>();

            LinkedMetricRepository linkedMetricRepository = new LinkedMetricRepository(dbContextFactory, _productContext);

            _controller = new MetricsController(null, null, null,
                null, null, linkedMetricRepository, null, null, null, null);
        }

        [Test]
        public async Task LinkedMetric_That_Exists_Is_Returned()
        {
            _productContext.ShortCode.Returns("test");
            _productContext.SubProductId.Returns("subtest");

            var result = await _controller.GetLinkedMetrics("testname");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
                Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
            });
        }

        [Test]
        public async Task LinkedMetric_That_Does_Not_Exist_Returns_NotFound()
        {
           var result = await _controller.GetLinkedMetrics("badtestname");

           Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task Linked_Metric_With_No_SubProduct_Is_Returned()
        {
            _productContext.ShortCode.Returns("test");
            _productContext.SubProductId.Returns(string.Empty);

            var result = await _controller.GetLinkedMetrics("testname");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
                Assert.That(result, Is.EqualTo(new[] { "d", "e", "f" }));
            });
        }

        [Test]
        public async Task LinkedMetric_That_Exists_Is_Returned_When_Queried_In_Mixed_Case()
        {
            _productContext.ShortCode.Returns("test");
            _productContext.SubProductId.Returns("subtest");

            var result = await _controller.GetLinkedMetrics("TestName");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<IEnumerable<string>>());
                Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
            });
        }
    }
}

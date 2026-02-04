using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class MetricAboutTests
    {
        private ILogger<MetricAboutRepository> _logger;
        private IProductContext _productContext;
        
        private IDbContextFactory<MetaDataContext> GetDbContextFactory() => ITestMetadataContextFactory.Create(StorageType.InMemory);

        [OneTimeSetUp]
        public void InitialiseData()
        {
            _productContext = new ProductContext("test", "12345", true, "surveyName");
            _logger = Substitute.For<ILogger<MetricAboutRepository>>();
        }

        [Test]
        public void CreatingMetricAboutUpdatesProductShortCode()
        {
            var dbContextFactory = GetDbContextFactory();
            var metricAboutRepository = new MetricAboutRepository(dbContextFactory, _productContext, _logger);

            var metricAbout = new MetricAbout
            {
                AboutContent = "AboutContent",
                AboutTitle = "AboutTitle",
                ProductShortCode = "not product shortcode",
                MetricName = "metricName"
            };
            metricAboutRepository.Create(metricAbout);
            
            var metricAboutList = metricAboutRepository.GetAllForMetric("metricName").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(metricAboutList.Count, Is.EqualTo(1));
                Assert.That(metricAboutList[0].ProductShortCode, Is.EqualTo(_productContext.ShortCode));
            });
        }

        [Test]
        public void GettingMetricAboutsFiltersOnProductShortCode()
        {
            // Setup
            var metricAbout1 = new MetricAbout
            {
                AboutContent = "AboutContent_include",
                AboutTitle = "AboutTitle_include",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "metricName"
            };
            var metricAbout2 = new MetricAbout
            {
                AboutContent = "AboutContent_exclude1",
                AboutTitle = "AboutTitle_exclude1",
                ProductShortCode = "not shortcode",
                MetricName = "metricName"
            };
            var metricAbout3 = new MetricAbout
            {
                AboutContent = "AboutContent_exclude2",
                AboutTitle = "AboutTitle_exclude2",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "different_metricName"
            };
            var dbContextFactory = GetDbContextFactory();
            var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Add(metricAbout1);
            dbContext.Add(metricAbout2);
            dbContext.Add(metricAbout3);
            dbContext.SaveChanges();

            var metricAboutRepository = new MetricAboutRepository(dbContextFactory, _productContext, _logger);
            var newMetrics = metricAboutRepository.GetAllForMetric("metricName").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(newMetrics.Count, Is.EqualTo(1));
                Assert.That(newMetrics[0].ProductShortCode, Is.EqualTo(_productContext.ShortCode));
                Assert.That(newMetrics[0].MetricName, Is.EqualTo("metricName"));
                Assert.That(newMetrics[0].AboutContent, Is.EqualTo("AboutContent_include"));
                Assert.That(newMetrics[0].AboutTitle, Is.EqualTo("AboutTitle_include"));
            });
        }

        [Test]
        public void NonEditableMetricAboutsShouldAppearLast()
        {
            var Editable1 = new MetricAbout
            {
                AboutContent = "AboutContent_Editable",
                AboutTitle = "DummyTitle",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "testMetricName",
                Editable = true,
            };
            var NotEditable = new MetricAbout
            {
                AboutContent = "AboutContent_NotEditable",
                AboutTitle = "DummyTitle",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "testMetricName",
                Editable = false
            };
            var Editable2 = new MetricAbout
            {
                AboutContent = "AboutContent_Editable",
                AboutTitle = "DummyTitle",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "testMetricName",
                Editable = true
            };

            var dbContextFactory = GetDbContextFactory();
            var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Add(Editable1);
            dbContext.Add(NotEditable);
            dbContext.Add(Editable2);
            dbContext.SaveChanges();

            var repository = new MetricAboutRepository(dbContextFactory, _productContext, _logger);
            var result = repository.GetAllForMetric("testMetricName");
            var lastMetric = result.Last();
            Assert.That(lastMetric.Editable, Is.False);
        }

        [Test]
        public void SaveSingleEditedMetricAbout()
        {
            var metricAbout1 = new MetricAbout
            {
                AboutContent = "AboutContent",
                AboutTitle = "AboutTitle",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "metricName"
            };

            var dbContextFactory = GetDbContextFactory();
            var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Add(metricAbout1);
            dbContext.SaveChanges();

            var metricAboutRepository = new MetricAboutRepository(dbContextFactory, _productContext, _logger);
            var allMetricAbouts = metricAboutRepository.GetAllForMetric("metricName").ToList();

            allMetricAbouts[0].AboutTitle = "AboutTitle_Amended";

            metricAboutRepository.UpdateList(allMetricAbouts.ToArray());

            var result = metricAboutRepository.GetAllForMetric("metricName").ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].AboutTitle, Is.EqualTo("AboutTitle_Amended"));
        }

        [Test]
        public void SaveMultipleEditedMetricAbouts()
        {
            var metricAbout1 = new MetricAbout
            {
                AboutContent = "AboutContent_1",
                AboutTitle = "AboutTitle_1",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "metricName"
            };

            var metricAbout2 = new MetricAbout
            {
                AboutContent = "AboutContent_2",
                AboutTitle = "AboutTitle_2",
                ProductShortCode = _productContext.ShortCode,
                MetricName = "metricName"
            };

            var dbContextFactory = GetDbContextFactory();
            var dbContext = dbContextFactory.CreateDbContext();
            dbContext.Add(metricAbout1);
            dbContext.Add(metricAbout2);
            dbContext.SaveChanges();

            var metricAboutRepository = new MetricAboutRepository(dbContextFactory, _productContext, _logger);
            var allMetricAbouts = metricAboutRepository.GetAllForMetric("metricName").ToList();

            allMetricAbouts[0].AboutContent = "AboutContent_1_Amended";
            allMetricAbouts[1].AboutContent = "AboutContent_2_Amended";

            metricAboutRepository.UpdateList(allMetricAbouts.ToArray());

            var result = metricAboutRepository.GetAllForMetric("metricName").ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].AboutContent, Is.EqualTo("AboutContent_1_Amended"));
            Assert.That(result[1].AboutContent, Is.EqualTo("AboutContent_2_Amended"));
        }
    }
}

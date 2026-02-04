using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Test.BrandVue.FrontEnd.Mocks;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    [SingleThreaded]
    public class DataPreloaderTests
    {

        [SetUp]
        public async Task SetUp()
        {
        }

        [TearDown]
        public async Task TearDown()
        {
        }

        private ISavedReportService GetSavedReportService()
        {
            ISavedReportService savedReportService = Substitute.For<ISavedReportService>();

            var reports = new List<Report>()
            {
                new Report()
                {
                    SavedReportId = 1,
                    PageId = 1
                },
            };
            var savedReports = new ReportsForSurveyAndUser()
            {
                DefaultReportId = 1,
                Reports = reports
            };

            var parsedReports = new List<ParsedReport>()
            {
                new ParsedReport(
                    new Measure(),
                    new AverageDescriptor(),
                    null,
                    new List<TargetInstances>(),
                    Array.Empty<Break>())
            };

            savedReportService.GetAllReports().ReturnsForAnyArgs(savedReports);
            savedReportService.ParseReportsForSubset(Arg.Any<IEnumerable<Report>>(), Arg.Any<Subset>()).ReturnsForAnyArgs(parsedReports);
            return savedReportService;
        }

        private IDataPresenceGuarantor GetDataPresenceGuarantor()
        {
            IDataPresenceGuarantor dataPresenceGuarantor = Substitute.For<IDataPresenceGuarantor>();

            dataPresenceGuarantor.EnsureDataIsLoaded(
                    Arg.Any<IRespondentRepository>(), 
                    Arg.Any<Subset>(),
                    Arg.Any<Measure>(),
                    Arg.Any<CalculationPeriod>(),
                    Arg.Any<AverageDescriptor>(),
                    Arg.Any<IFilter>(),
                    Arg.Any<IReadOnlyCollection<IDataTarget>>(),
                    Arg.Any<Break[]>(),
                    Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            return dataPresenceGuarantor;
        }

        public static ISubsetRepository GetSubsetRepository()
        {
            ISubsetRepository subsetRepository = Substitute.For<ISubsetRepository>();
            subsetRepository.Count.Returns(args => MockRepositoryData.AllowedSubsetList.Count);
            subsetRepository.GetEnumerator().Returns(MockRepositoryData.AllowedSubsetList.GetEnumerator());
            return subsetRepository;
        }

        public static IProductContext GetProductContext()
        {
            IProductContext productContext = Substitute.For<IProductContext>();
            productContext.ShortCode.Returns("TestShortCode");
            productContext.SubProductId.Returns("TestSubProductId");
            return productContext;
        }

        private (IDataPresenceGuarantor, DataPreloader, IMemoryCache) GetDataPreloaderInstance()
        {
            var logger = Substitute.For<ILogger<DataPreloader>>();
            var subsetRepository = GetSubsetRepository();
            var respondentRepositorySource = SourceDataRepositoryMocks.GetRespondentRepository();
            var savedReportService = GetSavedReportService();
            var dataPresenceGuarantor = GetDataPresenceGuarantor();
            var dataPreloadTaskCache = new DataPreloadTaskCache();
            var productContext = GetProductContext();

            return (dataPresenceGuarantor, new DataPreloader(
                logger,
                subsetRepository,
                respondentRepositorySource,
                savedReportService,
                dataPresenceGuarantor,
                dataPreloadTaskCache,
                productContext),
                    dataPreloadTaskCache.GetCache());
        }

        [Test]
        public void ShouldGetTwoReportsToLoad()
        {
            (var _, var dataPreloader, var _) = GetDataPreloaderInstance();

            var reports = dataPreloader.GetReportsToPreload();

            Assert.That(reports.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ShouldCallEnsureDataIsLoaded()
        {
            var preloadCountdown = new CountdownEvent(1);
            (var dataPresenceGuarantor, var dataPreloader, var _) = GetDataPreloaderInstance();

            dataPresenceGuarantor.When(x => x.EnsureDataIsLoaded(
                    Arg.Any<IRespondentRepository>(),
                    Arg.Any<Subset>(),
                    Arg.Any<Measure>(),
                    Arg.Any<CalculationPeriod>(),
                    Arg.Any<AverageDescriptor>(),
                    Arg.Any<IFilter>(),
                    Arg.Any<IReadOnlyCollection<IDataTarget>>(),
                    Arg.Any<Break[]>(),
                    Arg.Any<CancellationToken>()))
                .Do(x => preloadCountdown.Signal());

            dataPreloader.PreloadReportDataIntoMemory(CancellationToken.None);

            if (!preloadCountdown.Wait(10000))
            {
                Assert.Fail("DataPreloader did not call EnsureDataIsLoaded in expected time");
            }

            dataPresenceGuarantor.Received().EnsureDataIsLoaded(
                Arg.Any<IRespondentRepository>(),
                Arg.Any<Subset>(),
                Arg.Any<Measure>(),
                Arg.Any<CalculationPeriod>(),
                Arg.Any<AverageDescriptor>(),
                Arg.Any<IFilter>(),
                Arg.Any<IReadOnlyCollection<IDataTarget>>(),
                Arg.Any<Break[]>(),
                Arg.Any<CancellationToken>());
        }

        [Test]
        public void ShouldAddNewCacheEntry()
        {
            var taskCount = 10;
            (var _, var dataPreloader, var cache) = GetDataPreloaderInstance();

            var taskStatus = dataPreloader.StartCachingExportResult(taskCount);
            Assert.That(cache.Get(dataPreloader.CacheKey), Is.Not.Null);
        }

        [TestCase(10, 1)]
        [TestCase(10, 10)]
        public void ShouldIncrementCountInCache(int taskCount, int completedCount)
        {
            (var _, var dataPreloader, var cache) = GetDataPreloaderInstance();

            var taskStatus = dataPreloader.StartCachingExportResult(taskCount);
            for (int i = 0; i < completedCount; i++)
            {
                dataPreloader.UpdateCachedTask();
            }

            var cacheEntry = cache.Get(dataPreloader.CacheKey) as DataPreloadTask;

            Assert.That(cacheEntry.Status.CompletedCount, Is.EqualTo(completedCount));
        }

        [TestCase(10, 1, false)]
        [TestCase(10, 10, true)]
        [TestCase(10, 11, true)]
        public void ShouldHaveCorrectIsComplete(int taskCount, int completedCount, bool isComplete)
        {
            (var _, var dataPreloader, var cache) = GetDataPreloaderInstance();

            var taskStatus = dataPreloader.StartCachingExportResult(taskCount);
            for (int i = 0; i < completedCount; i++)
            {
                dataPreloader.UpdateCachedTask();
            }

            var cacheEntry = cache.Get(dataPreloader.CacheKey) as DataPreloadTask;

            Assert.That(cacheEntry.Status.IsComplete, Is.EqualTo(isComplete));
        }

        [Test]
        public void ShouldRemoveCacheEntry()
        {
            var taskCount = 10;
            (var _, var dataPreloader, var cache) = GetDataPreloaderInstance();

            var taskStatus = dataPreloader.StartCachingExportResult(taskCount);
            dataPreloader.UpdateCachedTask();

            Assert.That(cache.Get(dataPreloader.CacheKey), Is.Not.Null);

            dataPreloader.ClearTaskStatus();

            Assert.That(cache.Get(dataPreloader.CacheKey), Is.Null);
        }

        [Test]
        public void ShouldCancelToken()
        {
            (var _, var dataPreloader, var _) = GetDataPreloaderInstance();

            var taskStatus = dataPreloader.StartCachingExportResult(10);
            var token = taskStatus.CancellationTokenSource.Token;
            dataPreloader.CancelTask();

            Assert.That(token.IsCancellationRequested, Is.True);
        }

        [Test]
        public void ShouldMarkTaskAsCancelled()
        {
            (var _, var dataPreloader, var _) = GetDataPreloaderInstance();

            var taskStatus = dataPreloader.StartCachingExportResult(10);
            var token = taskStatus.CancellationTokenSource.Token;
            dataPreloader.CancelTask();
            var newStatus = dataPreloader.CheckTaskStatus();

            Assert.That(newStatus.IsCancelled, Is.True);
        }
    }
}

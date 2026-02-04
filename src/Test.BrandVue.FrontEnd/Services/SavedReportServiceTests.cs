using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Test.BrandVue.FrontEnd.Mocks;
using TestCommon;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class SavedReportServiceTests
    {
        private const string PrimaryUserId = "PrimaryUser123";
        private const string AuthCompany = "TestCompany";
        private const string SecondaryUserId = "SecondaryUser123";
        private ITestMetadataContextFactory _testMetadataContextFactory;
        private ISavedReportService _user1ReportService;
        private ISavedReportService _user2ReportService;

        const string SubsetId = "test";
        const string SubProductId = "12345";
        private readonly IProductContext _productContext = new ProductContext(SubsetId, SubProductId, true, "surveyName");
        private readonly IUserContext _primaryUserContext = Substitute.For<IUserContext>();
        private readonly IUserContext _secondaryUserContext = Substitute.For<IUserContext>();

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
            _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemoryTransactionless);
            _user1ReportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            _user2ReportService = GetReportService(_secondaryUserContext, _testMetadataContextFactory);
            _primaryUserContext.UserId.Returns(PrimaryUserId);
            _primaryUserContext.AuthCompany.Returns(AuthCompany);
            _secondaryUserContext.UserId.Returns(SecondaryUserId);
            _secondaryUserContext.AuthCompany.Returns(AuthCompany);

            _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemoryTransactionless);
            _user1ReportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            _user2ReportService = GetReportService(_secondaryUserContext, _testMetadataContextFactory);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _testMetadataContextFactory.RevertDatabase();
        }

        private ISavedReportService GetReportService(IUserContext user, IDbContextFactory<MetaDataContext> _testMetadataContextFactory, IPartsRepository partsRepositoryOverride = null, IPanesRepository panesRepositoryOverride = null, IPagesRepository pagesRepositoryOverride = null, IMeasureRepository measureRepositoryOverride = null)
        {
            var reportRepository = new SavedReportRepository(_productContext, _testMetadataContextFactory);
            var subsetRepository = Substitute.For<ISubsetRepository>();
            var partsRepository = partsRepositoryOverride ?? new PartsRepositorySql(_productContext, _testMetadataContextFactory);
            var panesRepository = panesRepositoryOverride ?? new PanesRepositorySql(_productContext, _testMetadataContextFactory, partsRepository, subsetRepository);
            var pagesRepository = pagesRepositoryOverride ?? new PagesRepositorySql(_productContext, _testMetadataContextFactory, panesRepository);
            var savedBreaksRepository = new SavedBreaksRepository(_productContext, _testMetadataContextFactory, user);
            var metricConfigurationRepository = new MetricConfigurationRepositorySql(_testMetadataContextFactory, _productContext, Substitute.For<IMetricFactory>(), Substitute.For<ILogger<IMetricConfigurationRepository>>());
            var variableConfigurationRepository = new VariableConfigurationRepository(_testMetadataContextFactory, _productContext);
            var weightingPlanRepository = new WeightingPlanRepository(_testMetadataContextFactory);
            var responseWeightingRepository = new ResponseWeightingRepository(_testMetadataContextFactory, _productContext);
            var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
            var weightingPlanService = new WeightingPlanService(_productContext, new MetricRepository(userPermissionsService), weightingPlanRepository, responseWeightingRepository);
            var measureRepository = measureRepositoryOverride ?? Substitute.For<IMeasureRepository>();
            var averageDescriptorRepository = SourceDataRepositoryMocks.GetAverageDescriptorRepository();
            var requestAdapter = Substitute.For<IRequestAdapter>();
            requestAdapter.CreateBreaks(Arg.Any<CrossMeasure[]>(), Arg.Any<string>()).ReturnsForAnyArgs(Array.Empty<Break>());
            var entityRepository = SourceDataRepositoryMocks.GetEntityRepository();
            return new SavedReportService(reportRepository, user, _productContext, pagesRepository, panesRepository, partsRepository, savedBreaksRepository, metricConfigurationRepository, variableConfigurationRepository, weightingPlanService, measureRepository, averageDescriptorRepository, requestAdapter, entityRepository);
        }

        private (int ReportId, CreateNewReportRequest Request) CreateReport(
            ISavedReportService reportService, ReportType reportType, bool isShared, bool isDefault, PageDescriptor page)
        {
            var request = new CreateNewReportRequest
            {
                ReportType = reportType,
                IsShared = isShared,
                IsDefault = isDefault,
                Page = page,
                Order = ReportOrder.ResultOrderDesc,
                Waves = new ReportWaveConfiguration
                {
                    WavesToShow = ReportWavesOptions.MostRecentNWaves,
                    NumberOfRecentWaves = 3,
                    Waves = null,
                },
                OverTimeConfig = new ReportOverTimeConfiguration
                {
                    Range = "test",
                    AverageId = "averageId"
                }
            };
            var reportId = reportService.CreateReport(request);
            return (reportId, request);
        }

        private PageDescriptor GetPage(string pageName, PaneDescriptor[] panes = null)
        {
            return new PageDescriptor
            {
                Name = pageName,
                DisplayName = pageName,
                PageType = "SubPage",
                Panes = panes ?? Array.Empty<PaneDescriptor>()
            };
        }

        [Test]
        public void ShouldCreateReportWithAllSettings()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var pageName = "ChartReport";
            var (chartReportId, createChartReport) = CreateReport(reportService, ReportType.Chart, true, true, GetPage(pageName));
            Assert.That(chartReportId, Is.Not.Zero);
            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var savedReport = dbContext.SavedReports.Single(r => r.Id == chartReportId);
            var page = dbContext.Pages.Single(r => r.Id == savedReport.ReportPageId);
            var expectedReport = new SavedReport
            {
                Id = chartReportId,
                SubProductId = _productContext.SubProductId,
                ProductShortCode = _productContext.ShortCode,
                IsShared = createChartReport.IsShared,
                CreatedByUserId = _primaryUserContext.UserId,
                ReportPageId = page.Id,
                Order = createChartReport.Order,
                ReportType = createChartReport.ReportType,
                Waves = createChartReport.Waves,
                ModifiedDate = savedReport.ModifiedDate,
                ModifiedGuid = savedReport.ModifiedGuid,
                OverTimeConfig = createChartReport.OverTimeConfig,
                //below are defaults set in SavedReportService
                DecimalPlaces = 1,
                Breaks = new List<CrossMeasure>(),
                SinglePageExport = false,
                IncludeCounts = true,
                HighlightLowSample = true,
                HighlightSignificance = false,
                IsDataWeighted = false,
                HideEmptyRows = false,
                HideEmptyColumns = false,
                HideTotalColumn = false,
                HideDataLabels = false,
                SignificanceType = CrosstabSignificanceType.CompareWithinBreak,
                DisplaySignificanceDifferences = DisplaySignificanceDifferences.ShowBoth,
                SigConfidenceLevel = SigConfidenceLevel.NinetyFive,
                BaseTypeOverride = BaseDefinitionType.SawThisQuestion,
                DefaultFilters = new List<DefaultReportFilter>(),
                LastModifiedByUser = _primaryUserContext.UserId
            };
            Assert.That(savedReport.ReportPage.Name, Is.EqualTo(pageName));
            Assert.That(savedReport.ReportPage.DisplayName, Is.EqualTo(pageName));
            //ignore the rest of the DbPage
            savedReport.ReportPage = null;
            Assert.That(JsonSerializer.Serialize(savedReport), Is.EqualTo(JsonSerializer.Serialize(expectedReport)));
        }

        [Test]
        public void CreatingNewDefaultReportShouldRemoveOldDefault()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var (reportIdA, _) = CreateReport(reportService, ReportType.Chart, true, true, GetPage("chartTest"));
            var defaultReportId = reportService.GetAllReportsForCurrentUser().DefaultReportId;
            Assert.That(defaultReportId, Is.EqualTo(reportIdA));
            var (reportIdB, _) = CreateReport(reportService, ReportType.Table, true, true, GetPage("tableTest"));
            defaultReportId = reportService.GetAllReportsForCurrentUser().DefaultReportId;
            Assert.That(reportIdB, Is.Not.EqualTo(reportIdA));
            Assert.That(defaultReportId, Is.EqualTo(reportIdB));
            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var defaultReports = dbContext.DefaultSavedReports.ToArray();
            Assert.That(defaultReports.Length, Is.EqualTo(1));
        }

        [Test]
        public void ShouldOnlyGetSharedReportsOrOwnedByUser()
        {
            var reportServiceA = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var reportServiceB = GetReportService(_secondaryUserContext, _testMetadataContextFactory);
            var (userASharedReportId, _) = CreateReport(reportServiceA, ReportType.Chart, isShared: true, false, GetPage("UserA_SharedReport"));
            var (userAPrivateReportId, _) = CreateReport(reportServiceA, ReportType.Table, isShared: false, false, GetPage("UserA_PrivateReport"));
            var (userBSharedReportId, _) = CreateReport(reportServiceB, ReportType.Chart, isShared: true, false, GetPage("UserB_SharedReport"));
            var (userBPrivateReportId, _) = CreateReport(reportServiceB, ReportType.Table, isShared: false, false, GetPage("UserB_PrivateReport"));
            var userAReports = reportServiceA.GetAllReportsForCurrentUser();
            Assert.That(userAReports.Reports.Select(r => r.SavedReportId), Is.EquivalentTo(new[] { userASharedReportId, userAPrivateReportId, userBSharedReportId }));
        }

        private IEnumerable<Measure> CreateMeasuresUserCantAccess()
        {
            return new List<Measure>
            {
                new Measure
                {
                    UrlSafeName = "noaccesstometric",
                    Name = "NoAccessToMetric",
                    VarCode = "NoAccessToMetric",
                    VariableConfigurationId = 1000
                }
            };
        }

        [TestCase("MetricA", true)]
        [TestCase("NoAccessToMetric", false)]
        public void ShouldGetReportsWithCorrectUserAccessFlag(string metricNameA, bool expectedUserCanAccessValue)
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory, measureRepositoryOverride: SourceDataRepositoryMocks.GetMeasureRepository(measuresNotAvailableToCurrentUser: CreateMeasuresUserCantAccess()));
            var originalPageName = "original";
            var metricNameB = "MetricB";

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            dbContext.MetricConfigurations.Add(new MetricConfiguration
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Name = metricNameA,
                CalcType = CalculationTypeParser.AsString(CalculationType.YesNo),
            });
            dbContext.MetricConfigurations.Add(new MetricConfiguration
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Name = metricNameB,
                CalcType = CalculationTypeParser.AsString(CalculationType.YesNo),
            });
            dbContext.SaveChanges();

            var pane = new PaneDescriptor
            {
                Height = 500,
                PageName = originalPageName,
                PaneType = "ReportSubPage",
                View = (int)ViewTypeEnum.SingleSurveyNav,
                Parts = new[]
                {
                    new PartDescriptor
                    {
                        PartType = PartType.ReportsTable,
                        Spec1 = metricNameA,
                        Spec2 = "1",
                        DefaultSplitBy = "brand",
                        HelpText = "i'm helping"
                    },
                    new PartDescriptor
                    {
                        PartType = PartType.ReportsTable,
                        Spec1 = metricNameB,
                        Spec2 = "2",
                        DefaultSplitBy = "product",
                        HelpText = "words go here",
                    }
                }
            };
            var page = GetPage("original", new[] { pane });
            var (originalReportId, _) = CreateReport(reportService, ReportType.Table, true, false, page);
            var reports = reportService.GetAllReportsForCurrentUser();
            Assert.That(reports.Reports.First().UserHasAccess, Is.EqualTo(expectedUserCanAccessValue));
        }

        [Test]
        public void ShouldGetDefaultReportIdIfHasDefault()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var (reportId, _) = CreateReport(reportService, ReportType.Chart, true, true, GetPage("test"));
            var reports = reportService.GetAllReportsForCurrentUser();
            Assert.That(reports.DefaultReportId, Is.EqualTo(reportId));
        }

        [Test]
        public void ShouldNotGetDefaultReportIdIfNoDefault()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            CreateReport(reportService, ReportType.Chart, true, false, GetPage("test"));
            var reports = reportService.GetAllReportsForCurrentUser();
            Assert.That(reports.DefaultReportId, Is.Null);
        }

        [Test]
        public void ShouldNotBeAbleToCreatePrivateReportAsDefault()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            Assert.Throws<BadRequestException>(() => CreateReport(reportService, ReportType.Chart, isShared: false, isDefault: true, GetPage("test")));
        }

        [Test]
        public void ShouldNotBeAbleToCreateReportWithProtectedName()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            foreach (var protectedName in PageHierarchyGenerator.PROTECTED_PAGE_NAMES)
            {
                Assert.Throws<BadRequestException>(() => CreateReport(reportService, ReportType.Chart, false, false, GetPage(protectedName)));
            }
        }

        [Test]
        public void ShouldNotBeAbleToCreateReportWithDuplicateName()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var reportName = "test";
            CreateReport(reportService, ReportType.Chart, false, false, GetPage(reportName));
            Assert.Throws<BadRequestException>(() => CreateReport(reportService, ReportType.Chart, false, false, GetPage(reportName)));
        }

        [Test]
        public void CanCheckIfReportPageNameAlreadyExists()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var reportName = "test";
            var alreadyExists = reportService.CheckReportPageNameAlreadyExists(reportName, null);
            Assert.That(alreadyExists, Is.False);
            CreateReport(reportService, ReportType.Chart, false, false, GetPage(reportName));
            alreadyExists = reportService.CheckReportPageNameAlreadyExists(reportName, null);
            Assert.That(alreadyExists, Is.True);
        }

        [Test]
        public void CheckingReportNameAlreadyExistsShouldIgnoreCurrentReport()
        {
            //Checking if there is a different report with the name as it won't be a clash if this report already has the name
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var reportName = "test";
            var (reportId, _) = CreateReport(reportService, ReportType.Chart, false, false, GetPage(reportName));
            var alreadyExists = reportService.CheckReportPageNameAlreadyExists(reportName, reportId);
            Assert.That(alreadyExists, Is.False);
        }

        [Test]
        public void ShouldCopyEntireReport()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var originalPageName = "original";
            var copiedPageName = "copy";
            var metricNameA = "MetricA";
            var metricNameB = "MetricB";

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            dbContext.MetricConfigurations.Add(new MetricConfiguration
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Name = metricNameA,
                CalcType = CalculationTypeParser.AsString(CalculationType.YesNo),
            });
            dbContext.MetricConfigurations.Add(new MetricConfiguration
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Name = metricNameB,
                CalcType = CalculationTypeParser.AsString(CalculationType.YesNo),
            });
            dbContext.SaveChanges();

            var pane = new PaneDescriptor
            {
                Height = 500,
                PageName = originalPageName,
                PaneType = "ReportSubPage",
                View = (int)ViewTypeEnum.SingleSurveyNav,
                Parts = new[]
                {
                    new PartDescriptor
                    {
                        PartType = PartType.ReportsTable,
                        Spec1 = metricNameA,
                        Spec2 = "1",
                        DefaultSplitBy = "brand",
                        HelpText = "i'm helping"
                    },
                    new PartDescriptor
                    {
                        PartType = PartType.ReportsTable,
                        Spec1 = metricNameB,
                        Spec2 = "2",
                        DefaultSplitBy = "product",
                        HelpText = "words go here",
                    }
                }
            };
            var page = GetPage("original", new[] { pane });
            var (originalReportId, _) = CreateReport(reportService, ReportType.Table, true, false, page);
            var copiedReportId = reportService.CopyReport(new CopySavedReportRequest
            {
                ReportId = originalReportId,
                ExistingPage = page,
                NewName = copiedPageName,
                NewDisplayName = copiedPageName,
                IsShared = true,
                IsDefault = false,
            });

            Assert.That(copiedReportId, Is.Not.EqualTo(originalReportId));
            var reports = reportService.GetAllReportsForCurrentUser().Reports;
            Assert.That(reports.Select(r => r.SavedReportId), Is.EquivalentTo(new[] { originalReportId, copiedReportId }));
            var originalReport = reports.Single(r => r.SavedReportId == originalReportId);
            var copiedReport = reports.Single(r => r.SavedReportId == copiedReportId);
            Assert.That(copiedReport.PageId, Is.Not.EqualTo(originalReport.PageId));
            //clear properties that are expected to be different
            originalReport.SavedReportId = 0;
            copiedReport.SavedReportId = 0;
            originalReport.PageId = 0;
            copiedReport.PageId = 0;
            copiedReport.ModifiedDate = originalReport.ModifiedDate;
            copiedReport.ModifiedGuid = originalReport.ModifiedGuid;
            Assert.That(JsonSerializer.Serialize(copiedReport), Is.EqualTo(JsonSerializer.Serialize(originalReport)));

            var pages = dbContext.Pages.ToList();
            var panes = dbContext.Panes.ToList();
            var parts = dbContext.Parts.ToList();
            Assert.That(pages.Select(p => p.Name), Is.EquivalentTo(new[] { originalPageName, copiedPageName }));
            Assert.That(panes.Select(p => p.PageName), Is.EquivalentTo(new[] { originalPageName, copiedPageName }));
            Assert.That(parts.Select(p => p.PaneId), Is.EquivalentTo(new[] { panes[0].PaneId, panes[0].PaneId, panes[1].PaneId, panes[1].PaneId }));
        }

        [Test]
        public void ShouldUpdateAllReportSettings()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var (reportId, _) = CreateReport(reportService, ReportType.Chart, false, false, GetPage("test"));
            var originalReport = reportService.GetAllReportsForCurrentUser().Reports.Single();

            var updateRequest = new UpdateReportSettingsRequest
            {
                PageDisplayName = "NewDisplayName",
                PageName = "NewName",
                SavedReportId = reportId,
                IsShared = true,
                IsDefault = true,
                Order = ReportOrder.ScriptOrderDesc,
                DecimalPlaces = 7,
                Waves = null,
                Breaks = new[] { new CrossMeasure { MeasureName = "testMeasure" } },
                IncludeCounts = false,
                HighlightLowSample = false,
                HighlightSignificance = true,
                DisplaySignificanceDifferences = DisplaySignificanceDifferences.ShowBoth,
                SignificanceType = CrosstabSignificanceType.CompareWithinBreak,
                SigConfidenceLevel = SigConfidenceLevel.NinetyNine,
                SinglePageExport = true,
                IsDataWeighted = true,
                HideEmptyRows = true,
                HideTotalColumn = true,
                HideDataLabels = true,
                BaseTypeOverride = BaseDefinitionType.AllRespondents,
                DefaultFilters = new[] { new DefaultReportFilter { MeasureName = "testFilter" } },
                ModifiedGuid = originalReport.ModifiedGuid,
                OverTimeConfig = new ReportOverTimeConfiguration
                {
                    Range = "test",
                    AverageId = "averageId"
                },
                SubsetId = SubsetId,
                LowSampleThreshold = 33
            };
            reportService.UpdateReportSettings(updateRequest);
            var updatedReports = reportService.GetAllReportsForCurrentUser();
            var newReport = updatedReports.Reports.Single();
            var expectedReport = new Report
            {
                SavedReportId = originalReport.SavedReportId,
                IsShared = true,
                PageId = originalReport.PageId,
                ReportOrder = updateRequest.Order,
                ModifiedDate = newReport.ModifiedDate,
                ModifiedGuid = newReport.ModifiedGuid,
                DecimalPlaces = updateRequest.DecimalPlaces,
                ReportType = newReport.ReportType,
                Waves = updateRequest.Waves,
                Breaks = updateRequest.Breaks.ToList(),
                IncludeCounts = updateRequest.IncludeCounts,
                HighlightLowSample = updateRequest.HighlightLowSample,
                HighlightSignificance = updateRequest.HighlightSignificance,
                SignificanceType = updateRequest.SignificanceType,
                DisplaySignificanceDifferences = DisplaySignificanceDifferences.ShowBoth,
                SigConfidenceLevel = updateRequest.SigConfidenceLevel,
                SinglePageExport = updateRequest.SinglePageExport,
                IsDataWeighted = updateRequest.IsDataWeighted,
                HideEmptyRows = updateRequest.HideEmptyRows,
                HideEmptyColumns = updateRequest.HideEmptyColumns,
                HideTotalColumn = updateRequest.HideTotalColumn,
                HideDataLabels = updateRequest.HideDataLabels,
                BaseTypeOverride = updateRequest.BaseTypeOverride.Value,
                DefaultFilters = updateRequest.DefaultFilters.ToList(),
                LastModifiedByUser = _primaryUserContext.UserId,
                OverTimeConfig = updateRequest.OverTimeConfig,
                SubsetId = updateRequest.SubsetId,
                UserHasAccess = true,
                LowSampleThreshold = updateRequest.LowSampleThreshold
            };
            Assert.That(JsonSerializer.Serialize(newReport), Is.EqualTo(JsonSerializer.Serialize(expectedReport)));
            Assert.That(updatedReports.DefaultReportId, Is.EqualTo(reportId));
            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            var page = dbContext.Pages.Single(p => p.Id == newReport.PageId);
            Assert.That(page.Name, Is.EqualTo(updateRequest.PageName));
            Assert.That(page.DisplayName, Is.EqualTo(updateRequest.PageDisplayName));
        }

        [Test]
        public void ShouldDeleteReportAndPage()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var (reportId, _) = CreateReport(reportService, ReportType.Chart, true, true, GetPage("test"));
            reportService.DeleteReport(reportId);

            using var dbContext = _testMetadataContextFactory.CreateDbContext();
            Assert.That(dbContext.SavedReports.ToArray(), Is.Empty);
            Assert.That(dbContext.DefaultSavedReports.ToArray(), Is.Empty);
            Assert.That(dbContext.Pages.ToArray(), Is.Empty);
        }

        [Test]
        public void ShouldNotModifyPartsInOutOfDateReport()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));

            var updateRequest = new ModifyReportPartsRequest
            {
                SavedReportId = reportId,
                Parts = Array.Empty<PartDescriptor>(),
                ExpectedGuid = "out of date GUID"
            };
            var deleteRequest = new DeleteReportPartRequest
            {
                SavedReportId = reportId,
                PartIdToDelete = 1,
                PartsToUpdate = Array.Empty<PartDescriptor>(),
                ExpectedGuid = "out of date GUID"
            };
            Assert.Throws<ReportOutOfDateException>(() => _user2ReportService.AddParts(updateRequest));
            Assert.Throws<ReportOutOfDateException>(() => _user2ReportService.UpdateParts(updateRequest));
            Assert.Throws<ReportOutOfDateException>(() => _user2ReportService.DeletePart(deleteRequest));
        }

        [Test]
        public void ShouldDetectChangesIfPartsWereAddedByAnotherUser()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));
            var report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            var updateRequest = new ModifyReportPartsRequest
            {
                SavedReportId = reportId,
                Parts = Array.Empty<PartDescriptor>(),
                ExpectedGuid = originalGuid
            };
            _user2ReportService.AddParts(updateRequest);
            var hasChanged = _user1ReportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.True);
        }

        [Test]
        public void ShouldNotDetectChangesIfPartsWereAddedInitialUser()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));
            var report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            var updateRequest = new ModifyReportPartsRequest
            {
                SavedReportId = reportId,
                Parts = Array.Empty<PartDescriptor>(),
                ExpectedGuid = originalGuid
            };
            _user1ReportService.AddParts(updateRequest);
            var hasChanged = _user1ReportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.False);
        }

        [Test]
        public void ShouldDetectChangesIfPartsWereUpdatedByAnotherUser()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));
            var report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            var updateRequest = new ModifyReportPartsRequest
            {
                SavedReportId = reportId,
                Parts = Array.Empty<PartDescriptor>(),
                ExpectedGuid = originalGuid
            };
            _user2ReportService.UpdateParts(updateRequest);
            var hasChanged = _user1ReportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.True);
        }

        [Test]
        public void ShouldNotDetectChangesIfPartsWereUpdatedByInitialUser()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));
            var report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            var updateRequest = new ModifyReportPartsRequest
            {
                SavedReportId = reportId,
                Parts = Array.Empty<PartDescriptor>(),
                ExpectedGuid = originalGuid
            };
            _user1ReportService.UpdateParts(updateRequest);
            var hasChanged = _user1ReportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.False);
        }

        [Test]
        public void ShouldDetectChangesIfPartsWereDeletedByAnotherUser()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));
            var report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            _user1ReportService.AddParts(new ModifyReportPartsRequest
            {
                SavedReportId = report.SavedReportId,
                ExpectedGuid = report.ModifiedGuid,
                Parts = [ new PartDescriptor
                    {
                        Spec1 = "test"
                    }
                ]
            });

            report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            var deleteRequest = new DeleteReportPartRequest
            {
                SavedReportId = reportId,
                PartIdToDelete = 1,
                PartsToUpdate = Array.Empty<PartDescriptor>(),
                ExpectedGuid = originalGuid
            };
            _user2ReportService.DeletePart(deleteRequest);
            var hasChanged = _user1ReportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.True);
        }

        [Test]
        public void ShouldNotDetectChangesIfPartsWereDeletedByInitialUser()
        {
            var (reportId, _) = CreateReport(_user1ReportService, ReportType.Chart, false, false, GetPage("test"));
            var report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            _user1ReportService.AddParts(new ModifyReportPartsRequest
            {
                SavedReportId = report.SavedReportId,
                ExpectedGuid = report.ModifiedGuid,
                Parts = [ new PartDescriptor
                    {
                        Spec1 = "test"
                    }
                ]
            });

            report = _user1ReportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            var deleteRequest = new DeleteReportPartRequest
            {
                SavedReportId = reportId,
                PartIdToDelete = 1,
                PartsToUpdate = Array.Empty<PartDescriptor>(),
                ExpectedGuid = originalGuid
            };
            _user1ReportService.DeletePart(deleteRequest);
            var hasChanged = _user1ReportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.False);
        }

        [Test]
        public void ShouldDetectChangesIfReportDeleted()
        {
            var fakePartsRepository = Substitute.For<IPartsRepository>();
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory, fakePartsRepository);
            var (reportId, _) = CreateReport(reportService, ReportType.Chart, false, false, GetPage("test"));
            var report = reportService.GetAllReportsForCurrentUser().Reports.Single();
            var originalGuid = report.ModifiedGuid;

            reportService.DeleteReport(reportId);
            var hasChanged = reportService.HasReportChanged(reportId, originalGuid);
            Assert.That(hasChanged, Is.True);
        }

        [Test]
        public void ShouldParseNoReportsFromEmptyList()
        {
            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory);
            var subset = new Subset { Id = "All", DisplayName = "All" };
            var reports = new List<Report>();
            var reportDetails = reportService.ParseReportsForSubset(reports, subset);
            Assert.That(reportDetails.Count(), Is.EqualTo(0));
        }

        [Test]
        public void ShouldParseReportsFromList()
        {
            var fakePartsRepository = Substitute.For<IPartsRepository>();
            fakePartsRepository.GetParts().Returns(new List<PartDescriptor>()
            {
                new PartDescriptor
                {
                    Id = 1,
                    PaneId = "Net Buzz_1",
                    PartType = PartType.ReportsTable,
                    Spec1 = "Net Buzz"
                }
            });

            var reportService = GetReportService(_primaryUserContext, _testMetadataContextFactory, fakePartsRepository, SourceDataRepositoryMocks.GetPanesRepository(), SourceDataRepositoryMocks.GetPagesRepository(), SourceDataRepositoryMocks.GetMeasureRepository());
            var subset = new Subset { Id = "All", DisplayName = "All" };
            var reports = new List<Report>
            {
                new Report()
                {
                    SavedReportId = 1,
                    PageId = 1
                }
            };

            var averageDescriptorRepository = SourceDataRepositoryMocks.GetAverageDescriptorRepository();

            var reportDetails = reportService.ParseReportsForSubset(reports, subset);
            Assert.That(reportDetails.Count(), Is.EqualTo(averageDescriptorRepository.Count));
        }
    }
}

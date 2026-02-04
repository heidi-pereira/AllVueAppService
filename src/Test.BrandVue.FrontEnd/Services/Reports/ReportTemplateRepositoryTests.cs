using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Services.Reports;
using NSubstitute;
using NUnit.Framework;
using System.Threading.Tasks;
using TestCommon;

namespace Test.BrandVue.FrontEnd.Services.Reports
{
    internal class ReportTemplateRepositoryTests
    {
        private ITestMetadataContextFactory _testMetadataContextFactory;
        private IReportTemplateRepository _reportTemplateRepository;
        private SavedReportTemplate _savedReportTemplate;
        private IUserContext _userContext;
        const string userId = "user123";

        [SetUp]
        public async Task SetUp()
        {
            _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemory);
            _userContext = Substitute.For<IUserContext>();
            _userContext.UserId.Returns(userId);
            _reportTemplateRepository = new ReportTemplateRepository(_testMetadataContextFactory, _userContext);
            _savedReportTemplate = new SavedReportTemplate(true,
                ReportOrder.ResultOrderDesc,
                1,
                ReportType.Chart,
                null,
                null,
                true,
                true,
                CrosstabSignificanceType.CompareToTotal,
                DisplaySignificanceDifferences.None,
                SigConfidenceLevel.NinetyNine,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                false,
                false,
                null,
                null,
                null,
                "subset",
                null);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _testMetadataContextFactory.RevertDatabase();
        }

        [Test]
        public async Task ReportTemplateRepositoryShouldReturnGetById()
        {
            const string templateDisplayName = "test";

            var template = new ReportTemplate()
            {
                TemplateDisplayName = templateDisplayName,
                SavedReportTemplate = _savedReportTemplate,
                UserId = userId
            };

            var createdTemplate = await _reportTemplateRepository.CreateAsync(template);

            var fetchedTemplate = _reportTemplateRepository.GetTemplateById(createdTemplate.Id);

            Assert.That(fetchedTemplate.TemplateDisplayName, Is.EqualTo(templateDisplayName));
            var savedTemplateJson = System.Text.Json.JsonSerializer.Serialize(_savedReportTemplate);
            var fetchedTemplateJson = System.Text.Json.JsonSerializer.Serialize(fetchedTemplate.SavedReportTemplate);
            Assert.That(fetchedTemplateJson, Is.EqualTo(savedTemplateJson));
            Assert.That(fetchedTemplate.UserId, Is.EqualTo(userId));
        }

        [Test]
        public async Task UserShouldOnlySeeTheirTemplatesAndNotCareAboutSharedNames()
        {
            // Add templates for two users
            await _reportTemplateRepository.CreateAsync(new ReportTemplate { TemplateDisplayName = "test", SavedReportTemplate = _savedReportTemplate, UserId = _userContext.UserId });
            await _reportTemplateRepository.CreateAsync(new ReportTemplate { TemplateDisplayName = "test", SavedReportTemplate = _savedReportTemplate, UserId = "userabc" });

            // Only test specific user's template should be returned
            var fetchedTemplates = _reportTemplateRepository.GetAllForUser();
            Assert.That(fetchedTemplates, Has.Count.EqualTo(1));
        }
    }
}

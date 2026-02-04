using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestCommon;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.AutoGeneration;
using System.Linq;
using BrandVue.Services.Reports;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Respondents;
using Microsoft.Extensions.Logging;
using BrandVue.SourceData.Subsets;
using BrandVue.EntityFramework.MetaData.Averages;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class ReportTemplateServiceTests
    {
        private const string PrimaryUserId = "PrimaryUser123";
        private const string AuthCompany = "TestCompany";
        private ReportTemplateRepository _reportTemplateRepository;
        private IAverageConfigurationRepository _averageConfigurationRepositorySeeding;
        private IAverageConfigurationRepository _averageConfigurationRepositorySaving;
        private ReportTemplateService _templateServiceForSavingTemplate;
        private ReportTemplateService _templateServiceForSeedingTemplate;
        private const string ShortCode = "survey";
        private const string SubProductIdSaving = "12345";
        private const string SubProductIdLoading = "678910";
        private const string AverageId = "testAverageId";
        private ITestMetadataContextFactory _testMetadataContextFactory;
        private readonly IProductContext _productContextSaving = new ProductContext(ShortCode, SubProductIdSaving, true, "surveyName");
        private readonly IProductContext _productContextSeeding = new ProductContext(ShortCode, SubProductIdLoading, true, "survey2Name");
        private IUserContext _userContext;

        private IMetricConfigurationRepository _metricConfigurationRepositorySeeding;
        private IVariableConfigurationRepository _variableConfigurationRepositorySaving;
        private IVariableConfigurationRepository _variableConfigurationRepositorySeeding;

        private IPartsRepository _partsRepositorySeeding;

        private const int ChartReportId = 1;
        private const int TableReportId = 2;
        private const string Metric1Name = "metric1";
        private const string Metric2Name = "metric2";
        private Dictionary<int, VariableDefinition> _definitionLookup;

        private IVariable<int?> _parsedVariable;

        private SavedReport _chartSavedReport;
        private SavedReport _tableSavedReport;
        private IResponseEntityTypeRepository _responseEntityTypeRepository;
        private IResponseFieldManager _responseFieldManager;
        private IEntityRepository _entityRepository;
        private IFieldExpressionParser _fieldExpressionParser;
        private IBaseExpressionGenerator _baseExpressionGenerator;
        private IVariableFactory _fakeVariableFactory;
        private ILogger<IMetricConfigurationRepository> _logger;

        [SetUp]
        public async Task SetUp()
        {
            _testMetadataContextFactory = await ITestMetadataContextFactory.CreateAsync(StorageType.InMemoryTransactionless);

            PopulateFields();
            GenerateReports();
            PopulateTemplateServiceForSavingTemplate();
            PopulateTemplateServiceForSeedingNewReport();
        }

        [TestCase("ChartReport", "This is a template created from a chart report.")]
        [TestCase("TableReport", "This is a template created from a table report.")]
        public async Task ShouldCreateTemplateFromExistingSavedReportAsync(string templateDisplayName, string templateDescription)
        {
            SavedReport originalReport;
            ReportTemplateModel templateModel;
            GetReportModel(templateDisplayName, templateDescription, out originalReport, out templateModel);
            var reportTemplate = await _templateServiceForSavingTemplate.SaveReportAsTemplate(templateModel);

            Assert.Multiple(() =>
            {
                Assert.That(reportTemplate.TemplateDisplayName, Is.EqualTo(templateDisplayName), $"Display name was {reportTemplate.TemplateDisplayName} but should be {templateDisplayName}");
                Assert.That(reportTemplate.TemplateDescription, Is.EqualTo(templateDescription), $"Template description was {reportTemplate.TemplateDescription} but should be {templateDescription}");
                Assert.That(reportTemplate.UserId, Is.EqualTo(_userContext.UserId), $"User ID was {reportTemplate.UserId} but should be {_userContext.UserId}");
                Assert.That(reportTemplate.CreatedAt, Is.Not.Null, $"CreatedDate was null but should have a value");

                if (originalReport.BaseVariableId.HasValue)
                {
                    Assert.That(reportTemplate.BaseVariable, Is.Not.Null, $"Base variable was null but should not be null when BaseVariableId is {originalReport.BaseVariableId}");
                    Assert.That(reportTemplate.BaseVariable.Definition, Is.InstanceOf<BaseGroupedVariableDefinition>(), $"Base variable definition was {reportTemplate.BaseVariable?.Definition?.GetType().Name} but should be BaseGroupedVariableDefinition");
                }
                else
                {
                    Assert.That(reportTemplate.BaseVariable, Is.Null, $"Base variable was {reportTemplate.BaseVariable} but should be null when BaseVariableId is null");
                }

                Assert.That(reportTemplate.SavedReportTemplate.ReportType, Is.EqualTo(originalReport.ReportType), $"Report type was {reportTemplate.SavedReportTemplate.ReportType} but should be {originalReport.ReportType}");
                Assert.That(reportTemplate.SavedReportTemplate.IsShared, Is.EqualTo(originalReport.IsShared), $"IsShared was {reportTemplate.SavedReportTemplate.IsShared} but should be {originalReport.IsShared}");
                Assert.That(reportTemplate.SavedReportTemplate.IncludeCounts, Is.EqualTo(originalReport.IncludeCounts), $"IncludeCounts was {reportTemplate.SavedReportTemplate.IncludeCounts} but should be {originalReport.IncludeCounts}");
                Assert.That(reportTemplate.SavedReportTemplate.HideTotalColumn, Is.EqualTo(originalReport.HideTotalColumn), $"HideTotalColumn was {reportTemplate.SavedReportTemplate.HideTotalColumn} but should be {originalReport.HideTotalColumn}");
                Assert.That(reportTemplate.SavedReportTemplate.HideDataLabels, Is.EqualTo(originalReport.HideDataLabels), $"HideDataLabels was {reportTemplate.SavedReportTemplate.HideDataLabels} but should be {originalReport.HideDataLabels}");
                Assert.That(reportTemplate.SavedReportTemplate.ShowMultipleTablesAsSingle, Is.EqualTo(originalReport.ShowMultipleTablesAsSingle), $"HideTotalColumn was {reportTemplate.SavedReportTemplate.ShowMultipleTablesAsSingle} but should be {originalReport.ShowMultipleTablesAsSingle}");
                Assert.That(reportTemplate.SavedReportTemplate.HighlightSignificance, Is.EqualTo(originalReport.HighlightSignificance), $"HighlightSignificance was {reportTemplate.SavedReportTemplate.HighlightSignificance} but should be {originalReport.HighlightSignificance}");
                Assert.That(reportTemplate.SavedReportTemplate.HideEmptyRows, Is.EqualTo(originalReport.HideEmptyRows), $"HideEmptyRows was {reportTemplate.SavedReportTemplate.HideEmptyRows} but should be {originalReport.HideEmptyRows}");
                Assert.That(reportTemplate.SavedReportTemplate.HideEmptyColumns, Is.EqualTo(originalReport.HideEmptyColumns), $"HideEmptyColumns was {reportTemplate.SavedReportTemplate.HideEmptyColumns} but should be {originalReport.HideEmptyColumns}");
                Assert.That(reportTemplate.SavedReportTemplate.SinglePageExport, Is.EqualTo(originalReport.SinglePageExport), $"SinglePageExport was {reportTemplate.SavedReportTemplate.SinglePageExport} but should be {originalReport.SinglePageExport}");
                Assert.That(reportTemplate.SavedReportTemplate.DecimalPlaces, Is.EqualTo(originalReport.DecimalPlaces), $"DecimalPlaces was {reportTemplate.SavedReportTemplate.DecimalPlaces} but should be {originalReport.DecimalPlaces}");
                Assert.That(reportTemplate.SavedReportTemplate.Waves, Is.EqualTo(originalReport.Waves), $"Waves was {reportTemplate.SavedReportTemplate.Waves} but should be {originalReport.Waves}");
                Assert.That(reportTemplate.SavedReportTemplate.Breaks, Is.EqualTo(originalReport.Breaks), $"Breaks was {reportTemplate.SavedReportTemplate.Breaks} but should be {originalReport.Breaks}");
                Assert.That(reportTemplate.SavedReportTemplate.SignificanceType, Is.EqualTo(originalReport.SignificanceType), $"SignificanceType was {reportTemplate.SavedReportTemplate.SignificanceType} but should be {originalReport.SignificanceType}");
                Assert.That(reportTemplate.SavedReportTemplate.DisplaySignificanceDifferences, Is.EqualTo(originalReport.DisplaySignificanceDifferences), $"DisplaySignificanceDifferences was {reportTemplate.SavedReportTemplate.DisplaySignificanceDifferences} but should be {originalReport.DisplaySignificanceDifferences}");
                Assert.That(reportTemplate.SavedReportTemplate.SigConfidenceLevel, Is.EqualTo(originalReport.SigConfidenceLevel), $"SigConfidenceLevel was {reportTemplate.SavedReportTemplate.SigConfidenceLevel} but should be {originalReport.SigConfidenceLevel}");
                Assert.That(reportTemplate.SavedReportTemplate.BaseTypeOverride, Is.EqualTo(originalReport.BaseTypeOverride), $"BaseTypeOverride was {reportTemplate.SavedReportTemplate.BaseTypeOverride} but should be {originalReport.BaseTypeOverride}");
                Assert.That(reportTemplate.SavedReportTemplate.DefaultFilters, Is.EqualTo(originalReport.DefaultFilters), $"DefaultFilters was {reportTemplate.SavedReportTemplate.DefaultFilters} but should be {originalReport.DefaultFilters}");
                Assert.That(reportTemplate.SavedReportTemplate?.OverTimeConfig?.AverageId, Is.EqualTo(originalReport?.OverTimeConfig?.AverageId), $"OverTimeConfig was {reportTemplate.SavedReportTemplate.OverTimeConfig} but should be {originalReport.OverTimeConfig}");
                Assert.That(reportTemplate.SavedReportTemplate?.OverTimeConfig?.Range, Is.EqualTo(originalReport?.OverTimeConfig?.Range), $"OverTimeConfig was {reportTemplate.SavedReportTemplate.OverTimeConfig} but should be {originalReport.OverTimeConfig}");
                Assert.That(reportTemplate.SavedReportTemplate?.OverTimeConfig?.CustomRange, Is.EqualTo(originalReport?.OverTimeConfig?.CustomRange), $"OverTimeConfig was {reportTemplate.SavedReportTemplate.OverTimeConfig} but should be {originalReport.OverTimeConfig}");
                Assert.That(reportTemplate.SavedReportTemplate.LowSampleThreshold, Is.EqualTo(originalReport?.LowSampleThreshold), $"Low sample threshold was {reportTemplate.SavedReportTemplate.LowSampleThreshold} but should be {originalReport.LowSampleThreshold}");

                Assert.That(reportTemplate.ReportTemplateParts.Count(), Is.EqualTo(2), $"Report template parts count was {reportTemplate.ReportTemplateParts.Count()} but should be 2");

                const int expectedUserDefinedVariables = 4; //6 variables declared but QuestionVariableDefinition and autogenerated variables are not included in template
                Assert.That(reportTemplate.UserDefinedVariableDefinitions.Count(), Is.EqualTo(expectedUserDefinedVariables), $"User defined variable definitions count was {reportTemplate.UserDefinedVariableDefinitions.Count()} but should be 4");
            });
        }

        [TestCase("ChartReport", "This is a template created from a chart report.")]
        [TestCase("TableReport", "This is a template created from a table report.")]
        public async Task ShouldSeedTemplateIntoNewSavedReport(string templateDisplayName, string templateDescription)
        {
            SavedReport originalReport;
            ReportTemplateModel templateModel;
            GetReportModel(templateDisplayName, templateDescription, out originalReport, out templateModel);
            var reportTemplate = _templateServiceForSavingTemplate.SaveReportAsTemplate(templateModel).Result;
            var generatedReport = await _templateServiceForSeedingTemplate.CreateReportFromTemplate(reportTemplate.Id, templateDisplayName);

            var originalUserDefinedVariables = _variableConfigurationRepositorySaving.GetAll()
                .Where(v => ReportTemplateService.IsCreatedByUser(v.Definition))
                .Count();

            var variablesInGeneratedReport = _variableConfigurationRepositorySeeding.GetAll().ToArray();
            var generatedMetricsCount = _metricConfigurationRepositorySeeding.GetAll().Count();
            var generatedPartsCount = _partsRepositorySeeding.GetParts().Count();
            var templatePartsCount = reportTemplate.ReportTemplateParts.Count();

            AverageConfiguration savedAverage = null;
            if (reportTemplate.AverageConfiguration != null)
            {
                savedAverage = _averageConfigurationRepositorySeeding.GetAll()
                    .Where(s => s.AverageId == AverageId)
                    .SingleOrDefault();
            }

            Assert.Multiple(() =>
            {
                // Report level settings
                Assert.That(generatedReport.ReportType, Is.EqualTo(originalReport.ReportType), $"ReportType was {generatedReport.ReportType} but should be {originalReport.ReportType}");
                Assert.That(generatedReport.Order, Is.EqualTo(originalReport.Order), $"Order was {generatedReport.Order} but should be {originalReport.Order}");
                Assert.That(generatedReport.DefaultFilters, Is.EqualTo(originalReport.DefaultFilters), $"DefaultFilters do not match between new report and saved report. Expected: {originalReport.DefaultFilters}, Actual: {generatedReport.DefaultFilters}");
                Assert.That(generatedReport.Waves, Is.EqualTo(originalReport.Waves), $"Waves do not match between new report and saved report. Expected: {originalReport.Waves}, Actual: {generatedReport.Waves}");
                Assert.That(generatedReport.Breaks, Is.EqualTo(originalReport.Breaks), $"Breaks do not match between new report and saved report. Expected: {originalReport.Breaks}, Actual: {generatedReport.Breaks}");
                Assert.That(generatedReport.IsShared, Is.EqualTo(originalReport.IsShared), $"IsShared was {generatedReport.IsShared} but should be {originalReport.IsShared}");
                Assert.That(generatedReport.IncludeCounts, Is.EqualTo(originalReport.IncludeCounts), $"IncludeCounts was {generatedReport.IncludeCounts} but should be {originalReport.IncludeCounts}");
                Assert.That(generatedReport.HideTotalColumn, Is.EqualTo(originalReport.HideTotalColumn), $"HideTotalColumn was {generatedReport.HideTotalColumn} but should be {originalReport.HideTotalColumn}");
                Assert.That(generatedReport.HideDataLabels, Is.EqualTo(originalReport.HideDataLabels), $"HideDataLabels was {generatedReport.HideDataLabels} but should be {originalReport.HideDataLabels}");
                Assert.That(generatedReport.ShowMultipleTablesAsSingle, Is.EqualTo(originalReport.ShowMultipleTablesAsSingle), $"HideTotalColumn was {generatedReport.ShowMultipleTablesAsSingle} but should be {originalReport.ShowMultipleTablesAsSingle}");
                Assert.That(generatedReport.HighlightSignificance, Is.EqualTo(originalReport.HighlightSignificance), $"HighlightSignificance was {generatedReport.HighlightSignificance} but should be {originalReport.HighlightSignificance}");
                Assert.That(generatedReport.HideEmptyRows, Is.EqualTo(originalReport.HideEmptyRows), $"HideEmptyRows was {generatedReport.HideEmptyRows} but should be {originalReport.HideEmptyRows}");
                Assert.That(generatedReport.HideEmptyColumns, Is.EqualTo(originalReport.HideEmptyColumns), $"HideEmptyColumns was {generatedReport.HideEmptyColumns} but should be {originalReport.HideEmptyColumns}");
                Assert.That(generatedReport.SinglePageExport, Is.EqualTo(originalReport.SinglePageExport), $"SinglePageExport was {generatedReport.SinglePageExport} but should be {originalReport.SinglePageExport}");
                Assert.That(generatedReport.DecimalPlaces, Is.EqualTo(originalReport.DecimalPlaces), $"DecimalPlaces was {generatedReport.DecimalPlaces} but should be {originalReport.DecimalPlaces}");
                Assert.That(generatedReport.SignificanceType, Is.EqualTo(originalReport.SignificanceType), $"SignificanceType was {generatedReport.SignificanceType} but should be {originalReport.SignificanceType}");
                Assert.That(generatedReport.DisplaySignificanceDifferences, Is.EqualTo(originalReport.DisplaySignificanceDifferences), $"DisplaySignificanceDifferences was {generatedReport.DisplaySignificanceDifferences} but should be {originalReport.DisplaySignificanceDifferences}");
                Assert.That(generatedReport.SigConfidenceLevel, Is.EqualTo(originalReport.SigConfidenceLevel), $"SigConfidenceLevel was {generatedReport.SigConfidenceLevel} but should be {originalReport.SigConfidenceLevel}");
                Assert.That(generatedReport.BaseTypeOverride, Is.EqualTo(originalReport.BaseTypeOverride), $"BaseTypeOverride was {generatedReport.BaseTypeOverride} but should be {originalReport.BaseTypeOverride}");
                Assert.That(generatedReport.CreatedByUserId, Is.EqualTo(_userContext.UserId), $"CreatedByUserId was {generatedReport.CreatedByUserId} but should be {_userContext.UserId}");
                Assert.That(generatedReport.LastModifiedByUser, Is.EqualTo(_userContext.UserId), $"LastModifiedByUser was {generatedReport.LastModifiedByUser} but should be {_userContext.UserId}");
                Assert.That(generatedReport.ProductShortCode, Is.EqualTo(_productContextSeeding.ShortCode), $"ProductShortCode was {generatedReport.ProductShortCode} but should be {_productContextSeeding.ShortCode}");
                Assert.That(generatedReport.SubProductId, Is.EqualTo(_productContextSeeding.SubProductId), $"SubProductId was {generatedReport.SubProductId} but should be {_productContextSeeding.SubProductId}");
                Assert.That(generatedReport.ModifiedGuid, Is.Not.Null.Or.Empty, "ModifiedGuid should not be null or empty");
                Assert.That(generatedReport.TemplateImportLog, Is.Not.Null, "Template log should not be empty");

                Assert.That(generatedReport.OverTimeConfig?.AverageId, Is.EqualTo(originalReport.OverTimeConfig?.AverageId), $"OverTimeConfig.AverageId was {generatedReport.OverTimeConfig?.AverageId} but should be {originalReport.OverTimeConfig?.AverageId}");
                Assert.That(generatedReport.OverTimeConfig?.SavedRanges, Is.EqualTo(originalReport.OverTimeConfig?.SavedRanges), $"OverTimeConfig.SavedRanges do not match. Expected: {originalReport.OverTimeConfig?.SavedRanges}, Actual: {generatedReport.OverTimeConfig?.SavedRanges}");
                Assert.That(generatedReport.OverTimeConfig?.Range, Is.EqualTo(originalReport.OverTimeConfig?.Range), $"OverTimeConfig.Range do not match. Expected: {originalReport.OverTimeConfig?.Range}, Actual: {generatedReport.OverTimeConfig?.Range}");
                Assert.That(generatedReport.LowSampleThreshold, Is.EqualTo(originalReport.LowSampleThreshold), $"Low sample threshold expected {originalReport.LowSampleThreshold} but was {generatedReport.LowSampleThreshold}");

                if (originalReport.OverTimeConfig != null)
                {
                    Assert.That(savedAverage, Is.Not.Null, "Saved average should not be null when OverTimeConfig is present");
                }

                if (reportTemplate.BaseVariable != null)
                {
                    Assert.That(generatedReport.BaseVariableId, Is.Not.Null, "Base variable should not be null when BaseVariableId is set");
                }
                else
                {
                    Assert.That(generatedReport.BaseVariableId, Is.Null, "Base variable should be null when BaseVariableId is not set");
                }

                // Assert variables
                const int variablesWithoutBackingField = 1;
                Assert.That(variablesInGeneratedReport.Length, Is.EqualTo(originalUserDefinedVariables - variablesWithoutBackingField),
                    $"Variable repository should have the same number of user defined variables as the original. Expected: {originalUserDefinedVariables}, Actual: {variablesInGeneratedReport.Length}");

                // Assert metrics
                Assert.That(generatedMetricsCount, Is.EqualTo(originalUserDefinedVariables - variablesWithoutBackingField),
                    $"Metric repository should have one metric per imported variable. Expected: {originalUserDefinedVariables}, Actual: {generatedMetricsCount}");

                // Assert parts
                Assert.That(generatedPartsCount, Is.EqualTo(templatePartsCount), $"Expected {templatePartsCount} parts in report but generated {generatedPartsCount}");

                //Assert template import log
                Assert.That(generatedReport.TemplateImportLog, Is.Not.Null, "Template import log should not be null");
                var missingFieldError = generatedReport.TemplateImportLog.Logs.FirstOrDefault(log => 
                    log.EventType == EventType.Variable && log.Severity == Severity.Error && log.Message.Contains("Unable to find field VariableThatDoesNotExist referenced in variable variable that is based on a non existent field, it will not be included in the report."));
                Assert.That(missingFieldError, Is.Not.Null, "There should be missing field errors in the template import log");
            });
        }

        private void PopulateFields()
        {
            _userContext = Substitute.For<IUserContext>();
            _userContext.UserId.Returns(PrimaryUserId);
            _userContext.AuthCompany.Returns(AuthCompany);

            _responseFieldManager = Substitute.For<IResponseFieldManager>();
            _responseFieldManager
                .TryGet(Arg.Any<string>(), out Arg.Any<ResponseFieldDescriptor>())
                .Returns(callInfo =>
                {
                    var fieldName = callInfo.Arg<string>();
                    if (fieldName == "PositiveBuzz" || fieldName == "1")
                    {
                        callInfo[1] = new ResponseFieldDescriptor(fieldName);
                        return true;
                    }
                    callInfo[1] = null;
                    return false;
                });

            _responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();

            _entityRepository = Substitute.For<IEntityRepository>();
            _entityRepository.GetSubsetUnionedInstanceIdsOf(Arg.Any<string>()).Returns(x => new HashSet<int> { 1, 2, 3, 4, 5 });

            _fieldExpressionParser = Substitute.For<IFieldExpressionParser>();
            _fieldExpressionParser.ParseUserNumericExpressionOrNull(Arg.Any<string>()).Returns(_parsedVariable);

            _baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            _fakeVariableFactory = Substitute.For<IVariableFactory>();
            _logger = Substitute.For<ILogger<IMetricConfigurationRepository>>();

            _reportTemplateRepository = new ReportTemplateRepository(_testMetadataContextFactory, _userContext);
        }

        private void PopulateTemplateServiceForSeedingNewReport()
        {
            var savedReportRepository = new SavedReportRepository(_productContextSeeding, _testMetadataContextFactory);
            _variableConfigurationRepositorySeeding = new VariableConfigurationRepository(_testMetadataContextFactory, _productContextSeeding);

            _averageConfigurationRepositorySeeding = new AverageConfigurationRepository(_testMetadataContextFactory, _productContextSeeding);

            var measureFactory = new MetricFactory(_responseFieldManager,
                _fieldExpressionParser,
                Substitute.For<SubsetRepository>(),
                _variableConfigurationRepositorySeeding,
                _fakeVariableFactory,
                _baseExpressionGenerator);

            _metricConfigurationRepositorySeeding = new MetricConfigurationRepositorySql(_testMetadataContextFactory,
                _productContextSeeding,
                measureFactory,
                _logger);

            _partsRepositorySeeding = new PartsRepositorySql(_productContextSeeding, _testMetadataContextFactory);
            var subsetRepositor = new SubsetRepository();
            var panesRepository = new PanesRepositorySql(_productContextSeeding, _testMetadataContextFactory, _partsRepositorySeeding, subsetRepositor);
            var pagesRepository = new PagesRepositorySql(_productContextSeeding, _testMetadataContextFactory, panesRepository);

            var savedReportService = new SavedReportService(savedReportRepository,
                _userContext,
                _productContextSeeding,
                pagesRepository,
                panesRepository,
                _partsRepositorySeeding,
                Substitute.For<ISavedBreaksRepository>(),
                _metricConfigurationRepositorySeeding,
                _variableConfigurationRepositorySeeding,
                Substitute.For<IWeightingPlanService>(),
                Substitute.For<IMeasureRepository>(),
                Substitute.For<IAverageDescriptorRepository>(),
                Substitute.For<IRequestAdapter>(),
                Substitute.For<IEntityRepository>());

            var variableValidator = new VariableValidator(_fieldExpressionParser,
                _variableConfigurationRepositorySeeding,
                _entityRepository,
                _responseEntityTypeRepository,
                _metricConfigurationRepositorySeeding,
                _responseFieldManager);

            var variableConfigurationFactory = new VariableConfigurationFactory(_fieldExpressionParser,
                _variableConfigurationRepositorySeeding,
                _responseEntityTypeRepository,
                _productContextSeeding,
                _metricConfigurationRepositorySeeding,
                _responseFieldManager,
                variableValidator);

            var metricConfigurationFactory = new MetricConfigurationFactory(_baseExpressionGenerator);
            var variableManager = new VariableManager(_variableConfigurationRepositorySeeding,
                _productContextSeeding,
                _metricConfigurationRepositorySeeding,
                _partsRepositorySeeding,
                Substitute.For<IVariableFactory>(),
                variableConfigurationFactory,
                Substitute.For<ISavedBreaksRepository>(),
                Substitute.For<IVariableValidator>(),
                pagesRepository,
                panesRepository,
                savedReportRepository,
                _baseExpressionGenerator,
                _fieldExpressionParser,
                Substitute.For<IWeightingPlanRepository>(),
                savedReportService,
                _entityRepository,
                Substitute.For<IMeasureRepository>(),
                Substitute.For<IClaimRestrictedSubsetRepository>(),
                metricConfigurationFactory,
                Substitute.For<ILogger<VariableManager>>()
            );

            _templateServiceForSeedingTemplate = new ReportTemplateService(
                savedReportRepository,
                _userContext,
                pagesRepository,
                panesRepository,
                _partsRepositorySeeding,
                _metricConfigurationRepositorySeeding,
                _variableConfigurationRepositorySeeding,
                _reportTemplateRepository,
                variableManager,
                _productContextSeeding,
                savedReportService,
                _averageConfigurationRepositorySeeding,
                _responseFieldManager);
        }

        private void PopulateTemplateServiceForSavingTemplate()
        {
            var savedReportRepository = Substitute.For<ISavedReportRepository>();
            savedReportRepository.GetById(ChartReportId).Returns(x => _chartSavedReport);
            savedReportRepository.GetById(TableReportId).Returns(x => _tableSavedReport);

            InitializeAndPopulateVariableConfigurationRepository1();

            var metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            var metric1 = new MetricConfiguration() { Name = Metric1Name, VarCode = "1" };
            var metric2 = new MetricConfiguration() { Name = Metric2Name, VarCode = "2" };
            metricConfigurationRepository.Get("metric1").Returns(metric1);
            metricConfigurationRepository.Get("metric2").Returns(metric2);
            metricConfigurationRepository.GetAll().Returns([metric1, metric2]);

            var partsRepository = SubstituteAndPopulatePartsRepository();
            var panesRepository = SubstituteAndPopulatePanesRepository();
            var pagesRepository = SubstituteAndPopulatePagesRepository();
            var savedReportService = new SavedReportService(savedReportRepository,
                _userContext,
                _productContextSaving,
                pagesRepository,
                panesRepository,
                partsRepository,
                Substitute.For<ISavedBreaksRepository>(),
                metricConfigurationRepository,
                _variableConfigurationRepositorySaving,
                Substitute.For<IWeightingPlanService>(),
                Substitute.For<IMeasureRepository>(),
                Substitute.For<IAverageDescriptorRepository>(),
                Substitute.For<IRequestAdapter>(),
                Substitute.For<IEntityRepository>());

            var variableValidator = new VariableValidator(_fieldExpressionParser,
                _variableConfigurationRepositorySaving,
                _entityRepository,
                _responseEntityTypeRepository,
                metricConfigurationRepository,
                _responseFieldManager);

            var variableConfigurationFactory = new VariableConfigurationFactory(_fieldExpressionParser,
                _variableConfigurationRepositorySaving,
                _responseEntityTypeRepository,
                _productContextSaving,
                metricConfigurationRepository,
                _responseFieldManager,
                variableValidator);

            var variableManager = new VariableManager(_variableConfigurationRepositorySaving,
                _productContextSaving,
                metricConfigurationRepository,
                partsRepository,
                Substitute.For<IVariableFactory>(),
                variableConfigurationFactory,
                Substitute.For<ISavedBreaksRepository>(),
                Substitute.For<IVariableValidator>(),
                pagesRepository,
                panesRepository,
                savedReportRepository,
                _baseExpressionGenerator,
                _fieldExpressionParser,
                Substitute.For<IWeightingPlanRepository>(),
                savedReportService,
                _entityRepository,
                Substitute.For<IMeasureRepository>(),
                Substitute.For<IClaimRestrictedSubsetRepository>(),
                Substitute.For<IMetricConfigurationFactory>(),
                Substitute.For<ILogger<VariableManager>>()
                );

            _averageConfigurationRepositorySaving = new AverageConfigurationRepository(_testMetadataContextFactory, _productContextSaving);
            var average = new AverageConfiguration
            {
                Id = 1,
                AverageId = AverageId,
                DisplayName = "Test Average",
                Order = 1,
                Group = Array.Empty<string>(),
                TotalisationPeriodUnit = TotalisationPeriodUnit.Month,
                NumberOfPeriodsInAverage = 3,
                WeightingMethod = WeightingMethod.None,
                WeightAcross = WeightAcross.AllPeriods,
                AverageStrategy = AverageStrategy.OverAllPeriods,
                MakeUpTo = MakeUpTo.HalfYearEnd,
                WeightingPeriodUnit = WeightingPeriodUnit.SameAsTotalization,
                IncludeResponseIds = false,
                IsDefault = false,
                AllowPartial = true,
                Disabled = false,
                SubsetIds = Array.Empty<string>(),
                ProductShortCode = _productContextSaving.ShortCode,
                SubProductId = _productContextSaving.SubProductId
            };
            _averageConfigurationRepositorySaving.Create(average);

            _templateServiceForSavingTemplate = new ReportTemplateService(
                savedReportRepository,
                _userContext,
                pagesRepository,
                panesRepository,
                partsRepository,
                metricConfigurationRepository,
                _variableConfigurationRepositorySaving,
                _reportTemplateRepository,
                variableManager,
                _productContextSaving,
                savedReportService,
                _averageConfigurationRepositorySaving,
                _responseFieldManager);
        }

        private static IPartsRepository SubstituteAndPopulatePartsRepository()
        {
            var partsRepository = Substitute.For<IPartsRepository>();
            partsRepository.GetParts().Returns(new List<PartDescriptor>
            {
                new PartDescriptor
                {
                    Id = 1,
                    PaneId = ChartReportId.ToString(),
                    Spec1 = Metric1Name,
                    Spec2 = "1",
                    PartType = PartType.ReportsCardFunnel
                },
                new PartDescriptor
                {
                    Id = 2,
                    PaneId = TableReportId.ToString(),
                    Spec1 = Metric1Name,
                    Spec2 = "1",
                    PartType = PartType.ReportsTable
                },
                new PartDescriptor
                {
                    Id = 3,
                    PaneId = ChartReportId.ToString(),
                    Spec1 = Metric2Name,
                    Spec2 = "2",
                    PartType = PartType.ReportsCardHeatmapImage
                },
                new PartDescriptor
                {
                    Id = 4,
                    PaneId = TableReportId.ToString(),
                    Spec1 = Metric2Name,
                    Spec2 = "2",
                    PartType = PartType.ReportsCardChart
                }
            });
            return partsRepository;
        }

        private static IPanesRepository SubstituteAndPopulatePanesRepository()
        {
            var panesRepository = Substitute.For<IPanesRepository>();
            panesRepository.GetPanes().Returns(new List<PaneDescriptor>
            {
                new PaneDescriptor
                {
                    Id = ChartReportId.ToString(),
                    PageName = "Chart",
                },
                new PaneDescriptor
                {
                    Id = TableReportId.ToString(),
                    PageName = "Table",
                }
            });
            return panesRepository;
        }

        private static IPagesRepository SubstituteAndPopulatePagesRepository()
        {
            var pagesRepository = Substitute.For<IPagesRepository>();
            pagesRepository.GetPages().Returns(new List<PageDescriptor>
            {
                new PageDescriptor
                {
                    Id = ChartReportId,
                    Name = "Chart",
                },
                new PageDescriptor
                {
                    Id = TableReportId,
                    Name = "Table",
                }
            });
            return pagesRepository;
        }

        private void InitializeAndPopulateVariableConfigurationRepository1()
        {
            PopulateDefinitionLookup();
            var getAllList = new List<VariableConfiguration>()
            {
                CreateVariableConfiguration(1, "1", "groupedVariableDefinition"),
                CreateVariableConfiguration(2, "2", "fieldExpressionVariable"),
                CreateVariableConfiguration(3, "3", "questionVariableDefinition"),
                CreateVariableConfiguration(4, "4", "field generated definition"),
                CreateVariableConfiguration(5, "5", "base variable"),
                CreateVariableConfiguration(6, "6", "variable that is based on a non existent field"),
            };
            _variableConfigurationRepositorySaving = Substitute.For<IVariableConfigurationRepository>();
            _variableConfigurationRepositorySaving.GetAll().Returns(getAllList);
        }

        private void GenerateReports()
        {
            _chartSavedReport = new SavedReport
            {
                Id = ChartReportId,
                SubProductId = _productContextSaving.SubProductId,
                ProductShortCode = _productContextSaving.ShortCode,
                IsShared = true,
                CreatedByUserId = _userContext.UserId,
                ReportPageId = ChartReportId,
                Order = ReportOrder.ScriptOrderAsc,
                ReportType = ReportType.Chart,
                Waves = null,
                ModifiedDate = DateTime.Now,
                ModifiedGuid = "12345",
                OverTimeConfig = new ReportOverTimeConfiguration
                {
                    Range = "test",
                    AverageId = AverageId
                },
                DecimalPlaces = 0,
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
                SigConfidenceLevel = SigConfidenceLevel.NinetyNine,
                BaseTypeOverride = BaseDefinitionType.SawThisQuestion,
                DefaultFilters = new List<DefaultReportFilter>(),
                LastModifiedByUser = _userContext.UserId,
                LowSampleThreshold = 14
            };

            _tableSavedReport = new SavedReport
            {
                Id = TableReportId,
                SubProductId = _productContextSaving.SubProductId,
                ProductShortCode = _productContextSaving.ShortCode,
                IsShared = false,
                CreatedByUserId = _userContext.UserId,
                ReportPageId = 1,
                Order = ReportOrder.ScriptOrderAsc,
                ReportType = ReportType.Chart,
                Waves = null,
                ModifiedDate = DateTime.Now,
                ModifiedGuid = "12345",
                OverTimeConfig = null,
                DecimalPlaces = 0,
                Breaks = new List<CrossMeasure>(),
                SinglePageExport = true,
                IncludeCounts = false,
                HighlightLowSample = false,
                HighlightSignificance = true,
                IsDataWeighted = false,
                HideEmptyRows = true,
                HideEmptyColumns = true,
                HideTotalColumn = true,
                HideDataLabels = true,
                ShowMultipleTablesAsSingle = true,
                SignificanceType = CrosstabSignificanceType.CompareToTotal,
                DisplaySignificanceDifferences = DisplaySignificanceDifferences.ShowUp,
                SigConfidenceLevel = SigConfidenceLevel.NinetyFive,
                BaseTypeOverride = BaseDefinitionType.AllRespondents,
                DefaultFilters = new List<DefaultReportFilter>(),
                LastModifiedByUser = _userContext.UserId,
                BaseVariableId = 5,
                LowSampleThreshold = 46
            };
        }

        private void GetReportModel(string templateDisplayName, string templateDescription, out SavedReport savedReport, out ReportTemplateModel model)
        {
            savedReport = templateDisplayName.Contains("Chart") ? _chartSavedReport : _tableSavedReport;
            model = new ReportTemplateModel
            {
                SavedReportId = savedReport.Id,
                TemplateDisplayName = templateDisplayName,
                TemplateDescription = templateDescription
            };
        }

        private VariableConfiguration CreateVariableConfiguration(int id, string identifier, string displayName)
        {
            return new VariableConfiguration
            {
                Id = id,
                ProductShortCode = _productContextSaving.ShortCode,
                SubProductId = _productContextSaving.SubProductId,
                Identifier = identifier,
                DisplayName = displayName,
                Definition = _definitionLookup[id]
            };
        }

        private void PopulateDefinitionLookup()
        {
            const string originalFieldName = "PositiveBuzz";
            _definitionLookup = new Dictionary<int, VariableDefinition>
            {
                { 1, new GroupedVariableDefinition
                    {
                        ToEntityTypeName = "PositiveBuzzBrandCategory",
                        Groups = new List<VariableGrouping>
                        {
                            new()
                            {
                                ToEntityInstanceName = "Car companies",
                                ToEntityInstanceId = 1,
                                Component = new InstanceListVariableComponent()
                                {
                                    InstanceIds = new List<int> {1, 4},
                                    FromVariableIdentifier = originalFieldName,
                                    FromEntityTypeName = "brand"
                                }
                            },
                            new()
                            {
                                ToEntityInstanceName = "Toilet paper companies",
                                ToEntityInstanceId = 2,
                                Component = new InstanceListVariableComponent()
                                {
                                    InstanceIds = new List<int> {2, 3},
                                    FromVariableIdentifier = originalFieldName,
                                    FromEntityTypeName = "brand"
                                }
                            }
                        }
                    }},
                { 2, new FieldExpressionVariableDefinition { Expression = $"field('{originalFieldName}')" } },
                { 3, new GroupedVariableDefinition
                    {
                        ToEntityTypeName = AutoGenerationConstants.NumericIdentifier + "something else",
                        ToEntityTypeDisplayNamePlural = AutoGenerationConstants.NumericIdentifier + "something else Plural",
                        Groups = new List<VariableGrouping>
                        {
                            new VariableGrouping
                            {
                                ToEntityInstanceName = "Auto generated group",
                                ToEntityInstanceId = 1,
                                Component = new InclusiveRangeVariableComponent
                                {
                                    FromVariableIdentifier = "1",
                                    Max = 20,
                                    Min = 1,
                                    ExactValues = new int[] { 1, 2, 3, 4, 5 },
                                    Operator = VariableRangeComparisonOperator.Between,
                                    ResultEntityTypeNames = new List<string> { "AutoGeneratedEntityType" }
                                }
                            }
                        }
                    }
                },
                { 4, new QuestionVariableDefinition { QuestionVarCode = "1" } },
                { 5, new BaseGroupedVariableDefinition
                    {
                        ToEntityTypeName = "Base variable",
                        ToEntityTypeDisplayNamePlural = "Base variable Plural",
                        Groups = new List<VariableGrouping>
                        {
                            new VariableGrouping
                            {
                                ToEntityInstanceName = "Auto generated group",
                                ToEntityInstanceId = 1,
                                Component = new InclusiveRangeVariableComponent
                                {
                                    FromVariableIdentifier = "1",
                                    Max = 20,
                                    Min = 1,
                                    ExactValues = new int[] { 1, 2, 3, 4, 5 },
                                    Operator = VariableRangeComparisonOperator.Between,
                                    ResultEntityTypeNames = new List<string> { "AutoGeneratedEntityType" }
                                }
                            }
                        }
                    }
                },
                { 6, new GroupedVariableDefinition
                    {
                        ToEntityTypeName = "DefinitionWithoutABackingField",
                        Groups = new List<VariableGrouping>
                        {
                            new()
                            {
                                ToEntityInstanceName = "Some instance",
                                ToEntityInstanceId = 1,
                                Component = new InstanceListVariableComponent()
                                {
                                    InstanceIds = new List<int> {1, 4},
                                    FromVariableIdentifier = "VariableThatDoesNotExist",
                                    FromEntityTypeName = "brand"
                                }
                            },
                        }
                    }
                },
            };
        }
    }
}
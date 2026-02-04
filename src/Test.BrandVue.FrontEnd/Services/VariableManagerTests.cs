using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Page;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using BrandVue.Variable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class VariableManagerTests
    {
        private IDbContextFactory<MetaDataContext> _dbContextFactory;
        private VariableConfigurationFactory _variableConfigurationFactory;
        private VariableManager _variableManager;
        private IPartsRepository _partsRepository;
        private IMetricConfigurationRepository _metricConfigurationRepository;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private MetricRepository _metricRepository;
        private ResponseFieldManager _responseFieldManager;
        private IVariableFactory _fakeVariableFactory;
        private ILoadableEntityInstanceRepository _entityRepository;
        private IProductContext _productContext;
        private ILogger<IMetricConfigurationRepository> _logger;
        private EntityTypeRepository _entityTypeRepository;
        private SubsetRepository _subsetRepository;
        private IPagesRepository _pagesRepository;
        private IPanesRepository _panesRepository;
        private ISavedReportRepository _savedReportRepository;
        private IWeightingPlanRepository _weightingPlanRepository;
        private IUserContext _userContext;
        private IClaimRestrictedSubsetRepository _claimRestrictedSubsetRepository;
        const string VariableName = "Vanilla variable";
        const string VariableIdentifier = "VanillaVariable";
        const string ToEntityTypeName = "ToEntityTypeName";
        const string FromVariableIdentifier = "OriginalFieldName";
        const string FromEntityTypeName = "brand";
        const string MetricName_MultiEntity1 = "Household composition_ME";
        const string MetricField_MultiEntity1 = "Metric_Field_MEa";
        const string MetricField_MultiEntity2 = "Metric_Field_ME2a";
        const string MetricField_BrandProduct = "Metric_Field_Brand_Product";
        const string SubProductId = "57";
        const int PartId = 1;
        const string PartPosition = "42";
        const string MetricName = "Household composition";
        const string MetricField = "Metric_Field";
        private const string UserId = "testuser123";
        private const string AuthCompany = "authcomp888";

        [OneTimeSetUp]
        public void SetupVariableManagerTests()
        {
            _entityRepository = CreateAndPopulateEntityRepository();
            _productContext = new ProductContext("test", SubProductId, true, "test survey", null);
            _logger = Substitute.For<ILogger<IMetricConfigurationRepository>>();
            _entityTypeRepository = new TestEntityTypeRepository();
            _subsetRepository = new SubsetRepository();
            var allSubset = new Subset()
            {
                Alias = "All",
                Id = "All"
            };
            _subsetRepository.Add(allSubset);
            _userContext = Substitute.For<IUserContext>();
            _userContext.UserId.Returns(UserId);
            _userContext.AuthCompany.Returns(AuthCompany);
            _responseFieldManager = new ResponseFieldManager(_entityTypeRepository);
            _fakeVariableFactory = Substitute.For<IVariableFactory>();
            _partsRepository = new PartsRepositorySql(_productContext, _dbContextFactory);
            _panesRepository = new PanesRepositorySql(_productContext, _dbContextFactory, _partsRepository, _subsetRepository);
            _pagesRepository = new PagesRepositorySql(_productContext, _dbContextFactory, _panesRepository);
        }

        [SetUp]
        public void InitialiseData()
        {
            _dbContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            var variableConfigurationRepository = new VariableConfigurationRepository(_dbContextFactory, _productContext);
            _metricRepository = CreateAndPopulateMeasureRepository(_responseFieldManager);
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityRepository, _entityTypeRepository);

            var testMetadataContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            _variableConfigurationRepository = new InMemoryRepositoryUpdatingVariableConfigurationRepository(variableConfigurationRepository,
                new VariableEntityLoader(_entityTypeRepository, _entityRepository, Substitute.For<ILoadableEntitySetRepository>()),
                fieldExpressionParser);

            var baseExpressionGenerator = new BaseExpressionGenerator(_metricConfigurationRepository, _responseFieldManager, _variableConfigurationRepository, fieldExpressionParser);
            var measureFactory = new MetricFactory(_responseFieldManager, fieldExpressionParser, _subsetRepository, _variableConfigurationRepository, _fakeVariableFactory, baseExpressionGenerator);
            _metricConfigurationRepository = new MetricConfigurationRepositorySql(_dbContextFactory, _productContext, measureFactory, _logger);
            var variableValidator = new VariableValidator(fieldExpressionParser, _variableConfigurationRepository, _entityRepository, _entityTypeRepository,
                _metricConfigurationRepository, _responseFieldManager);
            _partsRepository = new PartsRepositorySql(_productContext, _dbContextFactory);
            _panesRepository = new PanesRepositorySql(_productContext, _dbContextFactory, _partsRepository, _subsetRepository);
            _pagesRepository = new PagesRepositorySql(_productContext, _dbContextFactory, _panesRepository);
            _partsRepository.CreatePart(new PartDescriptor()
            {
                Id = PartId,
                Spec1 = MetricName,
                Spec2 = PartPosition
            });
            var savedBreaksRepository = Substitute.For<ISavedBreaksRepository>();
            _savedReportRepository = new SavedReportRepository(_productContext, _dbContextFactory);

            _variableConfigurationFactory = new VariableConfigurationFactory(
                fieldExpressionParser,
                _variableConfigurationRepository,
                _entityTypeRepository,
                _productContext,
                _metricConfigurationRepository,
                _responseFieldManager,
                variableValidator
            );

            _weightingPlanRepository = new WeightingPlanRepository(_dbContextFactory);

            var averageDescriptorRepository = Substitute.For<IAverageDescriptorRepository>();
            var requestAdapter = Substitute.For<IRequestAdapter>();
            var weightingPlanService = Substitute.For<IWeightingPlanService>();

            var savedReportService = new SavedReportService(_savedReportRepository,
                _userContext,
                _productContext,
                _pagesRepository,
                _panesRepository,
                _partsRepository,
                savedBreaksRepository,
                _metricConfigurationRepository,
                _variableConfigurationRepository,
                weightingPlanService,
                _metricRepository,
                averageDescriptorRepository,
                requestAdapter,
                _entityRepository);

            _claimRestrictedSubsetRepository = Substitute.For<IClaimRestrictedSubsetRepository>();
            _variableManager = new VariableManager(_variableConfigurationRepository,
                _productContext,
                _metricConfigurationRepository,
                _partsRepository,
                new VariableFactory(fieldExpressionParser, _entityTypeRepository),
                _variableConfigurationFactory,
                savedBreaksRepository,
                variableValidator,
                _pagesRepository,
                _panesRepository,
                _savedReportRepository,
                baseExpressionGenerator,
                fieldExpressionParser,
                _weightingPlanRepository,
                savedReportService,
                _entityRepository,
                _metricRepository,
                _claimRestrictedSubsetRepository,
                new MetricConfigurationFactory(baseExpressionGenerator),
                Substitute.For<ILogger<VariableManager>>()
            );
        }

        [Test]
        public void ShouldCreateRelatedVariableAndMetricForSingleEntity()
        {
            var report = DoCreateReport();
            var createVariableResultModel = DoCreateVariable(report);
            var createdVariable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId ?? throw new ArgumentException("Metric does not have variableConfigurationId"));

            Assert.Multiple(() =>
            {
                Assert.That(createVariableResultModel.Name, Is.EqualTo(VariableIdentifier), "Incorrect name for created metric");
                Assert.That(createVariableResultModel.BaseVals, Is.EqualTo("1>2"), "Metric has incorrect baseVals");
                Assert.That(createVariableResultModel.TrueVals, Is.EqualTo("1>2"), "Metric has incorrect trueVals");
                Assert.That(createVariableResultModel.VarCode, Is.EqualTo(VariableIdentifier), "Metric varcode does not match variable identifier");

                //Only assert we can get back the correct variable, specific configuration tests exist in VariableFactoryTests
                Assert.That(createdVariable.Identifier, Is.EqualTo(VariableIdentifier), "Variable has incorrect identifier");
            });
        }

        [Test]
        public void ShouldCreateRelatedVariableAndMetricForMultiEntity()
        {
            var report = DoCreateReport();
            var createVariableResultModel = DoCreateVariableForMultiEntity(report);
            var createdVariable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId ?? throw new ArgumentException("Metric does not have variableConfigurationId"));
            Assert.That(createdVariable.Identifier, Is.EqualTo(VariableIdentifier), "Variable has incorrect identifier");
        }

        [Test]
        public void ShouldAddGroupToVariable()
        {
            var report = DoCreateReport();
            var createVariableResultModel = DoCreateVariable(report);
            var variable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value);
            var groupedDefinition = (GroupedVariableDefinition)variable.Definition;

            var newComponentId = 3;
            var newComopnentToEntityInstanceName = "Added group";
            var instanceIds = new List<int> { 1, 2, 4 };

            groupedDefinition.Groups.Add(new VariableGrouping
            {
                ToEntityInstanceId = newComponentId,
                ToEntityInstanceName = newComopnentToEntityInstanceName,
                Component = new InstanceListVariableComponent()
                {
                    InstanceIds = instanceIds,
                    FromVariableIdentifier = FromVariableIdentifier,
                    FromEntityTypeName = FromEntityTypeName
                }
            }
            );

            _variableManager.UpdateVariable(createVariableResultModel.VariableConfigurationId.Value, variable.DisplayName, groupedDefinition, CalculationType.YesNo);

            var updatedMetric = _metricConfigurationRepository.Get(createVariableResultModel.Name);
            variable = _variableConfigurationRepository.Get(updatedMetric.VariableConfigurationId.Value);

            Assert.Multiple(() =>
            {
                var definition = variable.Definition as GroupedVariableDefinition;
                Assert.That(definition.Groups.Count(), Is.EqualTo(3), "Definition should contain 3 groups");

                Assert.That(updatedMetric.BaseVals, Is.EqualTo("1>3"), "Metric has incorrect baseVals");
                Assert.That(updatedMetric.TrueVals, Is.EqualTo("1>3"), "Metric has incorrect trueVals");
            });
        }

        [Test]
        public void ShouldNotBeAbleToDeleteVariableReferencedByAReport()
        {
            var report = DoCreateReport();
            var createVariableResultModel = DoCreateVariable(report);

            var pane = _panesRepository.GetPanes().FirstOrDefault(pane => pane.PageName == report.ReportPage.Name);

            var existingPartCount = _partsRepository.GetParts().Count(p => p.PaneId == pane.Id);

            var newPart = new PartDescriptor
            {
                PaneId = pane.Id,
                PartType = PartType.ReportsTable,
                Spec1 = createVariableResultModel.Name,
                Spec2 = existingPartCount.ToString(),
                DefaultSplitBy = "",
                HelpText = createVariableResultModel.HelpText,
            };
            _partsRepository.CreatePart(newPart);

            Assert.Throws<BadRequestException>(() => _variableManager.DeleteVariableById(createVariableResultModel.VariableConfigurationId.Value), "Should not be able to delete");
        }

        [Test]
        public void ShouldDeleteVariableAndMetric()
        {
            var report = DoCreateReport();
            var createVariableResultModel = DoCreateVariable(report);

            //Delete the referencing part so we can delete the metric/variable
            _partsRepository.DeletePart(2);

            _variableManager.DeleteVariableById(createVariableResultModel.VariableConfigurationId.Value);

            Assert.That(_metricConfigurationRepository.Get(createVariableResultModel.Name), Is.Null, "Metric should have been deleted");
            Assert.That(_variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value), Is.Null, "Variable should have been deleted");
        }

        [Test]
        public void ShouldCreateFieldExpressionVariable()
        {
            var fieldExpression = "1";
            var createVariableResultModel = CreateFieldExpressionVariable("fieldVariable", fieldExpression);
            var createdVariable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value);
            Assert.That(createdVariable.Definition is FieldExpressionVariableDefinition, Is.True);
            var fieldExpressionDefinition = (FieldExpressionVariableDefinition)createdVariable.Definition;
            Assert.That(fieldExpressionDefinition.Expression, Is.EqualTo(fieldExpression));
        }

        [Test]
        public void ShouldUpdateFieldExpressionVariable()
        {
            var oldName = "oldName";
            var newName = "newName";
            var originalExpression = "1";
            var newExpression = "2";
            var createVariableResultModel = CreateFieldExpressionVariable(oldName, originalExpression);
            var updatedDefinition = new FieldExpressionVariableDefinition
            {
                Expression = newExpression
            };
            _variableManager.UpdateVariable(createVariableResultModel.VariableConfigurationId.Value, newName, updatedDefinition, CalculationType.YesNo);
            var variable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value);
            Assert.That(variable.DisplayName, Is.EqualTo(newName));
            Assert.That(variable.Definition is FieldExpressionVariableDefinition fieldExpressionDefinition && fieldExpressionDefinition.Expression == newExpression, Is.True);
        }

        [Test]
        public void ShouldNotCreateFieldExpressionVariableIfInvalidPython()
        {
            string badExpression1 = null;
            string badExpression2 = "field_name_that_doesnt_exist && age = 30";
            Assert.Throws<BadRequestException>(() => CreateFieldExpressionVariable("fieldvar1", badExpression1));
            Assert.Throws<BadRequestException>(() => CreateFieldExpressionVariable("fieldvar2", badExpression2));
        }

        [Test]
        public void ShouldNotUpdateFieldExpressionVariableIfInvalidPython()
        {
            var originalExpression = "1";
            var badExpression1 = new FieldExpressionVariableDefinition
            {
                Expression = null
            };
            var badExpression2 = new FieldExpressionVariableDefinition
            {
                Expression = "field_name_that_doesnt_exist && age = 30"
            };
            var createVariableResultModel = CreateFieldExpressionVariable("fieldVariable", originalExpression);
            Assert.Throws<BadRequestException>(() => _variableManager.UpdateVariable(createVariableResultModel.VariableConfigurationId.Value, createVariableResultModel.VarCode, badExpression1, CalculationType.YesNo));
            Assert.Throws<BadRequestException>(() => _variableManager.UpdateVariable(createVariableResultModel.VariableConfigurationId.Value, createVariableResultModel.VarCode, badExpression2, CalculationType.YesNo));
            var variable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value);
            Assert.That(variable.Definition is FieldExpressionVariableDefinition fieldExpressionDefinition && fieldExpressionDefinition.Expression == originalExpression, Is.True);
        }

        [Test]
        public void UpdatingVariableDisplayNameShouldUpdateMetricDisplayNameButNotVarCodeAndNotDisplayName()
        {
            var newName = "updated variable name";
            var report = DoCreateReport();
            var createVariableResultModel = DoCreateVariable(report);
            var variable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value);
            _variableManager.UpdateVariable(variable.Id, newName, variable.Definition, CalculationType.YesNo);
            variable = _variableConfigurationRepository.Get(createVariableResultModel.VariableConfigurationId.Value);
            var metric = _metricConfigurationRepository.Get(createVariableResultModel.Name);
            Assert.That(variable.DisplayName, Is.EqualTo(newName));
            Assert.That(metric.VarCode, Is.Not.EqualTo(newName));
            Assert.That(metric.HelpText, Is.Not.EqualTo(newName));
        }

        [Test]
        public void ShouldCreateBaseFieldExpressionVariables()
        {
            var model = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = new BaseFieldExpressionVariableDefinition
                {
                    Expression = "1"
                }
            };
            var variableId = _variableManager.CreateBaseVariable(model);
            var variable = _variableConfigurationRepository.Get(variableId);
            Assert.That(variable.Definition is BaseFieldExpressionVariableDefinition, Is.True);
        }

        [Test]
        public void ShouldCreateBaseGroupedVariables()
        {
            var report = DoCreateReport();
            var baseVariableId = DoCreateBaseVariable(report);
            var variable = _variableConfigurationRepository.Get(baseVariableId);
            Assert.That(variable.Definition is BaseGroupedVariableDefinition, Is.True);
        }

        [Test]
        public void BaseVariablesShouldNotBeCreatedIfIncorrectType()
        {
            var fieldExpressionDefinition = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = new FieldExpressionVariableDefinition { Expression = "1" }
            };
            var groupedDefinition = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = CreateDefinition(FromVariableIdentifier, FromEntityTypeName)
            };
            Assert.Throws<BadRequestException>(() => _variableManager.CreateBaseVariable(fieldExpressionDefinition));
            Assert.Throws<BadRequestException>(() => _variableManager.CreateBaseVariable(groupedDefinition));
        }

        [Test]
        public void BaseVariablesShouldNotBeUpdatedToNonBaseVariable()
        {
            var fieldExpressionDefinition = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = new BaseFieldExpressionVariableDefinition { Expression = "1" }
            };
            var groupedDefinition = CreateDefinition(FromVariableIdentifier, FromEntityTypeName);
            var variableId = _variableManager.CreateBaseVariable(fieldExpressionDefinition);
            Assert.Throws<BadRequestException>(() => _variableManager.UpdateVariable(variableId, VariableName, groupedDefinition, CalculationType.YesNo));
        }

        [Test]
        public void BaseFieldExpressionVariablesShouldHaveResultEntityTypes()
        {
            var brandExpression = new BaseFieldExpressionVariableDefinition
            {
                Expression = $"any(response.{MetricField_BrandProduct}(brand=result.brand))"
            };
            var productExpression = new BaseFieldExpressionVariableDefinition
            {
                Expression = $"any(response.{MetricField_BrandProduct}(product=result.product))"
            };
            var model = new VariableConfigurationCreateModel()
            {
                Name = VariableName,
                Definition = brandExpression,
            };
            var variableId = _variableManager.CreateBaseVariable(model);
            var variable = _variableConfigurationRepository.Get(variableId);
            Assert.That(((BaseFieldExpressionVariableDefinition)variable.Definition).ResultEntityTypeNames, Is.EqualTo(new[] { "brand" }));
            _variableManager.UpdateVariable(variableId, VariableName, productExpression, CalculationType.YesNo);
            variable = _variableConfigurationRepository.Get(variableId);
            Assert.That(((BaseFieldExpressionVariableDefinition)variable.Definition).ResultEntityTypeNames, Is.EqualTo(new[] { "product" }));
        }

        [Test]
        public void ShouldDeleteBaseVariables()
        {
            var model = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = new BaseFieldExpressionVariableDefinition { Expression = "1" }
            };
            var variableId = _variableManager.CreateBaseVariable(model);
            _variableManager.DeleteBaseVariableById(variableId);
            var variable = _variableConfigurationRepository.Get(variableId);
            Assert.That(variable, Is.Null);
        }

        [Test]
        public void ShouldNotDeleteBaseVariableIfAppliedToMetric()
        {
            var model = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = new BaseFieldExpressionVariableDefinition { Expression = "1" }
            };
            var variableId = _variableManager.CreateBaseVariable(model);
            var metric = new MetricConfiguration()
            {
                Name = "metric",
                Field = MetricField,
                TrueVals = "1",
                CalcType = "yn",
                VarCode = "Household composition",
                SubProductId = SubProductId,
                Subset = "All",
                BaseVariableConfigurationId = variableId,
                BaseVariableConfiguration = _variableConfigurationRepository.Get(variableId)
            };
            _metricConfigurationRepository.Create(metric);
            Assert.Throws<BadRequestException>(() => _variableManager.DeleteBaseVariableById(variableId));
        }

        [Test]
        public void ShouldNotDeleteBaseVariableIfAppliedToPart()
        {
            var model = new VariableConfigurationCreateModel
            {
                Name = VariableName,
                Definition = new BaseFieldExpressionVariableDefinition { Expression = "1" }
            };
            var variableId = _variableManager.CreateBaseVariable(model);
            using var dbContext = _dbContextFactory.CreateDbContext();
            dbContext.Pages.Add(new DbPage
            {
                Name = "pagename",
                DisplayName = "pagename",
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
            });
            dbContext.Panes.Add(new DbPane
            {
                PaneId = "paneid",
                PageName = "pagename",
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
            });
            dbContext.Parts.Add(new DbPart
            {
                PaneId = "paneid",
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                BaseExpressionOverride = new BaseExpressionDefinition
                {
                    BaseVariableId = variableId
                }
            });
            dbContext.SaveChanges();
            Assert.Throws<BadRequestException>(() => _variableManager.DeleteBaseVariableById(variableId));
        }

        [Test]
        public void ShouldReplaceBreaksInChartReport()
        {
            var report = DoCreateReport(ReportType.Chart);
            report.Breaks = new[]
            {
                new CrossMeasure { MeasureName = "test" }
            }.ToList();
            _savedReportRepository.Update(report);
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Breaks, selectedPart: null);
            report = _savedReportRepository.GetById(report.Id);
            Assert.That(report.Breaks.Count, Is.EqualTo(1));
            Assert.That(report.Breaks.Select(b => b.MeasureName), Is.EqualTo(new[] { createVariableResultModel.Name }));
        }

        [Test]
        public void ShouldReplaceBreakInChartReportForReportsCardStackedMultiChartPart()
        {
            var report = DoCreateReport(ReportType.Chart);
            using var dbContext = _dbContextFactory.CreateDbContext();
            var pane = dbContext.Panes.Single();
            var spec2 = "0";
            var part = new DbPart
            {
                PaneId = pane.PaneId,
                Spec2 = spec2,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                PartType = PartType.ReportsCardStackedMulti,
                Breaks = new[]
                {
                    new CrossMeasure { MeasureName = "test" }
                }
            };
            dbContext.Parts.Add(part);
            dbContext.SaveChanges();
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Breaks, selectedPart: spec2);
            var partDescriptor = _partsRepository.GetById(part.Id);
            Assert.That(partDescriptor.Breaks.Length, Is.EqualTo(1));
            Assert.That(partDescriptor.Breaks.Select(b => b.MeasureName), Is.EqualTo(new[] { createVariableResultModel.Name }));
            Assert.That(partDescriptor.MultiBreakSelectedEntityInstance, Is.Null);
        }

        [Test]
        public void ShouldAppendBreaksInChartReportPart()
        {
            DbPart part;
            MetricConfiguration createVariableResultModel;
            AppendVariableAsBreakToReport(out _, out part, out createVariableResultModel, ReportType.Chart);
            var partDescriptor = _partsRepository.GetById(part.Id);
            Assert.That(partDescriptor.Breaks.Length, Is.EqualTo(2));
            Assert.That(partDescriptor.Breaks.Select(b => b.MeasureName), Is.EqualTo(new[] { "test", createVariableResultModel.Name }));
            Assert.That(partDescriptor.MultiBreakSelectedEntityInstance, Is.EqualTo(1));
        }

        [Test]
        public void ShouldAppendBreaksInTableReport()
        {
            var report = DoCreateReport(ReportType.Table);
            report.Breaks = new[]
            {
                new CrossMeasure { MeasureName = "test" }
            }.ToList();
            _savedReportRepository.Update(report);
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Breaks, selectedPart: null);
            report = _savedReportRepository.GetById(report.Id);
            Assert.That(report.Breaks.Count, Is.EqualTo(2));
            Assert.That(report.Breaks.Select(b => b.MeasureName), Is.EqualTo(new[] { "test", createVariableResultModel.Name }));
        }

        [Test]
        public void ShouldAppendBreaksInTableReportPart()
        {
            MetaDataContext dbContext;
            DbPart part;
            MetricConfiguration createVariableResultModel;
            AppendVariableAsBreakToReport(out dbContext, out part, out createVariableResultModel);
            var partDescriptor = _partsRepository.GetById(part.Id);
            Assert.That(partDescriptor.Breaks.Length, Is.EqualTo(2));
            Assert.That(partDescriptor.Breaks.Select(b => b.MeasureName), Is.EqualTo(new[] { "test", createVariableResultModel.Name }));
        }

        private void AppendVariableAsBreakToReport(out MetaDataContext dbContext, out DbPart part, out MetricConfiguration createVariableResultModel, ReportType reportType = ReportType.Table)
        {
            var report = DoCreateReport(reportType);
            dbContext = _dbContextFactory.CreateDbContext();
            var pane = dbContext.Panes.Single();
            var spec2 = "0";
            part = new DbPart
            {
                PaneId = pane.PaneId,
                Spec1 = MetricName,
                Spec2 = spec2,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Breaks = new[]
                {
                    new CrossMeasure { MeasureName = "test" }
                }
            };
            dbContext.Parts.Add(part);
            dbContext.SaveChanges();
            createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Breaks, selectedPart: spec2);
        }

        [Test]
        public void ShouldAppendFiltersInReport()
        {
            var report = DoCreateReport(ReportType.Table);
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Filters);
            report = _savedReportRepository.GetById(report.Id);
            Assert.That(report.DefaultFilters.Count, Is.EqualTo(1));
            Assert.That(report.DefaultFilters.Single().MeasureName, Is.EqualTo(createVariableResultModel.Name));
        }

        [Test]
        public void ShouldDetectVariableUsedAsFilter()
        {
            var report = DoCreateReport(ReportType.Table);
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Filters);
            var warnings = _variableManager.CheckVariableIsInUse((int)createVariableResultModel.VariableConfigurationId);
            Assert.That(warnings.Single().ObjectThatReferencesVariable == ObjectThatReferencesVariable.Filter);
        }

        [Test]
        public void ShouldDetectVariableUsedAsBreakInTableReportPart()
        {
            MetricConfiguration createVariableResultModel;
            AppendVariableAsBreakToReport(out _, out _, out createVariableResultModel);
            var warnings = _variableManager.CheckVariableIsInUse((int)createVariableResultModel.VariableConfigurationId);
            Assert.That(warnings.Single().ObjectThatReferencesVariable == ObjectThatReferencesVariable.Break);
        }

        [Test]
        public void ShouldReplaceWavesInReport()
        {
            var report = DoCreateReport(ReportType.Chart);
            report.Waves = new ReportWaveConfiguration
            {
                WavesToShow = ReportWavesOptions.AllWaves,
                NumberOfRecentWaves = 10,
                Waves = new CrossMeasure { MeasureName = "test" }
            };
            _savedReportRepository.Update(report);
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Waves, selectedPart: null);
            report = _savedReportRepository.GetById(report.Id);
            Assert.That(report.Waves.Waves.MeasureName, Is.EqualTo(createVariableResultModel.Name));
        }

        [Test]
        public void ShouldDetectVariableUsedAsWaveInReport()
        {
            var report = DoCreateReport(ReportType.Chart);
            report.Waves = new ReportWaveConfiguration
            {
                WavesToShow = ReportWavesOptions.AllWaves,
                NumberOfRecentWaves = 10,
                Waves = new CrossMeasure { MeasureName = "test" }
            };
            _savedReportRepository.Update(report);
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Waves, selectedPart: null);
            var warnings = _variableManager.CheckVariableIsInUse((int)createVariableResultModel.VariableConfigurationId);
            Assert.That(warnings.Single().ObjectThatReferencesVariable == ObjectThatReferencesVariable.Wave);
        }

        [Test]
        public void ShouldReplaceWavesInReportPart()
        {
            var report = DoCreateReport(ReportType.Chart);
            using var dbContext = _dbContextFactory.CreateDbContext();
            var pane = dbContext.Panes.Single();
            var spec2 = "0";
            var part = new DbPart
            {
                PaneId = pane.PaneId,
                Spec2 = spec2,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Waves = new ReportWaveConfiguration
                {
                    WavesToShow = ReportWavesOptions.AllWaves,
                    NumberOfRecentWaves = 10,
                    Waves = new CrossMeasure { MeasureName = "test" }
                }
            };
            dbContext.Parts.Add(part);
            dbContext.SaveChanges();
            var createVariableResultModel = DoCreateVariable(report, ReportVariableAppendType.Waves, selectedPart: spec2);
            var partDescriptor = _partsRepository.GetById(part.Id);
            Assert.That(partDescriptor.Waves.Waves.MeasureName, Is.EqualTo(createVariableResultModel.Name));
        }

        [Test]
        public void ShouldReplaceBaseInReport()
        {
            var report = DoCreateReport(ReportType.Chart);
            report.BaseVariableId = 99999;
            _savedReportRepository.Update(report);
            var baseVariableId = DoCreateBaseVariable(report, ReportVariableAppendType.Base, selectedPart: null);
            report = _savedReportRepository.GetById(report.Id);
            Assert.That(report.BaseVariableId, Is.EqualTo(baseVariableId));
        }

        [Test]
        public void ShouldReplaceBaseInReportPart()
        {
            var report = DoCreateReport(ReportType.Table);
            using var dbContext = _dbContextFactory.CreateDbContext();
            var pane = dbContext.Panes.Single();
            var spec2 = "0";
            var part = new DbPart
            {
                PaneId = pane.PaneId,
                Spec2 = spec2,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                BaseExpressionOverride = new BaseExpressionDefinition
                {
                    BaseMeasureName = "test",
                    BaseType = BaseDefinitionType.SawThisChoice,
                    BaseVariableId = 99999
                }
            };
            dbContext.Parts.Add(part);
            dbContext.SaveChanges();
            var baseVariableId = DoCreateBaseVariable(report, ReportVariableAppendType.Base, selectedPart: spec2);
            var partDescriptor = _partsRepository.GetById(part.Id);
            Assert.That(partDescriptor.BaseExpressionOverride.BaseVariableId, Is.EqualTo(baseVariableId));
        }

        [Test]
        public void ShouldFlattenMultiEntityDefinitionIntoMultipleSingleGroupVariables()
        {
            const string variableOneName = "Variable1";
            const string variableTwoName = "Variable2";

            var definition = new GroupedVariableDefinition()
            {
                ToEntityTypeName = "Something",
                ToEntityTypeDisplayNamePlural = "Somethings",
                
                Groups = new List<VariableGrouping>()
                {
                    new()
                    {
                        ToEntityInstanceName = variableOneName,
                        ToEntityInstanceId = 1,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {1},
                            FromVariableIdentifier = FromVariableIdentifier,
                            FromEntityTypeName = FromEntityTypeName
                        }
                    },
                    new()
                    {
                        ToEntityInstanceName = variableTwoName,
                        ToEntityInstanceId = 2,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {2},
                            FromVariableIdentifier = FromVariableIdentifier,
                            FromEntityTypeName = FromEntityTypeName
                        }
                    }
                }
            };

            var model = new VariableConfigurationCreateModel()
            {
                Name = "irrelevant information for this test",
                Definition = definition
            };
            var flattened = _variableManager.CreateFlattenedVariables(model);

            var firstVariable = _variableConfigurationRepository.GetByIdentifier(variableOneName);
            var secondVariable = _variableConfigurationRepository.GetByIdentifier(variableTwoName);
            Assert.That(firstVariable.Definition, Is.InstanceOf<SingleGroupVariableDefinition>(), "First variable was not created");
            Assert.That(secondVariable.Definition, Is.InstanceOf<SingleGroupVariableDefinition>(), "Second variable was not created");
        }

        private SavedReport DoCreateReport(ReportType reportType = ReportType.Chart)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();
            var pagesCount = dbContext.Pages.Count();
            var panesCount = dbContext.Panes.Count();
            var pageName = $"page_{pagesCount + 1}";
            var page = new DbPage
            {
                Name = pageName,
                DisplayName = pageName,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
            };
            dbContext.Pages.Add(page);
            dbContext.Panes.Add(new DbPane
            {
                PaneId = $"pane_{panesCount + 1}",
                PageName = pageName,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
            });
            dbContext.SaveChanges();
            var report = new SavedReport
            {
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                IsShared = true,
                CreatedByUserId = "me",
                ReportPageId = page.Id,
                Order = ReportOrder.ResultOrderAsc,
                DecimalPlaces = 3,
                ReportType = reportType,
                Waves = null,
                Breaks = new List<CrossMeasure>(),
                DefaultFilters = new List<DefaultReportFilter>(),
                ModifiedGuid = "abc",
                LastModifiedByUser = _userContext.UserId,

            };
            _savedReportRepository.Create(report);
            return _savedReportRepository.GetById(report.Id);
        }

        private MetricConfiguration DoCreateVariable(SavedReport report, ReportVariableAppendType appendType = ReportVariableAppendType.Part, string selectedPart = null)
        {
            var definition = CreateDefinition(FromVariableIdentifier, FromEntityTypeName);
            var model = new VariableConfigurationCreateModel()
            {
                Name = VariableName,
                Definition = definition,
                ReportSettings = new VariableConfigurationReportSettings()
                {
                    ReportIdToAppendTo = report.Id,
                    AppendType = appendType,
                    SelectedPart = selectedPart
                }
            };

            var createVariableResultModel = _variableManager.ConstructVariableAndRelatedMetadata(model);
            return createVariableResultModel.Metric;
        }

        private int DoCreateBaseVariable(SavedReport report, ReportVariableAppendType appendType = ReportVariableAppendType.Part, string selectedPart = null)
        {
            var definition = CreateDefinition(FromVariableIdentifier, FromEntityTypeName);
            var baseDefinition = new BaseGroupedVariableDefinition
            {
                ToEntityTypeName = definition.ToEntityTypeName,
                ToEntityTypeDisplayNamePlural = definition.ToEntityTypeDisplayNamePlural,
                Groups = definition.Groups.Take(1).ToList()
            };
            var model = new VariableConfigurationCreateModel()
            {
                Name = VariableName,
                Definition = baseDefinition,
                ReportSettings = new VariableConfigurationReportSettings()
                {
                    ReportIdToAppendTo = report.Id,
                    AppendType = appendType,
                    SelectedPart = selectedPart
                }
            };

            return _variableManager.CreateBaseVariable(model);
        }

        private MetricConfiguration CreateFieldExpressionVariable(string name, string fieldExpression)
        {
            var model = new VariableConfigurationCreateModel()
            {
                Name = name,
                Definition = new FieldExpressionVariableDefinition
                {
                    Expression = fieldExpression
                }
            };
            return _variableManager.ConstructVariableAndRelatedMetadata(model).Metric;
        }

        private MetricConfiguration DoCreateVariableForMultiEntity(SavedReport report)
        {
            var multiEntityField = _responseFieldManager.Get(MetricField_BrandProduct);
            var entity1 = new MetricConfiguration()
            {
                Name = MetricName_MultiEntity1,
                Field = multiEntityField.Name,
                TrueVals = "1",
                CalcType = "yn",
                BaseField = multiEntityField.Name,
                BaseVals = "-99>1",
                VarCode = "Household composition",
                SubProductId = SubProductId,
                Subset = "All"
            };
            _metricConfigurationRepository.Create(entity1);

            var netMeasure = new Measure()
            {
                Name = entity1.Name,
                CalculationType = CalculationType.YesNo,
                Field = multiEntityField,
                BaseField = multiEntityField,
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5, 6 } },
            };
            _metricRepository.TryAdd(entity1.Name, netMeasure);

            var definition = CreateDefinition(multiEntityField.Name, TestEntityTypeRepository.Brand.Identifier, new[] { TestEntityTypeRepository.Product.Identifier });
            var model = new VariableConfigurationCreateModel()
            {
                Name = VariableName,
                Definition = definition,
                ReportSettings = new VariableConfigurationReportSettings()
                {
                    ReportIdToAppendTo = report.Id,
                    AppendType = ReportVariableAppendType.Part,
                }
            };

            var createVariableResultModel = _variableManager.ConstructVariableAndRelatedMetadata(model);
            return createVariableResultModel.Metric;
        }

        private static GroupedVariableDefinition CreateDefinition(string fromVariableIdentifier, string fromEntityTypeName, IEnumerable<string> resultEntityTypeNames = null)
        {
            return new GroupedVariableDefinition()
            {
                ToEntityTypeName = ToEntityTypeName,
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceName = "ToEIName1",
                        ToEntityInstanceId = 1,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {1, 4},
                            FromVariableIdentifier = fromVariableIdentifier,
                            FromEntityTypeName = fromEntityTypeName,
                            ResultEntityTypeNames = resultEntityTypeNames?.ToList() ?? new List<string>()
                        },
                    },
                    new()
                    {
                        ToEntityInstanceName = "ToEIName2",
                        ToEntityInstanceId = 2,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {2, 3},
                            FromVariableIdentifier = fromVariableIdentifier,
                            FromEntityTypeName = fromEntityTypeName,
                            ResultEntityTypeNames =  resultEntityTypeNames?.ToList() ?? new List<string>()
                        }
                    }
                }
            };
        }

        private MetricRepository CreateAndPopulateMeasureRepository(ResponseFieldManager responseFieldManager)
        {
            var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
            var measureRepository = new MetricRepository(userPermissionsService);
            responseFieldManager.Add(MetricField, "All", TestEntityTypeRepository.Brand);
            var testResponseMonthsPopulator = new TestResponseMonthsPopulator(responseFieldManager);
            var measure = new Measure()
            {
                Name = MetricName,
                CalculationType = CalculationType.YesNo,
                Field = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField),
                BaseField = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4 } },
            };
            measureRepository.TryAdd(MetricName, measure);

            responseFieldManager.Add(MetricField_MultiEntity1, "All", TestEntityTypeRepository.Brand);
            responseFieldManager.Add(MetricField_MultiEntity2, "All", TestEntityTypeRepository.Product);
            responseFieldManager.Add(MetricField_BrandProduct, "All", TestEntityTypeRepository.Brand, TestEntityTypeRepository.Product);
            var multiEntityMeasure = new Measure()
            {
                Name = MetricName_MultiEntity1,
                CalculationType = CalculationType.YesNo,
                Field = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField_MultiEntity1),
                BaseField = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField_MultiEntity2),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4 } },
            };
            measureRepository.TryAdd(MetricName_MultiEntity1, multiEntityMeasure);

            responseFieldManager.Add(FromVariableIdentifier, "All", TestEntityTypeRepository.Brand);

            return measureRepository;
        }

        private EntityInstanceRepository CreateAndPopulateEntityRepository()
        {
            var entityRepository = new EntityInstanceRepository();
            var brand = new EntityType("Brand", "Brand", "Brands");
            var product = new EntityType("Product", "Product", "Products");
            var entityInstances = new List<EntityInstance>()
            {
                new EntityInstance()
                {
                    Id= 1,
                    Name = "1",
                    Identifier = "1"
                },
                new EntityInstance()
                {
                    Id= 2,
                    Name = "2",
                    Identifier = "2"
                },
                new EntityInstance()
                {
                    Id= 3,
                    Name = "3",
                    Identifier = "3"
                },
                new EntityInstance()
                {
                    Id= 4,
                    Name = "4",
                    Identifier = "4"
                },
            };

            foreach (var instance in entityInstances)
            {
                entityRepository.Add(brand, instance);
                entityRepository.Add(product, instance);
            }

            return entityRepository;
        }
    }
}

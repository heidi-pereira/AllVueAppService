using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Services;
using BrandVue.SourceData.Averages;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class NetManagerTests
    {
        private VariableConfigurationFactory _variableConfigurationFactory;
        private VariableManager _variableManager;
        private NetManager _netManager;
        private IPartsRepository _partsRepository;
        private IPagesRepository _pagesRepository;
        private IPanesRepository _panesRepository;
        private IMetricConfigurationRepository _metricConfigurationRepository;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private MetricRepository _metricRepository;
        private ResponseFieldManager _responseFieldManager;
        private IVariableFactory _fakeVariableFactory;
        private IEntityRepository _entityRepository;
        private IProductContext _productContext;
        private ILogger<IMetricConfigurationRepository> _logger;
        private EntityTypeRepository _entityTypeRepository;
        private SubsetRepository _subsetRepository;
        private IWeightingPlanRepository _weightingPlanRepository;
        private IClaimRestrictedSubsetRepository _claimRestrictedSubsetRepository;
        const string MetricName = "Household composition";
        const string MetricField = "Metric_Field";
        const string NetMetricField = "NetMetric_Field";
        const string MetricName_MultiEntity = "Household composition_ME";
        const string MetricField_MultiEntity1 = "Metric_Field_ME";
        const string MetricField_MultiEntity2 = "Metric_Field_ME2";
        const string MetricField_BrandProduct = "Metric_Field_Brand_Product";
        const int PartId = 1;
        const string PartPosition = "42";
        const string SubProductId = "57";

        [OneTimeSetUp]
        public void SetupNetManagerTests()
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
            _responseFieldManager = new ResponseFieldManager(_entityTypeRepository);
            _fakeVariableFactory = Substitute.For<IVariableFactory>();
        }

        [SetUp]
        public void InitialiseData()
        {
            var testMetadataContextFactory = ITestMetadataContextFactory.Create(StorageType.InMemory);
            _variableConfigurationRepository = new VariableConfigurationRepository(testMetadataContextFactory, _productContext);
            _metricRepository = CreateAndPopulateMeasureRepository(_responseFieldManager);
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(_responseFieldManager, _entityRepository, _entityTypeRepository);
            var baseExpressionGenerator = new BaseExpressionGenerator(_metricConfigurationRepository, _responseFieldManager, _variableConfigurationRepository, fieldExpressionParser);
            var measureFactory = new MetricFactory(_responseFieldManager, fieldExpressionParser, _subsetRepository, _variableConfigurationRepository, _fakeVariableFactory, baseExpressionGenerator);
            _metricConfigurationRepository = new MetricConfigurationRepositorySql(testMetadataContextFactory, _productContext, measureFactory, _logger);
            var variableValidator = new VariableValidator(fieldExpressionParser, _variableConfigurationRepository, _entityRepository, _entityTypeRepository,
                _metricConfigurationRepository, _responseFieldManager);
            _partsRepository = new PartsRepositorySql(_productContext, testMetadataContextFactory);
            _partsRepository.CreatePart(new PartDescriptor()
            {
                Id = PartId,
                Spec1 = MetricName,
                Spec2 = PartPosition
            });
            var savedBreaksRepository = Substitute.For<ISavedBreaksRepository>();
            var savedReportsRepository = Substitute.For<ISavedReportRepository>();
            _panesRepository = Substitute.For<IPanesRepository>();
            _pagesRepository = Substitute.For<IPagesRepository>();
            _variableConfigurationFactory = new VariableConfigurationFactory(
                fieldExpressionParser,
                _variableConfigurationRepository,
                _entityTypeRepository,
                _productContext,
                _metricConfigurationRepository,
                _responseFieldManager,
                variableValidator
            );

            _weightingPlanRepository = new WeightingPlanRepository(testMetadataContextFactory);
            var averageDescriptorRepository = Substitute.For<IAverageDescriptorRepository>();
            var requestAdapter = Substitute.For<IRequestAdapter>();

            var currentUserInformationProvider = Substitute.For<IUserContext>();
            var weightingPlanService = Substitute.For<IWeightingPlanService>();
            var savedReportService = new SavedReportService(savedReportsRepository,
                currentUserInformationProvider,
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
                savedReportsRepository,
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

            _netManager = new NetManager(_variableConfigurationRepository,
                _entityRepository,
                _productContext,
                _metricConfigurationRepository,
                _partsRepository,
                _metricRepository,
                _subsetRepository,
                new VariableFactory(fieldExpressionParser, _entityTypeRepository),
                _variableConfigurationFactory,
                _variableManager);
        }

        [Test]
        public void ShouldCreateNetAndMetaDataForSingleEntity()
        {
            CreateNet();

            var part = _partsRepository.GetById(PartId);
            var fetchedMetric = _metricConfigurationRepository.Get(part.Spec1);
            var netVariable = _variableConfigurationRepository.Get(fetchedMetric.VariableConfigurationId ?? throw new ArgumentException("Metric does not have variableConfigurationId"));
            var expectedNetToEntityTypeName = "NettedBrand";

            Assert.Multiple(() =>
            {
                Assert.That(part.Spec1, Is.EqualTo(fetchedMetric.Name), "Part name does not match net metric");
                Assert.That(part.Spec2, Is.EqualTo(PartPosition), "Part position has changed");

                Assert.That(fetchedMetric.OriginalMetricName, Is.EqualTo(MetricName), "Incorrect original metric name");
                Assert.That(fetchedMetric.Name.Contains(fetchedMetric.OriginalMetricName) && fetchedMetric.Name.Contains("NET"), Is.True, "Metric name does not match expected pattern");
                Assert.That(fetchedMetric.TrueVals, Is.EqualTo("1>5"), "Incorrect true vals");
                Assert.That(fetchedMetric.BaseVals, Is.EqualTo("1>5"), "Incorrect base vals");
                Assert.That(fetchedMetric.FilterValueMapping, Is.EqualTo("1:1|2:2|3:3|4:4|5:netted composition"), "Unexpected filterValueMapping");
                Assert.That(fetchedMetric.BaseExpression, Is.Null, "Unexpected base expression");
                Assert.That(_metricConfigurationRepository.Get(fetchedMetric.OriginalMetricName), Is.Not.Null, "Original metric should still exist");

                Assert.That(netVariable.DisplayName, Is.EqualTo(fetchedMetric.Name), "Net name does not match metric");
                var definition = netVariable.Definition as GroupedVariableDefinition;
                Assert.That(definition.ToEntityTypeName, Is.EqualTo(expectedNetToEntityTypeName), "Unexpected ToEntityTypeName");
            });
        }

        [Test]
        public void ShouldCreateNetAndMetaDataForMultiEntity()
        {
            var multiEntityMetric = new MetricConfiguration()
            {
                Name = MetricName_MultiEntity,
                Field = MetricField_BrandProduct,
                TrueVals = "1",
                CalcType = "yn",
                BaseField = MetricField_BrandProduct,
                BaseVals = "-99>1",
                VarCode = "Household composition",
                SubProductId = SubProductId,
                Subset = "All"
            };
            _metricConfigurationRepository.Create(multiEntityMetric);
            int[] selectedEntityInstances = { 1, 4 };
            _netManager.CreateNewNet("All", multiEntityMetric, PartId, "netted composition", selectedEntityInstances);

            var part = _partsRepository.GetById(PartId);
            var fetchedMetric = _metricConfigurationRepository.Get(part.Spec1);
            var netVariable = _variableConfigurationRepository.Get(fetchedMetric.VariableConfigurationId ?? throw new ArgumentException("Metric does not have variableConfigurationId"));

            Assert.Multiple(() =>
            {
                Assert.That(part.DefaultSplitBy, Is.EqualTo("NettedBrand"), "Incorrect defaultSplitBy");
                Assert.That(part.MultipleEntitySplitByAndFilterBy.SplitByEntityType, Is.EqualTo("NettedBrand"), "Incorrect splitByEntityType");
                Assert.That(part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes.Count(), Is.EqualTo(1), "Incorrect number of FilterByEntityTypes");
                Assert.That(part.MultipleEntitySplitByAndFilterBy.FilterByEntityTypes[0].Type, Is.EqualTo("product"), "Incorrect number of FilterByEntityTypes");

                Assert.That(fetchedMetric.OriginalMetricName, Is.EqualTo(MetricName_MultiEntity), "Incorrect original metric name");
                Assert.That(fetchedMetric.Name.Contains(fetchedMetric.OriginalMetricName) && fetchedMetric.Name.Contains("NET"), Is.True, "Metric name does not match expected pattern");
                Assert.That(_metricConfigurationRepository.Get(fetchedMetric.OriginalMetricName), Is.Not.Null, "Original metric should still exist");

                var defintion = netVariable.Definition as GroupedVariableDefinition;
                var component = defintion.Groups[0].Component as InstanceListVariableComponent;
                Assert.That(component.ResultEntityTypeNames.Count(), Is.EqualTo(1), "Incorrect ResultEntityTypeNames count");
                Assert.That(component.ResultEntityTypeNames[0], Is.EqualTo("product"), "Incorrect ResultEntityTypeName property");
            });
        }

        [Test]
        public void ShouldAddSecondNet()
        {
            CreateNet();

            var part = _partsRepository.GetById(PartId);
            AddGroupToNet(part.Spec1, "Lovely new net");

            var updatedMetric = _metricConfigurationRepository.Get(part.Spec1);
            var netVariable = _variableConfigurationRepository.Get(updatedMetric.VariableConfigurationId ?? throw new ArgumentException("Metric does not have variableConfigurationId"));
            Assert.Multiple(() =>
            {
                Assert.That(part.Spec2, Is.EqualTo(PartPosition), "Part position has changed");

                Assert.That(updatedMetric.TrueVals, Is.EqualTo("1>6"), "Incorrect true vals");
                Assert.That(updatedMetric.BaseVals, Is.EqualTo("1>6"), "Incorrect base vals");
                Assert.That(updatedMetric.FilterValueMapping, Is.EqualTo("1:1|2:2|3:3|4:4|5:netted composition|6:Lovely new net"), "Unexpected filterValueMapping");

                var definition = netVariable.Definition as GroupedVariableDefinition;
                Assert.That(definition.Groups.Count(), Is.EqualTo(6), "Incorrct number of groups in definition");
            });
        }

        [Test]
        public void ShouldRemoveNetAndKeepNetMetricWhenMultipleNetsExist()
        {
            CreateNet();

            var part = _partsRepository.GetById(PartId);
            AddGroupToNet(part.Spec1, "Lovely new net");

            var netMetric = _metricConfigurationRepository.Get(part.Spec1);
            _responseFieldManager.Add(NetMetricField, "All", TestEntityTypeRepository.NetBrand);
            var testResponseMonthsPopulator = new TestResponseMonthsPopulator(_responseFieldManager);
            var netMeasure = new Measure()
            {
                Name = netMetric.Name,
                CalculationType = CalculationType.YesNo,
                Field = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(NetMetricField),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5, 6 } },
            };
            _metricRepository.TryAdd(netMetric.Name, netMeasure);

            const int idToRemove = 5;

            _netManager.RemoveNet("All", PartId, netMetric.Name, (int)netMetric.VariableConfigurationId, idToRemove);

            //reload after update
            netMetric = _metricConfigurationRepository.Get(netMetric.Name);
            var variable = _variableConfigurationRepository.Get((int)netMetric.VariableConfigurationId);
            part = _partsRepository.GetById(PartId);

            Assert.That(netMetric.FilterValueMapping, Is.EqualTo("1:1|2:2|3:3|4:4|6:Lovely new net"), "Incorrect filterValueMapping");
            Assert.That(netMetric.OriginalMetricName, Is.EqualTo(MetricName), "Incorrect original metric name");
            Assert.That(netMetric.Name.Contains(netMetric.OriginalMetricName) && netMetric.Name.Contains("NET"), Is.True, "Metric name does not match expected pattern");
            Assert.That(netMetric.BaseExpression, Is.Null, "Unexpected base expression");

            var definition = variable.Definition as GroupedVariableDefinition;
            Assert.That(definition.Groups.Count(), Is.EqualTo(5), "Incorrct number of groups in definition");
            Assert.Throws<InvalidOperationException>(() => definition.Groups.First(d => d.ToEntityInstanceId == idToRemove), "Group has not been removed from variable");

            Assert.That(part.Spec1, Is.EqualTo(netMetric.Name), "Part is pointing to incorrect metric");
        }

        [Test]
        public void ShouldRemoveNetAndRevertMetricWhenSingleNetExists()
        {
            CreateNet();

            var part = _partsRepository.GetById(PartId);
            var netMetric = _metricConfigurationRepository.Get(part.Spec1);
            const string SubsetId = "All";
            _responseFieldManager.Add(NetMetricField, SubsetId, TestEntityTypeRepository.NetBrand);
            var testResponseMonthsPopulator = new TestResponseMonthsPopulator(_responseFieldManager);
            var netMeasure = new Measure()
            {
                Name = netMetric.Name,
                CalculationType = CalculationType.YesNo,
                Field = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(NetMetricField),
                LegacyPrimaryTrueValues = { Values = [1] },
                LegacyBaseValues = { Values = [1, 2, 3, 4, 5,] },
            };
            _metricRepository.TryAdd(netMetric.Name, netMeasure);

            _netManager.RemoveNet(SubsetId, PartId, netMetric.Name, (int)netMetric.VariableConfigurationId, 5);

            //reload after update
            part = _partsRepository.GetById(PartId);

            Assert.That(_metricConfigurationRepository.Get(netMetric.Name), Is.Null, "Metric for netting should not be present");
            Assert.That(_variableConfigurationRepository.Get((int)netMetric.VariableConfigurationId), Is.Null, "Variable should not be present");
            Assert.That(part.Spec1, Is.EqualTo(MetricName), "Part is pointing at the incorrect metric");
            Assert.That(part.Spec2, Is.EqualTo(PartPosition), "Part is in the incorrect position");
        }

        private void CreateNet()
        {
            var baseMetric = new MetricConfiguration()
            {
                Name = MetricName,
                Field = MetricField,
                TrueVals = "1",
                CalcType = "yn",
                BaseField = MetricField,
                BaseVals = "-99>1",
                VarCode = "Household composition",
                SubProductId = SubProductId
            };
            _metricConfigurationRepository.Create(baseMetric);
            int[] selectedEntityInstances = { 1, 4 };
            _netManager.CreateNewNet("All", baseMetric, PartId, "netted composition", selectedEntityInstances);
        }

        private void AddGroupToNet(string metricName, string netName)
        {
            var netMetric = _metricConfigurationRepository.Get(metricName);
            int[] selectedEntityInstances = { 1, 3 };
            _netManager.AddGroupToNet(netMetric, PartId, netName, selectedEntityInstances);
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
                Name = MetricName_MultiEntity,
                CalculationType = CalculationType.YesNo,
                Field = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField_MultiEntity1),
                BaseField = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField_MultiEntity2),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4 } },
            };
            measureRepository.TryAdd(MetricName_MultiEntity, multiEntityMeasure);

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

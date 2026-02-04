using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Metrics;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using TestCommon;
using TestCommon.DataPopulation;
using TestCommon.Extensions;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class ConfigureMetricServiceTests
    {
        const string MetricName = "Metric name";
        const string MetricField = "Metric_Field";
        const string SubProductId = "55";
        const string MetricName_MultiEntity = "Household composition_ME";
        const string MetricField_MultiEntity1 = "Metric_Field_MEa";
        const string MetricField_MultiEntity2 = "Metric_Field_ME2a";
        const string testVarCode = "test var code";
        private const string NewDisplayName = "newDisplayName";
        private const string ErrorMessage = "Update failed";
        private const string NewText = "New help text";
        private const string MetricWithNoVariable = "MetricWithNoVariable";
        private MetricConfiguration _singleEntityMetric = new();
        private MetricConfiguration _multiEntityMetric = new();
        private VariableConfiguration _variableConfiguration = new() {DisplayName = MetricName};
        private IConfigureMetricService _configureMetricService;
        private IMetricConfigurationRepository _metricConfigurationRepository;
        private MetricRepository _metricRepository;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private IMetricValidator _metricValidator;
        private IVariableValidator _variableValidator;
        private MetricConfiguration _metricWithNoVariable;

        [SetUp]
        public void InitialiseData()
        {
            _variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            _variableConfigurationRepository.Get(Arg.Any<int>()).Returns(_variableConfiguration);
            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            var responseFieldManager = new ResponseFieldManager(responseEntityTypeRepository);
            responseFieldManager.Add(MetricField, SubProductId);
            responseFieldManager.Add(MetricField_MultiEntity1, SubProductId);
            responseFieldManager.Add(MetricField_MultiEntity2, SubProductId);
            var entityRepository = CreateAndPopulateEntityRepository();
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, entityRepository, responseEntityTypeRepository);
            _variableValidator = Substitute.For<IVariableValidator>();
            _variableValidator.Validate(Arg.Any<VariableConfiguration>(), out Arg.Any<IReadOnlyCollection<string>>(), out Arg.Any<IReadOnlyCollection<string>>());
            _metricValidator = Substitute.For<IMetricValidator>();
            _metricValidator.ValidateMetricDisplayName(Arg.Any<string>());
            var variableFactory = new VariableFactory(fieldExpressionParser, responseEntityTypeRepository);
            var subsetRepository = new SubsetRepository();
            var baseExpressionGenerator = new BaseExpressionGenerator(
                _metricConfigurationRepository,
                responseFieldManager,
                _variableConfigurationRepository,
                fieldExpressionParser);
            var measureFactory = new MetricFactory(
                responseFieldManager,
                fieldExpressionParser,
                subsetRepository,
                _variableConfigurationRepository,
                variableFactory,
                baseExpressionGenerator);


            CreateAndPopulateMetricConfigRepository();
            CreateAndPopulateMeasureRepository(responseFieldManager);

            _configureMetricService = new ConfigureMetricService(
                _metricConfigurationRepository,
                _metricRepository,
                _variableConfigurationRepository,
                measureFactory,
                _variableValidator,
                _metricValidator);
        }

        [Test]
        public void ShouldUpdateEligibleForCrosstabOrAllVue()
        {
            _configureMetricService.UpdateEligibleForCrosstabOrAllVue(MetricName, false);
            var metric = _metricConfigurationRepository.Get(MetricName);
            var measure = _metricRepository.Get(MetricName);
            Assert.That(metric.EligibleForCrosstabOrAllVue, Is.False);
            Assert.That(measure.EligibleForCrosstabOrAllVue, Is.False);
        }

        [Test]
        public void ShouldUpdateUpdateMetricDefaultSplitBy()
        {
            _configureMetricService.UpdateMetricDefaultSplitBy(MetricName_MultiEntity, MetricField_MultiEntity2);
            var metric = _metricConfigurationRepository.Get(MetricName_MultiEntity);
            var measure = _metricRepository.Get(MetricName_MultiEntity);
            Assert.That(metric.DefaultSplitByEntityType, Is.EqualTo(MetricField_MultiEntity2));
            Assert.That(measure.DefaultSplitByEntityTypeName, Is.EqualTo(MetricField_MultiEntity2));
        }

        [Test]
        public void ShouldUpdateMetricDisabled()
        {
            _configureMetricService.UpdateMetricDisabled(MetricName, true);
            var metric = _metricConfigurationRepository.Get(MetricName);
            var measure = _metricRepository.Get(MetricName);
            Assert.That(metric.DisableMeasure, Is.True);
            Assert.That(measure.DisableMeasure, Is.True);
        }
        [Test]
        public void ShouldUpdateFilterForMetricDisabled()
        {
            _configureMetricService.UpdateMetricFilterDisabled(MetricName, true);
            var metric = _metricConfigurationRepository.Get(MetricName);
            var measure = _metricRepository.Get(MetricName);
            Assert.That(metric.DisableFilter, Is.True);
            Assert.That(measure.DisableFilter, Is.True);
        }

        #region UpdateMetricDisplayNameAndHelpText
        [Test]
        public void UpdateMetricDisplayNameAndHelpText_ShouldUpdateDisplayNameAndAndNotUpdateVarCode()
        {
            // Arrange
            _metricConfigurationRepository
                .When(x => x.Update(Arg.Any<MetricConfiguration>()))
                .Do(callInfo =>
                {
                    var updatedMetric = callInfo.Arg<MetricConfiguration>();
                    _singleEntityMetric.DisplayName = updatedMetric.DisplayName;
                    _singleEntityMetric.HelpText = updatedMetric.HelpText;
                });


            // Act
            var model = new MetricModalDataModel()
            {
                MetricName = MetricName,
                DisplayName = NewDisplayName,
                DisplayText = NewText,
                EntityInstanceIdMeanCalculationValueMapping = string.Empty
            };

            _configureMetricService.UpdateMetricModalData(model);

            // Assert
            var metric = _metricConfigurationRepository.Get(MetricName);
            _metricConfigurationRepository.Received(1).Update(Arg.Is<MetricConfiguration>(m =>
                m.DisplayName == NewDisplayName &&
                m.VarCode == testVarCode &&
                m.HelpText == NewText));
            Assert.That(metric.DisplayName, Is.EqualTo(NewDisplayName));
            Assert.That(metric.HelpText, Is.EqualTo(NewText));
            Assert.That(metric.VarCode, Is.EqualTo(testVarCode));
        }

        [Test]
        public void UpdateMetricDisplayNameAndHelpText_ShouldUpdateVariableAndMetric()
        {
            // Act
            var model = new MetricModalDataModel()
            {
                MetricName = MetricName,
                DisplayName = NewDisplayName,
                DisplayText = NewText,
                EntityInstanceIdMeanCalculationValueMapping = string.Empty
            };
            _configureMetricService.UpdateMetricModalData(model);

            // Assert
            _variableConfigurationRepository.Received(1).Update(Arg.Is<VariableConfiguration>(v => v.DisplayName == NewDisplayName));
            _metricConfigurationRepository.Received(1).Update(Arg.Is<MetricConfiguration>(m => m.DisplayName == NewDisplayName && m.HelpText == NewText));
        }

        [Test]
        public void UpdateMetricDisplayNameAndHelpText_ShouldUpdateOnlyMetric_WhenNoVariableConfigurationId()
        {
            // Arrange
            _metricConfigurationRepository.Get(MetricWithNoVariable).Returns(_metricWithNoVariable);

            // Act
            var model = new MetricModalDataModel()
            {
                MetricName = MetricWithNoVariable,
                DisplayName = NewDisplayName,
                DisplayText = NewText,
                EntityInstanceIdMeanCalculationValueMapping = string.Empty
            };
            _configureMetricService.UpdateMetricModalData(model);

            // Assert
            _variableConfigurationRepository.Received(0).Update(Arg.Any<VariableConfiguration>());
            _metricConfigurationRepository.Received(1).Update(Arg.Is<MetricConfiguration>(m => m.DisplayName == NewDisplayName && m.HelpText == NewText));
        }

        [Test]
        public void UpdateMetricDisplayNameAndHelpText_ShouldThrow_WhenNoMetricFound()
        {
            // Arrange
            _metricConfigurationRepository.Get(MetricName).Returns(null as MetricConfiguration);

            // Act & Assert
            var model = new MetricModalDataModel()
            {
                MetricName = MetricName,
                DisplayName = NewDisplayName,
                DisplayText = NewText,
                EntityInstanceIdMeanCalculationValueMapping = string.Empty
            };
            var ex = Assert.Throws<BadRequestException>(() => _configureMetricService.UpdateMetricModalData(model));
            Assert.That(ex.Message, Is.EqualTo($"Could not find matching metric {MetricName}"));
            _variableConfigurationRepository.Received(0).Update(Arg.Any<VariableConfiguration>());
            _metricConfigurationRepository.Received(0).Update(Arg.Any<MetricConfiguration>());
        }

        [Test]
        public void UpdateMetricDisplayNameAndHelpText_ShouldUpdateOnlyMetricText_WhenNewNameIsSame()
        {
            // Act
            var model = new MetricModalDataModel()
            {
                MetricName = MetricName,
                DisplayName = MetricName,
                DisplayText = NewText,
                EntityInstanceIdMeanCalculationValueMapping = string.Empty
            };
            _configureMetricService.UpdateMetricModalData(model);

            // Assert
            _variableConfigurationRepository.Received(0).Update(Arg.Is<VariableConfiguration>(v => v.DisplayName == MetricName));
            _metricValidator.Received(0).ValidateMetricDisplayName(Arg.Any<string>());
            _metricConfigurationRepository.Received(1).Update(Arg.Is<MetricConfiguration>(m => m.DisplayName == MetricName && m.HelpText == NewText));
        }

        [Test]
        public void UpdateMetricDisplayNameAndHelpText_ShouldRevertVariableUpdate_WhenMetricUpdateFails()
        {
            // Arrange
            _metricConfigurationRepository
               .When(x => x.Update(Arg.Is<MetricConfiguration>(mc => mc.DisplayName == NewDisplayName)))
               .Do(callInfo =>
               {
                   throw new BadRequestException(ErrorMessage);
               });

            // Act & Assert
            var model = new MetricModalDataModel()
            {
                MetricName = MetricName,
                DisplayName = NewDisplayName,
                DisplayText = NewText,
                EntityInstanceIdMeanCalculationValueMapping = string.Empty
            };
            var ex = Assert.Throws<BadRequestException>(() => _configureMetricService.UpdateMetricModalData(model));
            Assert.That(ex.Message, Is.EqualTo($"Error updating metric {MetricName}: {ErrorMessage}"));
            //we didn't update the variable
            _variableConfigurationRepository.Received(0).Update(Arg.Any<VariableConfiguration>());
        }
        #endregion

        #region Utility Methods
        private void CreateAndPopulateMetricConfigRepository()
        {
            _metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();

            _singleEntityMetric = new MetricConfiguration()
            {
                Name = MetricName,
                DisplayName = MetricName,
                Field = MetricField,
                TrueVals = "1",
                CalcType = "yn",
                BaseField = MetricField,
                BaseVals = "-99>1",
                VarCode = testVarCode,
                SubProductId = SubProductId,
                EligibleForCrosstabOrAllVue = true,
                DisableMeasure = false,
                VariableConfigurationId = 1
            };
            _metricConfigurationRepository.Get(MetricName).Returns(_singleEntityMetric);

            _multiEntityMetric = new MetricConfiguration()
            {
                Name = MetricName_MultiEntity,
                Field = MetricField_MultiEntity1,
                TrueVals = "1",
                CalcType = "yn",
                BaseField = MetricField_MultiEntity2,
                BaseVals = "-99>1",
                VarCode = "Household composition",
                SubProductId = SubProductId,
                Subset = "All",
                DisableMeasure = false
            };
            _metricConfigurationRepository.Get(MetricName_MultiEntity).Returns(_multiEntityMetric);

            _metricWithNoVariable = new MetricConfiguration()
            {
                Name = MetricWithNoVariable,
                Field = MetricField,
                TrueVals = "1",
                CalcType = "yn",
                BaseField = MetricField,
                BaseVals = "-99>1",
                SubProductId = SubProductId,
                EligibleForCrosstabOrAllVue = true,
                DisableMeasure = false
            };
        }

        private void CreateAndPopulateMeasureRepository(ResponseFieldManager responseFieldManager)
        {
            var userPermissionsService = Substitute.For<IUserDataPermissionsOrchestrator>();
            _metricRepository = new MetricRepository(userPermissionsService);

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
                EligibleForCrosstabOrAllVue = true,
                Disabled = false
            };
            _metricRepository.TryAdd(MetricName, measure);

            var multiEntityMeasure = new Measure()
            {
                Name = MetricName_MultiEntity,
                CalculationType = CalculationType.YesNo,
                Field = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField_MultiEntity1),
                BaseField = testResponseMonthsPopulator.TestResponseFactory.ResponseFieldManager.Get(MetricField_MultiEntity2),
                LegacyPrimaryTrueValues = { Values = new[] { 1 } },
                LegacyBaseValues = { Values = new[] { 1, 2, 3, 4, 5, 6 } },
                Disabled = false
            };
            _metricRepository.TryAdd(MetricName_MultiEntity, multiEntityMeasure);
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
        #endregion
    }
}

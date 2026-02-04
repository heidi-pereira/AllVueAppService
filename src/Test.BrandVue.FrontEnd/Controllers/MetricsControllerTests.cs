using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.Controllers.Api;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.MixPanel;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Microsoft.Extensions.Logging;
using Mixpanel;
using NSubstitute;
using NUnit.Framework;
using static BrandVue.MixPanel.MixPanel;

namespace Test.BrandVue.FrontEnd.Controllers
{
    [TestFixture]
    public class MetricsControllerTests
    {
        private const string METRIC_NAME = "NewName";
        private const string FIELD_EXPRESSION = "1";
        private static readonly MetricConfiguration BaseMetricConfiguration = new() { Id = 1 };
        private MetricsController _baseMetricController;
        private MetricConfiguration _initialMetricConfiguration;
        private static readonly int ExistingVariableId = 123;
        private static readonly int OrphanedVariableId = 789;
        private static readonly string ExistingFieldExpression = "123";
        private IMixpanelClient _mixpanelClient;

        [SetUp]
        public void SetUp()
        {
            _mixpanelClient = Substitute.For<IMixpanelClient>();
            _baseMetricController = GetMetricsController(BaseMetricConfiguration);
            _initialMetricConfiguration = BaseMetricConfiguration.ShallowCopy();
        }

        [Test]
        public async Task UpdatingAMetric_WithNoChanges_ShouldSucceed()
        {
            Assert.DoesNotThrowAsync(() =>
                _baseMetricController.UpdateMetricConfiguration(BaseMetricConfiguration.Id, BaseMetricConfiguration));
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task UpdatingAMetric_WithNewFieldExpression_ShouldSucceed()
        {
            // Although marked as obsolete, this is used in the metrics configuration page when modifying a metric
            _initialMetricConfiguration.FieldExpression = FIELD_EXPRESSION;
            var updatedMetric = await _baseMetricController.UpdateMetricConfiguration(BaseMetricConfiguration.Id, _initialMetricConfiguration);

            // Expect a new variable configuration to be created with the variable expression
            Assert.That(BaseMetricConfiguration.VariableConfigurationId, Is.Null);
            Assert.That(updatedMetric.VariableConfigurationId, Is.Not.Null);
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task UpdatingAMetric_WithNoNewFieldExpression_ShouldSucceed()
        {
            _initialMetricConfiguration.Name = METRIC_NAME;
            var updatedMetric = await _baseMetricController.UpdateMetricConfiguration(BaseMetricConfiguration.Id, _initialMetricConfiguration);

            // Don't expect a new variable configuration to be created as there's no new variable expression
            Assert.That(BaseMetricConfiguration.VariableConfigurationId, Is.Null);
            Assert.That(updatedMetric.VariableConfigurationId, Is.Null);
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task UpdatingAMetric_WithOrphanedVariableIdAndNoNewFieldExpression_ShouldSucceedAndRemoveVariableId()
        {
            _initialMetricConfiguration.VariableConfigurationId = OrphanedVariableId;
            var metricController = GetMetricsController(_initialMetricConfiguration);

            var newMetricConfiguration = _initialMetricConfiguration.ShallowCopy();
            newMetricConfiguration.Name = "NewName";

            var updatedMetric = await metricController.UpdateMetricConfiguration(_initialMetricConfiguration.Id, newMetricConfiguration);

            Assert.That(updatedMetric.VariableConfigurationId, Is.Null);
            Assert.That(updatedMetric.Name, Is.EqualTo(newMetricConfiguration.Name));
            Assert.That(updatedMetric.FieldExpression, Is.EqualTo(newMetricConfiguration.FieldExpression));
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task UpdatingAMetric_WithOrphanedVariableIdAndNewFieldExpression_ShouldSucceedAndUpdateVariableId()
        {
            _initialMetricConfiguration.VariableConfigurationId = OrphanedVariableId;
            var metricController = GetMetricsController(_initialMetricConfiguration);

            var newMetricConfiguration = _initialMetricConfiguration.ShallowCopy();
            newMetricConfiguration.FieldExpression = FIELD_EXPRESSION;

            var updatedMetric = await metricController.UpdateMetricConfiguration(_initialMetricConfiguration.Id, newMetricConfiguration);

            Assert.That(updatedMetric.VariableConfigurationId, Is.Not.Null);
            Assert.That(updatedMetric.VariableConfigurationId, Is.Not.EqualTo(OrphanedVariableId));
            Assert.That(updatedMetric.Name, Is.EqualTo(newMetricConfiguration.Name));
            Assert.That(updatedMetric.FieldExpression, Is.EqualTo(newMetricConfiguration.FieldExpression));
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task UpdatingAMetric_WithExistingVariableIdAndNewFieldExpression_ShouldSucceedAndUpdateFieldExpression()
        {
            _initialMetricConfiguration.VariableConfigurationId = ExistingVariableId;
            var metricController = GetMetricsController(_initialMetricConfiguration);

            var newMetricConfiguration = _initialMetricConfiguration.ShallowCopy();
            newMetricConfiguration.FieldExpression = FIELD_EXPRESSION;

            var updatedMetric = await metricController.UpdateMetricConfiguration(_initialMetricConfiguration.Id, newMetricConfiguration);

            Assert.That(updatedMetric.VariableConfigurationId, Is.EqualTo(ExistingVariableId));
            Assert.That(updatedMetric.FieldExpression, Is.EqualTo(newMetricConfiguration.FieldExpression));
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task UpdatingAMetric_WithExistingVariableIdRemovingTheFieldExpression_ShouldSucceedAndRemoveVariableId()
        {
            _initialMetricConfiguration.VariableConfigurationId = ExistingVariableId;
            var metricController = GetMetricsController(_initialMetricConfiguration);

            var newMetricConfiguration = _initialMetricConfiguration.ShallowCopy();

            var updatedMetric = await metricController.UpdateMetricConfiguration(_initialMetricConfiguration.Id, newMetricConfiguration);

            Assert.That(updatedMetric.VariableConfigurationId, Is.Null);
            await _mixpanelClient.Received(1).TrackAsync("Updated Metric", Arg.Any<object>(), Arg.Any<CancellationToken>());
        }

        private MetricsController GetMetricsController(MetricConfiguration overridenMetricConfiguration)
        {
            var measureRepository = Substitute.For<IMeasureRepository>();
            var metricAboutRepository = Substitute.For<IMetricAboutRepository>();
            var subsetRepository = Substitute.For<ISubsetRepository>();
            var userContext = Substitute.For<IUserContext>();
            var linkedMetricRepository = Substitute.For<ILinkedMetricRepository>();
            var measureBaseDescriptionGenerator = Substitute.For<IMeasureBaseDescriptionGenerator>();
            var variableConfigurationFactory = Substitute.For<IVariableConfigurationFactory>();
            var variableValidator = Substitute.For<IVariableValidator>();
            var productContext = Substitute.For<IProductContext>();

            var metricConfigRepository = Substitute.For<IMetricConfigurationRepository>();
            metricConfigRepository.Get(Arg.Any<int>()).Returns(overridenMetricConfiguration);

            variableConfigurationFactory.CreateVariableConfigFromParameters(default, default, default,
                    out Arg.Any<IReadOnlyCollection<string>>(), out Arg.Any<IReadOnlyCollection<string>>(), default)
                .ReturnsForAnyArgs(x => new VariableConfiguration
                {
                    DisplayName = (string)x[0],
                    Identifier = (string)x[1],
                    Definition = (FieldExpressionVariableDefinition)x[2]
                });

            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            variableConfigurationRepository.Create(default, default)
                .ReturnsForAnyArgs(x => (VariableConfiguration)x[0]);
            variableConfigurationRepository.Get(ExistingVariableId)
                .Returns(x => new VariableConfiguration
                {
                    Id = (int)x[0],
                    Definition = new FieldExpressionVariableDefinition
                    {
                        Expression = ExistingFieldExpression
                    }
                });
            variableConfigurationRepository.Get(OrphanedVariableId)
                .Returns(x => null);

            Init(_mixpanelClient, Substitute.For<ILogger<MixPanelLogger>>(), "BrandVue");

            return new MetricsController(measureRepository, metricConfigRepository,
                metricAboutRepository, subsetRepository, userContext, linkedMetricRepository,
                measureBaseDescriptionGenerator, variableConfigurationRepository, variableConfigurationFactory,
                variableValidator);
        }
    }
}

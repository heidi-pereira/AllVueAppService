using NUnit.Framework;
using NSubstitute;
using System.Collections.Generic;
using BrandVue.Services;
using BrandVue.SourceData.Dashboard;
using BrandVue.EntityFramework.MetaData;
using BrandVue.SourceData.Variable;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Entity;
using BrandVue.EntityFramework;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Subsets;
using System.Threading.Tasks;
using System.Linq;

namespace Test.BrandVue.FrontEnd.Services
{
    [TestFixture]
    public class NetManagerTestsPart2
    {
        private const string TestMetricName = "TestMetric";
        private const string NetName = "NetName";
        const string OriginalMetricName = "OriginalMetric";
        private const string EntityTypeName = "EntityType";
        private const string ResultEntityType = "ResultType";

        private NetManager _netManager;
        private IVariableConfigurationRepository _variableConfigurationRepository;
        private IMetricConfigurationRepository _metricConfigurationRepository;
        private IEntityRepository _entityRepository;
        private IProductContext _productContext;
        private IPartsRepository _partsRepository;
        private IMeasureRepository _measureRepository;
        private ISubsetRepository _subsetRepository;
        private IVariableFactory _variableFactory;
        private IVariableConfigurationFactory _variableConfigurationFactory;
        private IVariableManager _variableManager;

        [SetUp]
        public void SetUp()
        {
            _variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            _metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            _entityRepository = Substitute.For<IEntityRepository>();
            _productContext = Substitute.For<IProductContext>();
            _partsRepository = Substitute.For<IPartsRepository>();
            _measureRepository = Substitute.For<IMeasureRepository>();
            _subsetRepository = Substitute.For<ISubsetRepository>();
            _variableFactory = Substitute.For<IVariableFactory>();
            _variableConfigurationFactory = Substitute.For<IVariableConfigurationFactory>();
            _variableManager = Substitute.For<IVariableManager>();

            _netManager = new NetManager(
                _variableConfigurationRepository,
                _entityRepository,
                _productContext,
                _metricConfigurationRepository,
                _partsRepository,
                _measureRepository,
                _subsetRepository,
                _variableFactory,
                _variableConfigurationFactory,
                _variableManager
            );
        }

        [Test]
        public void AddGroupToNet_ShouldThrowNotFoundException_WhenVariableConfigurationIdIsNull()
        {
            var metric = new MetricConfiguration { Name = TestMetricName, VariableConfigurationId = null };

            var ex = Assert.Throws<NotFoundException>(() => _netManager.AddGroupToNet(metric, 1, NetName, new List<int>()));
            Assert.That(ex.Message, Is.EqualTo("Unable to update net, variable identifier not found. (metric name: TestMetric)"));
        }

        [Test]
        public void AddGroupToNet_ShouldAddGroupAndUpdateRepositories()
        {
            var metric = new MetricConfiguration { Name = TestMetricName, VariableConfigurationId = 1, OriginalMetricName = OriginalMetricName };
            var part = new PartDescriptor();
            var originalMetric = new MetricConfiguration { Name = OriginalMetricName };
            var existingVariable = new VariableConfiguration { Definition = new GroupedVariableDefinition { Groups = new List<VariableGrouping>() } };
            var firstComponent = new InstanceListVariableComponent { FromEntityTypeName = EntityTypeName, ResultEntityTypeNames = new List<string> { ResultEntityType } };
            var group = new VariableGrouping { Component = firstComponent };
            ((GroupedVariableDefinition)existingVariable.Definition).Groups.Add(group);

            _partsRepository.GetById(1).Returns(part);
            _metricConfigurationRepository.Get(OriginalMetricName).Returns(originalMetric);
            _variableConfigurationRepository.Get(1).Returns(existingVariable);

            _netManager.AddGroupToNet(metric, 1, NetName, new List<int> { 1, 2, 3 });

            _variableConfigurationRepository.Received(1).Update(existingVariable);
            _partsRepository.Received(1).UpdatePart(part);
            Assert.That(((GroupedVariableDefinition)existingVariable.Definition).Groups.Count, Is.EqualTo(2));
            Assert.That(((GroupedVariableDefinition)metric.VariableConfiguration.Definition).Groups.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task TestAddGroupToNet_Multithreading()
        {
            // Arrange
            var metric = new MetricConfiguration { VariableConfigurationId = 1, OriginalMetricName = OriginalMetricName };
            var partId = 1;
            var netName = "NetName";
            var nettedEntityInstanceIds = new List<int> { 1, 2, 3 };
            int groupsToAdd = 10;
            var initialGroupCount = 3;

            var definition = new GroupedVariableDefinition
            {
                Groups = Enumerable.Range(1, initialGroupCount).Select(i => new VariableGrouping
                {
                    ToEntityInstanceId = i, Component = new InstanceListVariableComponent
                    {
                        FromEntityTypeName = EntityTypeName, ResultEntityTypeNames = [ResultEntityType]
                    }
                }).ToList()
            };

            var existingVariable = new VariableConfiguration { Definition = definition };

            _metricConfigurationRepository.Get(Arg.Any<string>()).Returns(new MetricConfiguration());
            _variableConfigurationRepository.Get(Arg.Any<int>()).Returns(existingVariable);
            _partsRepository.GetById(Arg.Any<int>()).Returns(new PartDescriptor());

            var tasks = new List<Task>();

            // Act
            for (int i = 0; i < groupsToAdd; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    _netManager.AddGroupToNet(metric, partId, netName, nettedEntityInstanceIds);
                }));
            }

            await Task.WhenAll(tasks.ToArray());

            // Assert
            var idsUsed = ((GroupedVariableDefinition)existingVariable.Definition).Groups.Select(g => g.ToEntityInstanceId).ToArray();
            Assert.That(idsUsed.Distinct().Count(), Is.EqualTo(initialGroupCount + groupsToAdd));
            Assert.That(idsUsed.Where(id => id > groupsToAdd + initialGroupCount), Is.Empty);
        }
    }
}
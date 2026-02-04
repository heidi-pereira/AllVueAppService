using System.Collections.Generic;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.SourceData.Validation
{
    [TestFixture]
    public class VariableFactoryTests
    {
        private VariableConfigurationFactory _variableConfigurationFactory;

        [OneTimeSetUp]
        public void ConstructVariableFactory()
        {
            var fieldExpressionParser = Substitute.For<IFieldExpressionParser>();
            var entityRepository = CreateAndPopulateEntityRepository();
            var productContext = new ProductContext("test", "1", true, "test survey");
            var metricConfigurationRepository = Substitute.For<IMetricConfigurationRepository>();
            var responseEntityTypeRepository = EntityTypeRepository.GetDefaultEntityTypeRepository();
            var responseFieldManager = new ResponseFieldManager(responseEntityTypeRepository);
            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            var variableValidator = new VariableValidator(fieldExpressionParser, variableConfigurationRepository, entityRepository, responseEntityTypeRepository,
                metricConfigurationRepository, responseFieldManager);

            _variableConfigurationFactory = new VariableConfigurationFactory(fieldExpressionParser, variableConfigurationRepository, responseEntityTypeRepository, productContext, metricConfigurationRepository, responseFieldManager, variableValidator);
        }

        private EntityInstanceRepository CreateAndPopulateEntityRepository()
        {
            var entityRepository = new EntityInstanceRepository();
            var type = new EntityType("Brand", "Brand", "Brands");
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

            foreach(var instance in entityInstances)
            {
                entityRepository.Add(type, instance);
            }

            return entityRepository;
        }

        [Test]
        public void ShouldCreateVariableConfigFromParameters()
        {
            const string originalFieldName = "PositiveBuzz";
            const string fromEntityType = "Brand";
            const string toEntityTypeName = "ToEntityTypeName";
            const string name = "Positive Buzz Brand Category";
            const string firstGroupName = "Car companies";

            var definition = new GroupedVariableDefinition()
            {
                ToEntityTypeName = toEntityTypeName,
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceName = firstGroupName,
                        ToEntityInstanceId = 1,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {1, 4},
                            FromVariableIdentifier = originalFieldName,
                            FromEntityTypeName = fromEntityType
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
                            FromEntityTypeName = fromEntityType
                        }
                    }
                }
            };

            var identifier = _variableConfigurationFactory.CreateIdentifierFromName(name);
            var variableConfig = _variableConfigurationFactory.CreateVariableConfigFromParameters(name, identifier, definition, out var dependencyVariableInstanceIdentifiers, out _);

            Assert.Multiple(() => {
                Assert.That(variableConfig.Definition is GroupedVariableDefinition, "Incorrect definition");
                Assert.That(variableConfig.DisplayName, Is.EqualTo(name), "Incorrect name");

                var definition = variableConfig.Definition as GroupedVariableDefinition;
                Assert.That(definition.ToEntityTypeName, Is.EqualTo(toEntityTypeName), "Incorrect toEntityTypeName");
                Assert.That(definition.Groups.Count, Is.EqualTo(2), "Incorrect number of groups");

                var firstGroup = definition.Groups[0];
                Assert.That(firstGroup.ToEntityInstanceName, Is.EqualTo(firstGroupName), "Incorrect group ToEntityInstance name");
                Assert.That(firstGroup.Component is InstanceListVariableComponent, "Incorrect componenet type");

                var instanceListVariable = firstGroup.Component as InstanceListVariableComponent;
                Assert.That(instanceListVariable.FromEntityTypeName, Is.EqualTo(fromEntityType), "Incorrect FromEntityType");
                Assert.That(instanceListVariable.FromVariableIdentifier, Is.EqualTo(originalFieldName), "Incorrect FromEntityIdentifier");
                Assert.That(instanceListVariable.InstanceIds, Is.EqualTo(new List<int>(){1, 4}), "Incorrect instance Ids");
            });
        }
    }
}

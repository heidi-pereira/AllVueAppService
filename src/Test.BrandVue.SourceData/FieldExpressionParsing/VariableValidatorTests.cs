using System;
using System.Collections.Generic;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    [TestFixture]
    public class VariableValidatorTests
    {
        private IVariableValidator _variableValidator;
        private const string BrandEntityTypeName = "brand";
        private const string ProductEntityTypeName = "product";
        private const string BrandFieldName = "brandField";
        private const string ProductFieldName = "productField";
        private const string FieldExpressionVariable = "Variable Expression Variable";
        private const string MetricName = "AlwaysTrueMetric";

        [OneTimeSetUp]
        public void Setup()
        {
            var entityTypeRepository = new EntityTypeRepository();
            var brandEntityType = new EntityType(BrandEntityTypeName, BrandEntityTypeName, BrandEntityTypeName);
            entityTypeRepository.TryAdd(BrandEntityTypeName, brandEntityType);
            var productEntityType = new EntityType(ProductEntityTypeName, ProductEntityTypeName, ProductEntityTypeName);
            entityTypeRepository.TryAdd(ProductEntityTypeName, productEntityType);
            
            var responseFieldManager = new ResponseFieldManager(entityTypeRepository);
            responseFieldManager.Load(new[]
            {
                ("subset1", CreateFieldDefinitionModel(BrandFieldName, new EntityFieldDefinitionModel(BrandFieldName, brandEntityType, brandEntityType.Identifier).Yield())),
                ("subset1", CreateFieldDefinitionModel(ProductFieldName, new EntityFieldDefinitionModel(ProductFieldName, productEntityType, productEntityType.Identifier).Yield())),
            });

            var entityRepository = new EntityInstanceRepository();
            entityRepository.Add(brandEntityType, new EntityInstance { Id = 1, Identifier = "brand1", Name = "brand1" });
            entityRepository.Add(productEntityType, new EntityInstance { Id = 1, Identifier = "product1", Name = "product1" });
            var fieldExpressionParser = TestFieldExpressionParser.PrePopulateForFields(responseFieldManager, entityRepository, entityTypeRepository);
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.GetAll().Returns(new List<VariableConfiguration>
            {
                new() { Id = 1, DisplayName = FieldExpressionVariable, Definition = new FieldExpressionVariableDefinition { Expression = "1"}}
            });
            var metricRepository = Substitute.For<IMetricConfigurationRepository>();
            metricRepository.GetAll().Returns(new List<MetricConfiguration>
            {
                new() { Name = MetricName, VariableConfigurationId = 1 }
            });

            _variableValidator = new VariableValidator(fieldExpressionParser, variableRepository, entityRepository, entityTypeRepository, metricRepository, responseFieldManager);
        }

        [Test]
        public void GivenNull_Validate_ShouldThrow()
        {
            AssertVariableConfigurationMessage(null, "Value cannot be null");
        }

        [Test]
        public void GivenNullDefinition_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "NullDefinitionVariable",
                DisplayName = "Null Definition Variable",
                Definition = null
            };

            AssertVariableConfigurationMessage(variable, "Variables must have a definition");
        }

        [Test]
        public void GivenNoGroups_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "NoGroupsVariable",
                DisplayName = "No Groups Variable",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = BrandEntityTypeName,
                    ToEntityTypeDisplayNamePlural = BrandEntityTypeName,
                    Groups = null
                }
            };

            AssertVariableConfigurationMessage(variable, "Variables must have at least one group");
        }

        [Test]
        public void GivenEmptyGroups_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "EmptyGroupsVariable",
                DisplayName = "Empty Groups Variable",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = BrandEntityTypeName,
                    ToEntityTypeDisplayNamePlural = BrandEntityTypeName,
                    Groups = new List<VariableGrouping>()
                }
            };

            AssertVariableConfigurationMessage(variable, "Variables must have at least one group");
        }

        [Test]
        public void GivenEntityTypeNameAlreadyExists_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "EntityNameAlreadyExistsVariable",
                DisplayName = "Entity Name Already Exists Variable",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = BrandEntityTypeName,
                    ToEntityTypeDisplayNamePlural = BrandEntityTypeName,
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            Component = new InstanceListVariableComponent()
                            {
                                FromEntityTypeName = ProductEntityTypeName,
                                FromVariableIdentifier = ProductFieldName,
                                InstanceIds = new List<int>()
                            }
                        }
                    }
                }
            };

            AssertVariableConfigurationMessage(variable, "A variable or question with this name already exists or is too similar to this name");
        }

        [Test]
        public void GivenMultipleGroupsWithTheSameId_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "MultipleGroupsWithSameIdVariable",
                DisplayName = "Multiple Groups With Same Id Variable",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "newtype",
                    ToEntityTypeDisplayNamePlural = "newtype",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "Group1",
                            Component = new InstanceListVariableComponent
                            {
                                FromEntityTypeName = ProductEntityTypeName,
                                FromVariableIdentifier = ProductFieldName,
                                InstanceIds = new List<int>()
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "Group2",
                            Component = new InstanceListVariableComponent
                            {
                                FromEntityTypeName = ProductEntityTypeName,
                                FromVariableIdentifier = ProductFieldName,
                                InstanceIds = new List<int>()
                            }
                        }
                    }
                }
            };

            AssertVariableConfigurationMessage(variable, "Multiple groups have been defined with the same ID");
        }

        [Test]
        public void GivenGroupWithInvalidInstanceIdForEntityType_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "MultipleGroupsWithSameIdVariable",
                DisplayName = "Group With Invalid Instance Id Variable",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "newtype",
                    ToEntityTypeDisplayNamePlural = "newtype",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "Group1",
                            Component = new InstanceListVariableComponent
                            {
                                FromEntityTypeName = ProductEntityTypeName,
                                FromVariableIdentifier = ProductFieldName,
                                InstanceIds = new List<int> { 1 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 2,
                            ToEntityInstanceName = "Group2",
                            Component = new InstanceListVariableComponent
                            {
                                FromEntityTypeName = ProductEntityTypeName,
                                FromVariableIdentifier = ProductFieldName,
                                InstanceIds = new List<int> { 2 }
                            }
                        }
                    }
                }
            };

            AssertVariableConfigurationMessage(variable, "Invalid choice selected for group: Group2");
        }

        [Test]
        public void GivenNameClashesWithVariable_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "FieldExpressionVariable",
                DisplayName = FieldExpressionVariable,
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = BrandEntityTypeName,
                    ToEntityTypeDisplayNamePlural = BrandEntityTypeName,
                    Groups = new List<VariableGrouping>()
                }
            };

            AssertVariableConfigurationMessage(variable, "A variable or question with this name already exists");
        }

        [Test]
        public void GivenNameClashesWithField_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = ProductFieldName,
                DisplayName = ProductFieldName,
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = ProductEntityTypeName,
                    ToEntityTypeDisplayNamePlural = ProductEntityTypeName,
                    Groups = new List<VariableGrouping>()
                }
            };

            AssertVariableConfigurationMessage(variable, "A variable or question with this name already exists");
        }

        [Test]
        public void GivenNameClashesWithMetric_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = MetricName,
                DisplayName = MetricName,
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = ProductEntityTypeName,
                    ToEntityTypeDisplayNamePlural = ProductEntityTypeName,
                    Groups = new List<VariableGrouping>()
                }
            };

            AssertVariableConfigurationMessage(variable, "A variable or question with this name already exists");
        }

        [Test]
        public void GivenEmptyFieldExpression_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "EmptyFieldExpressionVariable",
                DisplayName = "Empty Variable Expression Variable",
                Definition = new FieldExpressionVariableDefinition
                {
                    Expression = ""
                }
            };

            AssertVariableConfigurationMessage(variable, "Value variable must have a non-empty expression");
        }

        [Test]
        public void GivenBadFieldExpression_Validate_ShouldThrow()
        {
            var variable = new VariableConfiguration
            {
                Identifier = "BadFieldExpressionVariable",
                DisplayName = "Bad Variable Expression Variable",
                Definition = new FieldExpressionVariableDefinition
                {
                    Expression = "1 if else = 1"
                }
            };

            AssertVariableConfigurationMessage(variable, "Parsing variable failed: unexpected token 'else'");
        }

        private void AssertVariableConfigurationMessage(VariableConfiguration variable, string expectedMessage)
        {
            var exception = Assert.Throws<BadRequestException>(() => _variableValidator.Validate(variable, out _, out _));
            Assert.That(exception?.Message, Is.EqualTo(expectedMessage), "Exception message was not expected");
        }

        private static FieldDefinitionModel CreateFieldDefinitionModel(string fieldName, IEnumerable<EntityFieldDefinitionModel> entityFieldDefinitionModels)
        {
            return new FieldDefinitionModel(fieldName, string.Empty, string.Empty, string.Empty, string.Empty, null, string.Empty,
                EntityInstanceColumnLocation.Unknown, string.Empty, false, null, entityFieldDefinitionModels, null);
        }
    }
}

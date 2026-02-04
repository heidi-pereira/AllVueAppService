using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using VerifyNUnit;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    internal class VariableDefinitionPythonExtensionsTests
    {
        [Test]
        public void GivenAgeVariableConfigDefinition_ContainsPythonExpression_ShouldReturnFalse()
        {
            var definition = CreateAgeCategoryVariable("does not matter").Definition;
            var definitionContainsPythonExpression = definition.ContainsPythonExpression();
            Assert.That(definitionContainsPythonExpression, Is.True);
        }

        [Test]
        public void GivenRegionVariableConfigDefinition_ContainsPythonExpression_ShouldReturnFalse()
        {
            var definition = CreateRegionCategoryVariable("does not matter").Definition;
            var definitionContainsPythonExpression = definition.ContainsPythonExpression();
            Assert.That(definitionContainsPythonExpression, Is.True);
        }

        [Test]
        public void GivenWaveVariableConfigDefinition_ContainsPythonExpression_ShouldReturnFalse()
        {
            var definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = "DataWaves_Decade",
                ToEntityTypeDisplayNamePlural = "DataWaves_Decades",
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceId = 1,
                        ToEntityInstanceName = "2000s",
                        Component = new DateRangeVariableComponent
                        {
                            MinDate = new DateTime(2000, 1, 1),
                            MaxDate = new DateTime(2009, 12, 31)
                        },
                    },
                    new()
                    {
                        ToEntityInstanceId = 2,
                        ToEntityInstanceName = "2010s",
                        Component = new DateRangeVariableComponent
                        {
                            MinDate = new DateTime(2010, 1, 1),
                            MaxDate = new DateTime(2019, 12, 31)
                        },
                    }
                }
            };
            Assert.That(definition.ContainsPythonExpression(), Is.False);
        }

        [Test]
        public async Task RangeComponentWithProfileFieldShouldGenerateValidExpression()
        {
            const string originalFieldName = "Age";
            var variableConfig = CreateAgeCategoryVariable(originalFieldName);

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        [Test]
        public async Task RangeComponentWithRegionProfileFieldShouldGenerateValidExpression()
        {
            const string originalFieldName = "Region";
            var variableConfig = CreateRegionCategoryVariable(originalFieldName);

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }


        [Test]
        public async Task VariableDependantOnRangeVariableShouldGenerateValidExpression()
        {
            const string originalFieldName = "Age";
            var variableConfig = CreateAgeCategoryVariable(originalFieldName);
            var variableDependantOnRangeVariable = new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Over 100",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "Over100",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "Over100",
                            ToEntityInstanceId = 1,
                            Component = new InstanceListVariableComponent()
                            {
                                InstanceIds = new List<int> {3},
                                FromVariableIdentifier = "AgeCategory",
                                FromEntityTypeName = "AgeCategory"
                            }
                        }
                    }
                }
            };
            var expression = variableDependantOnRangeVariable.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        private static VariableConfiguration CreateRegionCategoryVariable(string originalFieldName)
        {
            return new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Region Category",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "RegionCategory",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "North",
                            ToEntityInstanceId = 1,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                ExactValues  = new [] {10,20,30,40 },
                                Operator = VariableRangeComparisonOperator.Exactly,
                                FromVariableIdentifier = originalFieldName,
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "South",
                            ToEntityInstanceId = 2,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                ExactValues  = new [] {11,21,31,41 },
                                Min = 81,
                                Max = 100,
                                Operator = VariableRangeComparisonOperator.Exactly,
                                FromVariableIdentifier = originalFieldName,
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "East",
                            ToEntityInstanceId = 3,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                ExactValues  = new [] {12,22,32,42 },
                                Operator = VariableRangeComparisonOperator.Exactly,
                                FromVariableIdentifier = originalFieldName,
                            }
                        }
                    }
                }
            };
        }

        private static VariableConfiguration CreateAgeCategoryVariable(string originalFieldName)
        {
            return new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Age Category",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "AgeCategory",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "Young",
                            ToEntityInstanceId = 1,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                Min = 1,
                                Max = 80,
                                Operator = VariableRangeComparisonOperator.Between,
                                FromVariableIdentifier = originalFieldName,
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "Middle aged",
                            ToEntityInstanceId = 2,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                Min = 81,
                                Max = 100,
                                Operator = VariableRangeComparisonOperator.Between,
                                FromVariableIdentifier = originalFieldName,
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "'old",
                            ToEntityInstanceId = 3,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                Min = 101,
                                Max = 120,
                                Operator = VariableRangeComparisonOperator.Between,
                                FromVariableIdentifier = originalFieldName,
                            }
                        }
                    }
                }
            };
        }

        [TestCase(CompositeVariableSeparator.And)]
        [TestCase(CompositeVariableSeparator.Or)]
        public async Task RangeComponentWithMultipleProfileFieldsShouldGenerateValidExpression(CompositeVariableSeparator variableSeparator)
        {
            var variableConfig = new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Young Spending",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "YoungSpendingCategory",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "YoungSpendthrift",
                            ToEntityInstanceId = 1,
                            Component = new CompositeVariableComponent()
                            {
                                CompositeVariableComponents = new List<VariableComponent>()
                                {
                                    new InclusiveRangeVariableComponent()
                                    {
                                        Min = 1,
                                        Max = 30,
                                        Operator = VariableRangeComparisonOperator.Between,
                                        FromVariableIdentifier = "Age",
                                    },
                                    new InclusiveRangeVariableComponent()
                                    {
                                        Min = 1,
                                        Max = 20,
                                        Operator = VariableRangeComparisonOperator.Between,
                                        FromVariableIdentifier = "MaximumSpend",
                                    },
                                    new InclusiveRangeVariableComponent()
                                    {
                                        Min = 1,
                                        Max = 200,
                                        Operator = VariableRangeComparisonOperator.Between,
                                        FromVariableIdentifier = "NumberOfParakeets",
                                    }
                                },
                                CompositeVariableSeparator = variableSeparator
                            }
                        }
                    },
                    ToEntityTypeDisplayNamePlural = "plural"
                }
            };

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        [Test]
        public async Task RangeComponentWithAndAndOrProfileFieldsShouldGenerateValidExpression()
        {
            var variableConfig = new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Young Spending",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "YoungSpendingCategory",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "YoungSpendthrift",
                            ToEntityInstanceId = 1,
                            Component = new CompositeVariableComponent()
                            {
                                CompositeVariableComponents = new List<VariableComponent>()
                                {
                                    new CompositeVariableComponent()
                                    {
                                        CompositeVariableComponents = new List<VariableComponent>()
                                        {
                                            new InclusiveRangeVariableComponent()
                                            {
                                                Min = 1,
                                                Max = 30,
                                                Operator = VariableRangeComparisonOperator.Between,
                                                FromVariableIdentifier = "Age",
                                            },
                                            new InclusiveRangeVariableComponent()
                                            {
                                                Min = 1,
                                                Max = 20,
                                                Operator = VariableRangeComparisonOperator.Between,
                                                FromVariableIdentifier = "MaximumSpend",
                                            }
                                        },
                                        CompositeVariableSeparator = CompositeVariableSeparator.And
                                    },
                                    new InclusiveRangeVariableComponent()
                                    {
                                        Min = 1,
                                        Max = 200,
                                        Operator = VariableRangeComparisonOperator.Between,
                                        FromVariableIdentifier = "NumberOfParakeets",
                                    }
                                },
                                CompositeVariableSeparator = CompositeVariableSeparator.Or
                            }
                        }
                    },
                    ToEntityTypeDisplayNamePlural = "plural"
                }
            };

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }


        [Test]
        public async Task RangeComponentWithEntityFieldShouldGenerateValidExpression()
        {
            const string originalFieldName = "BrandCost";
            var variableConfig = new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Cost category",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "CostCategory",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "Cheap",
                            ToEntityInstanceId = 1,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                Min = 1,
                                Max = 100,
                                Operator = VariableRangeComparisonOperator.Between,
                                FromVariableIdentifier = originalFieldName
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "Middle",
                            ToEntityInstanceId = 2,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                Min = 101,
                                Max = 1000,
                                Operator = VariableRangeComparisonOperator.Between,
                                FromVariableIdentifier = originalFieldName
                            }
                        },
                        new()
                        {
                            ToEntityInstanceName = "Expensive",
                            ToEntityInstanceId = 3,
                            Component = new InclusiveRangeVariableComponent()
                            {
                                Min = 1001,
                                Max = 1200,
                                Operator = VariableRangeComparisonOperator.Between,
                                FromVariableIdentifier = originalFieldName
                            }
                        }
                    }
                }
            };

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        [Test]
        public async Task InstanceListComponentWithSingleEntityShouldReturnValidExpression()
        {
            var originalFieldName = "PositiveBuzz";
            var variableConfig = new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Positive Buzz Brand Category",
                Definition = new GroupedVariableDefinition
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
                }
            };

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        [TestCase(CompositeVariableSeparator.And)]
        [TestCase(CompositeVariableSeparator.Or)]
        public async Task InstanceListComponentWithMultipleEntitiesShouldReturnValidExpression(CompositeVariableSeparator variableSeparator)
        {
            var variableConfig = new VariableConfiguration
            {
                ProductShortCode = "retail",
                DisplayName = "Positive Buzz Brand Category",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "PositiveBuzzBrandCategory",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "Car companies",
                            ToEntityInstanceId = 1,
                            Component = new CompositeVariableComponent()
                            {
                                CompositeVariableComponents = new List<VariableComponent>()
                                {
                                    new InstanceListVariableComponent()
                                    {
                                        InstanceIds = new List<int> {1, 4},
                                        FromVariableIdentifier = "selection1",
                                        FromEntityTypeName = "brand"
                                    },
                                    new InstanceListVariableComponent()
                                    {
                                        InstanceIds = new List<int>{3,7},
                                        FromVariableIdentifier = "selection2",
                                        FromEntityTypeName = "brand"
                                    }
                                },
                                CompositeVariableSeparator = variableSeparator
                            }
                        }
                    }
                }
            };

            var expression = variableConfig.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        [Test]
        public void ValueVariablesShouldReturnExpression()
        {
            const string expression = "any(response.Location_Yesterday(location=result.location,categories=max(response.Brand_Yesterday(brand=result.brand))))";
            var expectedWrappedExpression = $"({expression}) or None";
            var config = new VariableConfiguration
            {
                ProductShortCode = "survey",
                DisplayName = "Purchase yesterday",
                Definition = new FieldExpressionVariableDefinition
                {
                    Expression = expression
                }
            };

            var result = config.Definition.GetPythonExpression();
            Assert.That(result, Is.EqualTo(expectedWrappedExpression));
        }

        [Test]
        public async Task MultiEntityVariablesShouldSpecifyOmittedTypes()
        {
            var variable = new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "FirstGrouping",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "FirstGrouping",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "First",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "AField",
                                FromEntityTypeName = "Type1",
                                ResultEntityTypeNames = new List<string>{"Type2", "Type3"},
                                InstanceIds = new List<int> { 1, 2 }
                            }
                        },
                    }
                }
            };

            var expression = variable.Definition.GetPythonExpression();
            await Verifier.Verify(expression, "py");
        }

        [Test]
        public void EvaluatableVariableDefinition_ShouldHaveCachedPythonExpressionProperty()
        {
            // Test that EvaluatableVariableDefinition exists and has the CachedPythonExpression property
            var fieldExpressionDefinition = new FieldExpressionVariableDefinition { Expression = "1" };
            var groupedDefinition = new GroupedVariableDefinition 
            { 
                ToEntityTypeName = "Test",
                Groups = new List<VariableGrouping>()
            };
            var singleGroupDefinition = new SingleGroupVariableDefinition
            {
                Group = new VariableGrouping
                {
                    ToEntityInstanceId = 1,
                    ToEntityInstanceName = "Test",
                    Component = new DateRangeVariableComponent()
                }
            };

            // Test that all evaluatable variable definitions inherit from EvaluatableVariableDefinition
            Assert.That(fieldExpressionDefinition, Is.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(groupedDefinition, Is.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(singleGroupDefinition, Is.InstanceOf<EvaluatableVariableDefinition>());

            // Test that CachedPythonExpression property can be set and retrieved
            fieldExpressionDefinition.CachedPythonExpression = "cached expression";
            Assert.That(fieldExpressionDefinition.CachedPythonExpression, Is.EqualTo("cached expression"));

            // Test that QuestionVariableDefinition does NOT inherit from EvaluatableVariableDefinition
            var questionDefinition = new QuestionVariableDefinition();
            Assert.That(questionDefinition, Is.Not.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(questionDefinition, Is.InstanceOf<VariableDefinition>());
        }

        [Test]
        public void EvaluatableVariableDefinition_CachedPythonExpression_ShouldMatchGetPythonExpression()
        {
            // Test that the cached expression matches what GetPythonExpression returns
            var fieldExpressionDefinition = new FieldExpressionVariableDefinition 
            { 
                Expression = "any(response.Location(location=result.location))" 
            };

            var expectedExpression = fieldExpressionDefinition.GetPythonExpression();
            fieldExpressionDefinition.CachedPythonExpression = expectedExpression;

            Assert.That(fieldExpressionDefinition.CachedPythonExpression, Is.EqualTo(expectedExpression));
            Assert.That(fieldExpressionDefinition.CachedPythonExpression, Contains.Substring("or None"));
        }

        [Test]
        public void EvaluatableVariableDefinition_Inheritance_ShouldExcludeQuestionVariableDefinition()
        {
            // Verify that all variable definition types inherit correctly
            var fieldExpression = new FieldExpressionVariableDefinition();
            var baseFieldExpression = new BaseFieldExpressionVariableDefinition();
            var grouped = new GroupedVariableDefinition();
            var baseGrouped = new BaseGroupedVariableDefinition();
            var singleGroup = new SingleGroupVariableDefinition();
            var question = new QuestionVariableDefinition();

            // These should inherit from EvaluatableVariableDefinition
            Assert.That(fieldExpression, Is.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(baseFieldExpression, Is.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(grouped, Is.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(baseGrouped, Is.InstanceOf<EvaluatableVariableDefinition>());
            Assert.That(singleGroup, Is.InstanceOf<EvaluatableVariableDefinition>());

            // This should NOT inherit from EvaluatableVariableDefinition
            Assert.That(question, Is.Not.InstanceOf<EvaluatableVariableDefinition>());

            // But all should inherit from VariableDefinition
            Assert.That(fieldExpression, Is.InstanceOf<VariableDefinition>());
            Assert.That(baseFieldExpression, Is.InstanceOf<VariableDefinition>());
            Assert.That(grouped, Is.InstanceOf<VariableDefinition>());
            Assert.That(baseGrouped, Is.InstanceOf<VariableDefinition>());
            Assert.That(singleGroup, Is.InstanceOf<VariableDefinition>());
            Assert.That(question, Is.InstanceOf<VariableDefinition>());
        }
    }
}

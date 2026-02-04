using System;
using System.Collections.Generic;
using System.Linq;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Variable;
using NSubstitute;
using NUnit.Framework;
using TestCommon.Extensions;

namespace Test.BrandVue.SourceData.FieldExpressionParsing
{
    class VariableDependantFieldExpressionTests : ExpressionTestBase
    {
        [TestCase(100, ExpectedResult = null)]
        [TestCase(101, ExpectedResult = 1)]
        public int? MultiEntityQuestion_AskedMultipleTimesForBothEntities_QuerySingle(int respondentAge)
        {
            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("age", respondentAge, Enumerable.Empty<EntityValue>())
            });

            const string originalFieldName = "age";
            AddVariable(CreateAgeCategoryVariable(originalFieldName));
            var variableDependantOnRangeVariable = new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "Over100",
                DisplayName = "Over100",
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
                                FromVariableIdentifier = "agecategory",
                                FromEntityTypeName = "agecategory"
                            }
                        }
                    }
                }
            };
            AddVariable(variableDependantOnRangeVariable);
            var filterExpression = Parser.ParseUserNumericExpressionOrNull("Over100");
            var over100EntityType = _entityTypeRepository.Get("Over100");
            var calculate = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(over100EntityType, 1)));
            return calculate(response);
        }

        [TestCase(true, ExpectedResult = 1)]
        [TestCase(false, ExpectedResult = 0)]
        public int? DataWave_QuerySingle(bool inWaveOne)
        {
            var dateInWave = DateTimeOffset.Parse("2020-01-01");
            var profileDate = inWaveOne ? dateInWave : DateTimeOffset.Parse("2025-01-01");
            _responseFieldManager.Add("age", types: Array.Empty<EntityType>());
            var response = _responseFactory.WithFieldValues(CreateProfile(profileDate), new[]
            {
                ("age", 30, Enumerable.Empty<EntityValue>())
            });

            var waveVariable = new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "Wave",
                DisplayName = "Wave",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "Wave",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceName = "Wave",
                            ToEntityInstanceId = 1,
                            Component = new DateRangeVariableComponent()
                            {
                                MinDate = dateInWave.AddDays(-10).Date,
                                MaxDate = dateInWave.AddDays(10).Date
                            }
                        }
                    }
                }
            };
            AddVariable(waveVariable);
            var filterExpression = Parser.ParseUserNumericExpressionOrNull("Wave == 1 and age > 20");
            var waveEntityType = _entityTypeRepository.Get("Wave");
            var calculate = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(waveEntityType, 1)));
            return calculate(response);
        }

        [TestCase(1, ExpectedResult = 1)]
        [TestCase(5, ExpectedResult = 1)]
        [TestCase(9, ExpectedResult = 2)]
        [TestCase(13, ExpectedResult = 2)]
        public int? SingleEntity_MultipleNested_QuerySingle(int regionId)
        {
            var regionType = new EntityType("Region", "Region", "Region");
            _entityTypeRepository.TryAdd("Region", regionType);
            _responseFieldManager.Add("Region", types: new[] { regionType });

            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", regionId, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, regionId) }),
            });

            var regionVariable = new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "RegionsNetted",
                DisplayName = "Regions netted",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "RegionsNetted",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "North",
                            Component = new InstanceListVariableComponent()
                            {
                                FromVariableIdentifier = "Region",
                                FromEntityTypeName = "Region",
                                InstanceIds = new List<int> { 1, 2, 3, 4 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 2,
                            ToEntityInstanceName = "Midlands",
                            Component = new InstanceListVariableComponent()
                            {
                                FromVariableIdentifier = "Region",
                                FromEntityTypeName = "Region",
                                InstanceIds = new List<int> { 5, 6, 7, 8 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 3,
                            ToEntityInstanceName = "South",
                            Component = new InstanceListVariableComponent()
                            {
                                FromVariableIdentifier = "Region",
                                FromEntityTypeName = "Region",
                                InstanceIds = new List<int> { 9, 10, 11, 12 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 4,
                            ToEntityInstanceName = "London",
                            Component = new InstanceListVariableComponent()
                            {
                                FromVariableIdentifier = "Region",
                                FromEntityTypeName = "Region",
                                InstanceIds = new List<int> { 13 }
                            }
                        },
                    }
                }
            };

            AddVariable(regionVariable);

            var nestedRegionVariable = new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "NestedRegionsNetted",
                DisplayName = "Nested regions netted",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "NestedRegionsNetted",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "North & Midlands",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "RegionsNetted",
                                FromEntityTypeName = "RegionsNetted",
                                InstanceIds = new List<int> { 1, 2 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 2,
                            ToEntityInstanceName = "South & London",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "RegionsNetted",
                                FromEntityTypeName = "RegionsNetted",
                                InstanceIds = new List<int> { 3, 4 }
                            }
                        }
                    }
                }
            };

            AddVariable(nestedRegionVariable);

            var filterExpression = Parser.ParseUserNumericExpressionOrNull("max(response.NestedRegionsNetted())");
            var type = _entityTypeRepository.Get("NestedRegionsNetted");
            var calculate = filterExpression.CreateForEntityValues(default);
            return calculate(response);
        }

        [Test]
        public void MultiEntity_GroupingDifferentTypes()
        {
            var type1 = new EntityType("Type1", "Type 1", "Type 1");
            _entityTypeRepository.TryAdd("Type1", type1);
            var type2 = new EntityType("Type2", "Type 2", "Type 2");
            _entityTypeRepository.TryAdd("Type2", type2);

            _responseFieldManager.Add("AField", types: new[] { type1, type2 });

            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("AField", 1, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(type1, 5), new EntityValue(type2, 5) }),
            });

            var firstGrouping = new VariableConfiguration
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
                                ResultEntityTypeNames = new List<string> { "Type2" },
                                InstanceIds = new List<int> { 1, 2 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 2,
                            ToEntityInstanceName = "Second",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "AField",
                                FromEntityTypeName = "Type1",
                                ResultEntityTypeNames = new List<string> { "Type2" },
                                InstanceIds = new List<int> { 3, 4 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 3,
                            ToEntityInstanceName = "Third",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "AField",
                                FromEntityTypeName = "Type1",
                                ResultEntityTypeNames = new List<string> { "Type2" },
                                InstanceIds = new List<int> { 5, 6 }
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 4,
                            ToEntityInstanceName = "Fourth",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "AField",
                                FromEntityTypeName = "Type1",
                                ResultEntityTypeNames = new List<string> { "Type2" },
                                InstanceIds = new List<int> { 7, 8 }
                            }
                        },
                    }
                }
            };

            AddVariable(firstGrouping);

            var secondGrouping = new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "SecondGrouping",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "SecondGrouping",
                    Groups = new List<VariableGrouping>
                    {
                        new ()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "First",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "FirstGrouping",
                                FromEntityTypeName = "Type2",
                                ResultEntityTypeNames = new List<string> { "FirstGrouping" },
                                InstanceIds = new List<int> {1, 2}
                            }
                        },
                        new()
                        {
                            ToEntityInstanceId = 2,
                            ToEntityInstanceName = "Second",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "FirstGrouping",
                                FromEntityTypeName = "Type2",
                                ResultEntityTypeNames = new List<string> { "FirstGrouping" },
                                InstanceIds = new List<int> { 5, 6 }
                            }
                        },
                    }
                }
            };

            AddVariable(secondGrouping);

            var filterExpression = Parser.ParseUserNumericExpressionOrNull("SecondGrouping");
            var firstGroupedType = _entityTypeRepository.Get("FirstGrouping");
            var secondGroupedType = _entityTypeRepository.Get("SecondGrouping");
            var calculate = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(firstGroupedType, 3), new EntityValue(secondGroupedType, 2)));

            Assert.That(calculate(response), Is.EqualTo(2));
        }

        [Test]
        public void BaseGroupedVariables_ShouldProduceWorkingFieldExpressions()
        {
            var regionType = new EntityType("Region", "Region", "Region");
            _entityTypeRepository.TryAdd("Region", regionType);
            _responseFieldManager.Add("Region", types: new[] { regionType });

            var regionVariable = new VariableConfiguration
            {
                Id = -1,
                ProductShortCode = "retail",
                Identifier = "RegionsNetted",
                DisplayName = "Regions netted",
                Definition = new BaseGroupedVariableDefinition
                {
                    ToEntityTypeName = "RegionsNetted",
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "North",
                            Component = new InstanceListVariableComponent()
                            {
                                FromVariableIdentifier = "Region",
                                FromEntityTypeName = "Region",
                                InstanceIds = new List<int> { 1, 2, 3, 4 }
                            }
                        }
                    }
                }
            };

            AddVariable(regionVariable);
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.Get(regionVariable.Id).Returns(regionVariable);
            var baseExpressionGenerator = new BaseExpressionGenerator(Substitute.For<IMetricConfigurationRepository>(), _responseFieldManager, variableRepository, Parser);

            var responseInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 1, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 1) }),
            });
            var responseNotInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 20, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 20) }),
            });

            var pythonExpression = baseExpressionGenerator.GetBaseVariablePythonExpression(regionVariable.Id, false, Enumerable.Empty<string>());
            var filterExpression = Parser.ParseUserBooleanExpression(pythonExpression);
            var calculate = filterExpression.CreateForEntityValues(default);
            Assert.That(calculate(responseInBase), Is.True);
            Assert.That(calculate(responseNotInBase), Is.False);
        }

        [Test]
        public void BaseFieldExpressionVariables_ShouldProduceWorkingFieldExpressions()
        {
            var regionType = new EntityType("Region", "Region", "Region");
            _entityTypeRepository.TryAdd("Region", regionType);
            _responseFieldManager.Add("Region", types: new[] { regionType });

            var regionVariable = new VariableConfiguration
            {
                Id = -1,
                ProductShortCode = "survey",
                Identifier = "RegionBase",
                DisplayName = "Regions: North / South",
                Definition = new BaseFieldExpressionVariableDefinition
                {
                    Expression = "any(response.Region(Region=[1,2,3,4]))"
                }
            };

            AddVariable(regionVariable);
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.Get(regionVariable.Id).Returns(regionVariable);
            var baseExpressionGenerator = new BaseExpressionGenerator(Substitute.For<IMetricConfigurationRepository>(), _responseFieldManager, variableRepository, Parser);

            var responseInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 1, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 1) }),
            });
            var responseNotInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 20, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 20) }),
            });

            var pythonExpression = baseExpressionGenerator.GetBaseVariablePythonExpression(regionVariable.Id, false, Enumerable.Empty<string>());
            var filterExpression = Parser.ParseUserBooleanExpression(pythonExpression);
            var calculate = filterExpression.CreateForEntityValues(default);
            Assert.That(calculate(responseInBase), Is.True);
            Assert.That(calculate(responseNotInBase), Is.False);
        }

        [Test]
        public void NonLiteralResponseArg()
        {
            var regionType = new EntityType("Region", "Region", "Region");
            _entityTypeRepository.TryAdd("Region", regionType);
            _responseFieldManager.Add("Region", types: new[] { regionType });

            var regionVariable = new VariableConfiguration
            {
                Id = -1,
                ProductShortCode = "survey",
                Identifier = "RegionBase",
                DisplayName = "Regions: North / South",
                Definition = new BaseFieldExpressionVariableDefinition
                {
                    Expression = "any(response.Region(Region=[1,2,3,4] if result.Region==1 else []))"
                }
            };

            AddVariable(regionVariable);
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.Get(regionVariable.Id).Returns(regionVariable);
            var baseExpressionGenerator = new BaseExpressionGenerator(Substitute.For<IMetricConfigurationRepository>(), _responseFieldManager, variableRepository, Parser);

            var responseInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 1, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 1) }),
            });
            var responseNotInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 20, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 20) }),
            });

            var pythonExpression = baseExpressionGenerator.GetBaseVariablePythonExpression(regionVariable.Id, false, Enumerable.Empty<string>());
            var filterExpression = Parser.ParseUserBooleanExpression(pythonExpression);
            var calculate = filterExpression.CreateForEntityValues(new(new EntityValue(regionType, 1)));
            Assert.That(calculate(responseInBase), Is.True);
            Assert.That(calculate(responseNotInBase), Is.False);
        }

        [Test]
        public void DictionaryResponseArg()
        {
            var regionType = new EntityType("Region", "Region", "Region");
            _entityTypeRepository.TryAdd("Region", regionType);
            _responseFieldManager.Add("Region", types: new[] { regionType });

            var regionVariable = new VariableConfiguration
            {
                Id = -1,
                ProductShortCode = "survey",
                Identifier = "RegionBase",
                DisplayName = "Regions: North / South",
                Definition = new BaseFieldExpressionVariableDefinition
                {
                    Expression = "any(response.Region(Region={1:[1,2], 2:[1,2], 3:[3]}.get(result.Region)))"
                }
            };

            AddVariable(regionVariable);
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.Get(regionVariable.Id).Returns(regionVariable);
            var baseExpressionGenerator = new BaseExpressionGenerator(Substitute.For<IMetricConfigurationRepository>(), _responseFieldManager, variableRepository, Parser);

            var responseInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 2, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 1) }),
            });
            var responseNotInBase = _responseFactory.WithFieldValues(CreateProfile(), new[]
            {
                ("Region", 3, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(regionType, 20) }),
            });

            var pythonExpression = baseExpressionGenerator.GetBaseVariablePythonExpression(regionVariable.Id, false, Enumerable.Empty<string>());
            var filterExpression = Parser.ParseUserBooleanExpression(pythonExpression);
            var calculate = filterExpression.CreateForEntityValues(new(new EntityValue(regionType, 1)));
            Assert.That(calculate(responseInBase), Is.True);
            Assert.That(calculate(responseNotInBase), Is.False);
        }

        [Test]
        public void BaseGroupedVariable_ShouldProduceWorkingResultTypesExpression()
        {
            var type1 = new EntityType("Type1", "Type 1", "Type 1");
            _entityTypeRepository.TryAdd("Type1", type1);
            var type2 = new EntityType("Type2", "Type 2", "Type 2");
            _entityTypeRepository.TryAdd("Type2", type2);
            _responseFieldManager.Add("AField", types: new[] { type1, type2 });

            var variableEntityTypeName = "basevartype";
            var baseVariable = new VariableConfiguration
            {
                Id = -1,
                ProductShortCode = "retail",
                Identifier = "FirstGrouping",
                Definition = new BaseGroupedVariableDefinition
                {
                    ToEntityTypeName = variableEntityTypeName,
                    Groups = new List<VariableGrouping>
                    {
                        new()
                        {
                            ToEntityInstanceId = 1,
                            ToEntityInstanceName = "First",
                            Component = new InstanceListVariableComponent
                            {
                                FromVariableIdentifier = "AField",
                                FromEntityTypeName = "Type2",
                                ResultEntityTypeNames = new List<string> { "Type1" },
                                InstanceIds = new List<int> { 1, 2, 3, 4, 5 }
                            }
                        },
                    }
                }
            };

            AddVariable(baseVariable);
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.Get(baseVariable.Id).Returns(baseVariable);
            var baseExpressionGenerator = new BaseExpressionGenerator(Substitute.For<IMetricConfigurationRepository>(), _responseFieldManager, variableRepository, Parser);

            var response = _responseFactory.WithFieldValues(CreateProfile(), new[]
{
                ("AField", 1, (IEnumerable<EntityValue>) new List<EntityValue> { new EntityValue(type1, 1), new EntityValue(type2, 2) }),
            });

            var pythonExpressionWithEntityType = baseExpressionGenerator.GetBaseVariablePythonExpression(baseVariable.Id, true, new[] { type1.Identifier });
            var filterExpression = Parser.ParseUserBooleanExpression(pythonExpressionWithEntityType);
            var calculate = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(type1, 1)));
            Assert.That(calculate(response), Is.True, "Respondent answered with type1: 1 so should be in base");
            calculate = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(type1, 3)));
            Assert.That(calculate(response), Is.False, "Respondent did not answer with type1: 3 so should not be in base");

            var pythonExpressionWithoutEntityType = baseExpressionGenerator.GetBaseVariablePythonExpression(baseVariable.Id, false, Enumerable.Empty<string>());
            filterExpression = Parser.ParseUserBooleanExpression(pythonExpressionWithoutEntityType);
            calculate = filterExpression.CreateForEntityValues(new EntityValueCombination(new EntityValue(type1, 3)));
            Assert.That(calculate(response), Is.True, "Result types not included, respondent should be in base");
        }

        [Test]
        public void BaseGroupedVariable_ExpressionResultTypesShouldNotIncludeUnspecifiedEntityTypes()
        {
            _entityTypeRepository.TryAdd("Type1", new EntityType("Type1", "Type 1", "Type 1"));
            _entityTypeRepository.TryAdd("Type2", new EntityType("Type2", "Type 2", "Type 2"));
            _entityTypeRepository.TryAdd("Type3", new EntityType("Type3", "Type 3", "Type 3"));
            var baseVariable = new VariableConfiguration
            {
                Id = -1,
                ProductShortCode = "retail",
                Identifier = "FirstGrouping",
                Definition = new BaseGroupedVariableDefinition
                {
                    ToEntityTypeName = "ThreeEntity",
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
                                ResultEntityTypeNames = new List<string> { "Type2", "Type3" },
                                InstanceIds = new List<int> { 1, 2, 3, 4, 5 }
                            }
                        },
                    }
                }
            };
            var variableRepository = Substitute.For<IVariableConfigurationRepository>();
            variableRepository.Get(baseVariable.Id).Returns(baseVariable);
            var baseExpressionGenerator = new BaseExpressionGenerator(Substitute.For<IMetricConfigurationRepository>(), _responseFieldManager, variableRepository, Parser);

            var expression = baseExpressionGenerator.GetBaseVariablePythonExpression(-1, true, new[] { "Type2" });
            Assert.That(expression.Contains("Type2=result.Type2", StringComparison.OrdinalIgnoreCase), Is.True);
            Assert.That(expression.Contains("Type3=result.Type3", StringComparison.OrdinalIgnoreCase), Is.False);
        }

        private static VariableConfiguration CreateAgeCategoryVariable(string originalFieldName)
        {
            return new VariableConfiguration
            {
                ProductShortCode = "retail",
                Identifier = "agecategory",
                DisplayName = "agecategory",
                Definition = new GroupedVariableDefinition
                {
                    ToEntityTypeName = "agecategory",
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
                                FromVariableIdentifier = originalFieldName
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
                                FromVariableIdentifier = originalFieldName
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
                                FromVariableIdentifier = originalFieldName
                            }
                        }
                    }
                }
            };
        }
    }
}

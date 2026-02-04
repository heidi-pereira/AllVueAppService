using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.BaseSizes;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.Services;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Respondents;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using Newtonsoft.Json;
using NSubstitute;
using NUnit.Framework;
using TestCommon;

namespace Test.BrandVue.FrontEnd
{
    public class CrosstabTests
    {
        #region Test Variables

        #region Constants
        private const string Brand = "Brand";
        private const string Generation = "Generation";
        private const string GenderIdentifier = "Gender";
        private const string EmploymentStatus = "Employment status";
        private const string MappedAgeBands = "Mapped Age Bands";
        const string MeasureBasedOnMultiChoiceQuestionName = "MeasureBasedOnMultiChoiceQuestion";
        const string MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName = "MultiChoiceVariable";
        const string MeasureBasedOnSingleChoiceQuestionName = "MeasureBasedOnSingleChoiceQuestion";
        const string MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName = "SingleChoiceVariable";

        #endregion

        #region Repositories
        private CrosstabResultsProvider _crosstabResultsProvider;
        private IFilterRepository _filterRepository;
        private ISubsetRepository _subsetRepository;
        private IQuestionTypeLookupRepository _questionTypeLookupRepository;
        private IEntityRepository _entityRepository;
        private IRequestAdapter _requestAdapter;
        private IResponseEntityTypeRepository _responseEntityTypeRepository;
        private IMeasureRepository _measureRepository;
        private IConvenientCalculator _convenientCalculator;
        private IBaseExpressionGenerator _baseExpressionGenerator;
        #endregion

        #region Entities
        private static readonly EntityInstance Female = new EntityInstance { Id = 0, Name = "Female" };
        private static readonly EntityInstance Male = new EntityInstance { Id = 1, Name = "Male" };
        private static readonly EntityInstance AgeBand1 = new EntityInstance { Id = 0, Name = "18-30" };
        private static readonly EntityInstance AgeBand2 = new EntityInstance { Id = 1, Name = "31-50" };
        private static readonly EntityInstance AgeBand3 = new EntityInstance { Id = 2, Name = "51+" };
        private static readonly EntityInstance Employed = new EntityInstance { Id = 0, Name = "Employed" };
        private static readonly EntityInstance Unemployed = new EntityInstance { Id = 1, Name = "Unemployed" };
        private static readonly EntityType GenderEntity = new EntityType(GenderIdentifier, GenderIdentifier, "Genders");
        private static readonly EntityType AgeEntity = new EntityType("AgeBand", "AgeBand", "AgeBands");
        private static readonly EntityType EmploymentStatusEntity = new EntityType(EmploymentStatus, EmploymentStatus, EmploymentStatus);
        #endregion

        #region Measures and variables
        private static readonly Measure TargetMeasure = EmploymentStatusEntity.AsSingleEntityMeasure(0, 1);
        private static readonly Measure GenerationMeasure = new Measure
        {
            Name = Generation,
            VarCode = Generation,
            DisplayName = Generation,
            Field = new ResponseFieldDescriptor("Age"),
            FilterValueMapping = "16,17,18,19,20,21,22,23,24:Generation Z|25,26,27,28,29,30,31,32,33,34,35,36,37,38,39:Millennials|40,41,42,43,44,45,46,47,48,49,50,51,52,53,54:Generation X|55,56,57,58,59,60,61,62,63,64,65,66,67,68,69,70,71,72,73:Baby Boomers|74,75,76,77,78,79,80,81,82,83,84,85,86,87,88,89,90,91,92,93,94,95,96,97,98,99,100:Silent Generation"
        };

        private static readonly Measure AgeBandsMapped = new Measure
        {
            Name = MappedAgeBands,
            VarCode = MappedAgeBands,
            DisplayName = MappedAgeBands,
            Field = new ResponseFieldDescriptor(AgeEntity.Identifier, AgeEntity) { ValueEntityIdentifier = AgeEntity.Identifier },
            FilterValueMapping = "0-1:Younger|2:Older",
            LegacyPrimaryTrueValues = new AllowedValues { Values = new[] { 0, 1, 2 } },
        };

        private static readonly Measure MeasureBasedOnSingleChoiceQuestion = new Measure
        {
            Name = MeasureBasedOnSingleChoiceQuestionName,
            VarCode = MeasureBasedOnSingleChoiceQuestionName,
            GenerationType = AutoGenerationType.CreatedFromField
        };


        private static readonly Measure MeasureBasedOnMultiChoiceQuestion = new Measure
        {
            Name = MeasureBasedOnMultiChoiceQuestionName,
            VarCode = MeasureBasedOnMultiChoiceQuestionName,
            GenerationType = AutoGenerationType.CreatedFromField
        };

        private static readonly Measure MeasureBasedOnMeasureBasedOnSingleChoiceQuestion = new Measure
        {
            Name = MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName,
            VarCode = MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName,
            DisplayName = MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName,
            GenerationType = AutoGenerationType.Original,
            VariableConfigurationId = 246
        };

        private static readonly VariableConfiguration SingleChoiceVariable = new VariableConfiguration
        {
            Id = MeasureBasedOnMeasureBasedOnSingleChoiceQuestion.VariableConfigurationId.Value,
            Identifier = MeasureBasedOnMeasureBasedOnSingleChoiceQuestion.VarCode,
            Definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = "test",
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceName = "test",
                        ToEntityInstanceId = 1,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {1, 4},
                            FromVariableIdentifier = MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName,
                            FromEntityTypeName = "test"
                        }
                    }
                }
            }
        };


        private static readonly Measure MeasureBasedOnMeasureBasedOnMultiChoiceQuestion = new Measure
        {
            Name = MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName,
            VarCode = MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName,
            DisplayName = MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName,
            GenerationType = AutoGenerationType.Original,
            VariableConfigurationId = 245
        };

        private static readonly VariableConfiguration MultiChoiceVariable = new VariableConfiguration
        {
            Id = MeasureBasedOnMeasureBasedOnMultiChoiceQuestion.VariableConfigurationId.Value,
            Identifier = MeasureBasedOnMeasureBasedOnMultiChoiceQuestion.VarCode,
            Definition = new GroupedVariableDefinition
            {
                ToEntityTypeName = "test",
                Groups = new List<VariableGrouping>
                {
                    new()
                    {
                        ToEntityInstanceName = "test",
                        ToEntityInstanceId = 1,
                        Component = new InstanceListVariableComponent()
                        {
                            InstanceIds = new List<int> {1, 4},
                            FromVariableIdentifier = MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName,
                            FromEntityTypeName = "test"
                        }
                    }
                }
            }
        };

        #endregion

        private readonly CrosstabCategory[] _defaultCategories = { new CrosstabCategory { Id = CrosstabResultsProvider.EntityInstanceColumn, Name = TargetMeasure.Name }, new CrosstabCategory { Id = CrosstabResultsProvider.TotalScoreColumn, Name = CrosstabResultsProvider.TotalScoreColumn } };
        private static readonly Subset Subset = new Subset { Id = "All" };

        private readonly Measure[] _measures = {
            GenderEntity.AsSingleEntityMeasure(0, 1),
            AgeEntity.AsSingleEntityMeasure(0, 1, 2),
            TargetMeasure,
            GenerationMeasure,
            AgeBandsMapped,
            MeasureBasedOnMultiChoiceQuestion,
            MeasureBasedOnMeasureBasedOnMultiChoiceQuestion,
            MeasureBasedOnSingleChoiceQuestion,
            MeasureBasedOnMeasureBasedOnSingleChoiceQuestion
        };

        private readonly VariableConfiguration[] _variables = { MultiChoiceVariable, SingleChoiceVariable };

        private static readonly Dictionary<string, IReadOnlyCollection<EntityInstance>> EntityInstancesByType = new Dictionary<string, IReadOnlyCollection<EntityInstance>>
        {
            {GenderEntity.Identifier, new[] {Female, Male}},
            {AgeEntity.Identifier, new[] {AgeBand1, AgeBand2, AgeBand3}},
            {EmploymentStatusEntity.Identifier, new[] {Employed, Unemployed}},
        };

        private static readonly Dictionary<string, EntityType> EntityTypesByName = new Dictionary<string, EntityType>
        {
            {GenderEntity.Identifier, GenderEntity},
            {AgeEntity.Identifier, AgeEntity},
            {EmploymentStatusEntity.Identifier, EmploymentStatusEntity},
        };

        private static readonly WeightedDailyResult DummyResult = new WeightedDailyResult(DateTimeOffset.Parse("2020-01-31")) { UnweightedSampleSize = 100, WeightedResult = 0.5 };
        private static CrosstabCategoryEqualityComparer CategoryEqualityComparer => new CrosstabCategoryEqualityComparer();
        #endregion

        [SetUp]
        public void ConstructResultsProviderWithMetrics()
        {
            CancellationToken cancellationToken = CancellationToken.None;
            TestContext.AddFormatter<CrosstabCategory>(c => JsonConvert.SerializeObject(c, Formatting.Indented));

            _subsetRepository = Substitute.For<ISubsetRepository>();
            _subsetRepository.Get(Arg.Any<string>()).Returns(Subset);

            _measureRepository = MeasureRepository(_measures);
            _entityRepository = EntityRepository(EntityInstancesByType);
            _requestAdapter = RequestAdapterForMeasure(TargetMeasure);
            _convenientCalculator = SingleResultConvenientCalculator(DummyResult, cancellationToken);
            _responseEntityTypeRepository = ResponseEntityTypeRepository();
            _baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            _questionTypeLookupRepository = Substitute.For<IQuestionTypeLookupRepository>();
            var mockedLookup = new Dictionary<string, MainQuestionType>
            {
                { TargetMeasure.Name, MainQuestionType.SingleChoice },
                { GenerationMeasure.Name, MainQuestionType.SingleChoice },
                { AgeBandsMapped.Name, MainQuestionType.SingleChoice },
                { MeasureBasedOnMultiChoiceQuestionName, MainQuestionType.MultipleChoice },
                { MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName, MainQuestionType.MultipleChoice },
                { MeasureBasedOnSingleChoiceQuestionName, MainQuestionType.SingleChoice },
                { MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName, MainQuestionType.SingleChoice }
            };
            _questionTypeLookupRepository.GetForSubset(Arg.Any<Subset>()).Returns(mockedLookup);
            _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(Arg.Any<Measure>(), Arg.Any<BaseExpressionDefinition>())
                .Returns(args => (Measure)args[0]);
            var resultsProvider = Substitute.For<IResultsProvider>();
            _filterRepository = Substitute.For<IFilterRepository>();
            var brandVueSettings = Substitute.For<IBrandVueDataLoaderSettings>();
            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();
            variableConfigurationRepository.Get(Arg.Any<int>()).Returns(args => _variables.Single(v => v.Id == args.Arg<int>()));
            variableConfigurationRepository.GetByIdentifier(Arg.Any<string>()).Returns(args => _variables.Single(v => v.Identifier == args.Arg<string>()));

            _crosstabResultsProvider = new CrosstabResultsProvider(
                _subsetRepository,
                _measureRepository,
                _entityRepository,
                _requestAdapter,
                _convenientCalculator,
                _responseEntityTypeRepository,
                _baseExpressionGenerator,
                resultsProvider,
                new AppSettings(),
                _questionTypeLookupRepository,
                brandVueSettings,
                variableConfigurationRepository,
                Substitute.For<IVariableManager>());
        }

        [TestCase(MeasureBasedOnMultiChoiceQuestionName)]
        [TestCase(MeasureBasedOnMeasureBasedOnMultiChoiceQuestionName)]
        public async Task ShouldDetermineThatMeasuresBasedOnMultiChoiceQuestionsAreValidForAverageMentionCalculations(string metricName)
        {
            var model = new CrosstabRequestModel(metricName,
                Subset.Id,
                new EntityInstanceRequest("", Array.Empty<int>()),
                Array.Empty<EntityInstanceRequest>(),
                new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(),
                new CompositeFilterModel(),
                new CrosstabRequestOptions());
                
            var isValid = _crosstabResultsProvider.IsValidMeasureForAverageMentions(model, Subset);
            Assert.That(isValid, Is.True);
        }

        [TestCase(MeasureBasedOnSingleChoiceQuestionName)]
        [TestCase(MeasureBasedOnMeasureBasedOnSingleChoiceQuestionName)]
        public async Task ShouldDetermineThatMeasuresBasedOnMultiChoiceQuestionsAreInvalidForAverageMentionCalculations(string metricName)
        {
            var model = new CrosstabRequestModel(metricName,
                Subset.Id,
                new EntityInstanceRequest("", Array.Empty<int>()),
                Array.Empty<EntityInstanceRequest>(),
                new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(),
                new CompositeFilterModel(),
                new CrosstabRequestOptions());

            var isValid = _crosstabResultsProvider.IsValidMeasureForAverageMentions(model, Subset);
            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task CrosstabWithNoCrossMetricsHasDefaultHeaders()
        {
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());

            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), _defaultCategories);
        }

        [Test]
        public async Task ShouldAddHeadersForEachEntityInstanceInCrossMeasure()
        {
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                new[] { new CrossMeasure { MeasureName = GenderEntity.Identifier } },
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());

            var additionalCategories = new CrosstabCategory
            {
                Id = GenderEntity.Identifier,
                Name = GenderEntity.Identifier,
                SubCategories =
                [
                    new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}", Name = Female.Name},
                    new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}", Name = Male.Name}
                ]
            }.Yield();

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        [Test]
        public async Task AllDataColumnHeadersShouldHaveUniqueIds()
        {
            var crossMeasure = new CrossMeasure { MeasureName = GenderEntity.Identifier };
            var duplicatedCrossMeasures = new[] { crossMeasure, crossMeasure };

            var primaryInstances = new EntityInstanceRequest(Brand, [1]);
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                duplicatedCrossMeasures,
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());


            var additionalCategories = new[]
            {
                new CrosstabCategory
                {
                    Id = GenderEntity.Identifier,
                    Name = GenderEntity.Identifier,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}", Name = Female.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}", Name = Male.Name}
                    }
                },
                new CrosstabCategory
                {
                    Id = GenderEntity.Identifier,
                    Name = GenderEntity.Identifier,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"1{GenderEntity.Identifier}{Female.Name}", Name = Female.Name},
                        new CrosstabCategory {Id = $"1{GenderEntity.Identifier}{Male.Name}", Name = Male.Name}
                    }
                }
            };

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        public CellResult GetPopulatedCellResult()
        {
            return new CellResult
            {
                Count = 100.0,
                Result = 0.5
            };
        }

        public CellResult GetUnPopulatedCellResult()
        {
            return new CellResult
            {
                Count = 0.0,
                Result = 0
            };
        }

        public CrosstabCategory CreateCategory(string id)
        {
            return new CrosstabCategory
            {
                Id = id,
                Name = id
            };
        }


        [Test]
        public async Task ShouldRemoveEmptyColumnsForSingleBreak()
        {
            const string breakId = "breakId";
            const string overall = CrosstabResultsProvider.TotalScoreColumn;
            const string breakEntity1 = "breakEntity1";
            const string breakEntity2 = "breakEntity2";

            var results = new CrosstabResults()
            {
                Categories =
                [
                    CreateCategory(CrosstabResultsProvider.EntityInstanceColumn),
                    CreateCategory(overall),
                    new CrosstabCategory
                    {
                        Id = breakId,
                        Name = breakId,
                        SubCategories =
                        [
                            new CrosstabCategory {Id = $"0{breakId} 1{breakEntity1}", Name=breakEntity1},
                            new CrosstabCategory {Id = $"0{breakId} 1{breakEntity2}", Name=breakEntity2}
                        ]
                    },
                ],
                InstanceResults =
                [
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 1, Identifier="1", Name="1" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity1}", GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity2}", GetUnPopulatedCellResult()}
                        }
                    },
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 2, Identifier="2", Name="2" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity1}", GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity2}", GetUnPopulatedCellResult()}
                        }
                    }
                ]
            };

            _crosstabResultsProvider.RemoveEmptyColumnsFromResults([results]);
            var breakColumn = results.Categories.Single(c => c.Id == breakId);

            Assert.That(breakColumn.SubCategories.Count(), Is.EqualTo(1));
            Assert.That(breakColumn.SubCategories.Single().Id, Is.EqualTo($"0{breakId} 1{breakEntity1}"));
        }

        [Test]
        public async Task ShouldKeepColumnIfNotAllValuesAreZero()
        {
            const string breakId = "breakId";
            const string overall = CrosstabResultsProvider.TotalScoreColumn;
            const string breakEntity1 = "breakEntity1";
            const string breakEntity2 = "breakEntity2";

            var results = new CrosstabResults()
            {
                Categories =
                [
                    CreateCategory(CrosstabResultsProvider.EntityInstanceColumn),
                    CreateCategory(overall),
                    new CrosstabCategory
                    {
                        Id = breakId,
                        Name = breakId,
                        SubCategories =
                        [
                            new CrosstabCategory {Id = $"0{breakId} 1{breakEntity1}", Name=breakEntity1},
                            new CrosstabCategory {Id = $"0{breakId} 1{breakEntity2}", Name=breakEntity2}
                        ]
                    },
                ],
                InstanceResults =
                [
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 1, Identifier="1", Name="1" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity1}", GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity2}", GetPopulatedCellResult()}
                        }
                    },
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 2, Identifier="2", Name="2" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity1}", GetPopulatedCellResult()},
                            {$"0{breakId} 1{breakEntity2}", GetUnPopulatedCellResult()}
                        }
                    }
                ]
            };

            _crosstabResultsProvider.RemoveEmptyColumnsFromResults([results]);
            var breakColumn = results.Categories.Single(c => c.Id == breakId);

            Assert.That(breakColumn.SubCategories.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task ShouldRemoveEmptyColumnsForTwoBreaks()
        {
            const string break1Id = "break1Id";
            const string break2Id = "break2Id";
            const string overall = CrosstabResultsProvider.TotalScoreColumn;
            const string break1Entity1 = "break1Entity1";
            const string break1Entity2 = "break1Entity2";
            const string break2Entity1 = "break2Entity1";
            const string break2Entity2 = "break2Entity2";

            var results = new CrosstabResults()
            {
                Categories =
                [
                    CreateCategory(CrosstabResultsProvider.EntityInstanceColumn),
                    CreateCategory(overall),
                    new CrosstabCategory
                    {
                        Id = break1Id,
                        Name = break1Id,
                        SubCategories =
                        [
                            new CrosstabCategory {Id = $"0{break1Id} 1{break1Entity1}", Name=break1Entity1},
                            new CrosstabCategory {Id = $"0{break1Id} 1{break1Entity2}", Name=break1Entity2}
                        ]
                    },
                    new CrosstabCategory
                    {
                        Id = break2Id,
                        Name = break2Id,
                        SubCategories =
                        [
                            new CrosstabCategory {Id = $"0{break2Id} 1{break2Entity1}", Name=break2Entity1},
                            new CrosstabCategory {Id = $"0{break2Id} 1{break2Entity2}", Name=break2Entity2}
                        ]
                    },

                ],
                InstanceResults =
                [
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 1, Identifier="1", Name="1" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"0{break1Id} 1{break1Entity1}", GetPopulatedCellResult()},
                            {$"0{break1Id} 1{break1Entity2}", GetPopulatedCellResult()},
                            {$"0{break2Id} 1{break1Entity1}", GetPopulatedCellResult()},
                            {$"0{break2Id} 1{break1Entity2}", GetUnPopulatedCellResult()}
                        }
                    },
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 2, Identifier="2", Name="2" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"0{break1Id} 1{break1Entity1}", GetPopulatedCellResult()},
                            {$"0{break1Id} 1{break1Entity2}", GetPopulatedCellResult()},
                            {$"0{break2Id} 1{break1Entity1}", GetPopulatedCellResult()},
                            {$"0{break2Id} 1{break1Entity2}", GetUnPopulatedCellResult()}
                        }
                    }
                ]
            };

            _crosstabResultsProvider.RemoveEmptyColumnsFromResults([results]);
            var firstInstanceResult = results.InstanceResults.First();
            Assert.That(firstInstanceResult.Values.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task ShouldRemoveEmptyColumnsForNestedBreaks()
        {
            const string overall = CrosstabResultsProvider.TotalScoreColumn;
            const string entityInstance1Id = "0are youQSC 2Male";
            const string entityInstance2Id = "0are youQSC 2Prefer to self-describe";
            const string nestedBreakInstance1 = "0encourage use coffeeQBR 8If I could get a reusable coffee cup for free / on sale";
            const string nestedBreakInstance2 = "0encourage use coffeeQBR 8If I found a reusable coffee cup style I like";

            var results = new CrosstabResults()
            {
                Categories =
                [
                    CreateCategory(CrosstabResultsProvider.EntityInstanceColumn),
                    CreateCategory(overall),
                    new CrosstabCategory
                    {
                        Id = entityInstance1Id,
                        Name = "Male",
                        SubCategories =
                        [
                            new CrosstabCategory {Id = $"{entityInstance1Id}{nestedBreakInstance1}", Name="If I could get a reusable coffee cup for free / on sale"},
                            new CrosstabCategory {Id = $"{entityInstance1Id}{nestedBreakInstance2}", Name="If I found a reusable coffee cup style I like"}
                        ]
                    },
                    new CrosstabCategory
                    {
                        Id = entityInstance2Id,
                        Name = "Prefer to self-describe",
                        SubCategories =
                        [
                            new CrosstabCategory {Id = $"{entityInstance2Id}{nestedBreakInstance1}", Name="If I could get a reusable coffee cup for free / on sale"},
                            new CrosstabCategory {Id = $"{entityInstance2Id}{nestedBreakInstance2}", Name="If I found a reusable coffee cup style I like"}
                        ]
                    },

                ],
                InstanceResults =
                [
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 1, Identifier="1", Name="1" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"{entityInstance1Id}{nestedBreakInstance1}", GetPopulatedCellResult()},
                            {$"{entityInstance1Id}{nestedBreakInstance2}", GetPopulatedCellResult()},
                            {$"{entityInstance2Id}{nestedBreakInstance1}", GetUnPopulatedCellResult()},
                            {$"{entityInstance2Id}{nestedBreakInstance2}", GetUnPopulatedCellResult()}
                        }
                    },
                    new InstanceResult()
                    {
                        EntityInstance = new EntityInstance { Id = 2, Identifier="2", Name="2" },
                        Values = new Dictionary<string, CellResult>
                        {
                            {overall, GetPopulatedCellResult()},
                            {$"{entityInstance1Id}{nestedBreakInstance1}", GetPopulatedCellResult()},
                            {$"{entityInstance1Id}{nestedBreakInstance2}", GetPopulatedCellResult()},
                            {$"{entityInstance2Id}{nestedBreakInstance1}", GetUnPopulatedCellResult()},
                            {$"{entityInstance2Id}{nestedBreakInstance2}", GetUnPopulatedCellResult()}
                        }
                    }
                ]
            };

            _crosstabResultsProvider.RemoveEmptyColumnsFromResults([results]);

            var firstInstanceResult = results.InstanceResults.First();
            Assert.That(firstInstanceResult.Values.Count, Is.EqualTo(3));
        }

        [Test]
        public async Task ShouldAddHeadersForEachEntityInstanceInCrossMeasureWithNestedCrossMeasure()
        {
            var crossMeasures = new[] { new CrossMeasure { MeasureName = GenderEntity.Identifier, ChildMeasures = new CrossMeasure { MeasureName = AgeEntity.Identifier }.Yield().ToArray() } };
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                crossMeasures,
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());


            var additionalCategories = new[]
            {
                new CrosstabCategory
                {
                    Id = $"0{GenderEntity.Identifier}{Female.Name}",
                    Name = Female.Name,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand1.Name}", Name = AgeBand1.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand2.Name}", Name = AgeBand2.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand3.Name}", Name = AgeBand3.Name}
                    }
                },
                new CrosstabCategory
                {
                    Id = $"0{GenderEntity.Identifier}{Male.Name}",
                    Name = Male.Name,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand1.Name}", Name = AgeBand1.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand2.Name}", Name = AgeBand2.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand3.Name}", Name = AgeBand3.Name}
                    }
                }
            };

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        [Test]
        public async Task ShouldAddHeadersForEachEntityInstanceInCrossMeasureWithNestedCrossMeasures()
        {
            var crossMeasures = new[] { new CrossMeasure { MeasureName = GenderEntity.Identifier, ChildMeasures = new [] {
                new CrossMeasure { MeasureName = AgeEntity.Identifier },
                new CrossMeasure { MeasureName = EmploymentStatusEntity.Identifier }}
            }};

            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                crossMeasures,
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());

            var additionalCategories = new[]
            {
                new CrosstabCategory
                {
                    Id = $"0{GenderEntity.Identifier}{Female.Name}",
                    Name = Female.Name,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand1.Name}", Name = AgeBand1.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand2.Name}", Name = AgeBand2.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand3.Name}", Name = AgeBand3.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}1{EmploymentStatusEntity.Identifier}{Employed.Name}", Name = Employed.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}1{EmploymentStatusEntity.Identifier}{Unemployed.Name}", Name = Unemployed.Name}
                    }
                },
                new CrosstabCategory
                {
                    Id = $"0{GenderEntity.Identifier}{Male.Name}",
                    Name = Male.Name,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand1.Name}", Name = AgeBand1.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand2.Name}", Name = AgeBand2.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand3.Name}", Name = AgeBand3.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}1{EmploymentStatusEntity.Identifier}{Employed.Name}", Name = Employed.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}1{EmploymentStatusEntity.Identifier}{Unemployed.Name}", Name = Unemployed.Name}
                    }
                }
            };

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        [Test]
        public async Task ShouldAddHeadersForEachEntityInstanceInCrossMeasureWithNestedCrossMeasureAndOneAppendedMeasure()
        {
            var crossMeasures = new[] { new CrossMeasure { MeasureName = GenderEntity.Identifier, ChildMeasures = new CrossMeasure { MeasureName = AgeEntity.Identifier }.Yield().ToArray() }, new CrossMeasure { MeasureName = GenderEntity.Identifier } };
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                crossMeasures,
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());


            var additionalCategories = new[]
            {
                new CrosstabCategory
                {
                    Id = $"0{GenderEntity.Identifier}{Female.Name}",
                    Name = Female.Name,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand1.Name}", Name = AgeBand1.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand2.Name}", Name = AgeBand2.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Female.Name}0{AgeEntity.Identifier}{AgeBand3.Name}", Name = AgeBand3.Name}
                    }
                },
                new CrosstabCategory
                {
                    Id = $"0{GenderEntity.Identifier}{Male.Name}",
                    Name = Male.Name,
                    SubCategories = new[]
                    {
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand1.Name}", Name = AgeBand1.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand2.Name}", Name = AgeBand2.Name},
                        new CrosstabCategory {Id = $"0{GenderEntity.Identifier}{Male.Name}0{AgeEntity.Identifier}{AgeBand3.Name}", Name = AgeBand3.Name}
                    }
                },
                new CrosstabCategory
                {
                    Id = GenderEntity.Identifier,
                    Name = GenderEntity.Identifier,
                    SubCategories = new []
                    {
                        new CrosstabCategory {Id = $"1{GenderEntity.Identifier}{Female.Name}", Name = Female.Name},
                        new CrosstabCategory {Id = $"1{GenderEntity.Identifier}{Male.Name}", Name = Male.Name}
                    }
                }
            };

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        [Test]
        public void ShouldCalculateIndexScoresCorrectly()
        {
            // Arrange - create categories: EntityInstance, Total, then one break with two leaf categories
            var categories = new List<CrosstabCategory>
            {
                CreateCategory(CrosstabResultsProvider.EntityInstanceColumn),
                CreateCategory(CrosstabResultsProvider.TotalScoreColumn),
                new CrosstabCategory
                {
                    Id = "break1",
                    Name = "break1",
                    SubCategories = new[]
                    {
                        new CrosstabCategory { Id = "c1", Name = "c1" },
                        new CrosstabCategory { Id = "c2", Name = "c2" }
                    }
                }
            };

            // Female: total 0.5, c1 0.3, c2 0.2 => indexes 60 and 40
            var femaleDict = new Dictionary<string, WeightedDailyResult>
            {
                { CrosstabResultsProvider.TotalScoreColumn, new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.5, UnweightedSampleSize = 10 } },
                { "c1", new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.3, UnweightedSampleSize = 5 } },
                { "c2", new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.2, UnweightedSampleSize = 5 } }
            };

            // Male: total 0.4, c1 0.1, c2 0.3 => indexes 25 and 75
            var maleDict = new Dictionary<string, WeightedDailyResult>
            {
                { CrosstabResultsProvider.TotalScoreColumn, new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.4, UnweightedSampleSize = 8 } },
                { "c1", new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.1, UnweightedSampleSize = 2 } },
                { "c2", new WeightedDailyResult(DateTimeOffset.Now) { WeightedResult = 0.3, UnweightedSampleSize = 6 } }
            };

            var cells = new[]
            {
                (Female, femaleDict),
                (Male, maleDict)
            };

            // Act - call CalculateIndexScores directly
            var result = _crosstabResultsProvider.CalculateIndexScores(TargetMeasure, categories, cells);

            // Assert
            Assert.That(result.ContainsKey(Female));
            Assert.That(result.ContainsKey(Male));

            var femaleIndexes = result[Female];
            Assert.That(femaleIndexes["c1"], Is.EqualTo(60));
            Assert.That(femaleIndexes["c2"], Is.EqualTo(40));

            var maleIndexes = result[Male];
            Assert.That(maleIndexes["c1"], Is.EqualTo(25));
            Assert.That(maleIndexes["c2"], Is.EqualTo(75));
        }

        [Test]
        public async Task ShouldAddHeadersForEachFilterMapping_ZeroEntity()
        {
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                new[] { new CrossMeasure { MeasureName = GenerationMeasure.Name } },
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());

            var additionalCategories = new CrosstabCategory
            {
                Id = GenerationMeasure.Name,
                Name = GenerationMeasure.Name,
                SubCategories = new[]
                {
                    new CrosstabCategory {Id = $"0{GenerationMeasure.Name}Generation Z", Name = "Generation Z"},
                    new CrosstabCategory {Id = $"0{GenerationMeasure.Name}Millennials", Name = "Millennials"},
                    new CrosstabCategory {Id = $"0{GenerationMeasure.Name}Generation X", Name = "Generation X"},
                    new CrosstabCategory {Id = $"0{GenerationMeasure.Name}Baby Boomers", Name = "Baby Boomers"},
                    new CrosstabCategory {Id = $"0{GenerationMeasure.Name}Silent Generation", Name = "Silent Generation"},
                }
            }.Yield();

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        [Test]
        public async Task ShouldAddHeadersForEachFilterMapping_SingleEntity()
        {
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                [new CrossMeasure { MeasureName = AgeBandsMapped.Name }],
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());


            var additionalCategories = new CrosstabCategory
            {
                Id = AgeBandsMapped.Name,
                Name = AgeBandsMapped.Name,
                SubCategories =
                [
                    new CrosstabCategory {Id = $"0{AgeBandsMapped.Name}Younger", Name = "Younger"},
                    new CrosstabCategory {Id = $"0{AgeBandsMapped.Name}Older", Name = "Older"},
                ]
            }.Yield();

            var expectedCategories = _defaultCategories.Concat(additionalCategories).ToArray();
            CommonAssert((await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single(), expectedCategories);
        }

        [TestCase(CalculationType.Average)]
        [TestCase(CalculationType.YesNo)]
        public async Task EntityResultsShouldContainCountValues(CalculationType calculationType)
        {
            OverrideOneTimeSetup(calculationType);
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions { CalculateSignificance = false, SignificanceType = CrosstabSignificanceType.CompareToTotal, IsDataWeighted = false, SigConfidenceLevel = SigConfidenceLevel.NinetyFive });

            var results = (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
            Assert.That(results.InstanceResults.First().Values.Values.First().Count, Is.Not.Null);
        }

        [TestCase(CalculationType.NetPromoterScore)]
        [TestCase(CalculationType.Special_ShouldNotBeUsed)]
        [TestCase(CalculationType.Text)]
        public async Task EntityResultsShouldNotContainCountValues(CalculationType calculationType)
        {
            OverrideOneTimeSetup(calculationType);
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions());
            var results = (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
            Assert.That(results.InstanceResults.First().Values.Values.First().Count, Is.Null);
        }

        [Test]
        public async Task EntityResultsShouldNotCalculateSignificanceForOverallColumn()
        {
            DummyResult.Significance = null;

            OverrideOneTimeSetup(CalculationType.YesNo);
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions { CalculateSignificance = true, SignificanceType = CrosstabSignificanceType.CompareToTotal, IsDataWeighted = false, SigConfidenceLevel = SigConfidenceLevel.NinetyFive });

            var results = (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
            Assert.That(results.InstanceResults.First().Values.Values.First().Significance, Is.Null);
        }

        [Test]
        public async Task EntityResultsShouldCalculateSignificanceIfMoreThanOneColumn()
        {
            DummyResult.Significance = null;
            OverrideOneTimeSetup(CalculationType.YesNo);
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                new[] { new CrossMeasure { MeasureName = AgeBandsMapped.Name } },
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions { CalculateSignificance = true, SignificanceType = CrosstabSignificanceType.CompareToTotal, IsDataWeighted = false, SigConfidenceLevel = SigConfidenceLevel.NinetyFive });

            var results = (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
            Assert.That(results.InstanceResults.First().Values.Values.Last().Significance, Is.Not.Null);
        }

        /// <summary>
        /// Regression sc60018
        /// </summary>
        [Test]
        public async Task EntityResultsShouldExcludeWhereEntityIsInvalid()
        {
            DummyResult.Significance = null;
            OverrideOneTimeSetup(CalculationType.YesNo, setMeasureBaseExpression: true);
            var primaryInstances = new EntityInstanceRequest(Brand, [99]);
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                [new CrossMeasure { MeasureName = AgeBandsMapped.Name }],
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions { CalculateSignificance = true, SignificanceType = CrosstabSignificanceType.CompareToTotal, IsDataWeighted = false, SigConfidenceLevel = SigConfidenceLevel.NinetyFive });
            var results = (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();
            Assert.That(results.InstanceResults.First().Values.Values.Last().Count == 0);
        }

        [Test]
        public async Task GetCrosstabResults_NameIsEqualToDisplayName()
        {
            // Arrange
            var primaryInstances = new EntityInstanceRequest(Brand, new[] { 1 });
            var measure = EmploymentStatusEntity.AsSingleEntityMeasureWithOriginalMeasureName(0, 1);
            var customDisplayName = "Custom display name";
            measure.DisplayName = customDisplayName;
            _measureRepository.Get(Arg.Any<string>()).Returns(measure);
            var model = new CrosstabRequestModel(
                TargetMeasure.Name,
                Subset.Id,
                primaryInstances,
                Array.Empty<EntityInstanceRequest>(),
                period: new Period(),
                Array.Empty<CrossMeasure>(),
                -1,
                new DemographicFilter(_filterRepository),
                new CompositeFilterModel(),
                options: new CrosstabRequestOptions { CalculateSignificance = false, SignificanceType = CrosstabSignificanceType.CompareToTotal, IsDataWeighted = false, SigConfidenceLevel = SigConfidenceLevel.NinetyFive });


            // Act
            var results = (await _crosstabResultsProvider.GetCrosstabResults(model, CancellationToken.None)).Single();

            // Assert
            Assert.That(results.Categories.Take(1).Single().Name, Is.EqualTo(customDisplayName));
        }

        #region HelperMethods
        private void OverrideOneTimeSetup(CalculationType calculationType, bool setMeasureBaseExpression = false)
        {
            TestContext.AddFormatter<CrosstabCategory>(c => JsonConvert.SerializeObject(c, Formatting.Indented));
            _subsetRepository = Substitute.For<ISubsetRepository>();
            var responseEntityTypeRepository = ResponseEntityTypeRepository();
            _subsetRepository.Get(Arg.Any<string>()).Returns(Subset);

            var measures = new Measure[]
            {
                GenderEntity.AsSingleEntityMeasure(0, 1),
                AgeEntity.AsSingleEntityMeasure(0, 1, 2),
                TargetMeasure,
                GenerationMeasure,
                AgeBandsMapped
            };
            var filterExpression =
                TestFieldExpressionParser.PrePopulateForFields(new ResponseFieldManager(responseEntityTypeRepository), Substitute.For<IEntityRepository>(), responseEntityTypeRepository).ParseUserBooleanExpression("");

            //manually override for the sake of the testing count values
            foreach (var measure in measures)
            {
                measure.CalculationType = calculationType;
                measure.BaseExpression = filterExpression;
            }

            DummyResult.Significance = null;

            _convenientCalculator = SingleResultConvenientCalculator(DummyResult, CancellationToken.None);

            var baseExpressionGenerator = Substitute.For<IBaseExpressionGenerator>();
            baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(Arg.Any<Measure>(), Arg.Any<BaseExpressionDefinition>())
                .Returns(args => (Measure)args[0]);
            var resultsProvider = Substitute.For<IResultsProvider>();
            var questionLookupRepository = Substitute.For<IQuestionTypeLookupRepository>();
            var brandVueSettings = Substitute.For<IBrandVueDataLoaderSettings>();
            var variableConfigurationRepository = Substitute.For<IVariableConfigurationRepository>();

            _crosstabResultsProvider = new CrosstabResultsProvider(
                _subsetRepository,
                _measureRepository,
                _entityRepository,
                _requestAdapter,
                _convenientCalculator,
                responseEntityTypeRepository,
                baseExpressionGenerator,
                resultsProvider,
                new AppSettings(),
                questionLookupRepository,
                brandVueSettings,
                variableConfigurationRepository,
                Substitute.For<IVariableManager>());
        }

        private static void CommonAssert(CrosstabResults crosstabResults, CrosstabCategory[] expectedCategories)
        {
            var bottomMostCategories = new CrosstabCategory { SubCategories = crosstabResults.Categories.ToArray() } //Dummy category to Follow
                .FollowMany(c => c.SubCategories)
                .Where(ca => !ca.SubCategories.Any())
                .Skip(1) //Results do not contain entity instance
                .Select(c => c.Id);

            Assert.That(crosstabResults.Categories, Is.EqualTo(expectedCategories).AsCollection.Using(CategoryEqualityComparer));
            Assert.That(crosstabResults.InstanceResults.Select(r => r.Values.Select(v => v.Key)), Has.All.EquivalentTo(bottomMostCategories));
        }

        private static IResponseEntityTypeRepository ResponseEntityTypeRepository()
        {
            var responseEntityTypeRepository = Substitute.For<IResponseEntityTypeRepository>();
            responseEntityTypeRepository.Get(Arg.Any<string>())
                .Returns(args => EntityTypesByName[args.Arg<string>()]);
            return responseEntityTypeRepository;
        }

        private static IConvenientCalculator SingleResultConvenientCalculator(WeightedDailyResult resultForMeasure,
            CancellationToken cancellationToken)
        {
            var convenientCalculator = Substitute.For<IConvenientCalculator>();
            convenientCalculator
                .GetCuratedResultsForAllMeasures(Arg.Any<ResultsProviderParameters>(), cancellationToken)
                .Returns(new ResultsForMeasure
                {
                    Measure = TargetMeasure,
                    NumberFormat = TargetMeasure.NumberFormat,
                    Data = new EntityWeightedDailyResults(Employed,
                        new List<WeightedDailyResult>(resultForMeasure.Yield())).Yield().ToArray()
                }.Yield().ToArray());
            return convenientCalculator;
        }

        private static IEntityRepository EntityRepository(IReadOnlyDictionary<string, IReadOnlyCollection<EntityInstance>> entityInstancesByType)
        {
            var entityRepository = Substitute.For<IEntityRepository>();
            entityRepository.GetInstancesOf(Arg.Any<string>(), Arg.Any<Subset>()).Returns(args => entityInstancesByType[args.Arg<string>()]);
            entityRepository.GetInstances(Arg.Any<string>(), Arg.Any<IEnumerable<int>>(), Subset).Returns(args => entityInstancesByType[args.Arg<string>()]);
            entityRepository.TryGetInstance(Arg.Any<Subset>(), Arg.Any<string>(), Arg.Any<int>(), out _).Returns(args =>
            {
                args[2] = entityInstancesByType[args.Arg<string>()].First(a => a.Id == args.Arg<int>());
                return true;
            });

            //.Returns(args => );
            return entityRepository;
        }

        private static IMeasureRepository MeasureRepository(params Measure[] measures)
        {
            var measureRepository = Substitute.For<IMeasureRepository>();
            measureRepository.Get(Arg.Any<string>()).Returns(args => measures.Single(m => m.Name == args.Arg<string>()));
            measureRepository.GetAll().Returns(measures);
            return measureRepository;
        }

        private class CrosstabCategoryEqualityComparer : IEqualityComparer<CrosstabCategory>
        {
            public bool Equals(CrosstabCategory x, CrosstabCategory y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Id == y.Id && x.Name == y.Name && x.SubCategories.SequenceEqual(y.SubCategories, this);
            }

            public int GetHashCode(CrosstabCategory obj)
            {
                return 0;
            }
        }

        private static IRequestAdapter RequestAdapterForMeasure(Measure targetMeasure)
        {
            var requestAdapter = Substitute.For<IRequestAdapter>();
            requestAdapter
                .CreateParametersForCalculation(Arg.Any<CrosstabRequestModel>(), Arg.Any<Measure>(), Arg.Any<TargetInstances>(), Arg.Any<CompositeFilterModel>(),
                    Arg.Any<TargetInstances[]>(), Arg.Any<bool>())
                .Returns(new ResultsProviderParameters { PrimaryMeasure = targetMeasure, Subset = Subset, CalculationPeriod = new CalculationPeriod(DateTimeOffset.Parse("2020-01-01"), DateTimeOffset.Parse("2020-01-31")) });
            return requestAdapter;
        }
        #endregion
    }

    internal static class FieldExtensions
    {
        private const string OriginalMetric = "OriginalMetric";
        public static Measure AsSingleEntityMeasure(this EntityType entityType, params int[] trueVals)
        {
            return new()
            {
                Name = entityType.Identifier,
                VarCode = entityType.Identifier,
                DisplayName = entityType.Identifier,
                Field = new ResponseFieldDescriptor(entityType.Identifier, entityType) { ValueEntityIdentifier = entityType.Identifier },
                LegacyPrimaryTrueValues = new AllowedValues { Values = trueVals }
            };
        }

        public static Measure AsSingleEntityMeasureWithOriginalMeasureName(this EntityType entityType, params int[] trueVals)
        {
            string originalMetricName = entityType.Identifier + " " + OriginalMetric;
            return new()
            {
                Name = entityType.Identifier,
                VarCode = entityType.Identifier,
                DisplayName = entityType.Identifier,
                Field = new ResponseFieldDescriptor(entityType.Identifier, entityType) { ValueEntityIdentifier = entityType.Identifier },
                LegacyPrimaryTrueValues = new AllowedValues { Values = trueVals },
                OriginalMetricName = originalMetricName
            };
        }
    }
}

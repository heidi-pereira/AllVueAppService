using BrandVue.EntityFramework;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.Averages;
using BrandVue.EntityFramework.MetaData.Breaks;
using BrandVue.EntityFramework.MetaData.Reports;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.Models;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.CalculationPipeline;
using BrandVue.SourceData.Entity;
using BrandVue.SourceData.Filters;
using BrandVue.SourceData.Import;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.QuotaCells;
using BrandVue.SourceData.Subsets;
using BrandVue.SourceData.Variable;
using System.Threading;

namespace BrandVue.Services
{
    public class CrosstabResultsProvider : ICrosstabResultsProvider
    {
        private readonly ISubsetRepository _subsetRepository;
        private readonly IMeasureRepository _measureRepository;
        private readonly IEntityRepository _entityRepository;
        private readonly IRequestAdapter _requestAdapter;
        private readonly IConvenientCalculator _convenientCalculator;
        private readonly IResponseEntityTypeRepository _responseEntityTypeRepository;
        private readonly IBaseExpressionGenerator _baseExpressionGenerator;
        private readonly CrosstabFilterModelFactory _crosstabFilterModelFactory;
        private readonly IResultsProvider _resultsProvider;
        private readonly AppSettings _appSettings;
        private readonly IBrandVueDataLoaderSettings _brandVueDataLoaderSettings;
        private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IVariableManager _variableManager;
        public const string TotalScoreColumn = "Total";
        public const string EntityInstanceColumn = "EntityInstance";

        public CrosstabResultsProvider(
            ISubsetRepository subsetRepository,
            IMeasureRepository measureRepository,
            IEntityRepository entityRepository,
            IRequestAdapter requestAdapter,
            IConvenientCalculator convenientCalculator,
            IResponseEntityTypeRepository responseEntityTypeRepository,
            IBaseExpressionGenerator baseExpressionGenerator,
            IResultsProvider resultsProvider,
            AppSettings appSettings,
            IQuestionTypeLookupRepository questionTypeLookupRepository,
            IBrandVueDataLoaderSettings brandVueDataLoaderSettings,
            IVariableConfigurationRepository variableConfigurationRepository,
            IVariableManager variableManager)
        {
            _subsetRepository = subsetRepository;
            _measureRepository = measureRepository;
            _entityRepository = entityRepository;
            _convenientCalculator = convenientCalculator;
            _responseEntityTypeRepository = responseEntityTypeRepository;
            _requestAdapter = requestAdapter;
            _baseExpressionGenerator = baseExpressionGenerator;
            _crosstabFilterModelFactory = new CrosstabFilterModelFactory(_entityRepository, questionTypeLookupRepository);
            _resultsProvider = resultsProvider;
            _appSettings = appSettings;
            _brandVueDataLoaderSettings = brandVueDataLoaderSettings;
            _questionTypeLookupRepository = questionTypeLookupRepository;
            _variableConfigurationRepository = variableConfigurationRepository;
            _variableManager = variableManager;
        }

        public async Task<CrosstabResults[]> GetCrosstabResults(CrosstabRequestModel model,
            CancellationToken cancellationToken)
        {
            CrosstabResults[] results;
            var primaryMeasure = _measureRepository.Get(model.PrimaryMeasureName);
            primaryMeasure = _baseExpressionGenerator.GetMeasureWithOverriddenBaseExpression(primaryMeasure, model.BaseExpressionOverride);

            if (_measureRepository.RequiresLegacyBreakCalculation(_appSettings, model.CrossMeasures))
            {
                var legacyCalculationParameters = CreateParametersForCrosstabsLegacy(model, TotalScoreColumn, primaryMeasure);
                results = await legacyCalculationParameters.ToAsyncEnumerable().SelectAwait(async parameters =>
                    await CalculateResultsLegacy(primaryMeasure,
                        parameters.Parameters,
                        CreateCategoriesForCrosstabs(model.CrossMeasures, model.SubsetId, parameters.FilterInstancesDescription),
                        model.Options,
                        cancellationToken)
                ).ToArrayAsync(cancellationToken);
            }
            else
            {
                var calculationParameters = CreateMultiEntityParametersForCrosstabs(model, primaryMeasure);

                results = await calculationParameters.ToAsyncEnumerable().SelectAwait(async parameters =>
                    await CalculateResults(primaryMeasure,
                        parameters.CalculationParameters,
                        CreateCategoriesForCrosstabs(model.CrossMeasures, model.SubsetId, parameters.FilterInstancesDescription),
                        model.Options,
                        cancellationToken)
                ).ToArrayAsync(cancellationToken);
            }

            if (model.Options.HideEmptyColumns)
            {
                RemoveEmptyColumnsFromResults(results);
            }

            if (primaryMeasure.EntityCombination.Count() == 2)
            {
                if (model.CrossMeasures.Length == 0)
                {
                    return [await CombineCrosstabResults(results, model)];
                }
                else if (model.Options.ShowMultipleTablesAsSingle)
                {
                    return [await CombineCrosstabResultsWithBreaks(results, model)];
                }
            }

            return results;
        }

        public async Task<CrosstabulatedResults[]> ExperimentalCrosstabResults(TemporaryVariableRequestModel model, CancellationToken cancellationToken)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var breaks = new Break[model.Breaks.Length];
            var startInstanceIndex = 0;
            for (int breakIndex = 0; breakIndex < model.Breaks.Length; breakIndex++)
            {
                var variableInstance = model.Breaks[breakIndex];
                var measure = _variableManager.ConstructTemporaryVariableMeasure(variableInstance.Definition);
                if (measure.EntityCombination.Count() > 1)
                {
                    throw new InvalidOperationException("Multi-entity not supported currently");
                }

                var baseInstances = measure.BaseExpression.UserEntityCombination.Any() ?
                    _entityRepository.GetInstancesOf(measure.BaseExpression.UserEntityCombination.Single().Identifier, subset).Select(x => x.Id).ToArray() :
                    [];
                var @break = new Break(
                    measure.PrimaryVariable,
                    measure.BaseExpression,
                    variableInstance.Definition.Groups.Select(g => g.ToEntityInstanceId).ToArray(),
                    baseInstances,
                    [],
                    startInstanceIndex);
                breaks[breakIndex] = @break;
                startInstanceIndex += @break.Instances.Length;
            }
            var numberOfBreakColumns = breaks.Sum(b => b.Instances.Length);
            var results = model.Rows.Select(async variableInstance =>
            {
                var measure = _variableManager.ConstructTemporaryVariableMeasure(variableInstance.Definition);
                var filterByEntityTypes = variableInstance.FilterBy.Select(f => f.Type).ToArray();

                var entityType = measure.EntityCombination.Single(entityType => !filterByEntityTypes.Contains(entityType.Identifier));
                var instances = variableInstance.Definition.Groups.Select(group => new EntityInstance {
                    Id = group.ToEntityInstanceId,
                    Name = group.ToEntityInstanceName
                }).ToArray();
                var targetInstances = new TargetInstances(entityType, instances);
                var filterInstances = variableInstance.FilterBy.Select(f =>
                {
                    var filterByType = measure.EntityCombination.Single(type => type.Identifier == f.Type);
                    var filterInstances = _entityRepository.GetInstances(f.Type, f.EntityInstanceIds, subset);
                    return new TargetInstances(filterByType, filterInstances);
                }).ToArray();

                var parameters = _requestAdapter.CreateParametersForCalculation(
                    model,
                    measure,
                    targetInstances,
                    model.FilterModel,
                    filterInstances,
                    breaks);

                var data = (await _convenientCalculator.CoalesceSingleDataPointPerEntityMeasure(parameters, cancellationToken))
                    .Single().Data.Select(x =>
                    {
                        var weightedDailyResults = x.WeightedResult.RootAndLeaves().ToArray();
                        if (weightedDailyResults.Length != numberOfBreakColumns + 1)
                        {
                            throw new InvalidOperationException("Missing break results");
                        }

                        var totalResult = weightedDailyResults[0];
                        foreach (var result in weightedDailyResults.Skip(1))
                        {
                            PipelineResultsProvider.MutateResultToIncludeSignificance(measure, result, totalResult, "Total", SigConfidenceLevel.NinetyFive);
                        }

                        return new EntityWeightedDailyResults(x.EntityInstance, weightedDailyResults);
                    }).ToArray();

                var totalColumnOnlyForSample = data.Select(x =>
                    new EntityWeightedDailyResults(x.EntityInstance, [x.WeightedDailyResults[0]])
                ).ToArray();

                return new CrosstabulatedResults
                {
                    Data = data,
                    SampleSizeMetadata = totalColumnOnlyForSample.GetSampleSizeMetadata(),
                    HasData = data.HasData()
                };
            });
            return await Task.WhenAll(results);
        }

        private async Task<CrosstabResults> CombineCrosstabResults(CrosstabResults[] results, CrosstabRequestModel model)
        {
            var primaryEntityType = _responseEntityTypeRepository.Get(model.PrimaryInstances.Type);
            var primaryEntityInstances = results.First().InstanceResults.Select(r => r.EntityInstance).ToArray();
            var filterByEntityType = _responseEntityTypeRepository.Get(model.FilterInstances.First().Type);
            var filterByEntityTypeName = filterByEntityType.DisplayNameSingular;
            var filterByEntityInstances = results.Select(r => r.Categories.Single(c => c.Id == EntityInstanceColumn));

            var filterByEntitySubCategories = filterByEntityInstances.Select(i => new CrosstabCategory() 
            {
                Id = $"{filterByEntityTypeName}{i.Name}",
                Name = i.Name
            });

            var categories = new List<CrosstabCategory>
            {
                new() { Id = EntityInstanceColumn, Name = primaryEntityType.DisplayNameSingular },
                new() { Id = filterByEntityType.Identifier, Name = filterByEntityTypeName, SubCategories = filterByEntitySubCategories.ToArray() }
            };

            var combinedInstanceResults = primaryEntityInstances.Select(instance =>
            {
                var resultsAcrossFilterByEntity = results
                    .Select(r => (
                        Name: filterByEntityTypeName + r.Categories.Single(c => c.Id == EntityInstanceColumn).Name,
                        Result: r.InstanceResults
                            .Single(ir => ir.EntityInstance.Id == instance.Id)
                            .Values.Values
                            .SingleOrDefault()
                            )

                    )
                    .ToArray();

                var cellResults = resultsAcrossFilterByEntity
                    .Select(r => r.Result)
                    .ToArray();

                var resultsOverall = CalculateOverallFromResults(cellResults);
                var resultsCombined = resultsAcrossFilterByEntity.Prepend(resultsOverall).ToDictionary(v => v.Name, v => v.Result);
                return new InstanceResult
                {
                    EntityInstance = instance,
                    Values = resultsCombined
                };
            }).ToArray();

            return new CrosstabResults
            {
                Categories = categories,
                InstanceResults = combinedInstanceResults,
                SampleSizeMetadata = combinedInstanceResults.GetSampleSizeMetadata()
            };
        }

        private async Task<CrosstabResults> CombineCrosstabResultsWithBreaks(CrosstabResults[] results, CrosstabRequestModel model)
        {
            var primaryEntityType = _responseEntityTypeRepository.Get(model.PrimaryInstances.Type);
            var primaryEntityInstances = results.First().InstanceResults.Select(r => r.EntityInstance).ToArray();

            var categories = results
                .Select(CreateNestedCategoryForCombinedTable)
                .Prepend(new() { Id = EntityInstanceColumn, Name = primaryEntityType.DisplayNameSingular })
                .ToArray();

            var combinedInstanceResults = primaryEntityInstances.Select(instance =>
            {
                var resultsAcrossEntities = results
                    .Select(r => r.InstanceResults.Single(ir => ir.EntityInstance.Id == instance.Id))
                    .ToArray();

                var totalsAcrossEntities = resultsAcrossEntities
                    .Select(r => r.Values[TotalScoreColumn])
                    .ToArray();

                var resultsCombined = resultsAcrossEntities
                    .SelectMany((r, index) => r.Values.Select(kvp =>
                        new KeyValuePair<string, CellResult>(AddNestedTablePrefix(kvp.Key, index), kvp.Value)
                    )).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                //we need a Total column to be used for sorting etc, even though it won't actually be displayed
                resultsCombined[TotalScoreColumn] = new CellResult
                {
                    Result = totalsAcrossEntities.Sum(r => r.Result),
                    Count = totalsAcrossEntities.Sum(r => r.Count),
                    SampleForCount = totalsAcrossEntities.Sum(r => r.SampleForCount),
                    SampleSizeMetaData = totalsAcrossEntities.First().SampleSizeMetaData,
                    SignificantColumns = []
                };

                return new InstanceResult
                {
                    EntityInstance = instance,
                    Values = resultsCombined
                };
            }).ToArray();

            return new CrosstabResults
            {
                Categories = categories,
                InstanceResults = combinedInstanceResults,
                SampleSizeMetadata = combinedInstanceResults.GetSampleSizeMetadata()
            };
        }

        private CrosstabCategory CreateNestedCategoryForCombinedTable(CrosstabResults entityResults, int resultIndex)
        {
            int GetCategoryDepth(CrosstabCategory category)
            {
                if (category.SubCategories == null || category.SubCategories.Count == 0)
                    return 0;

                return 1 + category.SubCategories
                    .Select(GetCategoryDepth)
                    .Max();
            }

            CrosstabCategory Extend(CrosstabCategory category, int depth)
            {
                if (depth <= 0) return category;

                return new CrosstabCategory
                {
                    Id = string.Empty,
                    Name = string.Empty,
                    DisplayName = string.Empty,
                    IsTotalCategory = category.IsTotalCategory,
                    SubCategories = [Extend(category, depth - 1)]
                };
            }

            var entityCategory = entityResults.Categories.First();

            var categories = entityResults.Categories.Skip(1);

            // add a prefix to IDs so they can be differentiated after joining with duplicates
            foreach (var category in categories.SelectMany(GetCategoryLeafNodes))
            {
                category.Id = AddNestedTablePrefix(category.Id, resultIndex);
            }

            var categoriesWithDepth = categories.Select(c => new
            {
                Category = c,
                Depth = GetCategoryDepth(c)
            }).ToArray();

            var maxDepth = categoriesWithDepth.Max(c => c.Depth);

            return new CrosstabCategory
            {
                Id = string.Empty,
                Name = entityCategory.Name,
                DisplayName = entityCategory.DisplayName,
                SubCategories = [.. categoriesWithDepth.Select(c => Extend(c.Category, maxDepth - c.Depth))]
            };
        }

        private static string AddNestedTablePrefix(string name, int index) => $"result{index}_{name}";

        private (string Name, CellResult Result) CalculateOverallFromResults(CellResult[] allResults)
        {
            var results = allResults
                .Where(r => r != null)
                .ToArray();
            double? count = results.Sum(r => r.Count);
            double sampleForCount = results.Sum(r => r.SampleForCount);
            double result = (count ?? 0.0) / sampleForCount;

            var cellResult = new CellResult
            {
                Result = result,
                Count = count,
                SampleForCount = sampleForCount,
                SampleSizeMetaData = results.First().SampleSizeMetaData,
                SignificantColumns = new List<char>()
            };

            return (TotalScoreColumn, cellResult);
        }


        public void RemoveEmptyColumnsFromResults(CrosstabResults[] results)
        {
            foreach (var result in results)
            {
                var hiddenColumns = 0;
                var identifiers = result.InstanceResults.FirstOrDefault()?.Values.Keys.ToArray();

                foreach (var identifier in identifiers)
                {
                    var hasData = false;
                    foreach (var instanceResult in result.InstanceResults)
                    {
                        if (instanceResult.Values[identifier].Result > 0)
                        {
                            hasData = true;
                            break;
                        }
                    }

                    if (!hasData)
                    {
                        RemoveInstanceResultByIdentifier(result, identifier);
                        RemoveSubCategoryByIdentifier(result, identifier);
                        identifiers = identifiers.Where(i => i != identifier).ToArray();
                        hiddenColumns++;
                    }
                }

                RemoveEmptyCategories(result, identifiers);
                result.HiddenColumns = hiddenColumns;
            }
        }

        private static void RemoveEmptyCategories(CrosstabResults result, string[] identifiers)
        {
            var newCategories = new List<CrosstabCategory>();
            var entityInstanceCategory = result.Categories.FirstOrDefault(c => c.Id == EntityInstanceColumn);
            newCategories.Add(entityInstanceCategory);
            foreach (var category in result.Categories)
            {
                foreach (var identifier in identifiers)
                {
                    if (identifier.Contains(category.Id))
                    {
                        if (!newCategories.Contains(category))
                        {
                            newCategories.Add(category);
                        }
                        break;
                    }
                }
            }
            result.Categories = newCategories;
        }

        private static void RemoveSubCategoryByIdentifier(CrosstabResults result, string identifier)
        {
            foreach (var category in result.Categories)
            {
                var newSubcategories = category.SubCategories.Where(s => s.Id != identifier);
                category.SubCategories = newSubcategories.ToArray();
            }
        }

        private static void RemoveInstanceResultByIdentifier(CrosstabResults result, string identifier)
        {
            foreach (var instanceResult in result.InstanceResults)
            {
                var newValues = new Dictionary<string, CellResult>();
                foreach (var kvp in instanceResult.Values)
                {
                    if (!identifier.Contains(kvp.Key))
                    {
                        newValues.Add(kvp.Key, kvp.Value);
                    }
                }

                instanceResult.Values = newValues;
            }
        }

        private async Task<CrosstabResults> CalculateResults(Measure primaryMeasure,
            ResultsProviderParameters calculationParameters,
            IEnumerable<CrosstabCategory> categoriesPerBreak,
            CrosstabRequestOptions options,
            CancellationToken cancellationToken)
        {
            var categories = categoriesPerBreak.ToList();
            string[] descriptions = categories.SelectMany(GetCategoryLeafNodes).Skip(1).Select(x => x.Id).ToArray();
            var overallResultPerRequestedInstance = (await _convenientCalculator.CoalesceSingleDataPointPerEntityMeasure(calculationParameters, cancellationToken)).Single().Data.Select(x =>
            {
                var weightedDailyResults = x.WeightedResult.RootAndLeaves().ToArray();
                if (descriptions.Length != weightedDailyResults.Length)
                {
                    throw new InvalidOperationException("Missing break results");
                }

                return (x.EntityInstance, Results:
                    weightedDailyResults
                        .Zip(descriptions, (wr, cc) => (ColumnId: cc, WeightedResult: wr))
                        .ToDictionary(c => c.ColumnId, c => c.WeightedResult));
            }).ToArray();

            return TransformResults(primaryMeasure, options, overallResultPerRequestedInstance, categories);
        }

        private EntityInstance GetFakeProfileEntityInstance(Measure primaryMeasure) =>
            new EntityInstance
            {
                Name = primaryMeasure.DisplayName
            };

        private CrosstabResults TransformResults(Measure primaryMeasure,
            CrosstabRequestOptions options,
            (EntityInstance EntityInstance, Dictionary<string, WeightedDailyResult> Results)[] overallResultPerRequestedInstance,
            List<CrosstabCategory> categories)
        {
            var significantColumnsLookup = new Dictionary<EntityInstance, Dictionary<string, List<char>>>();
            var indexScoreLookup = new Dictionary<EntityInstance, Dictionary<string, int>>();

            if (options?.CalculateSignificance ?? false)
            {
                switch (options.SignificanceType)
                {
                    case CrosstabSignificanceType.CompareToTotal:
                        CalculateSignificanceToTotalColumn(primaryMeasure, overallResultPerRequestedInstance, options.SigConfidenceLevel);
                        break;
                    case CrosstabSignificanceType.CompareWithinBreak:
                        significantColumnsLookup =
                            CalculateSignificanceWithinBreaks(primaryMeasure, categories, overallResultPerRequestedInstance, options.SigConfidenceLevel);
                        break;
                }
            }

            if (options.CalculateIndexScores)
            {
                indexScoreLookup = CalculateIndexScores(primaryMeasure, categories, overallResultPerRequestedInstance);
            }

            var entityResults = overallResultPerRequestedInstance
                .Select(r =>
                {
                    var significantColumnsForRow = significantColumnsLookup.ContainsKey(r.EntityInstance)
                        ? significantColumnsLookup[r.EntityInstance]
                        : new Dictionary<string, List<char>>();

                    var significantColumnsForIndexScore = indexScoreLookup.ContainsKey(r.EntityInstance)
                        ? indexScoreLookup[r.EntityInstance]
                        : new Dictionary<string, int>();

                    return new InstanceResult
                    {
                        EntityInstance = r.EntityInstance,
                        Values = r.Results.ToDictionary(d => d.Key, d => BuildCellResult(primaryMeasure, options, d, significantColumnsForRow, significantColumnsForIndexScore))
                    };
                }).ToArray();

            return new CrosstabResults
            {
                Categories = categories,
                InstanceResults = entityResults,
                SampleSizeMetadata = entityResults.GetSampleSizeMetadata(),
            };
        }

        public Dictionary<EntityInstance, Dictionary<string, int>> CalculateIndexScores(Measure measure,
            IReadOnlyCollection<CrosstabCategory> categoriesPerBreak,
            (EntityInstance EntityInstance, Dictionary<string, WeightedDailyResult>)[] cellsGroupedByEntityInstance)
        {
            var groups = categoriesPerBreak.Select(category => GetCategoryLeafNodes(category).ToArray()).Skip(2).ToArray();
            var indexScoreLookup = new Dictionary<EntityInstance, Dictionary<string, int>>();

            foreach (var (entityInstance, entityWeightedResultLookup) in cellsGroupedByEntityInstance)
            {
                indexScoreLookup[entityInstance] = new Dictionary<string, int>();
                var totalValue = entityWeightedResultLookup.First().Value.WeightedResult;

                foreach (var currentGroup in groups)
                {
                    foreach (var thisCategory in currentGroup)
                    {
                        string thisColumnId = thisCategory.Id;
                        var thisResult = entityWeightedResultLookup[thisColumnId];
                        if (thisResult.WeightedResult == 0 || totalValue == 0)
                        {
                            indexScoreLookup[entityInstance][thisColumnId] = 0;
                        }
                        else
                        {
                            var ratio = thisResult.WeightedResult / totalValue * 100;
                            indexScoreLookup[entityInstance][thisColumnId] = (int)Math.Round(ratio, MidpointRounding.AwayFromZero);
                        }
                    }
                }
            }
            return indexScoreLookup;
        }

        private CellResult BuildCellResult(Measure primaryMeasure,
            CrosstabRequestOptions options,
            KeyValuePair<string, WeightedDailyResult> d,
            Dictionary<string, List<char>> significantColumnsForRow,
            Dictionary<string, int> significantColumnsForIndexScore)
        {
            var cellResult = new CellResult
            {
                Result = d.Value.WeightedResult,
                Count = GetCount(d.Value, primaryMeasure, options.IsDataWeighted),
                SampleForCount = GetSampleForCount(d.Value, options.IsDataWeighted),
                SampleSizeMetaData = d.Value.GetSampleSizeMetadata(),
                Significance = d.Value.Significance,
                SignificantColumns = significantColumnsForRow.ContainsKey(d.Key)
                ? significantColumnsForRow[d.Key]
                : new List<char>(),
                IndexScore = significantColumnsForIndexScore.ContainsKey(d.Key)
                ? significantColumnsForIndexScore[d.Key]
                : null
            };

            if (options.IsDataWeighted)
            {
                cellResult.UnweightedSampleForCount = d.Value.UnweightedSampleSize;
            }

            return cellResult;
        }

        private static Dictionary<EntityInstance, Dictionary<string, List<char>>> CalculateSignificanceWithinBreaks(Measure measure,
            IReadOnlyCollection<CrosstabCategory> categoriesPerBreak,
            (EntityInstance EntityInstance, Dictionary<string, WeightedDailyResult>)[] cellsGroupedByEntityInstance,
            SigConfidenceLevel sigConfidenceLevel)
        {
            //Skip the entityInstance and Overall groups
            var groups = categoriesPerBreak.Select(category => GetCategoryLeafNodes(category).ToArray()).Skip(2).ToArray();
            
            //return dictionary instead of adding new property to the WeightedDailyResult class
            var significantColumnsLookup = new Dictionary<EntityInstance, Dictionary<string, List<char>>>();

            foreach (var (columnId, columnsForRow) in cellsGroupedByEntityInstance)
            {
                if (columnsForRow.Count < 2)
                {
                    //We can only calculate significance if there's more than one column
                    continue;
                }

                significantColumnsLookup[columnId] = new Dictionary<string, List<char>>();

                foreach (var currentGroup in groups)
                {
                    foreach (var thisCategory in currentGroup)
                    {
                        string thisColumnId = thisCategory.Id;
                        var thisResult = columnsForRow[thisColumnId];
                        significantColumnsLookup[columnId][thisColumnId] = new List<char>();

                        foreach (var otherCategory in currentGroup)
                        {
                            var otherResult = columnsForRow[otherCategory.Id];
                            var tscore = SignificanceCalculator.CalculateTScore(measure, thisResult, otherResult);
                            var significance = SignificanceCalculator.CalculateSignificance(tscore, sigConfidenceLevel);

                            if (significance != Significance.None)
                            {
                                significantColumnsLookup[columnId][thisColumnId].Add(otherCategory.SignificanceIdentifier);
                            }
                        }
                    }
                }
            }

            return significantColumnsLookup;
        }

        private static IEnumerable<CrosstabCategory> GetCategoryLeafNodes(CrosstabCategory current)
        {
            if (current.SubCategories?.Count() >= 1)
            {
                return current.SubCategories.SelectMany(GetCategoryLeafNodes);
            }
            else
            {
                return current.Yield();
            }
        }

        private static void CalculateSignificanceToTotalColumn(Measure measure,
            (EntityInstance EntityInstance, Dictionary<string, WeightedDailyResult>)[] cellsGroupedByEntityInstance,
            SigConfidenceLevel sigConfidenceLevel)
        {
            foreach (var (entityInstance, resultDictionary) in cellsGroupedByEntityInstance)
            {
                if (resultDictionary.Count < 2)
                {
                    //We can only calculate significance if there's more than one column
                    continue;
                }

                var overallResult = resultDictionary[TotalScoreColumn];
                foreach(var (_, weightedResult) in resultDictionary.Where(x => x.Key != TotalScoreColumn))
                {
                    weightedResult.Tscore =
                        SignificanceCalculator.CalculateTScore(measure, weightedResult, overallResult);
                    weightedResult.Significance =
                        SignificanceCalculator.CalculateSignificance(weightedResult.Tscore.Value, sigConfidenceLevel);
                }
            }
        }

        private double? GetCount(WeightedDailyResult dailyResult, Measure primaryMeasure, bool showWeightedCounts)
        {
            return primaryMeasure.CalculationType switch
            {
                CalculationType.YesNo => showWeightedCounts ? dailyResult.WeightedValueTotal : dailyResult.UnweightedValueTotal,
                CalculationType.Average => showWeightedCounts ? dailyResult.WeightedSampleSize : dailyResult.UnweightedSampleSize,
                _ => null
            };
        }

        private double GetSampleForCount(WeightedDailyResult dailyResult, bool showWeightedCounts) =>
            showWeightedCounts ? dailyResult.WeightedSampleSize : dailyResult.UnweightedSampleSize;

        // Result for ColumnId and EntityInstance
        private static IEnumerable<(string ColumnId, EntityInstance EntityInstance, WeightedDailyResult WeightedResult)> CreateCells((string ColumnId, ResultsForMeasure EntityResults) results)
        {
            return results.EntityResults.Data.Select(d => (results.ColumnId, d.EntityInstance, WeightedResult: d.WeightedDailyResults.SingleOrDefault() ?? new WeightedDailyResult(DateTimeOffset.Now) { UnweightedSampleSize = 0 }));
        }

        private IEnumerable<CrosstabCategory> CreateCategoriesForCrosstabs(CrossMeasure[] crossMeasures,
            string subsetId, string filterInstancesDescription)
        {
            return CreateCategories(crossMeasures, _subsetRepository.Get(subsetId), Enumerable.Empty<string>())
                .Prepend(new CrosstabCategory { Id = TotalScoreColumn, Name = TotalScoreColumn, IsTotalCategory = true })
                .Prepend(new CrosstabCategory { Id = EntityInstanceColumn, Name = filterInstancesDescription});
        }

        private IEnumerable<CrosstabCategory> CreateCategories(IEnumerable<CrossMeasure> crossMeasures, Subset subset, IEnumerable<string> ancestors)
        {
            return crossMeasures.SelectMany((m, crossMeasureIndex) =>
            {
                var crosstabCategories = GetBreaksForMeasure(m, subset).Select((filterModel, i) =>
                {
                    var ancestorsIncludingCurrent = ancestors.Concat($"{crossMeasureIndex}{m.MeasureName}{filterModel.Name}".Yield()).ToArray();
                    return new CrosstabCategory
                    {
                        Id = $"{string.Join("", ancestorsIncludingCurrent)}",
                        Name = filterModel.Name,
                        SignificanceIdentifier = (char)('a' + i),
                        SubCategories = CreateCategories(m.ChildMeasures, subset,
                            ancestorsIncludingCurrent).ToArray()
                    };
                });

                var measure = _measureRepository.Get(m.MeasureName);
                if (!m.ChildMeasures.Any() && !ancestors.Any())
                {
                    return new CrosstabCategory
                    {
                        Id = m.MeasureName,
                        Name = measure?.DisplayName ?? "Invalid measure",
                        SignificanceIdentifier = 'a',
                        SubCategories = crosstabCategories.ToArray()
                    }.Yield();
                }

                return crosstabCategories;
            });
        }

        private IEnumerable<(EntityType Type, EntityInstance Instance)>[] GetSlicedInstances(IEnumerable<(EntityType Type, EntityInstance Instance)>[] cartesianProduct, CrosstabRequestModel model)
        {
            int offset = 0;
            int count = cartesianProduct.Length;

            if (model.PageNo.HasValue && model.NoOfCharts.HasValue)
            {
                offset = (model.PageNo.Value - 1) * model.NoOfCharts.Value;
                count = model.PageNo.Value * model.NoOfCharts.Value;
                count = count < cartesianProduct.Length ? count : cartesianProduct.Length;
                count -= offset;
            }
            return new ArraySegment<IEnumerable<(EntityType Type, EntityInstance Instance)>>(cartesianProduct, offset, count).ToArray();
        }

        private IEnumerable<(string FilterInstancesDescription, ResultsProviderParameters CalculationParameters)> CreateMultiEntityParametersForCrosstabs(CrosstabRequestModel model, Measure primaryMeasure)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var cartesianProduct = getCartesianProductOfMultiEntityInstances(model, subset);

            //handle single entity
            if(cartesianProduct.Length == 0)
            {
                var primaryMeasureIsBasedOnSingleChoice = _questionTypeLookupRepository.GetForSubset(subset)
                    .TryGetValue(primaryMeasure.Name, out var questionType) && questionType == MainQuestionType.SingleChoice;

                var desiredInstances = _entityRepository.GetRequestedInstances(primaryMeasure, model.PrimaryInstances.EntityInstanceIds,
                    model.ActiveBrandId, subset, primaryMeasureIsBasedOnSingleChoice);
                var totalParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, desiredInstances,
                    model.FilterModel, Array.Empty<TargetInstances>(), false);
                yield return (primaryMeasure.DisplayName ?? primaryMeasure.OriginalMetricName ?? primaryMeasure.Name, totalParameters);
                yield break;
            }

            var slicedProducts = GetSlicedInstances(cartesianProduct, model);

            var primaryEntityType = _responseEntityTypeRepository.Get(model.PrimaryInstances.Type);
            var primaryInstances = _entityRepository.GetOrderedEntityInstancesFromIds(primaryEntityType, model.PrimaryInstances.EntityInstanceIds, model.ActiveBrandId, subset);
            var requestedInstances = new TargetInstances(primaryEntityType, primaryInstances);

            var exampleParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, requestedInstances, model.FilterModel, null, false);
            var quotaCells = _requestAdapter.GetFilterOptimizedQuotaCells(exampleParameters.Subset, exampleParameters.QuotaCells);

            foreach (var filterInstances in slicedProducts)
            {
                var filterInstance = filterInstances.Select(i => new TargetInstances(i.Type, new[] { i.Instance })).ToArray();
                var totalParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, requestedInstances, model.FilterModel, filterInstance, false);
                //totalParameters.QuotaCells = quotaCells;// TODO: Should we add this to actually use the performance optimisation?
                var description = string.Join(", ", filterInstances.Select(i => i.Instance.Name));
                yield return (description, totalParameters);
            }
        }

        private IEnumerable<(EntityType Type, EntityInstance Instance)>[]
            getCartesianProductOfMultiEntityInstances(CrosstabRequestModel model, Subset subset)
        {
            var instancesOfAllTypes = model.FilterInstances.Select(instances =>
            {
                var filterByEntityType = _responseEntityTypeRepository.Get(instances.Type);
                var filterInstances = _entityRepository.GetOrderedEntityInstancesFromIds(filterByEntityType, instances.EntityInstanceIds, model.ActiveBrandId, subset);
                return filterInstances.Select(instance => (Type: filterByEntityType, Instance: instance));
            });

            return instancesOfAllTypes.CartesianProduct(_appSettings.MaxCartesianProductSize).ToArray();
        }

        private async Task<CrosstabResults> CalculateResultsLegacy(Measure primaryMeasure,
            IEnumerable<CrosstabCalculationParameters> calculationParameters,
            IEnumerable<CrosstabCategory> categoriesPerBreak,
            CrosstabRequestOptions options,
            CancellationToken cancellationToken)
        {
            // Results for each column/break in the Crosstab table
            var columnResult = await calculationParameters
                .AsAsyncParallel()
                .AsOrdered()
                .SelectAwait(async p => (ColumnId: p.CalculationId, EntityResults: await CalculateResults(p.LegacyCalculationParameters, cancellationToken)), cancellationToken)
                .ToArrayAsync(cancellationToken);

            var crosstabCells = columnResult.SelectMany(CreateCells);
            var crosstabCellLookup = crosstabCells.ToLookup(r => r.EntityInstance ?? GetFakeProfileEntityInstance(primaryMeasure), r => (r.ColumnId, r.WeightedResult));
            var cellsGroupedByEntityInstance = crosstabCellLookup
                .Select(x => (EntityInstance: x.Key, Results: x.ToDictionary(y => y.ColumnId, y => y.WeightedResult))).ToArray();

            return TransformResults(primaryMeasure, options, cellsGroupedByEntityInstance, categoriesPerBreak.ToList());
        }

        private IEnumerable<(string FilterInstancesDescription, IEnumerable<CrosstabCalculationParameters> Parameters)> CreateParametersForCrosstabsLegacy(CrosstabRequestModel model, string overallScoreColumn, Measure primaryMeasure)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var cartesianProduct = getCartesianProductOfMultiEntityInstances(model, subset);

            if(cartesianProduct.Length == 0)
            {
                var primaryMeasureIsBasedOnSingleChoice = _questionTypeLookupRepository.GetForSubset(subset)
                    .TryGetValue(primaryMeasure.Name, out var questionType) && questionType == MainQuestionType.SingleChoice;

                var desiredInstances = _entityRepository.GetRequestedInstances(primaryMeasure, model.PrimaryInstances.EntityInstanceIds,
                    model.ActiveBrandId, subset, primaryMeasureIsBasedOnSingleChoice);
                var totalParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, desiredInstances, model.FilterModel,
                    Array.Empty<TargetInstances>(), true);
                var desiredQuoteCells = _requestAdapter.GetFilterOptimizedQuotaCells(totalParameters.Subset, totalParameters.QuotaCells);

                var result = CreateParametersFromCrossMeasuresLegacy(model.CrossMeasures, totalParameters.Subset, Array.Empty<(int, CrossMeasure)>(), model, primaryMeasure, desiredInstances, desiredQuoteCells)
                    .Prepend(new CrosstabCalculationParameters { CalculationId = overallScoreColumn, DisplayName = overallScoreColumn, LegacyCalculationParameters = totalParameters });
                yield return (model.PrimaryMeasureName, result);
                yield break;
            }

            var slicedProducts = GetSlicedInstances(cartesianProduct, model);

            var primaryEntityType = _responseEntityTypeRepository.Get(model.PrimaryInstances.Type);
            var primaryInstances = _entityRepository.GetOrderedEntityInstancesFromIds(primaryEntityType, model.PrimaryInstances.EntityInstanceIds, model.ActiveBrandId, subset);
            var requestedInstances = new TargetInstances(primaryEntityType, primaryInstances);

            var exampleParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, requestedInstances, model.FilterModel, null, true);
            var quotaCells = _requestAdapter.GetFilterOptimizedQuotaCells(exampleParameters.Subset, exampleParameters.QuotaCells);

            foreach (var filterInstances in slicedProducts)
            {
                var filterInstance = filterInstances.Select(i => new TargetInstances(i.Type, new[] { i.Instance })).ToArray();
                var totalParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, requestedInstances, model.FilterModel, filterInstance, true);
                var parameters = CreateParametersFromCrossMeasuresLegacy(model.CrossMeasures, totalParameters.Subset, Array.Empty<(int, CrossMeasure)>(), model, primaryMeasure, requestedInstances, filterInstance, quotaCells)
                    .Prepend(new CrosstabCalculationParameters { CalculationId = overallScoreColumn, DisplayName = overallScoreColumn, LegacyCalculationParameters = totalParameters });

                var description = string.Join(", ", filterInstances.Select(i => i.Instance.Name));
                yield return (description, parameters);
            }
        }

        private IEnumerable<CrosstabCalculationParameters> CreateParametersFromCrossMeasuresLegacy(
            IEnumerable<CrossMeasure> modelCrossMeasures,
            Subset subset, (int Index, CrossMeasure Measure)[] ancestors, CrosstabRequestModel model,
            Measure primaryMeasure, TargetInstances requestedInstances,
            IGroupedQuotaCells quotaCellOverride)
        {
            return modelCrossMeasures.SelectMany((cm, crossMeasureIndex) =>
            {
                var ancestorsWithCurrent = ancestors.Concat((Index: crossMeasureIndex, Measure: cm).Yield()).ToArray();
                if (cm.ChildMeasures.Any())
                {
                    return CreateParametersFromCrossMeasuresLegacy(cm.ChildMeasures, subset, ancestorsWithCurrent, model,
                        primaryMeasure, requestedInstances, quotaCellOverride);
                }

                var instancesForAncestors = ancestorsWithCurrent.Select(m => GetBreaksForMeasure(m.Measure, subset));
                var filterModels = instancesForAncestors.CartesianProduct(_appSettings.MaxCartesianProductSize);

                return filterModels.Select(fm =>
                {
                    var filterModelArray = fm.ToArray();
                    var breakNames = filterModelArray.Select(m => m.Name);
                    var breakNamesAndMeasures = ancestorsWithCurrent.Zip(breakNames, (measure, entityName) => $"{measure.Index}{measure.Measure.MeasureName}{entityName}");
                    string calculationId = string.Join("", breakNamesAndMeasures);

                    var filterModelsWithRequestModel = filterModelArray.Prepend(model.FilterModel);
                    var filters = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), filterModelsWithRequestModel);
                    var totalParameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, requestedInstances, filters, Array.Empty<TargetInstances>(), true);

                    totalParameters.Breaks = Array.Empty<Break>();
                    totalParameters.QuotaCells = quotaCellOverride;
                    return new CrosstabCalculationParameters
                    {
                        CalculationId = $"{calculationId}",
                        DisplayName = filterModelArray.Last().Name,
                        LegacyCalculationParameters = totalParameters
                    };
                });
            });
        }

        private IEnumerable<CrosstabCalculationParameters> CreateParametersFromCrossMeasuresLegacy(IEnumerable<CrossMeasure> modelCrossMeasures,
                Subset subset, (int Index, CrossMeasure Measure)[] ancestors, CrosstabRequestModel model, Measure primaryMeasure, TargetInstances requestedInstances, TargetInstances[] filterInstances,
                IGroupedQuotaCells quotaCellOverride)
        {
            return modelCrossMeasures.SelectMany((cm, crossMeasureIndex) =>
            {
                var ancestorsWithCurrent = ancestors.Concat((Index: crossMeasureIndex, Measure: cm).Yield()).ToArray();
                if (cm.ChildMeasures.Any())
                {
                    return CreateParametersFromCrossMeasuresLegacy(cm.ChildMeasures, subset, ancestorsWithCurrent, model,
                        primaryMeasure, requestedInstances, filterInstances, quotaCellOverride);
                }

                var instancesForAncestors = ancestorsWithCurrent.Select(m => GetBreaksForMeasure(m.Measure, subset));
                var filterModels = instancesForAncestors.CartesianProduct(_appSettings.MaxCartesianProductSize);

                return filterModels.Select(fm =>
                {
                    var filterModelArray = fm.ToArray();
                    var breakNames = filterModelArray.Select(m => m.Name);
                    var breakNamesAndMeasures = ancestorsWithCurrent.Zip(breakNames, (measure, entityName) => $"{measure.Index}{measure.Measure.MeasureName}{entityName}");
                    string calculationId = string.Join("", breakNamesAndMeasures);

                    var filterModelsWithRequestModel = filterModelArray.Prepend(model.FilterModel);
                    var filters = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), filterModelsWithRequestModel);

                    var parameters = _requestAdapter.CreateParametersForCalculation(model, primaryMeasure, requestedInstances, filters, filterInstances, true);
                    parameters.QuotaCells = quotaCellOverride;
                    return new CrosstabCalculationParameters
                    {
                        CalculationId = $"{calculationId}",
                        DisplayName = filterModelArray.Last().Name,
                        LegacyCalculationParameters = parameters
                    };
                });
            });
        }

        private IEnumerable<CompositeFilterModel> GetBreaksForMeasure(CrossMeasure cm, Subset subset)
        {
            var crossMeasureTyped = _measureRepository.Get(cm.MeasureName);
            return _crosstabFilterModelFactory.GetAllFiltersForMeasure(crossMeasureTyped, subset, cm.FilterInstances, cm.MultipleChoiceByValue);
        }

        /// <summary>
        /// This mechanism of creating filters is obsolete now that the engine calculates breaks much more efficiently.
        /// See usages of <see cref="MeasureRepositoryExtensions.RequiresLegacyBreakCalculation" /> for examples of how to transition away from using this while supporting any edge cases we haven't implemented yet.
        /// </summary>
        public IEnumerable<CompositeFilterModel> GetFlattenedBreaksForMeasure(CrossMeasure cm, string subsetId)
        {
            var subset = _subsetRepository.Get(subsetId);
            return GetFlattenedFilters(cm, subset);
        }

        /// <summary>
        /// This mechanism of creating filters is obsolete now that the engine calculates breaks much more efficiently.
        /// See usages of <see cref="MeasureRepositoryExtensions.RequiresLegacyBreakCalculation" /> for examples of how to transition away from using this while supporting any edge cases we haven't implemented yet.
        /// </summary>
        public IEnumerable<(string MeasureVarCode, IEnumerable<CompositeFilterModel> Filters)> GetGroupedFlattenedBreaks(CrossMeasure[] breaks, string subsetId)
        {
            var subset = _subsetRepository.Get(subsetId);
            foreach (var cm in breaks)
            {
                var measure = _measureRepository.Get(cm.MeasureName);
                yield return (measure.DisplayName, GetFlattenedFilters(cm, subset));
            }
        }

        /// <summary>
        /// This mechanism of creating filters is obsolete now that the engine calculates breaks in most cases.
        /// The name within is the only relevant part used.
        /// See usages of <see cref="MeasureRepositoryExtensions.RequiresLegacyBreakCalculation" /> for examples of how to transition away from using this while supporting any edge cases we haven't implemented yet.
        /// </summary>
        private IEnumerable<CompositeFilterModel> GetFlattenedFilters(CrossMeasure cm, Subset subset)
        {
            if (cm == null)
            {
                yield break;
            }
            var filters = GetBreaksForMeasure(cm, subset);
            foreach (var f in filters)
            {
                if (cm.ChildMeasures.Any())
                {
                    foreach (var child in cm.ChildMeasures)
                    {
                        var childFilters = GetFlattenedFilters(child, subset);
                        foreach (var cf in childFilters)
                        {
                            yield return new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), new[] { f, cf })
                            {
                                Name = $"{f.Name} - {cf.Name}"
                            };
                        }
                    }
                }
                else
                {
                    yield return f;
                }
            };
        }

        private async Task<ResultsForMeasure> CalculateResults(ResultsProviderParameters calculationParameters,
            CancellationToken cancellationToken)
        {
            var curatedResultsForMeasure = (await _convenientCalculator.GetCuratedResultsForAllMeasures(calculationParameters, cancellationToken)).Single();
            return new ResultsForMeasure
            {
                Measure = curatedResultsForMeasure.Measure,
                Data = curatedResultsForMeasure.Data
            };
        }

        /// <summary>
        /// This mechanism of creating filters is obsolete now that the engine calculates breaks much more efficiently.
        /// See usages of <see cref="MeasureRepositoryExtensions.RequiresLegacyBreakCalculation" /> for examples of how to transition away from using this while supporting any edge cases we haven't implemented yet.
        /// </summary>
        public async Task<CrosstabAverageResults> GetOverTimeAverageResultsWithBreaks(CuratedResultsModel model,
            CrossMeasure[] breaks, AverageType averageType, CancellationToken cancellationToken)
        {
            var subset = _subsetRepository.Get(model.SubsetId);
            var breakFilters = breaks.SelectMany(cm => GetFlattenedFilters(cm, subset));
            var overall = (await _resultsProvider.GetOverTimeAverageResults(model, averageType, cancellationToken)).Single().Results.WeightedDailyResults.SingleOrDefault();
            var perBreak = await breakFilters.ToAsyncEnumerable().SelectAwait(async f =>
                {
                    var filters = new[] { model.FilterModel, f };
                    var filterModel = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), filters);
                    var updatedModel = new CuratedResultsModel(model.DemographicFilter,
                        model.EntityInstanceIds,
                        model.SubsetId,
                        model.MeasureName,
                        model.Period,
                        model.ActiveBrandId,
                        filterModel,
                        model.SigDiffOptions,
                        model.Ordering,
                        model.OrderingDirection,
                        model.AdditionalMeasureFilters,
                        model.BaseExpressionOverride);
                    var result = (await _resultsProvider.GetOverTimeAverageResults(updatedModel, averageType, cancellationToken)).Single().Results;

                    return new CrosstabBreakAverageResults
                    {
                        BreakName = f.Name,
                        WeightedDailyResult = result.WeightedDailyResults[0]
                    };
                }
             ).ToArrayAsync(cancellationToken);
            return new CrosstabAverageResults
            {
                OverallDailyResult = new CrosstabBreakAverageResults()
                {
                    BreakName = TotalScoreColumn,
                    WeightedDailyResult = overall
                },
                DailyResultPerBreak = perBreak.ToArray(),
                AverageType = averageType
            };
        }

        public bool IsValidMeasureForAverageMentions(CrosstabRequestModel model, Subset subset)
        {
            var primaryMeasure = _measureRepository.Get(model.PrimaryMeasureName);

            if (primaryMeasure.GenerationType == AutoGenerationType.CreatedFromField || primaryMeasure.VariableConfigurationId == null)
            {
                return _questionTypeLookupRepository
                    .GetForSubset(subset)
                    .TryGetValue(model.PrimaryMeasureName, out var questionType) && questionType == MainQuestionType.MultipleChoice;
            }
            else
            {
                return ValidateThatUserCreatedMetricReferencesASingleMultiChoiceQuestion(subset, primaryMeasure);
            }
        }

        private bool ValidateThatUserCreatedMetricReferencesASingleMultiChoiceQuestion(Subset subset, Measure primaryMeasure)
        {
            var variableConfig = _variableConfigurationRepository.Get(primaryMeasure.VariableConfigurationId.Value);
            if (variableConfig.Definition is GroupedVariableDefinition groupedVariableDefinition)
            {
                var components = groupedVariableDefinition.Groups.Where(g => g.Component is InstanceListVariableComponent component);
                if (components.Count() != groupedVariableDefinition.Groups.Count())
                {
                    return false;
                }

                //validate all components are based on the same metric
                var instanceListComponents = components.Select(c => c.Component as InstanceListVariableComponent).ToArray();
                if (!instanceListComponents.Any())
                {
                    return false;
                }

                var firstComponentIdentifier = instanceListComponents.First().FromVariableIdentifier;
                if (instanceListComponents.Any(c => c.FromVariableIdentifier != firstComponentIdentifier))
                {
                    return false;
                }

                //validate that metric is valid for average mentions
                var firstComponentVariable = _variableConfigurationRepository.GetByIdentifier(firstComponentIdentifier);
                var parentMetric = _measureRepository.GetAll().Where(m => m.VariableConfigurationId == firstComponentVariable.Id);

                return _questionTypeLookupRepository
                    .GetForSubset(subset)
                    .TryGetValue(parentMetric.First().Name, out var questionType) && questionType == MainQuestionType.MultipleChoice;
            }
            else
            {
                return false;
            }
        }

        public async Task<CrosstabAverageResults[]> GetAverageResultsWithBreaks(CrosstabRequestModel model,
            AverageType averageType,
            CancellationToken cancellationToken)
        {
            var subset = _subsetRepository.Get(model.SubsetId);

            if(averageType == AverageType.Mentions && !IsValidMeasureForAverageMentions(model, subset))
            {
                return Array.Empty<CrosstabAverageResults>();
            }

            // See usages of MeasureRepositoryExtensions.RequiresLegacyBreakCalculation for examples of how to transition away from using this slow filter-based mechanism
            var breakFilters = model.CrossMeasures.SelectMany(cm => GetFlattenedFilters(cm, subset));
            var cartesianProduct = getCartesianProductOfMultiEntityInstances(model, subset);

            var lookup = _questionTypeLookupRepository.GetForSubset(subset);
            var questionType = lookup.TryGetValue(model.PrimaryMeasureName, out var type) ? type : MainQuestionType.Unknown;
            var verifiedAverageType = AverageHelper.VerifyAverageTypesForQuestionType(new[] { averageType }, questionType).Single();

            if (cartesianProduct.Count() == 0)
            {
                var requestModel = new MultiEntityRequestModel(model.PrimaryMeasureName,
                    model.SubsetId,
                    model.Period,
                    model.PrimaryInstances,
                    model.FilterInstances,
                    model.DemographicFilter,
                    model.FilterModel,
                    Array.Empty<MeasureFilterRequestModel>(),
                    new[] { model.BaseExpressionOverride },
                    model.Options.CalculateSignificance,
                    model.Options.SigConfidenceLevel);
                 var overall = (await _resultsProvider.GetUnorderedOverTimeAverageResults(requestModel, verifiedAverageType, cancellationToken)).WeightedDailyResults.Single();
                var results = (await GetCrosstabAverageResults(model, verifiedAverageType, breakFilters, requestModel, overall, cancellationToken)).Yield().ToArray();

                return results;
            }
            else
            {
                var slicedProducts = GetSlicedInstances(cartesianProduct, model);

                var results = await slicedProducts.ToAsyncEnumerable().SelectAwait(async filterInstances =>
                {
                    var filterBy = filterInstances
                        .Select(i => new EntityInstanceRequest(i.Type.Identifier, new[] { i.Instance.Id })).ToArray();
                    var requestModel = new MultiEntityRequestModel(model.PrimaryMeasureName,
                        model.SubsetId,
                        model.Period,
                        model.PrimaryInstances,
                        filterBy,
                        model.DemographicFilter,
                        model.FilterModel,
                        Array.Empty<MeasureFilterRequestModel>(),
                        new[] { model.BaseExpressionOverride },
                        model.Options.CalculateSignificance,
                        model.Options.SigConfidenceLevel);
                    var overall = (await _resultsProvider.GetUnorderedOverTimeAverageResults(requestModel, verifiedAverageType, cancellationToken)).WeightedDailyResults.Single();
                    var averageResults = await GetCrosstabAverageResults(model, verifiedAverageType, breakFilters, requestModel, overall, cancellationToken);
                    return (FilterInstances: filterInstances, AverageResults: averageResults);
                }).ToArrayAsync(cancellationToken);

                var primaryMeasure = _measureRepository.Get(model.PrimaryMeasureName);
                if (primaryMeasure.EntityCombination.Count() == 2)
                {
                    if (model.CrossMeasures.Length == 0)
                    {
                        return [CombineCrosstabAverageResultsIntoSingleResult(results, model)];
                    }
                    else if (model.Options.ShowMultipleTablesAsSingle)
                    {
                        return [CombineCrosstabAverageResultsWithBreaks([.. results.Select(r => r.AverageResults)], model)];
                    }
                }
                return [.. results.Select(r => r.AverageResults)];
            }
        }

        private static CrosstabAverageResults CombineCrosstabAverageResultsIntoSingleResult(
            (IEnumerable<(EntityType Type, EntityInstance Instance)> FilterInstances, CrosstabAverageResults AverageResults)[] results,
            CrosstabRequestModel model)
        {
            var combinedResults = results.Select(r =>
            {
                return new CrosstabBreakAverageResults
                {
                    BreakName = string.Join(" - ", r.FilterInstances.Select(i => i.Instance.Name)),
                    WeightedDailyResult = r.AverageResults.OverallDailyResult.WeightedDailyResult
                };
            }).ToArray();


            return new CrosstabAverageResults
            {
                AverageType = results.First().AverageResults.AverageType,
                DailyResultPerBreak = combinedResults
            };
        }

        private static CrosstabAverageResults CombineCrosstabAverageResultsWithBreaks(CrosstabAverageResults[] results, CrosstabRequestModel model)
        {
            return new CrosstabAverageResults
            {
                AverageType = results.First().AverageType,
                DailyResultPerBreak = results
                    .SelectMany(r => r.DailyResultPerBreak.Prepend(r.OverallDailyResult))
                    .ToArray()
            };
        }

        private async Task<CrosstabAverageResults> GetCrosstabAverageResults(CrosstabRequestModel model,
            AverageType averageType, IEnumerable<CompositeFilterModel> breakFilters,
            MultiEntityRequestModel requestModel, WeightedDailyResult overall, CancellationToken cancellationToken)
        {
            var perBreak = await breakFilters.ToAsyncEnumerable().SelectAwait(async f =>
            {
                var filters = new[] { model.FilterModel, f };
                var filterModel = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), filters);
                var updatedModel = new MultiEntityRequestModel(requestModel.MeasureName,
                    requestModel.SubsetId,
                    requestModel.Period,
                    requestModel.DataRequest,
                    requestModel.FilterBy,
                    requestModel.DemographicFilter,
                    filterModel,
                    requestModel.AdditionalMeasureFilters,
                    requestModel.BaseExpressionOverrides,
                    model.Options.CalculateSignificance,
                    model.Options.SigConfidenceLevel);
                var result = await _resultsProvider.GetUnorderedOverTimeAverageResults(updatedModel, averageType, cancellationToken);

                if (model.Options.HideEmptyColumns && result.WeightedDailyResults[0].WeightedResult == 0)
                {
                    return null;
                }

                return new CrosstabBreakAverageResults
                {
                    BreakName = f.Name,
                    WeightedDailyResult = result.WeightedDailyResults[0]
                };
            })
            .Where(b => b != null)
            .ToArrayAsync(cancellationToken);

            return new CrosstabAverageResults
            {
                OverallDailyResult = new CrosstabBreakAverageResults()
                {
                    BreakName = TotalScoreColumn,
                    WeightedDailyResult = overall
                },
                DailyResultPerBreak = perBreak.ToArray(),
                AverageType = averageType
            };
        }

        public async Task<CrosstabAverageResults> GetAverageForMultiEntityCharts(AverageMultiEntityChartModel model,
            CancellationToken cancellationToken)
        {
            var perBreak = new List<CrosstabBreakAverageResults>();
            if (model.Breaks != null)
            {
                var subset = _subsetRepository.Get(model.SubsetId);
                var breakFilters = GetFlattenedFilters(model.Breaks, subset);

                // See usages of MeasureRepositoryExtensions.RequiresLegacyBreakCalculation for examples of how to transition away from using this slow filter-based mechanism
                foreach (var breakFilter in breakFilters)
                {

                    var filters = new[] { model.RequestModel.FilterModel, breakFilter };
                    var filterModel = new CompositeFilterModel(FilterOperator.And, Enumerable.Empty<MeasureFilterRequestModel>(), filters);
                    var updatedModel = new MultiEntityRequestModel(model.RequestModel.MeasureName,
                        model.RequestModel.SubsetId,
                        model.RequestModel.Period,
                        model.RequestModel.DataRequest,
                        model.RequestModel.FilterBy,
                        model.RequestModel.DemographicFilter,
                        filterModel,
                        model.RequestModel.AdditionalMeasureFilters,
                        model.RequestModel.BaseExpressionOverrides,
                        model.RequestModel.IncludeSignificance,
                        model.RequestModel.SigConfidenceLevel);
                    var result = await _resultsProvider.GetUnorderedOverTimeAverageResults(updatedModel, model.AverageType, cancellationToken);

                    perBreak.Add(new CrosstabBreakAverageResults
                    {
                        BreakName = breakFilter.Name,
                        WeightedDailyResult = result.WeightedDailyResults[0]
                    });
                }

            }
            var overall = (await _resultsProvider.GetUnorderedOverTimeAverageResults(model.RequestModel, model.AverageType, cancellationToken)).WeightedDailyResults.Single();

            return new CrosstabAverageResults
            {
                OverallDailyResult = new CrosstabBreakAverageResults()
                {
                    BreakName = TotalScoreColumn,
                    WeightedDailyResult = overall
                },
                DailyResultPerBreak = perBreak.ToArray(),
                AverageType = model.AverageType
            };
        }
    }
}

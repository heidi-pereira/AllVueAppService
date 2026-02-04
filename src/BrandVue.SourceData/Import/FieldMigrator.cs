using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.Migrations.MetaData;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Calculation.Expressions;
using BrandVue.SourceData.Dashboard;
using BrandVue.SourceData.LazyLoading;
using BrandVue.SourceData.Measures;
using BrandVue.SourceData.Variable;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace BrandVue.SourceData.Import;

using VariableField = (VariableConfiguration VariableConfiguration, ResponseFieldDescriptor OriginalField);

internal class FieldMigrator
{
    private readonly IBrandVueDataLoaderSettings _settings;
    protected readonly ILoggerFactory _loggerFactory;
    private readonly ILogger _logger;
    private readonly IInvalidatableLoaderCache _invalidatableLoaderCache;

    private readonly ResponseFieldManager _responseFieldManager;
    private readonly VariableConfigurationRepository _writeableVariableConfigurationRepository;
    private readonly ISubsetRepository _subsetRepository;
    private readonly IProductContext _productContext;
    private readonly ILoadableEntityInstanceRepository _entityRepository;
    private readonly ILoadableEntityTypeRepository _entityTypeRepository;
    private readonly EntityInstanceRepositorySql _entityInstanceRepositorySql;
    private readonly EntityTypeRepositorySql _entityTypeRepositorySql;
    private readonly IDbContextFactory<MetaDataContext> _dbContextFactory;
    private readonly MetricConfigurationRepositorySql _metricConfigurationRepository;
    private readonly IQuestionTypeLookupRepository _questionTypeLookupRepository;
    private int _uniqueEntityTypeNameSuffix;
    private int _uniqueBaseEntityTypeNameSuffix;
    // Hack to workaround the production code hack that looks for "or None"
    private readonly string _allowZero = "0 or None or ";
    private FieldExpressionParser _fieldExpressionParser;

    public FieldMigrator(IBrandVueDataLoaderSettings settings, ILoggerFactory loggerFactory,
        IInvalidatableLoaderCache invalidatableLoaderCache, ResponseFieldManager responseFieldManager,
        VariableConfigurationRepository writeableVariableConfigurationRepository, ISubsetRepository subsetRepository,
        IProductContext productContext, ILoadableEntityInstanceRepository entityRepository,
        ILoadableEntityTypeRepository entityTypeRepository,
        EntityInstanceRepositorySql entityInstanceRepositorySql, EntityTypeRepositorySql entityTypeRepositorySql,
        IDbContextFactory<MetaDataContext> dbContextFactory,
        MetricConfigurationRepositorySql metricConfigurationRepository,
        IQuestionTypeLookupRepository questionTypeLookupRepository)
    {
        _settings = settings;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<FieldMigrator>();
        _invalidatableLoaderCache = invalidatableLoaderCache;
        _responseFieldManager = responseFieldManager;
        _writeableVariableConfigurationRepository = writeableVariableConfigurationRepository;
        _subsetRepository = subsetRepository;
        _productContext = productContext;
        _entityRepository = entityRepository;
        _entityTypeRepository = entityTypeRepository;
        _entityInstanceRepositorySql = entityInstanceRepositorySql;
        _entityTypeRepositorySql = entityTypeRepositorySql;
        _dbContextFactory = dbContextFactory;
        _metricConfigurationRepository = metricConfigurationRepository;
        _questionTypeLookupRepository = questionTypeLookupRepository;
    }

    public void Migrate(FieldExpressionParser fieldExpressionParser, MetricRepository metricRepository)
    {
        _fieldExpressionParser = fieldExpressionParser;
        // only write to database when feature flag MigrateFields is set AND there are no QuestionVariables in the db for the given SubProduct
        if (!_settings.AppSettings.MigrateFields) return;

        using var dbContext = _dbContextFactory.CreateDbContext();
        OverwriteGenderWeighting(dbContext);

        var subset = _subsetRepository.OrderBy(x => x.Disabled).ThenBy(x => x.Order).First(); //TODO All subsets?

        var (fieldToVariable, fieldToParentVariable) = MigrateFields(fieldExpressionParser, metricRepository, subset, dbContext);
        MigrateVariableDependencies(dbContext, fieldToVariable);
        MigrateMetrics(metricRepository, fieldToVariable, fieldToParentVariable, subset, dbContext);

        dbContext.SaveChanges();
        _invalidatableLoaderCache.InvalidateCacheEntry(_productContext.ShortCode, _productContext.SubProductId);
        _logger.LogWarning("MIGRATION COMPLETE: Attempting reload");
    }

    private void MigrateVariableDependencies(MetaDataContext dbContext, Dictionary<string, VariableField> fieldToVariable)
    {
        foreach (var variableConfig in dbContext.VariableConfigurations.For(_productContext, true))
        {
            MigrateVariableDependencies(variableConfig, fieldToVariable);
        }
    }

    private void MigrateVariableDependencies(VariableConfiguration variableConfig,
        Dictionary<string, VariableField> fieldToVariable)
    {
        var variable = GetDeclaredVariable(variableConfig.Identifier);
        if (variable == null)
        {
            return;
        }

        var fieldDependencies = GetMigratedVariablesForFieldDependencies(variable, fieldToVariable);
        var dependenciesToAdd = fieldDependencies.Select(variableForFieldDependency => new VariableDependency()
        {
            Variable = variableConfig,
            DependentUponVariable = variableForFieldDependency
        }).Where(d => d.DependentUponVariable.Identifier != variableConfig.Identifier && variableConfig.VariableDependencies.All(vd => vd.DependentUponVariable.Identifier != d.DependentUponVariable.Identifier));
        variableConfig.VariableDependencies.AddRange(dependenciesToAdd);
    }

    private IEnumerable<VariableConfiguration> GetMigratedVariablesForFieldDependencies(IVariable fromVariable, Dictionary<string, VariableField> fieldToVariable)
    {
        var dependencyVariableIdentifiers = GetVariableDependencyIdentifiers(fromVariable)?.VariableDependencyIdentifiers ?? [];
        var transitiveFieldDependencies = dependencyVariableIdentifiers
            .SelectMany(v =>
            {
                var declaredVariableOrNull = _fieldExpressionParser.GetDeclaredVariableOrNull(v);
                // Depends directly on field, but we still need that dependency
                return declaredVariableOrNull is CachedInMemoryFieldVariableInstance 
                    ? Array.Empty<ResponseFieldDescriptor>()
                    : declaredVariableOrNull.FieldDependencies;
            }).ToHashSet();
        var fieldDependencies = fromVariable.FieldDependencies
            .Except(transitiveFieldDependencies)
            .Select(fd => fieldToVariable.GetValueOrDefault(fd.Name).VariableConfiguration)
            .Where(x => x is not null);
        return fieldDependencies;
    }

    private IVariableWithDependencies GetVariableDependencyIdentifiers(IVariable variable)
    {
        if (variable is IDecoratedVariable v) return v.DecoratedVariable as IVariableWithDependencies;
        return variable as IVariableWithDependencies;
    }

    private IVariable GetDeclaredVariable(string variableConfigIdentifier)
    {
        return _fieldExpressionParser.GetDeclaredVariableOrNull(variableConfigIdentifier) ?? (IVariable) _fieldExpressionParser.GetDeclaredBooleanVariableOrNull(variableConfigIdentifier);
    }

    /// <summary>
    /// Previously depended on the non-entity version's value, working around issue where 0 wasn't coming through properly
    /// Should also be quicker to have a direct depdendency
    /// </summary>
    private void OverwriteGenderWeighting(MetaDataContext dbContext)
    {
        var genderWeightingVariable = dbContext.VariableConfigurations.For(_productContext, true)
            .FirstOrDefault(v => v.Identifier == "GenderWeighting");
        string fromVariableIdentifier = "Gender_entity";
        if (genderWeightingVariable?.Definition is GroupedVariableDefinition {Groups: {} groups} && _fieldExpressionParser.GetDeclaredVariableOrNull(fromVariableIdentifier) is
                { } genderEntity)
        {
            var female = groups.First(g => g.ToEntityInstanceName == "Female");
            string fromEntityTypeName = genderEntity.UserEntityCombination.Single().Identifier;
            female.Component = new InstanceListVariableComponent()
            {
                FromEntityTypeName = fromEntityTypeName,
                FromVariableIdentifier = fromVariableIdentifier,
                ResultEntityTypeNames = [],
                InstanceIds = [0],
                Operator = InstanceVariableComponentOperator.Or

            };
            var male = groups.First(g => g.ToEntityInstanceName == "Male");
            male.Component = new InstanceListVariableComponent()
            {
                FromEntityTypeName = fromEntityTypeName,
                FromVariableIdentifier = fromVariableIdentifier,
                ResultEntityTypeNames = [],
                InstanceIds = [1],
                Operator = InstanceVariableComponentOperator.Or

            };
            dbContext.Update(genderWeightingVariable);
        }
    }

    private void MigrateMetrics(MetricRepository metricRepository,
        Dictionary<string, VariableField> fieldToVariable,
        Dictionary<string, VariableField> fieldToParentVariable, Subset subset, MetaDataContext dbContext)
    {
        var metricConfigurations = _metricConfigurationRepository.ForProductContext(dbContext).OrderBy(m => m.Name).ToArray();
        var variablesByIdentifier = fieldToVariable.Concat(fieldToParentVariable)
            .Where(f => f.Value.VariableConfiguration?.Identifier != null)
            .ToLookup(f => f.Value.VariableConfiguration.Identifier)
            .ToDictionary(f => f.Key, f => f.First().Value.VariableConfiguration);
        foreach (var metricConfig in metricConfigurations)
        {
            using var _ = _logger.BeginScope(metricConfig.Name);



            if (metricConfig.BaseExpression is not null || metricConfig.BaseVariableConfiguration is not null)
            {
                metricConfig.BaseField = null;
                metricConfig.BaseVals = null;
            }
            else
            {
                metricConfig.BaseField ??= metricConfig.Field;
            }

            if (metricConfig.FieldExpression is not null || metricConfig.VariableConfiguration is not null)
            {
                metricConfig.Field = null;
                metricConfig.Field2 = null;
                metricConfig.TrueVals = null;
            }

            if (metricConfig.Field?.Length > 0) metricConfig.Field = NameGenerator.EnsureValidPythonIdentifier(metricConfig.Field);
            if (metricConfig.Field2?.Length > 0) metricConfig.Field2 = NameGenerator.EnsureValidPythonIdentifier(metricConfig.Field2);
            if (metricConfig.BaseField?.Length > 0) metricConfig.BaseField = NameGenerator.EnsureValidPythonIdentifier(metricConfig.BaseField);

            if (!metricRepository.TryGet(metricConfig.Name, out var existingMeasure))
            {
                _logger.LogWarning($"No measure for `{metricConfig.Name}`");
                continue;
            }
            if (existingMeasure.Subset?.EmptyOrContains(subset, EqualityComparer<Subset>.Default) == false)
            {
                continue;
            }
            var variableForField = metricConfig.VariableConfiguration is null ? fieldToVariable.GetValueOrDefault(metricConfig.Field) : (metricConfig.VariableConfiguration, null);
            var variableForFieldParent = metricConfig.VariableConfiguration is null ? fieldToParentVariable.GetValueOrDefault(metricConfig.Field) : (metricConfig.VariableConfiguration, null);

            if (variableForField.VariableConfiguration is null)
            {
                _logger.LogWarning($"No variable for `{metricConfig.Name}` with Field `{metricConfig.Field}`");
                continue;
            }

            var fieldQuestionType = variableForField.OriginalField?.GetDataAccessModel(subset.Id).QuestionModel.QuestionType;
            var originalTrueValsDescription = metricConfig.TrueVals;
            var primaryTrueValues = GetOriginalPrimaryVals(metricConfig);
            bool isUsingField = variableForField.OriginalField is not null && (primaryTrueValues.IsRange || primaryTrueValues.IsList);

            var variableForField2 = metricConfig.Field2 is not null ? fieldToVariable.GetValueOrDefault(metricConfig.Field2) : (null, null);

            var variableForField2Parent = metricConfig.Field2 is null ? (null, null) : fieldToParentVariable.GetValueOrDefault(metricConfig.Field2);

            if (existingMeasure.BaseExpression is null)
            {
                System.Diagnostics.Debug.WriteLine($"MIGRATION: Found metric {existingMeasure.Name} which has a null BaseExpression");
                continue;
            }

            var baseDependencyVariables = GetMigratedVariablesForFieldDependencies(existingMeasure.BaseExpression, fieldToVariable).ToArray();

            HackRetailSpontaneousAwareness(metricConfig);

            var baseFieldVariable = DeclareBaseExpression(fieldToVariable, dbContext, metricConfig, baseDependencyVariables);
            var baseFieldVariableParent = metricConfig.BaseField is null ? (null, null) : fieldToParentVariable.GetValueOrDefault(metricConfig.BaseField);

            if (baseFieldVariable.VariableConfiguration is null)
            {
                _logger.LogWarning($"No base variable for `{metricConfig.Name}` with BaseField `{metricConfig.BaseField}`");
                continue;
            }
            var (baseConfiguration, baseVariableGrouping) = CreateComposableBaseComponent(metricConfig, baseFieldVariable, baseFieldVariableParent, existingMeasure, baseDependencyVariables, subset);

            HackCharitiesSpontaneousAwareness(baseVariableGrouping);

            if (string.Equals(metricConfig.FieldOp, FieldOperation.Filter.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                //create base group that ANDs the two conditions(test example in UI)
                if (baseFieldVariable.VariableConfiguration is null || variableForField2.VariableConfiguration is null) throw new NotSupportedException(metricConfig.Name);
                var mainQuestionType = existingMeasure.Field2.GetDataAccessModel(subset.Id).QuestionModel.QuestionType;
                baseConfiguration = baseConfiguration with
                {
                    Identifier = baseConfiguration.Identifier + "_AND_" + variableForField2.VariableConfiguration.Identifier
                };
                VariableComponent field2VariableComponent = ComponentFromValues(existingMeasure.LegacySecondaryTrueValues, variableForField2, variableForField2Parent, existingMeasure.Field2.EntityCombination, mainQuestionType, subset);
                baseVariableGrouping.Component = new CompositeVariableComponent()
                {
                    CompositeVariableComponents = [baseVariableGrouping.Component, field2VariableComponent],
                    CompositeVariableSeparator = CompositeVariableSeparator.And
                };
                if (metricConfig.BaseVariableConfiguration is not null)
                {
                    throw new NotImplementedException("base configuration set, but combining with field2");
                }
            }

            var fieldVariableComponent = isUsingField
                ? ComponentFromValues(primaryTrueValues,
                    variableForField, variableForFieldParent, variableForField.OriginalField.EntityCombination,
                    fieldQuestionType.Value, subset)
                : null;

            if (string.Equals(metricConfig.FieldOp, FieldOperation.Or.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var field2QuestionType =
                    existingMeasure.Field2.GetDataAccessModel(subset.Id).QuestionModel.QuestionType;
                var field2VariableComponent = ComponentFromValues(
                    existingMeasure.LegacySecondaryTrueValues, variableForField2, variableForField2Parent,
                    existingMeasure.Field2.EntityCombination, field2QuestionType, subset);
                string displayName = metricConfig.Name + " (" + variableForField.OriginalField.Name + " or " + variableForField2.OriginalField.Name + ")";
                variableForField.VariableConfiguration = CreateVariableConfiguration(
                    SingleGroupDefinition([fieldVariableComponent, field2VariableComponent],
                        CompositeVariableSeparator.Or), displayName, variablesByIdentifier);
                dbContext.VariableConfigurations.Update(variableForField.VariableConfiguration);
                metricConfig.TrueVals = null;
            }
            else if(fieldVariableComponent is not null && (primaryTrueValues.IsRange || primaryTrueValues.IsList))
            {
                variableForField.VariableConfiguration = CreateVariableConfiguration(
                    SingleGroupDefinition([fieldVariableComponent],
                        CompositeVariableSeparator.Or),
                    metricConfig.Field + " in " + originalTrueValsDescription.Replace(">", " to "), variablesByIdentifier);
                metricConfig.TrueVals = null;
                if (existingMeasure.CalculationType == CalculationType.Average)
                {
                    // The output value previously stayed scaled down, that oddity now needs forcing in the config
                    var scaleFactor = existingMeasure.PrimaryFieldDependencies
                        .Select(f => f.GetDataAccessModel(subset.Id).ScaleFactor)
                        .SingleOrDefault(x => x is not null);
                    metricConfig.ScaleFactor = scaleFactor;
                }
            }

            // Base vals dont' apply in this case, and we didn't need to compose it with another filter, so just stick with the original
            if (metricConfig.BaseExpression is not null || metricConfig.BaseVariableConfiguration is not null || existingMeasure.CalculationType is CalculationType.Text)
            {
                baseConfiguration = baseFieldVariable.VariableConfiguration;
            }
            else
            {
                // Add new variable to wrap up base vals
                dbContext.VariableConfigurations.Update(baseConfiguration);
            }
            
            metricConfig.VariableConfiguration ??= variableForField.VariableConfiguration;

            metricConfig.BaseVariableConfiguration ??= baseConfiguration;

            metricConfig.Field = null;
            metricConfig.Field2 = null;
            metricConfig.FieldOp = null;
            metricConfig.BaseField = null;
            metricConfig.BaseVals = null;
            metricConfig.BaseExpression = null;

            dbContext.MetricConfigurations.Update(metricConfig);
        }
    }

    /// <summary>
    /// Just for charities because it currently relies on pulling through null-containing rows which doesn't work without the map file
    /// The trick is to depend on the uncoded question, which doesn't have nulls
    /// </summary>
    private void HackCharitiesSpontaneousAwareness(VariableGrouping baseVariableGrouping)
    {
        if (_productContext.ShortCode == "charities" && baseVariableGrouping?.Component is InclusiveRangeVariableComponent {FromVariableIdentifier: "Spontaneous_awareness_mission_base" or "Spontaneous_awareness_base" } c)
        {
            string fromVariableIdentifier = c.FromVariableIdentifier.Replace("_base", "_UK");
            var responseFieldDescriptor = _fieldExpressionParser.GetDeclaredVariableOrNull(fromVariableIdentifier);
            baseVariableGrouping.Component = new InstanceListVariableComponent()
            {
                FromVariableIdentifier = fromVariableIdentifier,
                FromEntityTypeName = responseFieldDescriptor.UserEntityCombination.First()
                    .Identifier,
                InstanceIds = EnumerateRange(1,8).ToList()
            };
        }
    }
    /// <summary>
    /// Just for retail because it currently relies on pulling through null-containing rows which doesn't work without the map file
    /// The trick is to depend on the uncoded question, which doesn't have nulls
    /// </summary>
    private void HackRetailSpontaneousAwareness(MetricConfiguration metricConfig)
    {
        if (_productContext.ShortCode == "retail" && metricConfig.BaseExpression == "len(response.Product_awareness_coded_base(product=result.product))")
        {
            metricConfig.BaseExpression = "len(response.Spontaneous_awareness(product=result.product))";
        }
    }

    private (VariableConfiguration baseConfiguration, VariableGrouping variableGrouping)
        CreateComposableBaseComponent(MetricConfiguration metricConfig,
            VariableField baseFieldVariable, VariableField baseFieldVariableParent,
            Measure existingMeasure,
            VariableConfiguration[] baseDependencyVariables, Subset subset)
    {
        if (metricConfig.BaseExpression is not null) return (null, null);

        string baseDisplayName = metricConfig.Name + " (Base " + baseFieldVariable.VariableConfiguration.Identifier + ")";
        ResponseFieldDescriptor baseField = baseFieldVariable.OriginalField;
        if (existingMeasure.BaseExpression is BooleanFromValueVariable {WrappedVariable: CachedInMemoryFieldVariableInstance {FieldDependencies: {Count: 1} existingBaseFields}})
        {
            baseField = existingBaseFields.Single();
        }

        var baseModel = baseField?.GetDataAccessModel(subset.Id);
        var baseValues = GetOriginalBaseVals(metricConfig);

        var questionType = baseModel?.QuestionModel.QuestionType ?? MainQuestionType.SingleChoice;
        if (metricConfig.IsAutoGenerated == AutoGenerationType.CreatedFromField &&
            metricConfig.FilterValueMapping == "Range:Range" || (questionType is MainQuestionType.Text || questionType is MainQuestionType.HeatmapImage) && existingMeasure.CalculationType != CalculationType.Text)
        {
            questionType = MainQuestionType.Value;//This is the fallback autogeneration case. It's possible these metrics don't even work.
        }
        VariableComponent baseVariableComponent = ComponentFromValues(baseValues, baseFieldVariable, baseFieldVariableParent, existingMeasure.BaseEntityCombination, questionType, subset);
        var variableGrouping = new VariableGrouping()
        {
            ToEntityInstanceId = 1,
            ToEntityInstanceName = baseDisplayName,
            Component = baseVariableComponent
        };
        
        string entityTypeName = baseFieldVariable.VariableConfiguration.Identifier + "_Type_" + _uniqueBaseEntityTypeNameSuffix++;
        var baseDependencies = new List<VariableDependency>();
        string baseIdentifier = NameGenerator.EnsureValidPythonIdentifier(baseDisplayName);
        var baseConfiguration = new VariableConfiguration()
        {
            Identifier = baseIdentifier,
            DisplayName = baseDisplayName,
            Definition = new BaseGroupedVariableDefinition()
            {
                Groups = [variableGrouping],
                ToEntityTypeName = entityTypeName,
                ToEntityTypeDisplayNamePlural = entityTypeName,
                AggregationType = AggregationType.MaxOfMatchingCondition
            },
            ProductShortCode = _productContext.ShortCode,
            SubProductId = _productContext.SubProductId,
            VariableDependencies = baseDependencies,
            
        };

        baseDependencies.AddRange(baseDependencyVariables
            .Select(v => new VariableDependency()
            {
                Variable = baseConfiguration, DependentUponVariable = v
            }));
        return (baseConfiguration, variableGrouping);
    }

    private static AllowedValues GetOriginalPrimaryVals(MetricConfiguration metricConfig)
    {
        var values = new AllowedValues();
        MetricFactory.SetPrimaryValues(metricConfig, values);
        return values;
    }

    private static AllowedValues GetOriginalBaseVals(MetricConfiguration metricConfig)
    {
        var baseValues = new AllowedValues();
        MetricFactory.SetBaseValues(metricConfig, baseValues);
        return baseValues;
    }

    private VariableField DeclareBaseExpression(Dictionary<string, VariableField> fieldToVariable,
        MetaDataContext dbContext,
        MetricConfiguration metricConfig,
        VariableConfiguration[] baseDependencyVariables)
    {
        VariableField baseFieldVariable;
        if (metricConfig.BaseExpression is not null)
        {
            string displayName = metricConfig.Name + " BaseExpression";
            string pythonIdentifier = NameGenerator.EnsureValidPythonIdentifier(displayName);
            var variableDependencies = new List<VariableDependency>();
            baseFieldVariable = (new VariableConfiguration
            {
                Identifier = pythonIdentifier,
                DisplayName = displayName,
                ProductShortCode = _productContext.ShortCode,
                SubProductId = _productContext.SubProductId,
                Definition = new BaseFieldExpressionVariableDefinition(){Expression = metricConfig.BaseExpression},
                VariableDependencies = variableDependencies
            }, null);
            variableDependencies.AddRange(baseDependencyVariables
                .Select(v => new VariableDependency()
                {
                    Variable = baseFieldVariable.VariableConfiguration, DependentUponVariable = v
                }));
            dbContext.VariableConfigurations.Add(baseFieldVariable.VariableConfiguration);
        }
        else
        {
            string metricConfigBaseField = metricConfig.BaseField ?? metricConfig.Field;
            baseFieldVariable = metricConfig.BaseVariableConfiguration != null ? (metricConfig.BaseVariableConfiguration, null) : metricConfigBaseField != null ? fieldToVariable.GetValueOrDefault(metricConfigBaseField) : (null, null);
        }

        return baseFieldVariable;
    }

    private VariableComponent ComponentFromValues(AllowedValues values, VariableField fromVariable,
        VariableField fromVariableParent,
        IReadOnlyCollection<EntityType> resultTypes, MainQuestionType questionType, Subset subset)
    {
        ScaleAllowedValues(subset, fromVariable.OriginalField, values);

        var resultEntityTypeNames = resultTypes.Select(t => t.Identifier).ToList();
       
        if (questionType is MainQuestionType.Value or MainQuestionType.MultipleChoice || fromVariableParent.OriginalField is not {} parentField || parentField.EntityCombination.Count() - 1 != fromVariable.OriginalField.EntityCombination.Count)
        {
            string fromIdentifier = fromVariable.VariableConfiguration.Identifier;

            if (values.IsRange)
            {
                return new InclusiveRangeVariableComponent()
                {
                    Min = values.Minimum.Value, Max = values.Maximum.Value,
                    Operator = VariableRangeComparisonOperator.Between,
                    FromVariableIdentifier = fromIdentifier,
                    ResultEntityTypeNames = resultEntityTypeNames
                };
            }

            // Note: Very few non-range base/secondary values
            //SELECT * from MetricConfigurations where fieldop = 'filter' and((truevals like '%|%' and truevals not like '%�%') or truevals like '%�%|%')
            //SELECT * from MetricConfigurations where fieldop = 'filter' and((BaseVals like '%|%'))

            // Simplify list of values to range
            if (values.IsList)
            {
                if (values.Values.IsEquivalent(EnumerateValuesInListRange(values)))
                {
                    return new InclusiveRangeVariableComponent()
                    {
                        Min = values.Values.Min(), Max = values.Values.Max(),
                        Operator = VariableRangeComparisonOperator.Between,
                        FromVariableIdentifier = fromIdentifier,
                        ResultEntityTypeNames = resultEntityTypeNames
                    };
                }
                return new InclusiveRangeVariableComponent()
                {
                    ExactValues = values.Values,
                    Operator = VariableRangeComparisonOperator.Exactly,
                    FromVariableIdentifier = fromIdentifier,
                    ResultEntityTypeNames = resultEntityTypeNames
                };
            }

            // Not null
            return new InclusiveRangeVariableComponent()
            {
                ExactValues = [int.MinValue], //HACK
                Operator = VariableRangeComparisonOperator.GreaterThan,
                Inverted = true,
                FromVariableIdentifier = fromIdentifier,
                ResultEntityTypeNames = resultEntityTypeNames,
            };
        }


        var originalFieldEntityCombination = fromVariableParent.OriginalField?.EntityCombination ?? [];
        var entities = originalFieldEntityCombination.Except(fromVariable.OriginalField?.EntityCombination ?? []).DefaultIfEmpty(originalFieldEntityCombination.Last());
        var entityFilteredByValues = entities.Last(); //Pick last which ends up as the value
        return new InstanceListVariableComponent()
            {
                FromEntityTypeName = entityFilteredByValues.Identifier,
                InstanceIds = (values.IsList ? values.Values : EnumerateRange(values.Minimum.Value, values.Maximum.Value)).ToList(),
                Operator = InstanceVariableComponentOperator.Or,
                FromVariableIdentifier = fromVariableParent.VariableConfiguration.Identifier,
                ResultEntityTypeNames = resultEntityTypeNames,
                AnswerMinimum = values.Minimum,
                AnswerMaximum = values.Maximum,
            };
    }

    private static void ScaleAllowedValues(Subset subset, ResponseFieldDescriptor field, AllowedValues values)
    {
        if (field?.GetDataAccessModelOrNull(subset.Id) is {ScaleFactor: not null} accessModel)
        {
            if (values.IsRange)
            {
                values.Minimum = Round(accessModel, values.Minimum.Value);
                values.Maximum = Round(accessModel, values.Maximum.Value);
            }
            else if (values.IsList)
            {
                values.Values = values.Values.Select(v => Round(accessModel, v)).ToArray();
            }
        }
    }

    private static int Round(FieldDefinitionModel accessModel, int valuesMinimum)
    {
        return (int)Math.Round(valuesMinimum / accessModel.ScaleFactor.Value, 0);
    }

    private static IEnumerable<int> EnumerateValuesInListRange(AllowedValues values) =>
        EnumerateRange(values.Values.Min(), values.Values.Max());

    private static IEnumerable<int> EnumerateRange(int start, int end) =>
        Enumerable.Range(start, end - start + 1);

    private (Dictionary<string, VariableField> fieldNameToVariableMapping, Dictionary<string, VariableField> fieldNameToParentVariableMapping) MigrateFields(FieldExpressionParser fieldExpressionParser, MetricRepository metricRepository,
        Subset subset,
        MetaDataContext dbContext)
    {
        var varCodesWithQuestionVariables = dbContext.VariableConfigurations.For(_productContext, true).AsEnumerable()
            .Select(v => (Variable: v, QuestionDefinition: v.Definition as QuestionVariableDefinition)).Where(v => v.QuestionDefinition is not null)
            .ToLookup(x => x.QuestionDefinition.QuestionVarCode, StringComparer.OrdinalIgnoreCase); //SQL Server case insensitive

        var fieldUsages = metricRepository.GetAll()
            .SelectMany(measure => measure.GetFieldDependencies().Distinct(), (m, f) => (Usage: (object) m, FieldName: f.Name))
            .Concat(_fieldExpressionParser.GetDeclaredVariables().SelectMany(v => v.FieldDependencies.Distinct(), (v, f) => (Usage: (object)v, FieldName: f.Name)))
            .GroupBy(t => t.FieldName, t => t.Usage, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToArray(), StringComparer.OrdinalIgnoreCase);
        var questionUsageCounts = metricRepository.GetAll()
            .SelectMany(measure => measure.GetFieldDependencies().Select(
                fd => fd.GetDataAccessModelOrNull(subset.Id)?.QuestionModel.VarCode
            ).Distinct())
            .Where(v => v is not null)
            .GroupBy(varCode => varCode)
            .ToDictionary(g => g.Key, g => g.Count());

        var responseFieldDescriptors = _responseFieldManager.GetAllFields().Where(f => f.IsAvailableForSubset(subset.Id));
        var fieldsByVarCode =
            responseFieldDescriptors.Select(field => (Field: field, Model: field.GetDataAccessModel(subset.Id)))
            .Where(t => t.Model?.QuestionModel?.VarCode != null)
            .GroupBy(t => t.Model.QuestionModel.VarCode, t => t.Field, StringComparer.OrdinalIgnoreCase);

        //var multiFieldGroups = fieldGroups.Where(f => f.Count() > 1).ToArray();

        var dependenciesOnField = fieldExpressionParser.GetDeclaredVariables()
            .Join(dbContext.VariableConfigurations.For(_productContext, true), v => v.Identifier, vc => vc.Identifier,
                (v, vc) => (v, vc))
            .SelectMany(v => v.v.FieldDependencies, (v, f) => (f, v))
            .ToLookup(t => t.f, t => t.v.vc);

        var fieldNameToVariableMapping = new Dictionary<string, VariableField>(StringComparer.OrdinalIgnoreCase);
        var fieldNameToParentVariableMapping = new Dictionary<string, VariableField>(StringComparer.OrdinalIgnoreCase);
        var entityInstanceConfigurations = _entityInstanceRepositorySql.EntityInstanceConfigurations(dbContext, true).ToDictionary(ei => (ei.EntityTypeIdentifier, ei.SurveyChoiceId));

        foreach (var fieldGroup in fieldsByVarCode.OrderBy(f => f.Key))
        {
            if (!questionUsageCounts.ContainsKey(fieldGroup.Key))
            {
                _logger.Log(LogLevel.Information,
                    $"Not attempting to migrate unused fields referencing the varcode {fieldGroup.Key}");
                continue;
            }

            var potentialParents = fieldGroup
                // >= is a temporary hack for missing entities due to https://app.shortcut.com/mig-global/story/85185/detect-dynamic-choiceset-parentage
                .Where(f => f.EntityCombination.Count >= f.GetDataAccessModel(subset.Id).QuestionModel.EntityCount)
                .OrderByDescending(f =>
                {
                    var variableIdentifier = GetIdentifier(f);
                    return varCodesWithQuestionVariables[fieldGroup.Key].Any(v => v.Variable.Identifier == variableIdentifier);
                }).ThenBy(f => f.GetDataAccessModel(subset.Id).ConfigIncorrect)
                .ThenByDescending(f => f.EntityCombination.Count)
                .ThenByDescending(f => fieldUsages.GetValueOrDefault(f.Name, []).Length);
            var parent = potentialParents.FirstOrDefault();

            if (parent is null)
            {
                System.Diagnostics.Debug.WriteLine($"MIGRATION: There is no parent for this question with varcode {fieldGroup.Key}");
                continue;
            }

            var usedNonParents = fieldGroup.Except(parent.Yield()).Where(f => fieldUsages.ContainsKey(f.Name))
                .ToArray();

            var nonParentsByIsAlias =
                usedNonParents.ToLookup(f => f.EntityCombination.Count == parent.EntityCombination.Count);
            var parentAliases = nonParentsByIsAlias[true].ToArray();

            var questionVariable = WriteQuestionVariable(fieldExpressionParser, parent, dbContext, varCodesWithQuestionVariables, dependenciesOnField, subset);
            fieldNameToVariableMapping[parent.Name] = (questionVariable, parent);
            foreach (var f in fieldGroup)
            {
                fieldNameToParentVariableMapping[f.Name] = (questionVariable, parent);
            }
            if (parent.EntityCombination.Any())
            {
                WriteEntityTypeAndInstancesToDb(parent, subset, dbContext, entityInstanceConfigurations);
            }

            WriteReferencingChildVariables(fieldExpressionParser, parentAliases, usedNonParents, parent,
                questionVariable, dbContext, dependenciesOnField, fieldNameToVariableMapping, subset);
        }

        return (fieldNameToVariableMapping, fieldNameToParentVariableMapping);
    }

    private void AddDependencyForDependents(
        ILookup<ResponseFieldDescriptor, VariableConfiguration> dependenciesOnField, ResponseFieldDescriptor? parent,
        VariableConfiguration questionVariable, MetaDataContext dbContext)
    {
        foreach (var variableConfiguration in dependenciesOnField[parent])
        {
            var variableDependency = new VariableDependency()
                { Variable = variableConfiguration, DependentUponVariable = questionVariable };
            if (variableConfiguration.Identifier == questionVariable.Identifier)
            {
                // Happens when we've removed a clashing unused questionvariable e.g. in finance "Consumer_Segment"
                _logger.LogWarning($"Skipping direct cyclic dependency for {questionVariable.Identifier}");
                continue;
            }

            variableConfiguration.VariableDependencies.Add(variableDependency);
            dbContext.VariableConfigurations.Update(variableConfiguration);
        }
    }

    private void WriteEntityTypeAndInstancesToDb(ResponseFieldDescriptor parent, Subset subset,
        MetaDataContext metaDataContext, Dictionary<(string EntityTypeIdentifier, int SurveyChoiceId), EntityInstanceConfiguration> entityInstanceConfigurations)
    {
        var fieldDefinitionModel = parent.GetDataAccessModel(subset.Id);
        var entityChoiceSetPairs = fieldDefinitionModel.OrderedEntityColumns.Join(fieldDefinitionModel.QuestionModel.GetAllChoiceSets(),
            ec => ec.DbLocation, c => c.Location, (ec, c) => (ec.EntityType, c.ChoiceSet));
        foreach (var (ec, choiceSet) in entityChoiceSetPairs)
        {
            _entityTypeRepositorySql.Save(ec.Identifier, ec.DisplayNameSingular,
                    ec.DisplayNamePlural, ec.SurveyChoiceSetNames,
                    ec.CreatedFrom, metaDataContext, allowDuplicateDisplayNames: true);

            var loadedInstances = _entityRepository.GetInstancesOf(ec.Identifier, subset).ToDictionary(ei => ei.Id);
            foreach (var choice in choiceSet.Choices)
            {
                var entityInstance = loadedInstances.GetValueOrDefault(choice.SurveyChoiceId);
                var existingConfig = entityInstanceConfigurations.GetValueOrDefault((ec.Identifier, choice.SurveyChoiceId));

                var config = Save(subset, metaDataContext, entityInstance, ec, choice, existingConfig);
                if (config != null)
                {
                    entityInstanceConfigurations[(ec.Identifier, choice.SurveyChoiceId)] = config;
                }
            }
        }
    }

    private EntityInstanceConfiguration Save(Subset subset, MetaDataContext metaDataContext, EntityInstance entityInstance, EntityType ec,
        Choice choice, EntityInstanceConfiguration existingConfig)
    {
        if (entityInstance is null)
        {
            return _entityInstanceRepositorySql.Save(subset, ec.Identifier, choice.SurveyChoiceId,
                choice.GetDisplayName(), false,
                null, choice.ImageURL, metaDataContext, existingConfig);
        }
        else if (entityInstance.Name != entityInstance.Identifier || entityInstance.StartDateForSubset(subset.Id) != existingConfig?.StartDateBySubset.GetValueOrDefault(subset.Id, null)
                                                                  || entityInstance.EnabledForSubset(subset.Id) != (existingConfig?.EnabledBySubset.GetValueOrDefault(subset.Id, true) ?? false))
        {
            return _entityInstanceRepositorySql.Save(subset, ec.Identifier, entityInstance.Id,
                entityInstance.Name, entityInstance.EnabledForSubset(subset.Id),
                entityInstance.StartDateForSubset(subset.Id), entityInstance.ImageURL, metaDataContext, existingConfig);
        }

        return null;
    }

    private void WriteReferencingChildVariables(FieldExpressionParser fieldExpressionParser,
        ResponseFieldDescriptor[] parentAliases, ResponseFieldDescriptor[] usedNonParents,
        ResponseFieldDescriptor parent, VariableConfiguration parentVariableConfiguration,
        MetaDataContext metaDataContext,
        ILookup<ResponseFieldDescriptor, VariableConfiguration> dependenciesOnField,
        Dictionary<string, VariableField> fieldNameToVariableMapping, Subset subset)
    {
        if (parentAliases.Any())
        {
            _logger.Log(LogLevel.Information,
                $"There are fields that are just aliases: {string.Join(", ", parentAliases.Select(p => p.Name))}");
        }
        foreach (var childField in usedNonParents)
        {
            var definition = GetGroupedVariableDefinition(childField, parentVariableConfiguration.Identifier, parent, subset) ??
                             GetChildVariableDefinition(childField, parentVariableConfiguration.Identifier, parent, subset);

            var childVariableConfiguration = CreateVariableConfiguration(definition, childField.Name);
            childVariableConfiguration.VariableDependencies.Add(new VariableDependency
            {
                Variable = childVariableConfiguration, DependentUponVariable = parentVariableConfiguration
            });

            var variable = WriteVariableConfigurationToDb(fieldExpressionParser, childVariableConfiguration.Identifier, childVariableConfiguration, metaDataContext);
            if (variable is not null) fieldNameToVariableMapping[childField.Name] = (variable, childField);
            AddDependencyForDependents(dependenciesOnField, childField, childVariableConfiguration, metaDataContext);
        }
    }

    private VariableConfiguration CreateVariableConfiguration(VariableDefinition definition, string displayName,
        Dictionary<string, VariableConfiguration> variablesByIdentifier = null)
    {
        var variableDependencies = new List<VariableDependency>();
        var variableConfiguration = new VariableConfiguration
        {
            DisplayName = displayName,
            Identifier = GetIdentifier(displayName, variablesByIdentifier),
            Definition = definition,
            ProductShortCode = _productContext.ShortCode,
            SubProductId = _productContext.SubProductId,
            VariableDependencies = variableDependencies
        };
        if (variablesByIdentifier is not null)
        {
            variableDependencies.AddRange(GetIdentifiersOfDependencies(definition, variablesByIdentifier)
                .Select(v => new VariableDependency(){Variable = variableConfiguration, DependentUponVariable = v}));
        }
        return variableConfiguration;
    }

    private VariableConfiguration[] GetIdentifiersOfDependencies(VariableDefinition definition, Dictionary<string, VariableConfiguration> variablesByIdentifier)
    {
        string numericExpression = definition.GetPythonExpression();
        try
        {
            var variable =
                _fieldExpressionParser.ParseUserNumericExpressionOrNull(numericExpression, false);
            return variable.VariableInstanceDependencies.Select(x => x.Identifier)
                .Select(variablesByIdentifier.GetValueOrDefault).Where(v => v != null).Distinct().ToArray();
        }
        catch(Exception e)
        {
            _logger.LogError(e, $"Variable won't load after migration - can't parse {numericExpression}");
            return [];
        }
    }

    private static string GetIdentifier(ResponseFieldDescriptor childField) => NameGenerator.EnsureValidPythonIdentifier(childField.Name);

    private static string GetIdentifier(string displayName,
        Dictionary<string, VariableConfiguration> variablesByIdentifier = null)
    {
        string firstPart = displayName.Split(" (").First();
        if (variablesByIdentifier?.ContainsKey(firstPart) != true) displayName = firstPart;
        return NameGenerator.EnsureValidPythonIdentifier(displayName);
    }

    private VariableConfiguration WriteQuestionVariable(FieldExpressionParser fieldExpressionParser,
        ResponseFieldDescriptor parent, MetaDataContext metaDataContext,
        ILookup<string, (VariableConfiguration Variable, QuestionVariableDefinition QuestionDefinition)>
            varCodesWithQuestionVariables, ILookup<ResponseFieldDescriptor, VariableConfiguration> dependenciesOnField, Subset subset)
    {
        string identifier = GetIdentifier(parent);
        var dataAccessModel = parent.GetDataAccessModel(subset.Id);

        double? scaleFactor = null;
        var question = dataAccessModel.QuestionModel;

        var currentScaleFactor = dataAccessModel.ScaleFactor.GetValueOrDefault(1);
        double? forceScaleFactor =
            Math.Abs(Math.Abs(scaleFactor.GetValueOrDefault(1) / currentScaleFactor) - 1) < 0.000000001
                ? null
                : currentScaleFactor;
        var questionVariableDefinition = new QuestionVariableDefinition
        {
            QuestionVarCode = question.VarCode,
            EntityTypeNames = dataAccessModel.OrderedEntityColumns
                .Select(x => (x.DbLocation.UnquotedColumnName, x.EntityType.Identifier)).ToList(),
            RoundingType = scaleFactor is not null ? SqlRoundingType.Floor : SqlRoundingType.Round,
            ForceScaleFactor = forceScaleFactor
        };
        var variableConfiguration = new VariableConfiguration
        {
            DisplayName = parent.Name,
            Identifier = identifier,
            Definition = questionVariableDefinition,
            ProductShortCode = _productContext.ShortCode,
            SubProductId = _productContext.SubProductId,
            VariablesDependingOnThis = new List<VariableDependency>(),
        };
        var existingForVarcode = varCodesWithQuestionVariables[question.VarCode].Where(x => !x.Variable.Identifier.EndsWith("_US")).ToArray();
        if (existingForVarcode.Count() > 1)
        {
            System.Diagnostics.Debug.WriteLine($"MIGRATION: Varcode {question.VarCode} already has multiple question variables");
        }

        foreach (var varcode in existingForVarcode)
        {
            if (varcode is { Variable: { } existingVariable, QuestionDefinition: { } existingQuestionDefinition })
            {
                if (!existingQuestionDefinition.EntityTypeNames.IsEquivalent(questionVariableDefinition.EntityTypeNames))
                    metaDataContext.VariableConfigurations.Remove(existingVariable);
            }
        }

        foreach (var varcode in existingForVarcode)
        {
            if (varcode is { Variable: { } existingVariable, QuestionDefinition: { } existingQuestionDefinition })
            {
                if (existingQuestionDefinition.EntityTypeNames.IsEquivalent(questionVariableDefinition.EntityTypeNames))
                {
                    if (existingVariable?.Identifier != variableConfiguration.Identifier)
                    {
                        _logger.LogWarning($@"Existing question variable with different identifier:
{JsonConvert.SerializeObject(existingVariable)}
Proposed variable:
{JsonConvert.SerializeObject(variableConfiguration)}");
                    }

                    return existingVariable;
                }
            }
        }

        var variableWrittenToDb = WriteVariableConfigurationToDb(fieldExpressionParser, identifier, variableConfiguration,
            metaDataContext, true);
        if (!existingForVarcode.Any())
        {
            AddDependencyForDependents(dependenciesOnField, parent, variableWrittenToDb, metaDataContext);
        }

        return variableWrittenToDb;
    }

    private VariableDefinition GetGroupedVariableDefinition(ResponseFieldDescriptor child, string identifier, ResponseFieldDescriptor parent, Subset subset)
    {
        var parentDataAccessModel = parent.GetDataAccessModel(subset.Id);
        var childDataAccessModel = child.GetDataAccessModel(subset.Id);

        //TODO Consider "1:value" case, could probably grab the datavaluelocation and do an "and result.thatentityname"
        if (parentDataAccessModel.OrderedEntityColumns.Length == childDataAccessModel.OrderedEntityColumns.Length) return null;
        bool invalid = false;
        var resultEntityTypeNames = new List<string>();
        var components = parentDataAccessModel.OrderedEntityColumns.Select((pe, i) =>
            {
                if (childDataAccessModel.FilterColumns.SingleOrDefault(cfc1 => cfc1.Location == pe.DbLocation) is
                    { Location: not null, Value: var filterValue })
                {
                    return new InstanceListVariableComponent()
                    {
                        FromEntityTypeName = pe.EntityType.Identifier,
                        FromVariableIdentifier = identifier,
                        InstanceIds = [filterValue],
                        Operator = InstanceVariableComponentOperator.And,
                        ResultEntityTypeNames = resultEntityTypeNames,
                        AnswerMinimum = null
                    };
                }

                if (childDataAccessModel.OrderedEntityColumns.SingleOrDefault(cfc1 => cfc1.DbLocation == pe.DbLocation) is {EntityType:
                        { } childType})
                {
                    if (childType.Identifier != pe.EntityType.Identifier)
                    {
                        invalid = true; //Needs to be renamed in variable expression
                        return null;
                    }
                    resultEntityTypeNames.Add(pe.EntityType.Identifier);
                }
                else
                {
                    // TODO: Create a component here that drops an entity rather than falling back on variable expression
                }

                return null;
            }
        ).Where(x => x is not null).ToList<VariableComponent>();
        
        if (invalid || !components.Any()) return null;
        return SingleGroupDefinition(components, CompositeVariableSeparator.And);
    }

    private VariableDefinition GetChildVariableDefinition(ResponseFieldDescriptor child, string identifier,
        ResponseFieldDescriptor parent, Subset subset)
    {
        var parentDataAccessModel = parent.GetDataAccessModel(subset.Id);
        var childDataAccessModel = child.GetDataAccessModel(subset.Id);

        //TODO Consider "1:value" case, could probably grab the datavaluelocation and do an "and result.thatentityname"
        if (parentDataAccessModel.OrderedEntityColumns.Length == childDataAccessModel.OrderedEntityColumns.Length)
            return new FieldExpressionVariableDefinition()
            {
                Expression = _allowZero + parent.Name,
            };

        var resultEntityTypeNames = new List<string>();
        bool forceExpression = false;
        List<(string Constraint, VariableComponent Component)> entities = parentDataAccessModel.OrderedEntityColumns.Select(pe =>
            {
                if (childDataAccessModel.FilterColumns.SingleOrDefault(cfc1 => cfc1.Location == pe.DbLocation) is
                    { Location: not null, Value: var filterValue })
                {
                    string s = $"{pe.EntityType.Identifier} = {filterValue}";
                    return (s, (VariableComponent)new InstanceListVariableComponent()
                    {
                        FromEntityTypeName = pe.EntityType.Identifier,
                        FromVariableIdentifier = identifier,
                        InstanceIds = [filterValue],
                        Operator = InstanceVariableComponentOperator.And,
                        ResultEntityTypeNames = resultEntityTypeNames, //TODO Check if this is right, it possibly should just be all entity types except this one
                        AnswerMinimum = null
                    });
                }


                if (childDataAccessModel.OrderedEntityColumns.SingleOrDefault(cfc1 => cfc1.DbLocation == pe.DbLocation)
                    is
                    {
                        EntityType:
                        { } childType
                    })
                {
                    if (childType.Identifier != pe.EntityType.Identifier)
                    {
                        resultEntityTypeNames.Add(childType.Identifier);
                        string e = $"{pe.EntityType.Identifier} = result.{childType.Identifier}";
                        forceExpression = true;//Need expression to rename entity
                        return (e, (VariableComponent)null);
                    }

                    resultEntityTypeNames.Add(childType.Identifier);
                    return (VariableComponentsPythonExtensions.GetTypeEqualsResultType(pe.EntityType.Identifier), null);
                }

                forceExpression = true;
                return (null, null);
            }
        ).ToList();

        if (entities.Any() && !forceExpression)
        {
            var variableComponents = entities.Select(e => e.Component).ToList();
            return SingleGroupDefinition(variableComponents, CompositeVariableSeparator.And);
        }

        var entityTypes = string.Join(", ", entities.Select(x => x.Constraint).Where(x => x is not null));
        string expression = _allowZero + $"max(response.{identifier}({entityTypes}), default=None)";
        return new FieldExpressionVariableDefinition()
        {
            Expression = expression,
        };
    }

    private VariableDefinition SingleGroupDefinition(List<VariableComponent> components, CompositeVariableSeparator compositeVariableSeparator)
    {
        string entityTypeName = "Condition_" + _uniqueEntityTypeNameSuffix++;
        return new SingleGroupVariableDefinition()
        {
            Group =
                new VariableGrouping
                {
                    ToEntityInstanceId = 1,
                    ToEntityInstanceName = entityTypeName + "_1",
                    Component = components.OnlyOrDefault() ?? new CompositeVariableComponent()
                    {
                        CompositeVariableSeparator = compositeVariableSeparator,
                        CompositeVariableComponents = components
                    }
                },
            AggregationType = AggregationType.MaxOfMatchingCondition

        };
    }

    private VariableConfiguration WriteVariableConfigurationToDb(IFieldExpressionParser fieldExpressionParser,
        string identifier,
        VariableConfiguration variableConfiguration,
        MetaDataContext metaDataContext, bool skipExistenceCheck = false)
    {
        if (!skipExistenceCheck && metaDataContext.VariableConfigurations.For(_productContext, true).FirstOrDefault(v => v.Identifier == identifier) is
                { } matchedVariable)
        {
            if (!JsonConvert.SerializeObject(matchedVariable.Definition)
                    .Equals(JsonConvert.SerializeObject(variableConfiguration.Definition)))
            {
                _logger.LogWarning(
                    $"Unable to create QuestionVariable: {identifier}, custom variable exists with the same identifier");
                return null;
            }
        }
        else
        {
            variableConfiguration =
                _writeableVariableConfigurationRepository.Create(variableConfiguration, metaDataContext, null);
        }

        try
        {
            fieldExpressionParser.DeclareOrUpdateVariable(variableConfiguration);
        }
        catch
        {
        }

        return variableConfiguration;
    }
}
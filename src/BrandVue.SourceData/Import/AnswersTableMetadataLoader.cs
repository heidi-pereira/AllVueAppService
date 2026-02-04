using System.Collections.Immutable;
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.MetaData;
using BrandVue.EntityFramework.MetaData.VariableDefinitions;
using BrandVue.EntityFramework.ResponseRepository;
using BrandVue.SourceData.AnswersMetadata;
using BrandVue.SourceData.Settings;
using BrandVue.SourceData.Variable;
using Humanizer;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import
{
    public class AnswersTableMetadataLoader
    {
        public const string WasAskedQuestionTextPrefix = "Was asked: ";
        private readonly IInstanceSettings _instanceSettings;
        private readonly IBrandVueDataLoaderSettings _settings;
        private readonly ILogger _logger;
        private readonly IChoiceSetReader _choiceSetReader;
        private readonly IVariableConfigurationRepository _variableConfigurationRepository;
        private readonly IProductContext _productContext;
        private readonly MapFileQuestionAdapter _mapFileQuestionAdapter;

        public AnswersTableMetadataLoader(ILogger logger, IInstanceSettings instanceSettings,
            IChoiceSetReader choiceSetReader, IVariableConfigurationRepository variableConfigurationRepository,
            IProductContext productContext, IBrandVueDataLoaderSettings settings)
        {
            _mapFileQuestionAdapter = new MapFileQuestionAdapter(logger);
            _logger = logger;
            _instanceSettings = instanceSettings;
            _choiceSetReader = choiceSetReader;
            _variableConfigurationRepository = variableConfigurationRepository;
            _productContext = productContext;
            _settings = settings;
        }

        public void AdjustForAnswersTable(ISubsetRepository subsetRepository,
            ILoadableEntityTypeRepository entityTypeRepository,
            ILoadableEntityInstanceRepository entityInstanceRepository,
            ILoadableEntitySetRepository entitySetRepository,
            ILoadableResponseFieldManager responseFieldManager)
        {

            foreach (var subset in subsetRepository)
            {
                if (subset.Disabled) continue;

                int[] surveyIds = subset.GetSurveyIdForSubset();

                if (subset.Id == BrandVueDataLoader.All)
                {
                    bool isValidSubset = _choiceSetReader.SurveyHasNonTestCompletes(surveyIds);
                    if (!isValidSubset) continue;
                }

                var (questionsRaw, choiceSetsGroups) = _choiceSetReader.GetChoiceSetTuple(surveyIds);
                if (questionsRaw == null)
                {
                    continue;
                }
                var questions = questionsRaw
                    .ToLookup(q => q.VarCode, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        qs => qs.Key,
                        qs => qs.OrderBy(q => q.SurveyId).First(),
                        StringComparer.OrdinalIgnoreCase
                    );

                //Picks the one from the earliest survey set, for stability's sake
                var choiceSetGroupAliasLookup = choiceSetsGroups
                    .SelectMany(g => g.Alternatives.Select(a => (a.Name, g.Canonical)))
                    .ToLookup(p => p.Name, p => p.Canonical, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(p => p.Key, p => p.OrderBy(cs => cs.SurveyId).First(),
                        StringComparer.OrdinalIgnoreCase);

                foreach (var question in questions.Select(questionEntry => questionEntry.Value))
                {
                    question.SectionChoiceSet =
                        GetCanonicalForChoiceSet(choiceSetGroupAliasLookup, question.SectionChoiceSet);
                    question.PageChoiceSet =
                        GetCanonicalForChoiceSet(choiceSetGroupAliasLookup, question.PageChoiceSet);
                    question.QuestionChoiceSet =
                        GetCanonicalForChoiceSet(choiceSetGroupAliasLookup, question.QuestionChoiceSet);
                    question.AnswerChoiceSet =
                        GetCanonicalForChoiceSet(choiceSetGroupAliasLookup, question.AnswerChoiceSet);
                }

                _mapFileQuestionAdapter.MapExistingFieldsToQuestions(entityInstanceRepository, responseFieldManager, subset,
                    questions, choiceSetGroupAliasLookup);

                if (_instanceSettings.GenerateFromAnswersTable || _settings.AutoCreateEntities)
                {
                    var populatedEntityTypes = entityTypeRepository.Where(t => entityInstanceRepository.GetInstancesOf(t.Identifier, subset).Any());
                    var subsetEntityChoiceSetMapper = new SubsetEntityChoiceSetMapper(subset, choiceSetsGroups,
                        entityTypeRepository, entityInstanceRepository, entitySetRepository,
                        choiceSetGroupAliasLookup,
                        _logger, _instanceSettings.ForceBrandTypeAsDefault, populatedEntityTypes,
                        _choiceSetReader,
                        _productContext);

                    var fieldModelsBySubset = responseFieldManager.GetAllFields()
                        .SelectMany(_ => subsetRepository,
                            (f, s) => (Subset: s, FieldModel: f.GetDataAccessModelOrNull(s.Id)))
                        .Where(f => f.FieldModel?.QuestionModel != null)
                        .ToLookup(f => f.FieldModel.QuestionModel.VarCode, StringComparer.OrdinalIgnoreCase); // Case insensitive sql server
                    CreateFieldsForUnusedQuestions(responseFieldManager, fieldModelsBySubset, subset, choiceSetGroupAliasLookup,
                        questions.Values, subsetEntityChoiceSetMapper);
                }
            }

            if (_instanceSettings.GenerateFromAnswersTable)
            {
                SetDefaultEntityTypeIfBrandNotSet(subsetRepository, entityTypeRepository, responseFieldManager, entityInstanceRepository, entitySetRepository);
            }

        }

        private static ChoiceSet GetCanonicalForChoiceSet(Dictionary<string, ChoiceSet> choiceSetGroupAliasLookup, ChoiceSet choiceset) =>
            choiceset is null ? null : choiceSetGroupAliasLookup[choiceset.Name];

        private static void SetDefaultEntityTypeIfBrandNotSet(ISubsetRepository subsetRepository,
            ILoadableEntityTypeRepository entityTypeRepository,
            ILoadableResponseFieldManager responseFieldManager, ILoadableEntityInstanceRepository entityInstanceRepository,
            ILoadableEntitySetRepository entitySetRepository)
        {
            var firstEnabledSubset = subsetRepository.OrderBy(x => x.Disabled).ThenBy(x => x.Order).First();
            var defaultEntity = responseFieldManager.GetAllFields().SelectMany(f => f.EntityCombination, (f, e) => (f, e))
                .GroupBy(kvp => kvp.e)
                .Where(g => entityInstanceRepository.GetInstancesOf(g.Key.Identifier, firstEnabledSubset).Count > 1)
                .OrderByDescending(g => g.Key.IsBrand)
                .ThenByDescending(g => g.Count())
                .FirstOrDefault()?.Key;

            if (defaultEntity != null)
            {
                entityTypeRepository.SetDefaultEntityType(defaultEntity);

                AddEntitySets(subsetRepository, entityInstanceRepository, entitySetRepository, defaultEntity.Identifier);
            }

            if (defaultEntity != null && !defaultEntity.IsBrand)
            {
                if (!entityInstanceRepository.GetInstancesOf(EntityType.Brand, firstEnabledSubset).Any())
                {
                    entityTypeRepository.Remove(EntityType.Brand);
                }
                else if (!defaultEntity.IsBrand)
                {
                    AddEntitySets(subsetRepository, entityInstanceRepository, entitySetRepository, EntityType.Brand);
                }
            }
        }

        private static void AddEntitySets(ISubsetRepository subsetRepository, ILoadableEntityInstanceRepository entityInstanceRepository,
            ILoadableEntitySetRepository entitySetRepository, string defaultEntityTypeName)
        {
            foreach (var subset in subsetRepository)
            {
                var firstThirty = entityInstanceRepository.GetInstancesOf(defaultEntityTypeName, subset).Take(30).ToArray();
                var emptyAveragesArray = Array.Empty<EntitySetAverageMappingConfiguration>();
                var entitySet = new EntitySet(null, "Default", firstThirty, null, false, false, emptyAveragesArray, firstThirty.FirstOrDefault());
                entitySetRepository.Add(entitySet, defaultEntityTypeName, subset);
            }
        }

        private static DbLocation GetAnswerColumnName(Question question)
        {
            return question.AnswerChoiceSet != null ? DbLocation.AnswerEntity : DbLocation.AnswerShort;
        }

        private void CreateFieldsForUnusedQuestions(ILoadableResponseFieldManager responseFieldManager,
            ILookup<string, (Subset Subset, FieldDefinitionModel FieldModel)> fieldModelsByVarCode, Subset subset,
            Dictionary<string, ChoiceSet> choiceSetGroupAliasLookup,
            IReadOnlyCollection<Question> qStats, SubsetEntityChoiceSetMapper entityChoiceSetMapper)
        {
            var allVariables = _variableConfigurationRepository.GetAll().ToArray();
            var variableIdentifiersUsed = allVariables.Select(v => v.Identifier).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var variableDisplayNamesUsed = allVariables.Select(v => v.DisplayName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var questionVariablesByVarCode = allVariables
                .Where(v => v.Definition is QuestionVariableDefinition)
                .Select(v => (v.Identifier, Definition: (QuestionVariableDefinition)v.Definition))
                .ToLookup(v => v.Definition.QuestionVarCode, StringComparer.OrdinalIgnoreCase);

            var fieldModelsForSubset = responseFieldManager.GetAllFields()
                .Select(f => f.GetDataAccessModelOrNull(subset.Id)).Where(f => f != null)
                .ToArray();
            var usedFieldNamesForSubset = fieldModelsForSubset.Select(f => f.Name).ToHashSet();
            var fieldNameGenerator = new NameGenerator(qStats);
            var questionsToAddFieldsFor = qStats
                .Select(q => QuestionVariableMapping.CreateQuestionVariableMapping(q, choiceSetGroupAliasLookup, questionVariablesByVarCode, fieldModelsByVarCode))
                // Crucial to create variables with configuration first so they get priority on assigning names to choice sets
                .OrderByDescending(t => t.FieldModels.Length).ThenByDescending(t => t.VariableAndId.Definition is not null) 
                .ThenBy(t => t.QuestionModel.VarCode).ToArray();

            foreach (var (question, originalVariableSetup, canonicalChoiceSets, allFieldModelsForVarCode) in questionsToAddFieldsFor)
            {
                using var _ = _logger.BeginScope("Varcode {varcode}", question.VarCode);
                var variable = originalVariableSetup;
                if (variable == default)
                {
                    // Don't generate for tags unless already loaded from map file
                    if (IsTag(question) && allFieldModelsForVarCode.Length == 0) continue;
                    var choiceSets = question.AvailableQuestionChoiceSets();

                    var entityDefinitionsToCreate = choiceSets
                        .Select(cs => GetOrCreateType(cs, entityChoiceSetMapper).EntityModel)
                        .ToArray();
                    if (HasNoDuplicateEntityTypes(entityDefinitionsToCreate))
                    {
                        var idealFieldIdentifier = fieldNameGenerator.GenerateFieldName(question);
                        var existing = allFieldModelsForVarCode
                            .Where(x => x.Subset.Equals(subset) && x.FieldModel.Name.Equals(idealFieldIdentifier, StringComparison.OrdinalIgnoreCase))
                            .ToArray();
                        if (existing.Any() && existing.All(x => x.FieldModel.OrderedEntityCombination.Length != entityDefinitionsToCreate.Length))
                        {
                            idealFieldIdentifier += "_entity";
                        }
                        variable = AddVariable(question, entityDefinitionsToCreate, idealFieldIdentifier, subset, responseFieldManager, variableIdentifiersUsed, variableDisplayNamesUsed, usedFieldNamesForSubset, allFieldModelsForVarCode);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "{product} Skipping generating question variable for varcode '{varcode}' since it has duplicate entity types",
                            _productContext, question.VarCode);
                        continue;
                    }
                }

                if (variable == default) continue;

                var choicesetTypeMapping = CreateTypesForChoiceSets(variable.Definition, canonicalChoiceSets, entityChoiceSetMapper);
                EnsureEntityInstancesAndSetsCreatedForThisSubset(entityChoiceSetMapper, choicesetTypeMapping);

                if (NoFieldForSubset(allFieldModelsForVarCode, subset)
                    || NoOtherSubsetWillSetItUp(allFieldModelsForVarCode, variable))
                {
                    var fieldDefinitionModelsToAdd = ConstructPotentialFields(variable, question,
                        choicesetTypeMapping.Select(c => c.EntityModel).ToArray());
                    fieldDefinitionModelsToAdd =
                        fieldDefinitionModelsToAdd.Where(f => CanAdd(responseFieldManager, usedFieldNamesForSubset, f));
                    AddFields(responseFieldManager, subset, fieldDefinitionModelsToAdd);
                }
            }
        }

        private static bool NoOtherSubsetWillSetItUp(
            (Subset Subset, FieldDefinitionModel FieldModel)[] allFieldModelsForVarCode,
            (string Identifier, QuestionVariableDefinition Definition) variable) =>
            allFieldModelsForVarCode.All(f =>
                f.FieldModel.OrderedEntityCombination.Length != variable.Definition.EntityTypeNames.Count
            );

        /// <remarks> e.g. from map file </remarks>
        private static bool NoFieldForSubset(
            (Subset Subset, FieldDefinitionModel FieldModel)[] allFieldModelsForVarCode, Subset subset) =>
            !allFieldModelsForVarCode.Any(f => f.Subset.Equals(subset));

        private void EnsureEntityInstancesAndSetsCreatedForThisSubset(SubsetEntityChoiceSetMapper entityChoiceSetMapper,
            (ChoiceSet ChoiceSet, EntityFieldDefinitionModel EntityModel)[] choiceSetTypeMapping)
        {
            foreach (var (cs, type) in choiceSetTypeMapping)
            {
                entityChoiceSetMapper.CreateEntityInstancesAndSets(type.EntityType, cs);
            }
        }

        private record QuestionVariableMapping(
            Question QuestionModel,
            (string Identifier, QuestionVariableDefinition Definition) VariableAndId,
            List<(ChoiceSet ChoiceSet, DbLocation Location)> CanonicalChoiceSets,
            (Subset Subset, FieldDefinitionModel FieldModel)[] FieldModels)
        {
            public static QuestionVariableMapping CreateQuestionVariableMapping(Question q,
                Dictionary<string, ChoiceSet> choiceSetGroupAliasLookup,
                ILookup<string, (string Identifier, QuestionVariableDefinition Definition)> questionVariablesByVarCode,
                ILookup<string, (Subset Subset, FieldDefinitionModel FieldModel)> fieldModelsByVarCode)
            {
                var canonicalChoiceSets = q.GetCanonicalChoiceSets(choiceSetGroupAliasLookup, q.SupportsSingleEntryChoiceSets).ToList();
                var requiredDbLocations = canonicalChoiceSets.Select(cs => cs.Location.UnquotedColumnName);
                var existingQuestionVariablesWithVarCode = questionVariablesByVarCode[q.VarCode];
                var v = existingQuestionVariablesWithVarCode.FirstOrDefault(v => v.Definition.EntityTypeNames.Select(t => t.DbLocationUnquotedColumnName).IsEquivalent(requiredDbLocations));
                var fieldModels = fieldModelsByVarCode[q.VarCode].ToArray();
                return new QuestionVariableMapping(QuestionModel: q, VariableAndId: v, CanonicalChoiceSets: canonicalChoiceSets, FieldModels: fieldModels);
            }
        }

        private (string Identifier, QuestionVariableDefinition questionVariableDefinition) AddVariable(
            Question question,
            EntityFieldDefinitionModel[] entityDefinitionsToCreate,
            string idealFieldIdentifier, Subset subset, ILoadableResponseFieldManager responseFieldManager,
            HashSet<string> variableIdentifiersUsed,
            HashSet<string> variableDisplayNamesUsed,
            HashSet<string> usedFieldNamesForSubset,
            (Subset Subset, FieldDefinitionModel FieldModel)[] allFieldModelsForVarCode)
        {
            bool compatibleAcrossSubsets = CompatibleAcrossSubsets(responseFieldManager, idealFieldIdentifier, entityDefinitionsToCreate);
            var attempts =
                compatibleAcrossSubsets ? 3 : 1;
            if (ChooseUnique(variableIdentifiersUsed, idealFieldIdentifier, '_', attempts) is not {} identifier)
            {
                if (!compatibleAcrossSubsets) return AddVariableWithSubsetSuffix(question, entityDefinitionsToCreate,
                    idealFieldIdentifier, subset, responseFieldManager, variableIdentifiersUsed,
                    variableDisplayNamesUsed, usedFieldNamesForSubset, allFieldModelsForVarCode);

                _logger.LogWarning("{product} Found incompatible variable, but compatible field. Skipping.", _productContext);
                return (null, null);

            }

            //Possible perf optimization: CreateMany method for first startup
            var questionVariableDefinition = new QuestionVariableDefinition
            {
                QuestionVarCode = question.VarCode,
                EntityTypeNames = CreateEntityTypeNames(entityDefinitionsToCreate),
            };
            var variable = (Identifier: identifier, questionVariableDefinition);

            var fieldDefinitionModels = ConstructPotentialFields(variable, question, entityDefinitionsToCreate).ToArray();
            var nameInUseWithinSubset = fieldDefinitionModels.Any(f => usedFieldNamesForSubset.Contains(f.Name));
            if (!allFieldModelsForVarCode.Any(f => f.Subset.Equals(subset)))
            {
                if (nameInUseWithinSubset)
                {
                    // Still return the variable for use, but don't save since it's configured elsewhere (map file)
                    _logger.LogWarning(
                        "{product} Skipping saving question variable for varcode '{varcode}' since it clashes with an existing field",
                        _productContext, question.VarCode);
                }
                else
                {
                    var differsPerSubset =
                        fieldDefinitionModels.Any(f =>
                            !CompatibleAcrossSubsets(responseFieldManager, f.Name, f.OrderedEntityColumns));
                    if (differsPerSubset) // e.g. Drinks US has household income as a radio question, Drinks UK has it as a scale question
                    {
                        return AddVariableWithSubsetSuffix(question, entityDefinitionsToCreate,
                            idealFieldIdentifier,
                            subset, responseFieldManager, variableIdentifiersUsed, variableDisplayNamesUsed,
                            usedFieldNamesForSubset, allFieldModelsForVarCode);
                    }


                    string idealDisplayName = identifier.Humanize();
                    var displayName = ChooseUnique(variableDisplayNamesUsed, idealDisplayName, ' ', 10);
                    if (displayName == null)
                    {
                        _logger.LogWarning(
                            "{product} Skipping generating question variable for varcode '{varcode}' since it clashes with the existing variable display name '{idealDisplayName}'",
                            _productContext, question.VarCode, idealDisplayName);
                        return default;
                    }

                    var variableConfiguration = new VariableConfiguration
                    {
                        DisplayName = displayName,
                        Identifier = variable.Identifier,
                        Definition = questionVariableDefinition,
                        ProductShortCode = _productContext.ShortCode,
                        SubProductId = _productContext.SubProductId,
                        VariablesDependingOnThis = new List<VariableDependency>(),
                    };
                    _variableConfigurationRepository.Create(variableConfiguration, ImmutableArray<string>.Empty);
                }
            }

            return variable;
        }

        private (string Identifier, QuestionVariableDefinition questionVariableDefinition) AddVariableWithSubsetSuffix(Question question,
            EntityFieldDefinitionModel[] entityDefinitionsToCreate, string idealFieldIdentifier, Subset subset,
            ILoadableResponseFieldManager responseFieldManager, HashSet<string> variableIdentifiersUsed,
            HashSet<string> variableDisplayNamesUsed, HashSet<string> usedFieldNamesForSubset,
            (Subset Subset, FieldDefinitionModel FieldModel)[] allFieldModelsForVarCode)
        {

            var subsetSuffix = "_" + subset.Id;
            if (!idealFieldIdentifier.EndsWith(subsetSuffix))
            {
                var addVariable = AddVariable(question, entityDefinitionsToCreate, idealFieldIdentifier + subsetSuffix,
                        subset, responseFieldManager, variableIdentifiersUsed, variableDisplayNamesUsed,
                        usedFieldNamesForSubset, allFieldModelsForVarCode);
                return addVariable;
            }

            _logger.LogWarning(
                "{product} Skipping generating question variable for varcode '{varcode}' since it clashes with the existing variable identifier '{identifier}'",
                _productContext, question.VarCode, idealFieldIdentifier);
            return (null, null);
        }

        private static List<(string UnquotedColumnName, string Name)> CreateEntityTypeNames(EntityFieldDefinitionModel[] entityDefinitionsToCreate)
        {
            return entityDefinitionsToCreate
                .Select(x => (x.DbLocation.UnquotedColumnName, Name: x.EntityType.Identifier)).ToList();
        }

        private static bool CompatibleAcrossSubsets(ILoadableResponseFieldManager responseFieldManager, string fieldName, IEnumerable<EntityFieldDefinitionModel> entityFieldDefinitionModels)
        {
            return !responseFieldManager.TryGet(fieldName, out var existing) || existing.CompatibleAccessModelAcrossSubsets(FieldDefinitionModel.CanonicalColumnOrder(entityFieldDefinitionModels));
        }

        private static void AddFields(ILoadableResponseFieldManager responseFieldManager, Subset subset,
            IEnumerable<FieldDefinitionModel> fieldDefinitionModelsToAdd)
        {
            foreach (var fieldDefinitionModel in fieldDefinitionModelsToAdd)
            {
                responseFieldManager.Load((subset.Id, fieldDefinitionModel));
            }
        }

        private IEnumerable<FieldDefinitionModel> ConstructPotentialFields((string Identifier, QuestionVariableDefinition Definition) variable, Question question, EntityFieldDefinitionModel[] entityFieldDefinitionModels)
        {
            var fieldDefinitionModelsToAdd = Enumerable.Empty<FieldDefinitionModel>();
            if (HasNoDuplicateEntityTypes(entityFieldDefinitionModels) && !_settings.AutoCreateEntities)
            {
                // If there's an answer choiceset we create a version without that entity for convenience in using as a base
                // Though this could also be done with variable  expressions
                if (question.AnswerChoiceSet != null)
                {
                    var askedEntityDefinitions = entityFieldDefinitionModels.Where(d => d.DbLocation != DbLocation.AnswerEntity).ToArray();
                    fieldDefinitionModelsToAdd = fieldDefinitionModelsToAdd.Concat(ConstructPotentialFields(question, askedEntityDefinitions, variable.Identifier, variable.Definition.RoundingType, variable.Definition.ForceScaleFactor, FieldType.Asked));
                }

                fieldDefinitionModelsToAdd = fieldDefinitionModelsToAdd.Concat(ConstructPotentialFields(question, entityFieldDefinitionModels, fieldIdentifierBase: variable.Identifier, variable.Definition.RoundingType, variable.Definition.ForceScaleFactor, FieldType.Standard));
            }

            return fieldDefinitionModelsToAdd;
        }

        private static (ChoiceSet ChoiceSet, EntityFieldDefinitionModel EntityModel)[] CreateTypesForChoiceSets(
            QuestionVariableDefinition questionVariableDefinition,
            List<(ChoiceSet ChoiceSet, DbLocation Location)> canonicalChoiceSets,
            SubsetEntityChoiceSetMapper entityChoiceSetMapper)
        {
            var entityDefinitions = canonicalChoiceSets.Join(questionVariableDefinition.EntityTypeNames,
                cs => cs.Location.UnquotedColumnName, v => v.DbLocationUnquotedColumnName,
                (cs, v) => GetOrCreateType(cs, entityChoiceSetMapper, v.EntityTypeName)
            ).ToArray();
            return entityDefinitions;
        }

        private static bool HasNoDuplicateEntityTypes(EntityFieldDefinitionModel[] entityDefinitions)
        {
            //Currently in AnswersTableLazyDataLoader.GetColumnGroups it expects there to be a single column for an entity type
            //Excluding these from being created as it will fall over when trying to build measure sql otherwise
            var entityTypes = entityDefinitions.Select(e => e.EntityType).ToArray();
            return entityTypes.Distinct().Count() == entityTypes.Length;
        }

        private static string ChooseUnique(HashSet<string> variableDisplayNamesUsed, string baseDisplayName, char suffixSeparator, int maxDuplicates)
        {
            int i = 2;
            string name = baseDisplayName;
            while (!variableDisplayNamesUsed.Add(name))
            {
                if (i > maxDuplicates) return null;
                name = baseDisplayName + suffixSeparator + i++;
            }

            return name;
        }
        private static bool CanAdd(ILoadableResponseFieldManager responseFieldManager, HashSet<string> usedFieldNames, FieldDefinitionModel fieldDefinitionModel)
        {
            return usedFieldNames.Add(fieldDefinitionModel.Name) &&
                   CompatibleAcrossSubsets(responseFieldManager, fieldDefinitionModel.Name, fieldDefinitionModel.OrderedEntityColumns);
        }

        private IEnumerable<FieldDefinitionModel> ConstructPotentialFields(Question question, EntityFieldDefinitionModel[] entityDefinitions,
            string fieldIdentifierBase, SqlRoundingType roundingType, double? forceScaleFactor, FieldType fieldType)
        {
            string questionText = question.QuestionText;
            switch (fieldType)
            {
                case FieldType.Asked:
                {
                    fieldIdentifierBase += "_asked";
                    questionText = WasAskedQuestionTextPrefix + questionText;
                }
                    break;
                case FieldType.Base:
                {
                    var identifier = entityDefinitions.FirstOrDefault()?.SafeSqlEntityIdentifier;
                    fieldIdentifierBase += $"_base{(identifier == null ? "" : $"_{identifier}")}";
                }
                    break;
            }

            if (entityDefinitions.Count() > 1)
            {
                foreach (var entityDefinition in entityDefinitions)
                {
                    foreach (var field in ConstructPotentialFields(question,
                                 new[] { entityDefinition }, fieldIdentifierBase, roundingType,
                                 forceScaleFactor, FieldType.Base))
                    {
                        yield return field;
                    }
                }
            }

            double? scaleFactor = null;
            if (question.NumberFormat != null)
            {
                scaleFactor = (int)Math.Pow(10, GetDecimalPlaces(question.NumberFormat));
            }
            else if (question.AnswerChoiceSet is null && question.MinimumValue.HasValue && question.MaximumValue.HasValue)
            {
                scaleFactor = forceScaleFactor;
            }

            var fieldDefinitionModel =
                new FieldDefinitionModel(fieldIdentifierBase, "", "", "", questionText, scaleFactor, question.VarCode, EntityInstanceColumnLocation.Unknown, entityDefinitions.SingleOrDefault(d => d.DbLocation == DbLocation.AnswerEntity)?.EntityType.Identifier ?? "", false, null, entityDefinitions, roundingType)
                {
                    QuestionModel = question,
                    ValueDbLocation = GetAnswerColumnName(question),
                    UnsafeSqlVarCodeBase = question.VarCode,
                    IsAutoGenerated = true,
                    FieldType = fieldType,
                };
            yield return fieldDefinitionModel;
        }

        public static int GetDecimalPlaces(string input)
        {
            var countOfDecimalPlaceholders = input.LastIndexOf('.') is var lastDotIndex
                                             && lastDotIndex != -1
                ? input.Substring(lastDotIndex + 1).TakeWhile(c => c == '#' || c == '0').Count()
                : 0;
            return countOfDecimalPlaceholders;
        }

        private static bool IsTag(Question q)
        {
            return q.MasterType == "TAG";
        }

        private static (ChoiceSet ChoiceSet, EntityFieldDefinitionModel EntityModel) GetOrCreateType(
            (ChoiceSet ChoiceSet, DbLocation Location) cs, SubsetEntityChoiceSetMapper subsetEntityChoiceSetMapper,
            string forceEntityTypeName = null)
        {
            var responseEntityType = subsetEntityChoiceSetMapper.GetOrCreateType(cs.ChoiceSet, forceEntityTypeName);
            return (cs.ChoiceSet, EntityModel: new EntityFieldDefinitionModel(cs.Location, responseEntityType, responseEntityType.Identifier));
        }
    }
}
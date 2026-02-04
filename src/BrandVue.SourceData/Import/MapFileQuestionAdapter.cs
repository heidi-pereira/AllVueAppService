using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.SourceData.CommonMetadata;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.Import
{
    /// <summary>
    /// Every startup, map-file brandvues match the map file field definitions, entity types and entity instances with the question and choice set models.
    /// </summary>
    public class MapFileQuestionAdapter
    {
        private readonly ILogger _logger;

        public MapFileQuestionAdapter(ILogger logger)
        {
            _logger = logger;
        }

        public void MapExistingFieldsToQuestions(IEntityRepository entityRepository, IResponseFieldManager responseFieldManager, Subset subset,
            Dictionary<string, Question> questionsByVarcodeBase, Dictionary<string, ChoiceSet> choiceSetGroupAliasLookup)
        {
            // Deterministic order is needed as mapping of one field can affect mapping of another
            foreach (var field in responseFieldManager.GetAllFields().OrderBy(f => f.Name))
            {
                var fieldDefinitionModel = field.GetDataAccessModelOrNull(subset.Id);
                if (fieldDefinitionModel is null)
                    continue;

                string varCodeBase = fieldDefinitionModel.FullV2VarCode;
                string varCodeSuffix = null;
                if (varCodeBase is null) continue;
                if (!questionsByVarcodeBase.TryGetValue(varCodeBase, out var question))
                {

                    (varCodeBase, _) = SplitOnLast(varCodeBase, '!');
                    if (!questionsByVarcodeBase.TryGetValue(varCodeBase, out question))
                    {
                        (varCodeBase, varCodeSuffix) = SplitOnLast(varCodeBase, '_');
                        if (!questionsByVarcodeBase.TryGetValue(varCodeBase, out question))
                        {
                            // Set to some default so a valid db query can be generated later (which will return 0 results)
                            // This is to match the graceful hiding of issues in the current code, but it is logged here at least
                            fieldDefinitionModel.ValueDbLocation = DbLocation.AnswerShort;
                            _logger.LogWarning($"Cannot find question for {fieldDefinitionModel.FullV2VarCode} {LoggingTags.Question} {LoggingTags.Config}");
                            continue;
                        }
                    }
                }

                fieldDefinitionModel.QuestionModel = question;
                fieldDefinitionModel.UnsafeSqlVarCodeBase = varCodeBase;
                fieldDefinitionModel.MapDataColumnsToAnswerColumns(question, _logger);


                var (unmatchedChoiceSets, unmatchedEntityCols) = MapVarcodeSuffixToSectionChoiceSet(choiceSetGroupAliasLookup, question, fieldDefinitionModel, varCodeSuffix);
                MatchExistingEntities(fieldDefinitionModel, unmatchedChoiceSets, entityRepository, subset, unmatchedEntityCols);
            }
        }

        private static (string First, string Second) SplitOnLast(string @base, char separator)
        {
            var varCodeParts = @base.Split(separator);
            string varCodeSuffix = null;
            if (varCodeParts.Length > 1)
            {
                varCodeSuffix = varCodeParts.Skip(varCodeParts.Length - 1).FirstOrDefault();
                @base = string.Join(separator.ToString(), varCodeParts.Take(varCodeParts.Length - 1));
            }

            return (@base, varCodeSuffix);
        }

        private (List<(ChoiceSet ChoiceSet, DbLocation Location)> unmatchedChoiceSets, List<EntityFieldDefinitionModel> unmatchedEntityCols) MapVarcodeSuffixToSectionChoiceSet(Dictionary<string, ChoiceSet> choiceSetGroupAliasLookup, Question question,
            FieldDefinitionModel fieldDefinitionModel, string varCodeSuffix)
        {
            var questionCanonicalChoiceSets = question.GetCanonicalChoiceSets(choiceSetGroupAliasLookup, includeSingleEntryChoiceSets:true).ToArray();
            var unmatchedChoiceSets = questionCanonicalChoiceSets
                .Where(cs =>
                    cs.Location != DbLocation.SectionEntity &&
                    !fieldDefinitionModel.FilterColumns.Any(fc => fc.Location == cs.Location)).ToList();
            var unmatchedEntityCols = fieldDefinitionModel.OrderedEntityColumns.ToList();

            var sectionChoiceSet = questionCanonicalChoiceSets.SingleOrDefault(cs => cs.Location == DbLocation.SectionEntity);
            if (varCodeSuffix != null)
            {
                var suffixParts = varCodeSuffix.Trim('{', '}').Split(new[] {':'}, StringSplitOptions.RemoveEmptyEntries);

                if (sectionChoiceSet.ChoiceSet == null)
                {
                    _logger.LogWarning(
                        $"Field {fieldDefinitionModel.Name} has a suffix of {varCodeSuffix} which will be ignored, please remove it from the definition {LoggingTags.ChoiceSet} {LoggingTags.Config}");
                    fieldDefinitionModel.ConfigIncorrect = true;
                }
                else if (int.TryParse(varCodeSuffix, out int sectionId))
                {
                    var col = (sectionChoiceSet.Location, sectionId);
                    fieldDefinitionModel.AddFilter(col);
                    _logger.LogInformation(
                        $"Field filter: `{fieldDefinitionModel.Name} has {(col.Location.SafeSqlReference)} set to {sectionId}");
                    _logger.LogInformation(
                        $"No existing entity mapping: {sectionChoiceSet.ChoiceSet.Name} for {fieldDefinitionModel.Name}");
                }
                else if (!suffixParts.Any() && unmatchedEntityCols.OnlyOrDefault(c => c.EntityType.IsBrand) is {} brandColumn)
                {
                    // V1 map file
                    MapColumn(brandColumn, sectionChoiceSet.Location, sectionChoiceSet.ChoiceSet, unmatchedChoiceSets);
                    unmatchedEntityCols.Remove(brandColumn);
                }
                else if (suffixParts.OnlyOrDefault(p => p != "value") is {} entityName &&
                         unmatchedEntityCols.OnlyOrDefault(t =>
                             StringComparer.OrdinalIgnoreCase.Equals(t.EntityType.Identifier, entityName)) is {} entityColumn)
                {
                    // V2 map file such as Positive_buzz_{brand} or Positive_buzz_{brand:value}
                    MapColumn(entityColumn, sectionChoiceSet.Location, sectionChoiceSet.ChoiceSet, unmatchedChoiceSets);
                    unmatchedEntityCols.Remove(entityColumn);
                }
                else
                {
                    _logger.LogWarning(
                        $"Couldn't find section entity for {fieldDefinitionModel.Name} from {fieldDefinitionModel.FullV2VarCode}, falling back on normal matching");
                    unmatchedChoiceSets.Insert(0, sectionChoiceSet);
                }

                // I think we could move this into dashboard builder along with the other value location stuff
                if (suffixParts.Contains("value"))
                {
                    fieldDefinitionModel.ValueDbLocation = DbLocation.SectionEntity;
                }
            }

            return (unmatchedChoiceSets, unmatchedEntityCols);
        }

        private void MatchExistingEntities(FieldDefinitionModel fieldDefinitionModel,
            List<(ChoiceSet ChoiceSet, DbLocation Location)> unmatchedChoiceSets,
            IEntityRepository entityRepository, Subset subset, IReadOnlyCollection<EntityFieldDefinitionModel> unmatchedEntityCols)
        {
            if (unmatchedEntityCols.Count > 0 && unmatchedChoiceSets.Count == 0)
            {
                _logger.LogError($"{fieldDefinitionModel.Name} has entity column(s) ({string.Join(", ", unmatchedEntityCols.Select(c => c.EntityType.Identifier))}) but no choice sets");
            }
            else
            {
                var bestFirstMatches = unmatchedEntityCols.Select(col =>
                    (Column: col, Mappings:
                        unmatchedChoiceSets.Select(ucs => (ucs.ChoiceSet, ucs.Location,
                                MatchedAlready: col.EntityType.SurveyChoiceSetNames.Contains(ucs.ChoiceSet.Name),
                                Similarity: GetSimilarity(entityRepository, subset, col, ucs.ChoiceSet)))
                            .OrderByDescending(m => m.MatchedAlready)
                            .ThenByDescending(m => m.Similarity).ToArray())
                ).OrderByDescending(m => m.Mappings.First().Similarity);

                foreach (var (column, mappings) in bestFirstMatches)
                {
                    var bestMatch = mappings.First();
                    var bestMatchChoiceSet = bestMatch.ChoiceSet;
                    if (unmatchedChoiceSets.Any(c => c.ChoiceSet == bestMatchChoiceSet))
                    {
                        MapColumn(column, bestMatch.Location, bestMatchChoiceSet, unmatchedChoiceSets);
                        var nextBestMatch = mappings.Skip(1).FirstOrDefault();
                        LogEntityTypeToChoiceSetMatchQuality(fieldDefinitionModel.Name, column.EntityType.Identifier, bestMatch, nextBestMatch);
                    }
                    else
                    {
                        // This means it was also the best match for the other column. Definitely worth checking
                        _logger.LogWarning(
                            $"Already mapped to something else, abandoning {bestMatch.Similarity}% mapping: {column.EntityType.Identifier}, {bestMatchChoiceSet.Name} {LoggingTags.ChoiceSet} {LoggingTags.Config}");
                    }
                }
            }

            EdgeCaseGuessValueLocation(fieldDefinitionModel, unmatchedChoiceSets);

            foreach (var choiceSet in unmatchedChoiceSets.Where(cs => cs.ChoiceSet.Choices.Count > 1 &&  cs.Location != fieldDefinitionModel.ValueDbLocation))
            {
                _logger.LogWarning(
                    $"Field {fieldDefinitionModel.Name} is missing an entity definition for {choiceSet.ChoiceSet.Name}, so will get ALL");
            }
        }

        private void LogEntityTypeToChoiceSetMatchQuality(string fieldName, string entityTypeName, (ChoiceSet ChoiceSet, DbLocation Location, bool MatchedAlready, byte Similarity) bestMatch, (ChoiceSet ChoiceSet, DbLocation Location, bool MatchedAlready, byte Similarity) nextBestMatch)
        {
            var nextBestMatchSimilarity = nextBestMatch.ChoiceSet != null ? nextBestMatch.Similarity : 0;
            var nextMatchSimilarityDiff = bestMatch.Similarity - nextBestMatchSimilarity;
            var logLevel =
                bestMatch.Similarity < 80 || bestMatch.Similarity < 100 && nextMatchSimilarityDiff < 20
                    ?
                    LogLevel.Error
                    :
                    bestMatch.Similarity < 90
                        ? LogLevel.Warning
                        :
                        bestMatch.Similarity < 95 || nextMatchSimilarityDiff < 20
                            ? LogLevel.Information
                            :
                            LogLevel.Debug;
            if (!_logger.IsEnabled(logLevel)) return;

            var logMessage = $"Field: {fieldName}, entityType: {entityTypeName} matched to: {bestMatch.ChoiceSet.Name} ({bestMatch.Similarity}%).";
            var logMessageSuffix = nextBestMatch.ChoiceSet == null ? "No other match found." : $"Next best match was: {nextBestMatch.ChoiceSet.Name} ({nextBestMatch.Similarity}%).";
            _logger.Log(logLevel, $"{logMessage} {logMessageSuffix} {LoggingTags.ChoiceSet} {LoggingTags.Config}");
        }

        /// <summary>
        /// This is probably nearly redundant. If it doesn't turn up in any log, remove it.
        /// </summary>
        private void EdgeCaseGuessValueLocation(FieldDefinitionModel fieldDefinitionModel, List<(ChoiceSet ChoiceSet, DbLocation Location)> unmatchedChoiceSets)
        {
            if (fieldDefinitionModel.ValueDbLocation == null)
            {
                _logger.LogWarning($"Guessing DataValueColumn for {fieldDefinitionModel.Name}");
                if (unmatchedChoiceSets.Count == 1 &&
                    IsSingleChoice(fieldDefinitionModel.QuestionModel))
                {
                    // For single choice questions, the answer value is always 1, so we probably should pull through the unmatched choiceset value instead
                    fieldDefinitionModel.ValueDbLocation = unmatchedChoiceSets.Single().Location;
                    unmatchedChoiceSets.Clear();
                }
                else if (unmatchedChoiceSets.Any(cs => cs.Location == DbLocation.AnswerEntity))
                {
                    // Assume the answer is the value by default - this case will end up logged below if it was ambiguous since there'll be one left over
                    fieldDefinitionModel.ValueDbLocation = DbLocation.AnswerEntity;
                    unmatchedChoiceSets.RemoveAll(cs => cs.Location == DbLocation.AnswerEntity);
                }
                else
                {
                    // We still set this even for text type questions, because some texts contain integers (which are cast and stored here)
                    // For text metrics (wordles) there's a separate code path in AnswerTextResponseRepository, which always looks at text
                    fieldDefinitionModel.ValueDbLocation = DbLocation.AnswerShort;
                }
            }
        }

        private static void MapColumn(EntityFieldDefinitionModel column, DbLocation bestMatchLocation, ChoiceSet bestMatchChoiceSet,
            List<(ChoiceSet ChoiceSet, DbLocation Location)> unmatchedChoiceSets)
        {
            column.DbLocation = bestMatchLocation;
            column.EntityType.SurveyChoiceSetNames.Add(bestMatchChoiceSet.Name);
            unmatchedChoiceSets.RemoveAll(c =>
                c.Location == column.DbLocation
            );
        }

        private static byte GetSimilarity(IEntityRepository entityRepository, Subset subset, EntityFieldDefinitionModel col, ChoiceSet choiceSet)
        {
            var instances = entityRepository.GetInstancesOf(col.EntityType.Identifier, subset);
            return SimilarityPercentage(instances, choiceSet);
        }

        private static byte SimilarityPercentage(IReadOnlyCollection<EntityInstance> instances, ChoiceSet cs2)
        {
            var denominator = Math.Max(instances.Count, cs2.Choices.Count);
            var numerator= 100 * instances.Count(c1 =>
                    cs2.ChoicesBySurveyChoiceId.ContainsKey(c1.Id) // I tried comparing the names, but they're usually slightly differently punctuated
            );
            return (byte) (numerator / denominator);
        }

        private static bool IsSingleChoice(Question q) =>
            q.MasterType switch
            {
                "COMBO" => true,
                "RADIO" => true,
                _ => false
            };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MIG.SurveyPlatform.MapGeneration.Model;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    /// <summary>
    /// Lots of action parsing is already done by CreateMaximumChoiceSets.
    /// The code here picks out specific cases whether we need to tweak field definitions because:
    ///  * Aren't dealt with by that method (outside this codebase) and should be potentially pushed down to there.
    ///  * Could be dealt with more succinctly in the map file, but we haven't invented a syntax for it yet.
    ///  * It requires intelligence to make a connection between the information in the mqml and what field is required, so this contains a heuristic.
    /// </summary>
    internal class ActionParser
    {
        private readonly HashSet<string> m_SubsetChoiceSetNames;
        private const int PreAction = 0;
        private static readonly Regex ValueEqualsRegex = new Regex(@"^\s*Value\s*\(\s*#(?<FieldNameGroup>[^#]*)#\s*\)\s*=\s*(?<FieldValueGroup>[^)])\s*$");
        private static readonly Regex BrandTextFromIdFieldRegex = new Regex(@"\s*ChoiceSetItemById\s*\(\s*(?<ChoiceSetName>[^,]*?)\s*,\s*#(?<BrandIdFieldPrefix>[^)#]*)_#(?<BrandIdVariable>[^)#]*?)##\s*\)");

        private readonly HashSet<string> m_ConsumerSegmentQuestionNames;
        private readonly HashSet<string> m_BrandChoiceSetNames;

        public ActionParser(IEnumerable<string> consumerSegmentQuestionNames, IEnumerable<string> brandChoiceSetNames, params QChoiceSet[] subsets)
        {
            m_SubsetChoiceSetNames = subsets.Any() ? subsets.First().Parent.GetNamesOfRelatedChoiceSets(subsets)
            : new HashSet<string>();
            m_ConsumerSegmentQuestionNames = new HashSet<string>(consumerSegmentQuestionNames);
            m_BrandChoiceSetNames = new HashSet<string>(brandChoiceSetNames);
        }

        public IReadOnlyCollection<FieldMutator> GetFieldMutators(QQuestionnaire quest, IReadOnlyCollection<BrandContextTag> brandContextTags)
        {
            var mutatorList = quest.PageList.SelectMany(page =>
                GetFieldMutatorsForRepeatingPage(page).Concat(GetFieldMutatorsForRepeatingSection(page))
            ).ToList();
            var fieldMutators = mutatorList.ToLookup(fm => fm.ApplyToBaseVariableCode);
            return mutatorList.Concat(GetFieldMutatorsForBrandContextTags(quest, fieldMutators, brandContextTags)).ToList();
        }

        private IEnumerable<FieldMutator> GetFieldMutatorsForBrandContextTags(QQuestionnaire quest, ILookup<string, FieldMutator> fieldMutators, IReadOnlyCollection<BrandContextTag> brandContextTags)
        {
            return quest.PageList.SelectMany(p => GetFieldMutatorsForPageBrandContextTags(p, brandContextTags, fieldMutators));
        }

        /// <summary>
        /// The dashboard builder needs a separate field for each repeat of a page since fields cannot be arrays/tabular.
        /// This may differ in the mqrep, I'm not sure.
        /// </summary>
        private IEnumerable<FieldMutator> GetFieldMutatorsForRepeatingPage(QPage page)
        {
            return page.ActionList
                .Where(a => a.ActionClass == PreAction && a.Active && a.ActionType == SurveyEngine.Core.ActionType.SimpleRoute && a.Query.Contains("Value"))
                .Select(a => ValueEqualsRegex.Match(a.Query))
                .Where(m => m.Success)
                .SelectMany(m => m_ConsumerSegmentQuestionNames.Select(n => new FieldMutator
                {
                    ApplyToBaseVariableCode = n,
                    ShouldOutputBaseField = true,
                    BaseNameSuffix = "_" + page.Name,
                    ProfileFieldName = m.Groups["FieldNameGroup"].Value,
                    ProfileFieldValue = m.Groups["FieldValueGroup"].Value
                }));
        }

        /// <summary>
        /// Ideally people wouldn't write survey questions in which we have to scan the text to guess it's related to a brand choice set, but currently there's no standard way to explicitly tell us that if it's not the choice set for the question
        /// </summary>
        /// <param name="quest"></param>
        /// <returns></returns>
        public IEnumerable<BrandContextTag> GetBrandContextTags(QQuestionnaire quest)
        {
            return quest.PageList.SelectMany(GetBrandContextTags);
        }

        private IEnumerable<BrandContextTag> GetBrandContextTags(QPage page)
        {
            return page.ActionList
                .Where(a => a.Active && a.ActionType == SurveyEngine.Core.ActionType.SetTag && a.Query.Contains("ChoiceSetItemById"))
                .Select(tag => new { TagName = tag.TagName, Match = BrandTextFromIdFieldRegex.Match(tag.Query) })
                .Where(m => m.Match.Success && m_BrandChoiceSetNames.Contains(m.Match.Groups["ChoiceSetName"].Value))
                .ToLookup(m => m.Match.Groups["BrandIdFieldPrefix"].Value)
                .Select(group => new BrandContextTag
                {
                    TextTagName = group.First().TagName, //All the same
                    ChoiceSetNames = group.Select(t => t.Match.Groups["ChoiceSetName"].Value).ToList(),
                    BrandIdFieldPrefix = group.Key
                });
        }

        private IEnumerable<FieldMutator> GetFieldMutatorsForPageBrandContextTags(QPage page, IReadOnlyCollection<BrandContextTag> brandContextTags,
            ILookup<string, FieldMutator> fieldMutators)
        {
            return page.Questions.oList
                .SelectMany(q =>
                {
                    var tagsUsed = brandContextTags.Where(tag => q.TextsContain(tag.TextTagReference)).ToList();
                    return GetNewFieldMutatorsForQuestionBrandContextTags(fieldMutators, tagsUsed, q);
                });
        }

        private static IEnumerable<FieldMutator> GetNewFieldMutatorsForQuestionBrandContextTags(ILookup<string, FieldMutator> existingFieldMutators, List<BrandContextTag> tagsUsed, QQuestion question)
        {
            if (!tagsUsed.Any()) yield break;
            if (tagsUsed.Count() > 1) throw new NotImplementedException("Too many brand context tags used in question");
            var tagBrandIdField = tagsUsed.First().BrandIdFieldPrefix;

            var varRoot = question.OriginalNameOrVarCode;
            var existingFieldMutatorsForQuestion = existingFieldMutators[varRoot].ToList();

            if (!existingFieldMutatorsForQuestion.Any())
                yield return new FieldMutator()
                {
                    ApplyToBaseVariableCode = varRoot,
                    ShouldOutputBaseField = false,
                    IsBrandField = true,
                    BrandIdTag = tagBrandIdField
                };
            foreach (var existingFieldMutator in existingFieldMutatorsForQuestion)
            {
                existingFieldMutator.ShouldOutputBaseField = false;
                existingFieldMutator.IsBrandField = true;
                existingFieldMutator.BrandIdTag = tagBrandIdField + existingFieldMutator.FieldSuffix;
                //They're already in the collection, no need to return again
            }
        }

        /// <summary>
        /// Brand fields' repeating sections are dealt with by the dashboard builder.
        /// Profile fields need to manually list each possible repetition separately.
        /// </summary>
        public IEnumerable<FieldMutator> GetFieldMutatorsForRepeatingSection(QPage page)
        {
            return page.ActionList
                .Where(a => a.Active && a.ActionType == SurveyEngine.Core.ActionType.RepeatSectionForChoiceSet && !m_BrandChoiceSetNames.Contains(a.BasedOn))
                .SelectMany(action => GetFieldMutatorsForRepeatingSection(page.ParentQuestionnaire, action));
        }

        private IEnumerable<FieldMutator> GetFieldMutatorsForRepeatingSection(QQuestionnaire quest, QAction action)
        {
            var repeatedSection = quest.SectionFromName(action.Query);
            var questions = repeatedSection.Pages.oList.SelectMany(p => p.Questions.oList);
            var choicesToRepeatSectionFor = quest.ChoiceSetFromName(action.BasedOn).Choices.oList;

            return questions.SelectMany(q =>
            {
                if (m_SubsetChoiceSetNames.Contains(action.BasedOn))
                {
                    return new[]
                    {
                        new FieldMutator
                        {
                            ApplyToBaseVariableCode = q.OriginalNameOrVarCode,
                            ShouldOutputBaseField = false,
                            BaseNameSuffix = "_",
                            FieldSuffix = "_",
                            ProfileFieldName = "",
                            ProfileFieldValue = "",
                            HasSubsetNumericSuffix = true
                        }
                    };
                };
                return choicesToRepeatSectionFor.Select(sectionId => new FieldMutator
                {
                    ApplyToBaseVariableCode = q.OriginalNameOrVarCode,
                    ShouldOutputBaseField = false,
                    BaseNameSuffix = "_" + sectionId.Name,
                    FieldSuffix = "_" + sectionId.ID.ToString(),
                    ProfileFieldName = "",
                    ProfileFieldValue = ""
                });
            });
        }
    }
}
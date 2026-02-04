using System.Collections.Generic;
using System.Linq;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    internal static class QuestionnaireExtensions
    {
        public static HashSet<string> GetNamesOfRelatedChoiceSets(this QQuestionnaire quest, IReadOnlyCollection<QChoiceSet> chosenBrandChoiceSets, ushort lowerBoundPercent = 90, ushort upperBoundpercent = 110)
        {
            var chosenChoiceSetAncestors = new HashSet<string>(chosenBrandChoiceSets.SelectMany(GetOriginalAncestorNames));
            var relatedChoiceSets = quest.ChoiceSets.ChoiceSetList
                .Where(current => GetOriginalAncestorNames(current).Any(name => chosenChoiceSetAncestors.Contains(name)))
                .Select(cs => cs.Name);

            return new HashSet<string>(relatedChoiceSets);
        }

        private static IEnumerable<string> GetOriginalAncestorNames(QChoiceSet choiceSet)
        {
            if (choiceSet.PriorAncestor == null && choiceSet.PriorAncestor_AddTo == null) return new[] {choiceSet.Name};

            return new[] {choiceSet.PriorAncestor, choiceSet.PriorAncestor_AddTo}
                .Where(cs => cs != null).SelectMany(GetOriginalAncestorNames);
        }
    }
}
using BrandVue.EntityFramework.Answers;
using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.SourceData.Import
{
    public static class QuestionExtensions
    {
        public static IEnumerable<(ChoiceSet ChoiceSet, DbLocation Location)> GetCanonicalChoiceSets(this Question q, IReadOnlyDictionary<string, ChoiceSet> choiceSetGroupAliasLookup, bool includeSingleEntryChoiceSets = false)
        {
            var canonicalChoiceSets = q.GetAllChoiceSets().Where(cs => cs.ChoiceSet != null)
                .Select(cs => (choiceSetGroupAliasLookup[cs.ChoiceSet.Name], cs.Location));
            if (includeSingleEntryChoiceSets)
            {
                canonicalChoiceSets = Question.GetChoiceSetsWithAtLeastOneChoice(canonicalChoiceSets);
            }
            else
            {
                canonicalChoiceSets = Question.GetChoiceSetsWithAtLeastTwoChoices(canonicalChoiceSets);
            }
            return canonicalChoiceSets;
        }
    }
}
using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.SourceData.AnswersMetadata
{
    public class ChoiceSetGroup
    {
        public ChoiceSet Canonical { get; }
        public ChoiceSet[] Alternatives { get; }

        public ChoiceSetGroup(ChoiceSet canonical, ChoiceSet[] alternatives)
        {
            Canonical = canonical;
            Alternatives = alternatives;
        }
    }
}
using System.Linq;

namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal class Subset
    {
        public Subset(string nameInChoiceSet)
        {
            Description = nameInChoiceSet;
            var firstBracketIndex = nameInChoiceSet.IndexOf("(");
            var displayName = (firstBracketIndex != -1 ? nameInChoiceSet.Substring(0, firstBracketIndex) : nameInChoiceSet).Humanize();
            DisplayName = displayName;
            DisplayNameShort = displayName;
        }

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string DisplayNameShort { get; }
        public string Iso2LetterCountryCode { get; } = "GB";
        public string Description { get; }
        public int Order { get; set; }
        public string NumericSuffix { get; set; }
        public string Disabled { get; } = "";
        public string Environment { get; } = "";
        public string ExternalUrl { get; } = "";
    }
}
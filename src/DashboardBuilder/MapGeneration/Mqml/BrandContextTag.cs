using System.Collections.Generic;

namespace MIG.SurveyPlatform.MapGeneration.Mqml
{
    /// <summary>
    /// Tags which if they're mentioned in the page/question text, mean the question is associated with the brand id that tag contains
    /// </summary>
    public class BrandContextTag
    {
        public IReadOnlyCollection<string> ChoiceSetNames { get; set; }
        public string BrandIdFieldPrefix { get; set; }
        public string TextTagName { get; set; }
        public string TextTagReference => $"#{TextTagName}#";
    }
}
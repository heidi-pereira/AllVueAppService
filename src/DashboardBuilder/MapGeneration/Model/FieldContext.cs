using System.Collections.Generic;

namespace MIG.SurveyPlatform.MapGeneration.Model
{
    internal class FieldContext
    {
        public string SectionName { get; set; }
        public string PageName { get; set; }
        public string HumanBaseName { get; set; }
        public string HumanNameSuffix { get; set; }
        public float ScaleMin { get; set; }
        public float ScaleMax { get; set; }
        public bool IsRatingQuestion { get; set; }
        public bool IsImplicitlyAskedToEveryoneWhoSeesBrand { get; set; }

        public string GetHumanName(string baseSeparator)
        {
            var humanSuffix = !string.IsNullOrEmpty(HumanNameSuffix) ? $"{baseSeparator}{HumanNameSuffix}" : "";
            return HumanBaseName + humanSuffix;
        }
        public IReadOnlyCollection<string> Categories { get; set; }
        public QQuestion.MainOptTypes QuestionType { get; set; }
    }
}
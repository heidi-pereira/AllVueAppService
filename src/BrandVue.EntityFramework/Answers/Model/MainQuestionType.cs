namespace BrandVue.EntityFramework.Answers.Model
{
    public enum MainQuestionType
    {
        SingleChoice,
        MultipleChoice,
        Text,
        Value,
        Unknown,
        CustomVariable,
        GeneratedNumeric,
        HeatmapImage
    }

    public static class MainQuestionTypeExtensions
    {
        public static string DisplayName(this MainQuestionType questionType)
        {
            return questionType switch
            {
                MainQuestionType.SingleChoice => "Single choice",
                MainQuestionType.MultipleChoice => "Multiple choice",
                MainQuestionType.Text => "Text",
                MainQuestionType.Value => "Value",
                MainQuestionType.Unknown => "Unknown",
                MainQuestionType.CustomVariable => "Custom variable",
                MainQuestionType.GeneratedNumeric => "Automatically generated",
                MainQuestionType.HeatmapImage => "Heatmap image",
                _ => ""
            };
        }
    }
}

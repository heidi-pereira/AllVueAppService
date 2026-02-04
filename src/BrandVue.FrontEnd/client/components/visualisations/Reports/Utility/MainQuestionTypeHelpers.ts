import * as BrandVueApi from "../../../../BrandVueApi";

export const mainQuestionTypeToString = (questionType: BrandVueApi.MainQuestionType): string => {
    switch (questionType) {
        case BrandVueApi.MainQuestionType.SingleChoice:
            return "Single choice";
        case BrandVueApi.MainQuestionType.MultipleChoice:
            return "Multiple choice";
        case BrandVueApi.MainQuestionType.Text:
            return "Text";
        case BrandVueApi.MainQuestionType.Value:
            return "Value";
        case BrandVueApi.MainQuestionType.Unknown:
            return "Unknown";
        case BrandVueApi.MainQuestionType.CustomVariable:
            return "Custom variable";
        case BrandVueApi.MainQuestionType.GeneratedNumeric:
            return "Automatically generated";
        case BrandVueApi.MainQuestionType.HeatmapImage:
            return "Heatmap";
        default:
            return "";
    }
};

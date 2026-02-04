using BrandVue.EntityFramework.Answers.Model;

namespace BrandVue.Services.ServiceModels
{
    public class ResponseFieldModel
    {
        public string VarCode { get; set; }
        public string Label { get; set; }
        public string ValueLabels { get; set; }
        public string QuestionText { get; set; }
        public int QuestionId { get; set; }
        public string MasterType { get; set; }
        public ChoiceSetType? SplitByType { get; set; }
        public ChoiceSetType? FilterByType { get; set; }
        public int? SplitByChoiceId { get; set; }
        public int? FilterByChoiceId { get; set; }
        public IReadOnlyCollection<IGrouping<int, Answer>> GroupedData { get; internal set; } //responseID and answer

        public ResponseFieldModel(
            string name,
            string label,
            string valueLabels,
            string questionText,
            int questionId,
            string masterType,
            ChoiceSetType? splitByType,
            int? splitByChoice,
            ChoiceSetType? filterByType,
            int? filterByChoice)
        {
            VarCode = name;
            Label = label;
            ValueLabels = valueLabels;
            QuestionText = questionText;
            QuestionId = questionId;
            MasterType = masterType;
            SplitByType = splitByType;
            FilterByType = filterByType;
            SplitByChoiceId = splitByChoice;
            FilterByChoiceId = filterByChoice;
        }

        public ResponseFieldModel(string varCode, string label, string valueLabels, string questionText, int questionId, string masterType)
        {
            VarCode = varCode;
            Label = label;
            ValueLabels = valueLabels;
            QuestionText = questionText;
            QuestionId = questionId;
            MasterType = masterType;
        }
    }
}

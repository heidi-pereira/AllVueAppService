namespace Vue.Common.BrandVueApi.Models
{
    public class QuestionAnswer
    {
        public string AnswerType { get; set; }
        public List<QuestionChoice>? Choices { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public double? Multiplier { get; set; }
    }
}

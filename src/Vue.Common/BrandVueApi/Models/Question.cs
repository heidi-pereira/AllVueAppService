namespace Vue.Common.BrandVueApi.Models
{
    public class Question
    {
        public string QuestionId { get; set; }
        public string QuestionText { get; set; }
        public QuestionAnswer AnswerSpec { get; set; }
        public string[] Classes { get; set; }
    }
}

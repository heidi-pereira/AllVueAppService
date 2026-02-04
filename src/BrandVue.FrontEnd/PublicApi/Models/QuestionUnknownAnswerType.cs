namespace BrandVue.PublicApi.Models
{
    public class QuestionUnknownAnswerType : QuestionAnswer
    {
        public QuestionUnknownAnswerType()
        {
            AnswerType = AnswerTypeEnum.Unknown;
        }
    }
}
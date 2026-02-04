using Vue.Common.BrandVueApi.Models;

namespace Vue.Common.BrandVueApi
{
    public class QuestionWithSurveySets: Question
    {
        public List<SurveySet> SurveySets { get; set; } = new();
    }
    public record QuestionsAvailable(List<SurveySet> SurveySets,List<QuestionWithSurveySets> UnionOfQuestions);

    public interface IBrandVueApiClient
    {
        Task<QuestionsAvailable> GetProjectQuestionsAvailableAsync(string companyShortCode, string productShortCode, string subProductId, CancellationToken cancellationToken);
    }
}

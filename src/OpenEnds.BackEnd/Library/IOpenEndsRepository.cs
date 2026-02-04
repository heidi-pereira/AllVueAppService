using OpenEnds.BackEnd.Model;

namespace OpenEnds.BackEnd.Library
{
    public interface IOpenEndsRepository
    {
        Task<IList<QuestionWithAnswerStats>> GetQuestionsWithAnswerCount(string subProductId, List<int> surveyIds);
        Task<VueQuestion> GetQuestionByIdAsync(string subProductId, int questionId);
        Task<int> AnswerCountForQuestion(int questionId);
        Task<List<int>> GetUnarchivedResponsesForSurvey(ICollection<int> surveyIds);
        Task<IList<VueAnswer>> GetAnswersForQuestion(int questionId);
        Task<List<int>> GetSurveyIdsAsync(string subProductId);
        Task<string?> GetSurveyGroupNameByUrlSafeNameAsync(string urlSafeName);
        Task<string?> GetSurveyNameByIdAsync(int surveyId);
        Task<AllVueConfiguration?> GetSurveyConfigurationAsync(string subProductId);
    }
}
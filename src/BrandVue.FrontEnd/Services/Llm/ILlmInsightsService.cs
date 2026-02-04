using System.Threading;
using BrandVue.Models;
using BrandVue.SourceData.LlmInsights;


namespace BrandVue.Services.Llm
{
    public interface ILlmInsightsService
    {
        Task<LlmInsightResults> GetLlmInsightsAsync(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation> requestData, 
            CancellationToken cancellationToken);
        Task UpdateFeedbackUserCommentAsync(string id, string userComment, CancellationToken cancellationToken); 
        Task UpdateFeedbackUserUsefulnessAsync(string id,  bool? isUseful, CancellationToken cancellationToken);
        Task UpdateUserFeedbackSegmentCorrectnessAsync(string id, int segmentId, bool? isCorrect, CancellationToken cancellationToken);
        LlmInsightResults ToDto(LlmInsightsDocument llmInsightsDocument, string userId);
    }
}
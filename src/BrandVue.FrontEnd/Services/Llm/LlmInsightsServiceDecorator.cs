using System.Threading;
using BrandVue.Models;
using BrandVue.SourceData.LlmInsights;
namespace BrandVue.Services.Llm
{
    public class LlmInsightsServiceDecorator(
        ILlmInsightsRepository llmInsightsRepository,
        IUserContext userContext,
        ILlmInsightsService llmInsightsService) : ILlmInsightsService
    {
        public async Task<LlmInsightResults> GetLlmInsightsAsync(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation> requestData,
            CancellationToken cancellationToken)
        {
            string hash = requestData.ToHash();
            LlmInsightsDocument aiSummaryDoc = await llmInsightsRepository.GetAsync(hash, cancellationToken);
            if (aiSummaryDoc is not null)
                return llmInsightsService.ToDto(aiSummaryDoc, userContext.UserId);

            return await llmInsightsService.GetLlmInsightsAsync(requestData, cancellationToken);
        }

        public async Task UpdateFeedbackUserCommentAsync(string id, string userComment, CancellationToken cancellationToken)
        {
            await llmInsightsService.UpdateFeedbackUserCommentAsync(id, userComment, cancellationToken);
        }

        public async Task UpdateFeedbackUserUsefulnessAsync(string id, bool? isUseful, CancellationToken cancellationToken)
        {
            await llmInsightsService.UpdateFeedbackUserUsefulnessAsync(id, isUseful, cancellationToken);
        }

        public async Task UpdateUserFeedbackSegmentCorrectnessAsync(string id, int segmentId, bool? isCorrect, CancellationToken cancellationToken)
        {
            await llmInsightsService.UpdateUserFeedbackSegmentCorrectnessAsync(id, segmentId, isCorrect, cancellationToken);
        }

        public LlmInsightResults ToDto(LlmInsightsDocument llmInsightsDocument, string userId)
        {
            return llmInsightsService.ToDto(llmInsightsDocument, userId);
        }

    }
}

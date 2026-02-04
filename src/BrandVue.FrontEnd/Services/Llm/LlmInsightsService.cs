using System.Threading;
using BrandVue.EntityFramework.Exceptions;
using BrandVue.Models;
using BrandVue.SourceData.LlmInsights;

namespace BrandVue.Services.Llm;

public class LlmInsightsService : ILlmInsightsService
{
    private readonly ILlmInsightsGeneratorService _llmInsightsGeneratorService;
    private readonly ILlmInsightsRepository _llmInsightsRepository;
    private readonly IUserContext _userContext;

    public LlmInsightsService(
        ILlmInsightsGeneratorService llmInsightsGeneratorService,
        ILlmInsightsRepository llmInsightsRepository,
        IUserContext userContext)
    {
        _llmInsightsGeneratorService = llmInsightsGeneratorService;
        _llmInsightsRepository = llmInsightsRepository;
        _userContext = userContext;
    }

    public async Task<LlmInsightResults> GetLlmInsightsAsync(
        IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation> requestData, 
        CancellationToken cancellationToken)
    {
        var aiResults = await _llmInsightsGeneratorService.GetLlmInsightsFromResults(requestData);

        var aiSummary = aiResults?.Select((s, index) =>
            new LlmInsightsSegment(index + 1, s.Title, s.Insight, s.Significance, s.RelatedHeadlines?.Select(h =>
                new LlmInsightsRelatedHeadline(h.Headline, h.Date, h.Source)).ToArray())).ToArray();

        LlmInsightsDocument aiSummaryDoc = new LlmInsightsDocument(requestData.Request, requestData.AverageData, requestData.ToHash(), aiSummary);

        await _llmInsightsRepository.UpsertAsync(aiSummaryDoc, cancellationToken);

        return ToDto(aiSummaryDoc, _userContext.UserId);
    }
    
    public async Task UpdateFeedbackUserCommentAsync(string llmInsightsId, string userComment, CancellationToken cancellationToken)
    {
        LlmInsightsDocument aiSummaryDoc = await _llmInsightsRepository.GetAsync(llmInsightsId, cancellationToken);
        if (aiSummaryDoc is null)
            throw new NotFoundException($"Could not find AI Summary with id:{llmInsightsId}");

        aiSummaryDoc.UpdateFeedbackUserComment(_userContext.UserId, userComment);
        await _llmInsightsRepository.UpsertAsync(aiSummaryDoc, cancellationToken);
    }

    public async Task UpdateFeedbackUserUsefulnessAsync(string llmInsightsId, bool? isUseful, CancellationToken cancellationToken)
    {
        LlmInsightsDocument aiSummaryDoc = await _llmInsightsRepository.GetAsync(llmInsightsId, cancellationToken);
        if (aiSummaryDoc is null)
            throw new NotFoundException($"Could not find AI Summary with id:{llmInsightsId}");

        aiSummaryDoc.UpdateFeedbackUserUsefulness(_userContext.UserId, isUseful);
        await _llmInsightsRepository.UpsertAsync(aiSummaryDoc, cancellationToken);
    }

    public async Task UpdateUserFeedbackSegmentCorrectnessAsync(string llmInsightsId, int segmentId, bool? isCorrect, CancellationToken cancellationToken)
    {
        LlmInsightsDocument aiSummaryDoc = await _llmInsightsRepository.GetAsync(llmInsightsId, cancellationToken);
        if (aiSummaryDoc is null)
            throw new NotFoundException($"Could not find AI Summary with id:{llmInsightsId}");

        aiSummaryDoc.UpdateUserFeedbackSegmentCorrectness(_userContext.UserId, segmentId, isCorrect);
        await _llmInsightsRepository.UpsertAsync(aiSummaryDoc, cancellationToken);
    }
    
    public LlmInsightResults ToDto(LlmInsightsDocument llmInsightsDocument, string userId)
    {
        var userFeedback = llmInsightsDocument.GetUserFeedback(userId);

        return new LlmInsightResults(
            llmInsightsDocument.Id,
            llmInsightsDocument.AiSummary?.Select(s => new LlmInsight(
                s.SegmentId,
                s.Title,
                s.Insight,
                s.Significance,
                userFeedback?.SegmentCorrectness?.ContainsKey(s.SegmentId) == true ? userFeedback.SegmentCorrectness[s.SegmentId] : null,
                s.RelatedHeadlines?.Select(h => new LlmInsightRelatedHeadline(h.Headline, h.Date, h.Source)).ToList()
                )).ToArray(),
                userFeedback is null ? null : new LlmInsightUserFeedback(
                    userFeedback.CreatedDt,
                    userFeedback.UserComment,
                    userFeedback.IsUseful
                    )
            );
    }
}
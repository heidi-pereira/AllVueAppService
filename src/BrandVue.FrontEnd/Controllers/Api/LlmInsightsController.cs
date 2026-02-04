using Microsoft.AspNetCore.Mvc;
using BrandVue.Models;
using BrandVue.Services.Llm;
using System.Threading;
using System.ComponentModel.DataAnnotations;
using BrandVue.AuthMiddleware.FeatureToggle;
using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace BrandVue.Controllers.Api;

[SubProductRoutePrefix("api/llminsights")]
[FeatureToggle(FeatureCode.llm_insights)]
public class LlmInsightsController : ApiController
{
    private readonly ILlmInsightsService _llmInsightsService;
    private readonly IUserContext _userContext;

    public LlmInsightsController(ILlmInsightsService llmInsightsService, IUserContext userContext)
    {
      
        _llmInsightsService = llmInsightsService;
        _userContext = userContext;
    }
    
    private async Task<ActionResult<LlmInsightResults>> HandleInsightRequest<T>(T requestData, CancellationToken cancellationToken) where T : IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation>
    {
        var results = await _llmInsightsService.GetLlmInsightsAsync(requestData, cancellationToken);
        return Ok(results);
    }

    [HttpPost]
    [Route("insights/" + nameof(OverTimeRequestData))]
    public Task<ActionResult<LlmInsightResults>> OverTimeRequestInsight(
        [FromBody]OverTimeRequestData requestData, CancellationToken cancellationToken)
        => HandleInsightRequest(requestData, cancellationToken);
    
    [HttpPost]
    [Route("insights/" + nameof(RankingRequestData))]
    public Task<ActionResult<LlmInsightResults>> RankingRequestInsight(
        [FromBody] RankingRequestData request, CancellationToken cancellationToken)
        => HandleInsightRequest(request, cancellationToken);
    
    [HttpPost]
    [Route("insights/" + nameof(ProfileRequestData))]
    public Task<ActionResult<LlmInsightResults>> ProfileRequestInsight(
        [FromBody] ProfileRequestData request, CancellationToken cancellationToken)
        => HandleInsightRequest(request, cancellationToken);

    
    [HttpPost]
    [Route("insights/" + nameof(ScorecardPerformanceRequestData))]
    public Task<ActionResult<LlmInsightResults>> ScorecardPerformanceRequestInsight(
        [FromBody]ScorecardPerformanceRequestData request, CancellationToken cancellationToken)
        => HandleInsightRequest(request, cancellationToken);
    
    [HttpPost]
    [Route("insights/" + nameof(CompetitionRequestData))]
    public Task<ActionResult<LlmInsightResults>> CompetitionRequestInsight(
        [FromBody]CompetitionRequestData request, CancellationToken cancellationToken)
        => HandleInsightRequest(request, cancellationToken);
    
    [HttpPost]
    [Route("insights/" + nameof(FunnelRequestData))]
    public Task<ActionResult<LlmInsightResults>> FunnelRequestInsight(
        [FromBody]FunnelRequestData request, CancellationToken cancellationToken)
        => HandleInsightRequest(request, cancellationToken);
    

    [HttpPost]
    [Route("weightedDailyResultsInsight/{id}/Feedback/UserComment")]
    public async Task<ActionResult> LlmInsightsFeedbackUserComment([FromRoute] string id, [FromBody] LlmInsightsFeedbackUserCommentRequest request, CancellationToken cancellationToken)
    {
        await _llmInsightsService.UpdateFeedbackUserCommentAsync(id, request.UserComment, cancellationToken);
        return Ok();
    }

    [HttpPost]
    [Route("weightedDailyResultsInsight/{id}/Feedback/Usefulness")]
    public async Task<ActionResult> LlmInsightSegmentFeedbackUsefulness([FromRoute] string id, [FromBody] LlmInsightSegmentFeedbackUsefulnessRequest request, CancellationToken cancellationToken)
    {
        await _llmInsightsService.UpdateFeedbackUserUsefulnessAsync(id, request.IsUseful, cancellationToken);
        return Ok();
    }

    [HttpPost]
    [Route("weightedDailyResultsInsight/{id}/Feedback/SegmentCorrectness")]
    public async Task<ActionResult> LlmInsightFeedbackSegmentCorrectness([FromRoute] string id, [FromBody] LlmInsightFeedbackSegmentCorrectnessRequest request, CancellationToken cancellationToken)
    {
        await _llmInsightsService.UpdateUserFeedbackSegmentCorrectnessAsync(id, request.SegmentId, request.IsCorrect, cancellationToken);
        return Ok();
    }
}

public record LlmInsightsFeedbackUserCommentRequest([StringLength(maximumLength:1024)] string? UserComment);

public record LlmInsightSegmentFeedbackUsefulnessRequest(bool? IsUseful);

public record LlmInsightFeedbackSegmentCorrectnessRequest([Range(0,int.MaxValue)] int SegmentId, bool? IsCorrect);
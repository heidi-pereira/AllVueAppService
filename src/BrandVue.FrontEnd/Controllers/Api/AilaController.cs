
using BrandVue.Services;
using BrandVue.Services.Llm;
using BrandVue.Services.Llm.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
namespace BrandVue.Controllers.Api;

[SubProductRoutePrefix("api/aila")]
public partial class AilaController : ApiController
{
    private readonly IAilaApiClient _apiClient;
    private readonly IChatCompletionService _chatCompletionService;
    private const string SYSTEM_MESSAGE_TEMPLATE = "Summarize the following survey responses. The user's browser locale is '{0}'. Each response is on a new line." +
                " Highlight the most common themes, sentiments, and any notable outliers." +
                " Do not mention the user's locale - this is just to influence your spelling and output language." +
                " Present the summary in a clear, concise format using bullet points where applicable." +
                " Do not start with 'Here is a summary...', just begin the summary right away." +
                " If any numerical data or trends are mentioned, include those in the summary.";

    public AilaController(IAilaApiClient apiClient, 
        IChatCompletionService chatCompletionService)
    {
        _apiClient = apiClient;
        _chatCompletionService = chatCompletionService;
    }

    [HttpPost]
    [Route("summarise")]
    public async Task<ActionResult<string>> Summarise([FromForm] SummariseRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Text))
        {
            return BadRequest("Request cannot be null or empty.");
        }
        string summary = string.Empty;

        if (request.UseGemini)
        {
            var response = await _chatCompletionService.GetSurveyResponseSummary(request.Text, request.BrowserLanguage, HttpContext.RequestAborted);
            summary = response.Content;
        }
        else
        {
            summary = await _apiClient.CreateChatCompletionAsync(
                userPrompt: request.Text,
                systemPrompt: string.Format(SYSTEM_MESSAGE_TEMPLATE, request.BrowserLanguage),
                cancellationToken: HttpContext.RequestAborted);
        }

        return Ok(summary);
    }
}

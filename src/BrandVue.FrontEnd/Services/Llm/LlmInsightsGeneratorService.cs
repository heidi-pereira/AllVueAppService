using BrandVue.Models;
using OpenAI.Chat;
using System.Threading;
using Newtonsoft.Json;
using NJsonSchema;
using Microsoft.Extensions.Options;


namespace BrandVue.Services.Llm
{
    public class LlmInsightsGeneratorService : ILlmInsightsGeneratorService
    {
        private readonly AzureAiClientSettings _clientSettings;
        private readonly IAzureChatCompletionService _chatCompletionService;
        private readonly IRequestAdapter _requestAdapter;
        
        public LlmInsightsGeneratorService(IOptions<AzureAiClientSettings> clientSettings,
            IAzureChatCompletionService chatCompletionService, IRequestAdapter requestAdapter)
        {
            _clientSettings = clientSettings.Value;
            _chatCompletionService = chatCompletionService;
            _requestAdapter = requestAdapter;
        }


        public async Task<IEnumerable<LlmInsightResult>> GetLlmInsightsFromResults(IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation> requestData)
        {
            var requestParameters = requestData.GetResultsProviderParameters(_requestAdapter);
            var metric = requestParameters.PrimaryMeasure;
            var isPercentage = metric.NumberFormat?.Contains("%") ?? true;
            var entityInstances = requestParameters.EntityInstances.ToDictionary(x => x.Id);
            var entityType = requestParameters.PrimaryMeasure.EntityCombination.FirstOrDefault();
            var userPrompt = UserPromptCsv(requestData, requestParameters);
            var systemPrompt =
                "You are an expert market researcher. " +
                "Focus on more recent data. Avoid lists. Do not make vague general statements. " +
                "Try and make factual statements and do not make guesses or speculation like \"potentially due to a campaign\". " +
                "Ignore changes that are close to the margin of error, though do mention consistently high or low performers. Do not mention margin of error," +
                "Evaluate how interesting each of these insights might be on a scale to 10" +
                (requestParameters.FocusEntityInstanceId != null && entityType != null
                    ? $", with more interest in {entityInstances[requestParameters.FocusEntityInstanceId.Value].Name}. "
                    : ". ") +
                "Try and reference known events that might impact the data. DO NOT invent headlines. " +
                requestData.ChartSpecificSystemPromptInfo() +
                "Avoid stating numbers and data as the user can already see the values on a chart. If numbers are to be used, format them in a human readable way. " +
                (isPercentage
                    ? "Format percentages as whole numbers, not decimals. "
                    : "") +
                (entityType != null
                    ? $"There may be nothing interesting or multiple notable points for a single {entityType.DisplayNameSingular}"
                    : "") +
                "Give me up to 5 interesting insights from the data, formatted using the function tool. ";
            var insightFunction = CreateInsightFunctionTool();
            var chatCompletionsAsync = await _chatCompletionService.GetChatCompletionAsync(
            [
                ChatMessage.CreateSystemMessage(systemPrompt),
                ChatMessage.CreateUserMessage(userPrompt)
            ], new()
            {
                Tools = { insightFunction },
                Temperature = _clientSettings.Temperature
            }, CancellationToken.None);

            return chatCompletionsAsync?.ToolCalls
                .Where(x => x.Kind == ChatToolCallKind.Function && x.FunctionName == insightFunction.FunctionName)
                .Select(x => JsonConvert.DeserializeObject<LlmInsightResult>(x.FunctionArguments)) ?? [];
        }
        
        private string UserPromptCsv(IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation> requestData, ResultsProviderParameters rpp)
        {
            return requestData.FormatDataPrompt(rpp);
        }
        
        private ChatTool CreateInsightFunctionTool()
        {
            return ChatTool.CreateFunctionTool("GetInsight", "Gets marketing insights",
                BinaryData.FromString(JsonSchema.FromType<LlmInsightResult>().ToJson()));
        }
    }
}
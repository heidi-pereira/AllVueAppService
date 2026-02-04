using System.Threading;
using BrandVue.SourceData.Measures;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NJsonSchema;
using OpenAI.Chat;
using static BrandVue.Services.Llm.Discovery.PromptHelper;

namespace BrandVue.Services.Llm.Discovery
{
    public class LlmDiscoveryService : ILlmDiscoveryService
    {
        private readonly AzureAiClientSettings _clientSettings;
        private readonly IAzureChatCompletionService _chatCompletionService;
        private readonly IMetadataStructureProvider _metadataStructureProvider;
        private readonly ApproachVersion _approachVersion = ApproachVersion.TwoSequentialPrompts;


        public LlmDiscoveryService(
            [NotNull] IOptions<AzureAiClientSettings> clientSettings,
            [NotNull] IAzureChatCompletionService chatCompletionService,
            [NotNull] IMetadataStructureProvider metadataStructureProvider)
        {
            if (clientSettings == null) throw new ArgumentNullException(nameof(clientSettings));
            _clientSettings = clientSettings.Value;
            _chatCompletionService =
                chatCompletionService ?? throw new ArgumentNullException(nameof(chatCompletionService));
            _metadataStructureProvider = metadataStructureProvider ??
                                         throw new ArgumentNullException(nameof(metadataStructureProvider));
        }

        public async Task<IEnumerable<T>> GetNavigationSuggestions<T>(
            string userRequest,
            string subsetId,
            IOutputAdapter<T> outputAdapter,
            CancellationToken cancellationToken)
        {

            switch (_approachVersion)
            {
                case ApproachVersion.SingleLargePrompt:
                    return outputAdapter.CreateFromFunctionCalls(await GetFunctionCalls_SingleLargePrompt(userRequest, subsetId, cancellationToken));
                case ApproachVersion.TwoSequentialPrompts:
                    return outputAdapter.CreateFromFunctionCalls(await GetFunctionCalls_TwoSequentialPrompts(userRequest, subsetId, cancellationToken));
                case ApproachVersion.IterativePrompts:
                    return outputAdapter.CreateFromFunctionCalls(await GetFunctionCalls_IterativePrompts(userRequest, subsetId, cancellationToken));
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task<IEnumerable<IDiscoveryFunctionToolInvocation>> GetFunctionCalls_SingleLargePrompt(
            string userRequest,
            string subsetId,
            CancellationToken cancellationToken)
        {
            var measures = _metadataStructureProvider.GetMeasures(subsetId);
            var pages = _metadataStructureProvider.GetPages(subsetId);
            var sysPrompt = PromptHelper.Prompt_SingleLargePrompt(measures, pages);

            List<ChatMessage> messages =
            [
                ChatMessage.CreateSystemMessage(sysPrompt),
                ChatMessage.CreateUserMessage(userRequest)
            ];

            ChatCompletionOptions options = new()
            {
                Tools =
                {
                    GetNavigationOptionsTool
                },
                Temperature = _clientSettings.Temperature
            };

            var completion = await _chatCompletionService.GetChatCompletionAsync(messages, options, cancellationToken);

            var navigationSuggestion = completion?.ToolCalls
                .Where(x => x.Kind == ChatToolCallKind.Function &&
                            x.FunctionName == GetNavigationOptionsTool.FunctionName)
                .Select(x => JsonConvert.DeserializeObject<NavigationOptionFunction>(x.FunctionArguments)) ?? [];

            return navigationSuggestion;
        }

        private async Task<IEnumerable<IDiscoveryFunctionToolInvocation>> GetFunctionCalls_IterativePrompts(
            string userRequest,
            string subsetId,
            CancellationToken cancellationToken)
        {
            var result = new List<NavigationOptionFunction>();
            var measures = _metadataStructureProvider.GetMeasures(subsetId);

            List<ChatMessage> messages =
            [
                ChatMessage.CreateSystemMessage(Prompt_IterativePrompts_1(measures)),
                ChatMessage.CreateUserMessage(userRequest)
            ];

            ChatCompletionOptions options = new()
            {
                Tools =
                {
                    GetNavigationOptionsTool,
                    GetPagesFunctionTool
                },
                Temperature = _clientSettings.Temperature
            };

            bool requiresAction;
            int navIterations = 0;
            int pagesCalledCount = 0;

            do
            {
                requiresAction = false;

                var completion =
                    await _chatCompletionService.GetChatCompletionAsync(messages, options, cancellationToken);

                switch (completion.FinishReason)
                {
                    case ChatFinishReason.Stop:
                        {
                            messages.Add(new AssistantChatMessage(completion));
                            break;
                        }

                    case ChatFinishReason.ToolCalls:
                        {
                            messages.Add(new AssistantChatMessage(completion));

                            foreach (ChatToolCall toolCall in completion.ToolCalls)
                            {
                                switch (toolCall.FunctionName)
                                {
                                    case nameof(GetPages):
                                        {

                                            pagesCalledCount++;

                                            var metrics = JsonConvert
                                                .DeserializeObject<PagesByMetricsFunction>(toolCall.FunctionArguments)
                                                .metrics;

                                            var selectedMeasures = measures.Where(a => metrics.Contains(a.Name));
                                            var toolResult = GetPages(subsetId, metrics);
                                            messages =
                                            [
                                                ChatMessage.CreateSystemMessage(PromptHelper.Prompt_IterativePrompts_2(selectedMeasures,toolResult)),
                                                ChatMessage.CreateUserMessage(userRequest)
                                            ];

                                            break;
                                        }
                                    case nameof(NavigationOptionFunction):
                                        {
                                            messages.Add(new ToolChatMessage(toolCall.Id, toolCall.FunctionArguments));
                                            result.Add(JsonConvert.DeserializeObject<NavigationOptionFunction>(toolCall.FunctionArguments));
                                            navIterations++;
                                            break;
                                        }
                                    default:
                                        {
                                            throw new NotImplementedException();
                                        }
                                }
                            }
                            if (navIterations < 5 && pagesCalledCount <= 1)
                                requiresAction = true;
                            break;
                        }

                    case ChatFinishReason.Length:
                        throw new NotImplementedException("Incomplete model output due to MaxTokens parameter or token limit exceeded.");

                    case ChatFinishReason.ContentFilter:
                        throw new NotImplementedException("Omitted content due to a content filter flag.");

                    case ChatFinishReason.FunctionCall:
                        throw new NotImplementedException("Deprecated in favor of tool calls.");

                    default:
                        throw new NotImplementedException(completion.FinishReason.ToString());
                }
            } while (requiresAction);

            return result;
        }

        private async Task<IEnumerable<IDiscoveryFunctionToolInvocation>> GetFunctionCalls_TwoSequentialPrompts(
            string userRequest,
            string subsetId,
            CancellationToken cancellationToken)
        {
            var result = new List<NavigationOptionFunction>();

            var measures = _metadataStructureProvider.GetMeasures(subsetId);

            List<ChatMessage> messages =
            [
                ChatMessage.CreateSystemMessage(Prompt_TwoSequentialPrompts_1(measures)),
                ChatMessage.CreateUserMessage(userRequest)
            ];

            ChatCompletionOptions options = new()
            {
                Tools =
                {
                    GetPagesFunctionTool
                },
                Temperature = _clientSettings.Temperature
            };

            // Step 1 - get LLM to generate list of metrics
            var completion1 = await _chatCompletionService.GetChatCompletionAsync(messages, options, cancellationToken);

            IEnumerable<Measure> suggestedMeasures = null;
            IEnumerable<PageDescriptorAndReferencedMetrics> pages = null;
            MeasuresNames measureNames = null;

            if (completion1.FinishReason == ChatFinishReason.ToolCalls)
            {
                var toolCallArgs = completion1?.ToolCalls
                    .FirstOrDefault(x => x.Kind == ChatToolCallKind.Function && x.FunctionName == GetPagesFunctionTool.FunctionName);

                if (toolCallArgs is not null)
                {
                    measureNames = JsonConvert.DeserializeObject<MeasuresNames>(toolCallArgs.FunctionArguments);
                    suggestedMeasures = measures.Where(measure => measureNames.metrics.Contains(measure.Name)).ToList();
                }
            }

            pages = _metadataStructureProvider.GetPagesAndReferencedMetrics(subsetId, measureNames?.metrics);
            messages =
            [
                ChatMessage.CreateSystemMessage(Prompt_TwoSequentialPrompts_2(suggestedMeasures,pages)),
                ChatMessage.CreateUserMessage(userRequest)
            ];

            options = new()
            {
                Tools =
                {
                    GetNavigationOptionsTool
                },
                Temperature = _clientSettings.Temperature
            };

            var completion2 = await _chatCompletionService.GetChatCompletionAsync(messages, options, cancellationToken);

            var navigationSuggestion = completion2?.ToolCalls
                .Where(x => x.Kind == ChatToolCallKind.Function &&
                            x.FunctionName == GetNavigationOptionsTool.FunctionName)
                .Select(x => JsonConvert.DeserializeObject<NavigationOptionFunction>(x.FunctionArguments)) ?? [];

            return navigationSuggestion;
        }

        private static readonly ChatTool GetNavigationOptionsTool =
            ChatTool.CreateFunctionTool(
                nameof(NavigationOptionFunction),
                "Generates BrandVue navigation suggestions",
                BinaryData.FromString(JsonSchema.FromType<NavigationOptionFunction>().ToJson()));

        private IEnumerable<PageDescriptorAndReferencedMetrics> GetPages(string subsetId, string[] metricNames)
        {
            return _metadataStructureProvider.GetPagesAndReferencedMetrics(subsetId, metricNames);
        }

        private static readonly ChatTool GetPagesFunctionTool = ChatTool.CreateFunctionTool(
            functionName: nameof(GetPages),
            functionDescription: "Get a list of pages & descriptions",
            functionParameters: BinaryData.FromString(JsonSchema.FromType<PagesByMetricsFunction>().ToJson())
        );

    }
}

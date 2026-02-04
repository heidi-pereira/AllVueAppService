using System.Threading;

namespace BrandVue.Services.Llm.Interfaces;

public interface IChatCompletionService
{
    Task<ChatCompletionMessage> GetChatCompletionAsync(IEnumerable<ChatCompletionMessage> messages, 
        CancellationToken cancellationToken);

    Task<ChatCompletionMessage> GetSurveyResponseSummary(string context, string locale, CancellationToken cancellationToken);
}
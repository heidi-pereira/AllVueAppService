using System.Threading;
using OpenAI;
using OpenAI.Chat;

namespace BrandVue.Services.Llm
{
    public interface IAzureChatCompletionService
    {
        Task<ChatCompletion> GetChatCompletionAsync(IEnumerable<ChatMessage> messages, ChatCompletionOptions options, CancellationToken cancellationToken);
    }
}
using System.Threading;

namespace BrandVue.Services
{
    public interface IAilaApiClient
    {   
        /// <summary>
        /// Create a chat completion.
        /// </summary>
        /// <param name="userPrompt">The prompt for the LLM to process.</param>
        /// <param name="systemPrompt">A special prompt to the LLM on how to process the user message, e.g. to constrain style, formatting etc.</param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ArgumentNullException">User prompt cannot be empty or null</exception>
        /// <exception cref="ArgumentException">User prompt cannot be empty</exception>
        /// <exception cref="AilaApiException">Network errors and problems callling the API.</exception>
        Task<string> CreateChatCompletionAsync(string userPrompt, string systemPrompt, CancellationToken cancellationToken);
    }
}

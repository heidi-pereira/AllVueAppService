using System.Threading;

namespace BrandVue.Services.Llm.Discovery
{
    public interface ILlmDiscoveryService
    {
        Task<IEnumerable<T>> GetNavigationSuggestions<T>(
            string userRequest, 
            string subsetId,
            IOutputAdapter<T> outputAdapter, 
            CancellationToken cancellationToken);
    }

    public interface IDiscoveryFunctionToolInvocation
    {
        
    }
}

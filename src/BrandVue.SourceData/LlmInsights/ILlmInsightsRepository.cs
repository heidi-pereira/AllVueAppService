using System.Threading;
using System.Threading.Tasks;

namespace BrandVue.SourceData.LlmInsights
{
    public interface ILlmInsightsRepository
    {
        public Task<LlmInsightsDocument> GetAsync(string id, CancellationToken cancellationToken);
        public Task UpsertAsync(LlmInsightsDocument llmInsightsDocument, CancellationToken cancellationToken);

    } 
}

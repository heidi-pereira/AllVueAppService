using BrandVue.Models;

namespace BrandVue.Services.Llm
{
    public interface ILlmInsightsGeneratorService
    {
        Task<IEnumerable<LlmInsightResult>> GetLlmInsightsFromResults(
            IInsightRequestData<AbstractCommonResultsInformation, IEntityRequestModel, AbstractCommonResultsInformation> request);
    }
}

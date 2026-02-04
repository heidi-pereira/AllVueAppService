using System.Threading;
using Microsoft.AspNetCore.Mvc;
using BrandVue.Services.Llm.Discovery;
using BrandVue.AuthMiddleware.FeatureToggle;
using BrandVue.EntityFramework;
using BrandVue.EntityFramework.MetaData.FeatureToggle;

namespace BrandVue.Controllers.Api
{
    [SubProductRoutePrefix("api/llmdiscovery")]
    [FeatureToggle(FeatureCode.llm_discovery)]
    public class LlmDiscoveryController : ApiController
    {
        private readonly ILlmDiscoveryService _llmDiscoveryService;
        private readonly IOutputAdapter<AnnotatedQueryParams> _outputAdapter;

        public LlmDiscoveryController(
            ILlmDiscoveryService llmDiscoveryService, 
            IOutputAdapter<AnnotatedQueryParams> outputAdapter
            )
        {
            _llmDiscoveryService = llmDiscoveryService;
            _outputAdapter = outputAdapter;
        }

        [HttpPost]
        [Route("discover")]
        public async Task<IEnumerable<AnnotatedQueryParams>> Discover([FromBody] LlmDiscoveryRequest request, CancellationToken cancellationToken)
        {
            return await _llmDiscoveryService.GetNavigationSuggestions(request.UserRequest, request.SubsetId, _outputAdapter, cancellationToken);
        }


        public record LlmDiscoveryRequest(string UserRequest, string SubsetId) : ISubsetIdProvider;

    }
}

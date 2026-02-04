using System.Threading;
using System.Threading.Tasks;
using BrandVue;
using BrandVue.Services.Llm;
using BrandVue.Services.Llm.Discovery;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;

namespace Test.BrandVue.FrontEnd.Llm.Discovery
{
    [Explicit("Setting to explicit as these tests have been re-written")]
    public class LlmDiscoveryServiceTests
    {
        private readonly IMetadataStructureProvider _metadataStructureProvider;
        private readonly IOutputAdapter<AnnotatedQueryParams> _outputAdapter;
        private readonly LlmDiscoveryService _service;
        private readonly IAzureChatCompletionService _chatCompletionService;
        private readonly string _subsetId = "UK";

        public LlmDiscoveryServiceTests()
        {
            var settings = Options.Create(new AzureAiClientSettings() {
                MaxRetries = 0,
                Temperature = 0,
                DefaultTimeout = 30,
                Endpoint = "https://savanta-aiml-studio-eastus.openai.azure.com/",
                Key = "FOOBAR",
                Deployment = "Savanta-Aila-4o"});
            _metadataStructureProvider = Substitute.For<IMetadataStructureProvider>();
            _chatCompletionService = Substitute.For<IAzureChatCompletionService>();
            _outputAdapter = new NavLinkParameterAdapter();
            _service = new LlmDiscoveryService(settings, _chatCompletionService, _metadataStructureProvider);
        }
        
        [Test]
        public async Task BasicMetricSearch()
        {
            var result = await _service.GetNavigationSuggestions(
                "I want to know which vodka is the favoured among martini drinkers while mercury is in retrograde",
                _subsetId,
                _outputAdapter, CancellationToken.None);
            // basic request, mock AI response and return QueryParams
            //Assert.That(result is not null);
        }
        
        [Test]
        public async Task AdditionalDiscoveryRequestCanHappen() 
        {
            //We want the discovery service to eventually be able to return to the context and request pages/filters/entity sets etc.
        }
        
        [Test]
        public async Task HandlesAiFailure()
        {
            //Mock AI refusing request
        }
    }
}
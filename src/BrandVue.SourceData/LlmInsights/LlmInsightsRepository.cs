using Microsoft.Azure.Cosmos;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace BrandVue.SourceData.LlmInsights
{
    public class LlmInsightsRepository : ILlmInsightsRepository
    {
        private readonly ILogger<LlmInsightsRepository> _logger;
        private readonly Container _container;
        private const string Container_Name = "LlmSummaries";

        public LlmInsightsRepository(IOptions<LlmAzureCosmosDbSettings> llmAzureCosmosDbSettings, ILogger<LlmInsightsRepository> logger)
        {
            _logger = logger;
            try
            {

                CosmosClient cosmosClient = new CosmosClient(llmAzureCosmosDbSettings.Value.ConnString);

                InitAsync(cosmosClient, llmAzureCosmosDbSettings.Value.DatabaseId).GetAwaiter().GetResult();

                Database database = cosmosClient.GetDatabase(llmAzureCosmosDbSettings.Value.DatabaseId);
                _container = database.GetContainer(Container_Name);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        private async Task InitAsync(CosmosClient cosmosClient,string databaseId)
        {
            await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
            await cosmosClient.GetDatabase(databaseId).CreateContainerIfNotExistsAsync(Container_Name, "/id");
        }

        public async Task<LlmInsightsDocument> GetAsync(string id, CancellationToken cancellationToken)
        {
            if (_container is null)
                return null;

            try
            {
                return await _container.ReadItemAsync<LlmInsightsDocument>(id, new PartitionKey(id),
                    cancellationToken: cancellationToken);
            }
            catch (CosmosException cosmosException)
            {
                // This is a valid case, the document does not exist (we want to return null)
                if (cosmosException.StatusCode == HttpStatusCode.NotFound)
                    return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw new Exception(ex.ToString());
            }

            return null;
        }

        public async Task UpsertAsync(LlmInsightsDocument llmInsightsDocument, CancellationToken cancellationToken)
        {
            if (_container is null)
                return;
            try
            {
                await _container.UpsertItemAsync(llmInsightsDocument, new PartitionKey(llmInsightsDocument.Id), cancellationToken: cancellationToken);
            }
            catch (CosmosException cosmosException)
            {
                _logger.LogError(cosmosException.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                throw new Exception(ex.ToString());
            }
        }
    }
}

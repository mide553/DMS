using Elastic.Clients.Elasticsearch;
using PaperlessModels.DTOs;
using OcrWorker.Exceptions;

namespace OcrWorker.Services
{
    public interface ISearchIndexService
    {
        public Task CreateIndexIfNotExistsAsync(string indexName);
        public Task IndexAsync(IndexedDocument document);
    }

    public class ElasticSearchService : ISearchIndexService
    {
        private readonly ElasticsearchClient _client;
        private readonly ILogger<ElasticSearchService> _logger;
        private readonly string _indexName = "documents";

        public ElasticSearchService(IConfiguration config, ILogger<ElasticSearchService> logger)
        {
            string url = config["ELASTICSEARCH_URL"] ?? throw new MissingConfigurationItemException("ElasticSearch URL");

            var settings = new ElasticsearchClientSettings(
                new Uri(url)
            )
            .DefaultIndex(_indexName);

            _client = new ElasticsearchClient(settings);
            _logger = logger;
        }

        public async Task CreateIndexIfNotExistsAsync(string indexName)
        {
            var exists = await _client.Indices.ExistsAsync(indexName);

            if (!exists.Exists)
            {
                var createResponse = await _client.Indices.CreateAsync(indexName);

                if (!createResponse.IsValidResponse)
                {
                    throw new IndexCreationException();
                }

                _logger.LogInformation($"Index {indexName} created");
            }
        }

        public async Task IndexAsync(IndexedDocument document)
        {
            _logger.LogInformation($"Indexing document {document.DocumentId}");

            await CreateIndexIfNotExistsAsync(_indexName);

            var response = await _client.IndexAsync(document, i => i
                .Index(_indexName)
                .OpType(OpType.Index)
            );

            if (!response.IsValidResponse)
            {
                _logger.LogError($"Could not index document with id {document.DocumentId}");
                throw new DocumentIndexException();
            }

            _logger.LogInformation($"Document with id {document.DocumentId} indexed to {_indexName}");
        }
    }
}

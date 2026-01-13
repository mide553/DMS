using Elastic.Clients.Elasticsearch;
using PaperlessModels.DTOs;
using PaperlessREST.Exceptions;

namespace PaperlessREST.Services
{
    public interface ISearchIndexService
    {
        Task<List<IndexedDocument>> SearchAsync(string searchText, int userId);
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

        public async Task<List<IndexedDocument>> SearchAsync(string searchText, int userId)
        {
            var searchResponse = await _client.SearchAsync<IndexedDocument>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Filter(f => f
                            .Term(t => t
                                .Field(d => d.UserId)
                                .Value(userId)
                            )
                        )
                        .Must(m => m
                            .Match(mt => mt
                                .Field(d => d.Content)
                                .Query(searchText)
                            )
                        )
                    )
                )
                .From(0)    // Starting index
                .Size(10)   // Number of documents per page
            );

            if (!searchResponse.IsValidResponse)
            {
                _logger.LogError($"Error searching for documents: {searchResponse.DebugInformation}");
                throw new Exception("Elasticsearch query failed");  // TODO rename Exception
            }

            return searchResponse.Documents.ToList();
        }
    }
}

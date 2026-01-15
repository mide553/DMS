using OcrWorker.Exceptions;
using OcrWorker.Services;
using PaperlessModels.DTOs;
using System.Text;
using System.Text.Json;

namespace OcrWorker
{
    public interface IMessageQueueHandler
    {
        public Task HandleMessageAsync(byte[] messageBytes);
        public Task ProcessDocumentAsync(int id, string fileName, int userId);
        public Task PublishMessageAsync(int id, string text);
    }

    public class Worker : BackgroundService, IMessageQueueHandler
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IDocumentStorageService _documentStorage;
        private readonly IDocumentExtractorService _documentExtractor;
        private readonly ISearchIndexService _searchIndexService;
        private readonly ILogger<Worker> _logger;
        private readonly string _subscribeQueueName = "ocr_queue";
        private readonly string _publishQueueName = "genai_queue";

        public Worker(IMessageQueueService messageQueueService, IDocumentStorageService documentStorage, IDocumentExtractorService documentExtractor, ISearchIndexService searchIndexService, ILogger<Worker> logger)
        {
            _messageQueueService = messageQueueService;
            _documentStorage = documentStorage;
            _documentExtractor = documentExtractor;
            _searchIndexService = searchIndexService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Subscribe to RabbitMQ
            await _messageQueueService.SubscribeAsync(_subscribeQueueName, this);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task HandleMessageAsync(byte[] messageBytes)
        {
            try
            {
                // Get message
                var json = Encoding.UTF8.GetString(messageBytes);
                var message = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (!message.TryGetValue("id", out var idString) ||
                !int.TryParse(idString, out int id))
                {
                    throw new InvalidMessageException("id");
                }

                if (!message.TryGetValue("filename", out var fileName) ||
                    string.IsNullOrWhiteSpace(fileName))
                {
                    throw new InvalidMessageException("filename");
                }

                if (!message.TryGetValue("userId", out var userIdString) ||
                !int.TryParse(userIdString, out int userId))
                {
                    throw new InvalidMessageException("userId");
                }

                _logger.LogInformation($"Received OCR job for document {id}");

                await ProcessDocumentAsync(id, fileName, userId);
            }
            catch (InvalidMessageException ex)
            {
                _logger.LogError(ex, "Invalid message received - discarding");
            }
            catch (OcrWorkerProcessException ex)
            {
                _logger.LogError(ex, "Error processing message");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
            }
        }
        
        public async Task ProcessDocumentAsync(int id, string fileName, int userId)
        {
            var localPath = Path.Combine("/tmp", fileName);
            try
            {
                // Download file
                await _documentStorage.DownloadFileAsync(fileName, localPath);

                // Perform OCR
                string text = await ExtractTextAsync(localPath);

                // Send text to summarizer
                await PublishMessageAsync(id, text);

                // Index document
                IndexedDocument document = new IndexedDocument
                {
                    DocumentId = id,
                    UserId = userId,
                    Content = text
                };
                await _searchIndexService.IndexAsync(document);

                _logger.LogInformation($"Finished process on document {id}");
            }
            catch (Exception ex)
            {
                // Delete temp file after upload
                if (Path.Exists(localPath))
                    System.IO.File.Delete(localPath);

                throw new OcrWorkerProcessException(ex);
            }
        }

        public async Task<string> ExtractTextAsync(string localPath)
        {
            string fileName = Path.GetFileName(localPath);

            // Extract text from document
            string text = await _documentExtractor.ExtractDocument(localPath);

            _logger.LogInformation($"Finished process on document {fileName}");
            return text;
        }

        public async Task PublishMessageAsync(int id, string text)
        {
            var payload = new Dictionary<string, string>
            {
                { "id", id.ToString() },
                { "text", text }
            };

            await _messageQueueService.PublishAsync(_publishQueueName, payload);

            _logger.LogInformation($"Text in queue {_publishQueueName} ready to be summarized");
        }
    }
}

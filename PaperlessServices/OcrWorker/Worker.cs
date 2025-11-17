using OcrWorker.Exceptions;
using OcrWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OcrWorker
{
    public interface IWorker
    {
        public Task<string> ProcessDocumentAsync(string localPath);
    }

    public class Worker : BackgroundService, IWorker, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly IDocumentStorageService _documentStorage;
        private readonly IDocumentExtractorService _documentExtractor;
        private readonly ILogger<Worker> _logger;

        public Worker(IDocumentStorageService documentStorage, IDocumentExtractorService documentExtractor, IConfiguration config, ILogger<Worker> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"] ?? throw new MissingConfigurationItemException("RabbitMQ Host"),
                UserName = config["RABBITMQ_USER"] ?? throw new MissingConfigurationItemException("RabbitMQ User"),
                Password = config["RABBITMQ_PASSWORD"] ?? throw new MissingConfigurationItemException("RabbitMQ Password")
            };
            _connection = factory.CreateConnectionAsync("OcrWorker-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _documentStorage = documentStorage;
            _documentExtractor = documentExtractor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Declare Queue
            string queueName = "ocr_queue";
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Create Consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    // Get message
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
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

                    _logger.LogInformation($"Received OCR job for {fileName}");

                    var localPath = Path.Combine("/tmp", fileName);
                    try
                    {
                        // Download file
                        await _documentStorage.DownloadFileAsync(fileName, localPath);

                        // Perform OCR
                        string text = await ProcessDocumentAsync(localPath);

                        // Send text to summarizer
                        await PublishForSummarizer(id, text);
                    
                        // Acknowledge message (deletes from queue)
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        // Delete temp file after upload
                        if (Path.Exists(localPath)) 
                            System.IO.File.Delete(localPath);

                        throw new OcrWorkerProcessException(ex);
                    }
                }
                catch (InvalidMessageException ex)
                {
                    _logger.LogError(ex, "Invalid message received - discarding");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (OcrWorkerProcessException ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
            };

            // Consume message from Queue
            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task<string> ProcessDocumentAsync(string localPath)
        {
            string fileName = Path.GetFileName(localPath);
            
            // Extract text from document
            string text = _documentExtractor.ExtractDocument(localPath);

            _logger.LogInformation($"Finished process on document {fileName}");
            return text;
        }

        private async Task PublishForSummarizer(int id, string text)
        {
            // Declare Queue
            string summarizerQueueName = "genai_queue";
            await _channel.QueueDeclareAsync(
                summarizerQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Publish message
            var payload = new Dictionary<string, string>
            {
                { "id", id.ToString() },
                { "text", text }
            };

            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync<BasicProperties>(
                exchange: "",
                routingKey: summarizerQueueName,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body
            );
            _logger.LogInformation($"Text in queue {summarizerQueueName} ready to be summarized");
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Stopping OCR worker...");
            // Close channel
            if (_channel is not null)
                await _channel.CloseAsync();

            // Close connection
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}

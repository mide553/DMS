using OcrWorker.Services;
using OcrWorker.Exceptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OcrWorker
{
    public interface IWorker
    {
        public void ProcessDocument(string localPath);
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
                HostName = config["RABBITMQ_HOST"],
                UserName = config["RABBITMQ_USER"],
                Password = config["RABBITMQ_PASSWORD"]
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
                // Get message
                var fileName = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation($"Received OCR job for {fileName}");

                var localPath = Path.Combine("/tmp", fileName);
                try
                {
                    // Download file
                    await _documentStorage.DownloadFileAsync(fileName, localPath);

                    // Perform OCR
                    ProcessDocument(localPath);

                    // Acknowledge message (deletes file)
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document message");

                    // Delete temp file after upload
                    if (Path.Exists(localPath)) 
                        System.IO.File.Delete(localPath);

                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    throw new OcrWorkerProcessException(ex);
                }
            };

            // Consume message from Queue
            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer:
                consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public void ProcessDocument(string localPath)
        {
            string fileName = Path.GetFileName(localPath);
            
            // Extract text from document
            string text = _documentExtractor.ExtractDocument(localPath);
            
            _logger.LogInformation($"Finished process on document {fileName}");
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Stopping RabbitMQ worker...");
            // Close channel
            if (_channel is not null)
                await _channel.CloseAsync();

            // Close connection
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}
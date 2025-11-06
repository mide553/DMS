using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using OcrWorker.Exceptions;
using OcrWorker.Services;
using Tesseract;

namespace OcrWorker
{
    public class Worker : BackgroundService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly IDocumentStorageService _documentStorage;
        private readonly ILogger<Worker> _logger;

        public Worker(IDocumentStorageService documentStorage, ILogger<Worker> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            };
            _connection = factory.CreateConnectionAsync("OcrWorker-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _documentStorage = documentStorage;
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
                    await ProcessDocumentAsync(localPath);

                    // Acknowledge message (deletes file)
                    _logger.LogInformation($"Finished process on document {fileName}");
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                    throw new OcrWorkerProcessException(ex);
                }
                finally
                {
                    // Delete temp file after upload
                    System.IO.File.Delete(localPath);
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

        private Task ProcessDocumentAsync(string localPath)
        {
            // OCR Processing
            try
            {
                using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(localPath);
                using var page = engine.Process(img);
                var text = page.GetText();

                string fileName = Path.GetFileName(localPath);
                _logger.LogInformation($"OCR Output for {fileName}:\n{text}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert images to text");
                throw new Exception("Failed to convert images to text");
            }
            return Task.CompletedTask;
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
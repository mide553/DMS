using PaperlessModels.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using OcrWorker.Exceptions;

namespace OcrWorker
{
    public class Worker : BackgroundService, IAsyncDisposable
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConnection _connection;
        private readonly IChannel _channel;

        public Worker(IConfiguration config, ILogger<Worker> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"],
                UserName = config["RABBITMQ_USER"],
                Password = config["RABBITMQ_PASSWORD"]
            };
            _connection = factory.CreateConnectionAsync("OcrWorker-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
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
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                try
                {
                    // Convert message to document
                    var document = JsonSerializer.Deserialize<Document>(json);

                    if (document is not null)
                    {
                        _logger.LogInformation($"Processing document {document.Name}");
                        await ProcessDocumentAsync(document, stoppingToken);
                    }

                    // Acknowledge message (deletes file)
                    _logger.LogInformation($"Finished process on document {document.Name}");
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Invalid JSON message: {json}");
                    throw new InvalidJsonMessageException(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing document message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
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

        private Task ProcessDocumentAsync(Document document, CancellationToken token)
        {
            // Simulated OCR work
            _logger.LogInformation($"Performing OCR on {document.Name}");
            return Task.Delay(2000, token);
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
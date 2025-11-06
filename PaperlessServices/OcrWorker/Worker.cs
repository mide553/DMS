using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using OcrWorker.Exceptions;

namespace OcrWorker
{
    public class Worker : BackgroundService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            };
            _connection = factory.CreateConnectionAsync("OcrWorker-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

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

        private Task ProcessDocumentAsync(string localPath, CancellationToken token)
        {
            // Simulated OCR work
            string fileName = Path.GetFileName(localPath);
            _logger.LogInformation($"Performing OCR on {fileName}");
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
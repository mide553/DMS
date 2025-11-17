using PaperlessREST.Exceptions;
using PaperlessModels.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace PaperlessREST
{
    public interface IWorker
    {
        public Task ProcessDocumentAsync(string fileName, string summary);
    }

    public class Worker : BackgroundService, IWorker, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        //private readonly IDocumentController _documentController;
        private readonly ILogger<Worker> _logger;

        //public Worker(IDocumentController documentController, IConfiguration config, ILogger<Worker> logger)
        public Worker(IConfiguration config, ILogger<Worker> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"] ?? throw new MissingConfigurationItemException("RabbitMQ Host"),
                UserName = config["RABBITMQ_USER"] ?? throw new MissingConfigurationItemException("RabbitMQ User"),
                Password = config["RABBITMQ_PASSWORD"] ?? throw new MissingConfigurationItemException("RabbitMQ Password")
            };

            _connection = factory.CreateConnectionAsync("PaperlessREST-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            //_documentController = documentController;

            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Declare Queue
            string queueName = "result_queue";
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
                // TODO json machen mit filename und summary?
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var message = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                int id = Int32.Parse(message["id"]);
                string summary = message["summary"];

                //if (String.IsNullOrEmpty(fileName) || String.IsNullOrEmpty(summary)
                //{
                //    // TODO
                //}

        // {"id":"1","summary":"The document states that it is a sample text."}

                // TODO
                _logger.LogInformation($"Received summary for id {id}:\n{summary}");

                
                DocumentDto documentDto = new DocumentDto { Summary = summary };

                //Controllers.DocumentController controller = new Controllers.DocumentController();

                //_documentController.UpdateDocument(id, documentDto);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);

                //_logger.LogInformation($"Received summary for {fileName}");

                //try
                //{
                //    // Add summary to document
                //    await ProcessDocumentAsync(fileName, summary);

                //    // Acknowledge message (deletes from queue)
                //    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                //}
                //catch (Exception ex)
                //{
                //    _logger.LogError(ex, "Error saving summary");

                //    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                //    throw new OcrWorkerProcessException(ex);
                //}
            };

            // Consume message from Queue
            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task ProcessDocumentAsync(string fileName, string summary)
        {
            
            _logger.LogInformation($"Saved summary to document {fileName}");
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Stopping PaperlessREST worker...");
            // Close channel
            if (_channel is not null)
                await _channel.CloseAsync();

            // Close connection
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}

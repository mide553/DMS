using RabbitMQ.Client;
using PaperlessModels.Models;
using System.Text;
using System.Text.Json;
using PaperlessREST.Exceptions;

namespace PaperlessREST.Services
{
    public interface IMessageQueueService
    {
        Task PublishAsync(string queue, Document document);
    }

    public class RabbitMQService : IMessageQueueService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger _logger;

        public RabbitMQService(IConfiguration config, ILogger<RabbitMQService> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"],
                UserName = config["RABBITMQ_USER"],
                Password = config["RABBITMQ_PASSWORD"]
            };

            _connection = factory.CreateConnectionAsync("PaperlessREST-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _logger = logger;
        }

        public async Task PublishAsync(string queue, Document document)
        {
            // Declare Queue
            await _channel.QueueDeclareAsync(
                queue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Convert message to JSON
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document));
            try
            {
                var json = JsonSerializer.Serialize(document);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to convert {document.Name} to json");
                throw new FailedConvertException(typeof(Document).ToString(), typeof(JsonSerializer).ToString(), ex);
            }

            // Publish message
            _logger.LogInformation($"Document {document.Name} in queue {queue} ready to be processed");
            await _channel.BasicPublishAsync<BasicProperties>(
                exchange: "",
                routingKey: queue,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body
            );
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Stopping RabbitMQ Publisher...");
            // Close channel
            if (_channel is not null)
                await _channel.CloseAsync();

            // Close connection
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}

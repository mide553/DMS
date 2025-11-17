using PaperlessREST.Exceptions;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace PaperlessREST.Services
{
    public interface IMessageQueueService
    {
        Task PublishAsync(int id, string message);
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
                HostName = config["RABBITMQ_HOST"] ?? throw new MissingConfigurationItemException("RabbitMQ Host"),
                UserName = config["RABBITMQ_USER"] ?? throw new MissingConfigurationItemException("RabbitMQ User"),
                Password = config["RABBITMQ_PASSWORD"] ?? throw new MissingConfigurationItemException("RabbitMQ Password")
            };

            _connection = factory.CreateConnectionAsync("PaperlessREST-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _logger = logger;
        }

        public async Task PublishAsync(int id, string fileName)
        {
            string queueName = "ocr_queue";

            // Declare Queue
            await _channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Publish message
            var payload = new Dictionary<string, string>
            {
                { "id", id.ToString() },
                { "filename", fileName }

            };

            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync<BasicProperties>(
                exchange: "",
                routingKey: queueName,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body
            );

            _logger.LogInformation($"Message in queue ({queueName}) ready to be processed");
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

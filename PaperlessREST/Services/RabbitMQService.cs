using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaperlessREST.Exceptions;

namespace PaperlessREST.Services
{
    public interface IMessageQueueService
    {
        Task PublishAsync(string queueName, Dictionary<string, string> payload);
        Task SubscribeAsync(string queueName, IMessageQueueHandler handler);
    }

    public class RabbitMQService : IMessageQueueService, IAsyncDisposable
    {
        private IConfiguration _config;
        private IConnection _connection;
        private IChannel _channel;
        private readonly ILogger<RabbitMQService> _logger;

        public RabbitMQService(IConfiguration config, ILogger<RabbitMQService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task EnsureInitializedAsync()
        {
            if (_channel != null) return;

            var factory = new ConnectionFactory
            {
                HostName = _config["RABBITMQ_HOST"] ?? throw new MissingConfigurationItemException("RabbitMQ Host"),
                UserName = _config["RABBITMQ_USER"] ?? throw new MissingConfigurationItemException("RabbitMQ User"),
                Password = _config["RABBITMQ_PASSWORD"] ?? throw new MissingConfigurationItemException("RabbitMQ Password")
            };

            _connection = await factory.CreateConnectionAsync("PaperlessREST-Connection");
            _channel = await _connection.CreateChannelAsync();

            _logger.LogInformation("RabbitMQ initialized.");
        }

        public async Task PublishAsync(string queueName, Dictionary<string, string> payload)
        {
            await EnsureInitializedAsync();

            // Declare Queue
            await _channel.QueueDeclareAsync(
                queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Publish message
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

        public async Task SubscribeAsync(string queueName, IMessageQueueHandler handler)
        {
            await EnsureInitializedAsync();

            // Declare Queue
            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Create Consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (sender, ea) =>
            {
                try
                {
                    await handler.HandleMessageAsync(ea.Body.ToArray());

                    // Acknowledge message (deletes from queue)
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                    throw new MessageHandlingException(ex);
                }
            };

            _logger.LogInformation($"Worker ({handler.ToString()}) subscribed to queue ({queueName})");

            // Consume message from Queue
            await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: consumer
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

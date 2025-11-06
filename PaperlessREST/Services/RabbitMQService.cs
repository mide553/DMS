using RabbitMQ.Client;
using System.Text;

namespace PaperlessREST.Services
{
    public interface IMessageQueueService
    {
        Task PublishAsync(string queue, string message);
    }

    public class RabbitMQService : IMessageQueueService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ILogger _logger;

        public RabbitMQService(ILogger<RabbitMQService> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER"),
                Password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
            };

            _connection = factory.CreateConnectionAsync("PaperlessREST-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _logger = logger;
        }

        public async Task PublishAsync(string queue, string message)
        {
            // Declare Queue
            await _channel.QueueDeclareAsync(
                queue,
                durable: true,
                exclusive: false,
                autoDelete: false
            );
            
            // Publish message
            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync<BasicProperties>(
                exchange: "",
                routingKey: queue,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body
            );

            _logger.LogInformation($"{message} in queue {queue} ready to be processed");
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

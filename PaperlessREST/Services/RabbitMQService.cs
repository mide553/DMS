using RabbitMQ.Client;
using PaperlessREST.Model;
using System.Text;
using System.Text.Json;

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

        public RabbitMQService(IConfiguration config)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"],
                UserName = config["RABBITMQ_USER"],
                Password = config["RABBITMQ_PASSWORD"]
            };

            _connection = factory.CreateConnectionAsync("PaperlessREST-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
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
            var json = JsonSerializer.Serialize(document);

            // Publish message
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
            // Close channel
            if (_channel is not null)
                await _channel.CloseAsync();

            // Close connection
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}

using GenAIWorker.Services;
using GenAIWorker.Exceptions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace GenAIWorker
{
    public interface IWorker
    {
        public Task ProcessText(string text);
    }
    public class Worker : BackgroundService, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly ISummarizer _summarizer;
        private readonly ILogger<Worker> _logger;

        public Worker(ISummarizer summarizer, IConfiguration config, ILogger<Worker> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"] ?? throw new MissingConfigurationItemException("RabbitMQ Host"),
                UserName = config["RABBITMQ_USER"] ?? throw new MissingConfigurationItemException("RabbitMQ User"),
                Password = config["RABBITMQ_PASSWORD"] ?? throw new MissingConfigurationItemException("RabbitMQ Password")
            };
            _connection = factory.CreateConnectionAsync("OcrWorker-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _summarizer = summarizer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Declare Queue
            string queueName = "genai_queue";
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
                var text = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation($"Received summarizing job");
                
                try
                {
                    // Process text
                    await ProcessText(text);
                }
                catch (Exception ex)
                {
                    throw;
                }
                
                // Acknowledge message (deletes from queue)
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
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

        public async Task ProcessText(string text)
        {
            // Summarize text
            string summary = await _summarizer.SummarizeTextAsync(text);

            // Send summary to api
            await PublishForApiAsync(summary);
            _logger.LogInformation($"Finished process on text");
        }

        private async Task PublishForApiAsync(string message)
        {
            // Declare Queue
            string resultQueueName = "result_queue";
            await _channel.QueueDeclareAsync(
                resultQueueName,
                durable: true,
                exclusive: false,
                autoDelete: false
            );

            // Publish message
            var body = Encoding.UTF8.GetBytes(message);
            await _channel.BasicPublishAsync<BasicProperties>(
                exchange: "",
                routingKey: resultQueueName,
                mandatory: false,
                basicProperties: new BasicProperties(),
                body: body
            );
            _logger.LogInformation($"Summary in queue ready to be saved");
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogInformation("Stopping GenAI worker...");
            // Close channel
            if (_channel is not null)
                await _channel.CloseAsync();

            // Close connection
            if (_connection is not null)
                await _connection.CloseAsync();
        }
    }
}

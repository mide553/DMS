using GenAIWorker.Exceptions;
using GenAIWorker.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

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
                try
                {
                    // Get message
                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var message = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (!message.TryGetValue("id", out var idString) ||
                        !int.TryParse(idString, out int id))
                    {
                        throw new InvalidMessageException("id");
                    }

                    if (!message.TryGetValue("text", out var text) ||
                        string.IsNullOrWhiteSpace(text))
                    {
                        throw new InvalidMessageException("text");
                    }

                    _logger.LogInformation($"Received summarizing job for document {id}");

                    try
                    {
                        // Process text
                        string summary = await ProcessText(text);

                        // Send summary to api
                        await PublishForApiAsync(id, summary);
                    }
                    catch (Exception ex)
                    {
                        throw new GenaiWorkerProcessException(ex);
                    }

                    // Acknowledge message (deletes from queue)
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (InvalidMessageException ex)
                {
                    _logger.LogError(ex, "Invalid message received - discarding");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (GenaiWorkerProcessException ex)
                {
                    _logger.LogError(ex, "Error summarizing document text");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
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

        public async Task<string> ProcessText(string text)
        {
            // Summarize text
            string summary = await _summarizer.SummarizeTextAsync(text);
            _logger.LogInformation($"Finished process on text");

            return summary;
        }

        private async Task PublishForApiAsync(int id, string summary)
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
            var payload = new Dictionary<string, string>
            {
                { "id", id.ToString() },
                { "summary", summary }
            };

            var json = JsonSerializer.Serialize(payload);
            var body = Encoding.UTF8.GetBytes(json);

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

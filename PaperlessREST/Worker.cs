using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using PaperlessModels.DTOs;
using PaperlessREST.Services;
using PaperlessREST.Exceptions;

namespace PaperlessREST
{
    public interface IWorker
    {
        public Task ProcessDocumentAsync(int id, string summary);
    }

    public class Worker : BackgroundService, IWorker, IAsyncDisposable
    {
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;

        public Worker(IServiceProvider serviceProvider, IConfiguration config, ILogger<Worker> logger)
        {
            var factory = new ConnectionFactory
            {
                HostName = config["RABBITMQ_HOST"] ?? throw new MissingConfigurationItemException("RabbitMQ Host"),
                UserName = config["RABBITMQ_USER"] ?? throw new MissingConfigurationItemException("RabbitMQ User"),
                Password = config["RABBITMQ_PASSWORD"] ?? throw new MissingConfigurationItemException("RabbitMQ Password")
            };

            _connection = factory.CreateConnectionAsync("PaperlessREST-Connection").GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _serviceProvider = serviceProvider;

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

                    if (!message.TryGetValue("summary", out var summary) ||
                        string.IsNullOrWhiteSpace(summary))
                    {
                        throw new InvalidMessageException("summary");
                    }

                    _logger.LogInformation($"Received summary for document {id}");

                    try
                    {
                        // Save summary
                        await ProcessDocumentAsync(id, summary);
                
                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        throw new UpdateSummaryException(ex);
                    }
                }
                catch (InvalidMessageException ex)
                {
                    _logger.LogError(ex, "Invalid message received - discarding");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                }
                catch (UpdateSummaryException ex)
                {
                    _logger.LogError(ex, "Failed to update document summary");
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
                consumer: consumer
            );

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task ProcessDocumentAsync(int id, string summary)
        {
            try
            {
                // Create new DI scope for scoped service DocumentService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();

                    // Get document to update
                    DocumentDto currDoc = await documentService.GetDocumentByIdAsync(id);
                    
                    // Add summary to document
                    DocumentDto doc = new DocumentDto 
                    { 
                        FileName = currDoc.FileName,
                        ByteSize = currDoc.ByteSize,
                        LastModified = currDoc.LastModified,
                        Summary = summary 
                    };

                    await documentService.UpdateDocumentAsync(id, doc);

                    _logger.LogInformation($"Saved summary to document {id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker encountered an error");
            }
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

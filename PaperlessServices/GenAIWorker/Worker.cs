using GenAIWorker.Exceptions;
using GenAIWorker.Services;
using System.Text;
using System.Text.Json;

namespace GenAIWorker
{
    public interface IMessageQueueHandler
    {
        public Task HandleMessageAsync(byte[] messageBytes);
        public Task<string> ProcessTextAsync(string text);
        public Task PublishMessageAsync(int id, string summary);
    }
    public class Worker : BackgroundService, IMessageQueueHandler
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly ISummarizer _summarizer;
        private readonly ILogger<Worker> _logger;
        private readonly string _subscribeQueueName = "genai_queue";
        private readonly string _publishQueueName = "result_queue";

        public Worker(IMessageQueueService messageQueueService, ISummarizer summarizer, ILogger<Worker> logger)
        {
            _messageQueueService = messageQueueService;
            _summarizer = summarizer;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Subscribe to RabbitMQ
            await _messageQueueService.SubscribeAsync(_subscribeQueueName, this);

            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task HandleMessageAsync(byte[] messageBytes)
        {
            try
            {
                // Get message
                var json = Encoding.UTF8.GetString(messageBytes);
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

                // Process text
                string summary = await ProcessTextAsync(text);

                // Send summary to api
                await PublishMessageAsync(id, summary);
            }
            catch (InvalidMessageException ex)
            {
                _logger.LogError(ex, "Invalid message received - discarding");
            }
            catch (GenaiWorkerProcessException ex)
            {
                _logger.LogError(ex, "Error summarizing document text");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
            }
        }

        public async Task<string> ProcessTextAsync(string text)
        {
            try
            {
                // Summarize text
                string summary = await _summarizer.SummarizeTextAsync(text);
                _logger.LogInformation($"Finished process on text");

                return summary;
            }
            catch (Exception ex)
            {
                throw new GenaiWorkerProcessException(ex);
            }
        }

        public async Task PublishMessageAsync(int id, string summary)
        {
            var payload = new Dictionary<string, string>
            {
                { "id", id.ToString() },
                { "summary", summary }
            };

            await _messageQueueService.PublishAsync(_publishQueueName, payload);
            
            _logger.LogInformation($"Summary in queue {_publishQueueName} ready to be saved");
        }
    }
}

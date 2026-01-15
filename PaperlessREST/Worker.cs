using PaperlessREST.Data;
using PaperlessREST.Exceptions;
using PaperlessREST.Services;
using PaperlessModels.DTOs;
using System.Text;
using System.Text.Json;

namespace PaperlessREST
{
    public interface IMessageQueueHandler
    {
        public Task HandleMessageAsync(byte[] messageBytes);
        public Task ProcessDocumentAsync(int id, string summary);
    }

    public class Worker : BackgroundService, IMessageQueueHandler
    {
        private readonly IMessageQueueService _messageQueueService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;
        private readonly string _queueName = "result_queue";

        public Worker(IMessageQueueService messageQueueService, IServiceProvider serviceProvider, ILogger<Worker> logger)
        {
            _messageQueueService = messageQueueService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Subscribe to RabbitMQ
            await _messageQueueService.SubscribeAsync(_queueName, this);

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

                if (!message.TryGetValue("summary", out var summary) || 
                    string.IsNullOrWhiteSpace(summary))
                {
                    throw new InvalidMessageException("summary");
                }

                _logger.LogInformation($"Received summary for document {id}");

                // Save summary
                await ProcessDocumentAsync(id, summary);
            }
            catch (InvalidMessageException ex)
            {
                _logger.LogError(ex, "Invalid message received - discarding");
            }
            catch (UpdateSummaryException ex)
            {
                _logger.LogError(ex, "Failed to update document summary");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error");
            }
        }

        public async Task ProcessDocumentAsync(int id, string summary)
        {
            try
            {
                // Create new DI scope for scoped service DocumentService
                using (var scope = _serviceProvider.CreateScope())
                {
                    var documentService = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDBContext>();

                    // Get document from database directly to get userId
                    var document = await context.Documents.FindAsync(id);

                    if (document == null)
                    {
                        _logger.LogWarning($"Document with ID {id} not found");
                        return;
                    }

                    // Get document to update
                    DocumentDto currDoc = await documentService.GetDocumentByIdAsync(id, document.UserId);

                    // Add summary to document
                    DocumentDto doc = new DocumentDto
                    {
                        FileName = currDoc.FileName,
                        ByteSize = currDoc.ByteSize,
                        LastModified = currDoc.LastModified,
                        UserId = currDoc.UserId,
                        Summary = summary
                    };

                    await documentService.UpdateDocumentAsync(id, doc, document.UserId);
                    _logger.LogInformation($"Saved summary to document {id}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker encountered an error");
                throw new UpdateSummaryException(ex);
            }
        }
    }
}

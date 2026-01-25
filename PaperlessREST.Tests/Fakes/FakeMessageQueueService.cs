using PaperlessREST.Services;

namespace PaperlessREST.Tests.Fakes
{
    public class FakeMessageQueueService : IMessageQueueService
    {
        public List<Dictionary<string, string>> PublishedMessages { get; } = new();

        public Task PublishAsync(string queueName, Dictionary<string, string> payload)
        {
            PublishedMessages.Add(payload);
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(string queueName, IMessageQueueHandler handler)
            => Task.CompletedTask;
    }
}

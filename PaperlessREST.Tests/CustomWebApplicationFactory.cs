using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PaperlessREST.Tests.Fakes;
using PaperlessREST.Tests.Helpers;
using PaperlessREST.Services;

namespace PaperlessREST.Tests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public FakeDocumentStorageService FakeDocumentStorage { get; } = new();
        public FakeMessageQueueService FakeMessageQueue { get; } = new();
        public FakeSearchIndexService FakeSearchIndex { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Remove real implementations
                services.RemoveAll<IDocumentStorageService>();
                services.RemoveAll<IMessageQueueService>();
                services.RemoveAll<ISearchIndexService>();

                // Register mocks
                services.AddSingleton<IDocumentStorageService>(FakeDocumentStorage);
                services.AddSingleton<IMessageQueueService>(FakeMessageQueue);
                services.AddSingleton<ISearchIndexService>(FakeSearchIndex);

                // Replace Authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test", options => { });
            });
        }
    }
}

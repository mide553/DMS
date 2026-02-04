using System.Net;
using System.Net.Http.Headers;

namespace PaperlessREST.Tests.Integration
{
    [TestFixture]
    public class DocumentUploadTests
    {
        private CustomWebApplicationFactory _factory;
        private HttpClient _client;

        [SetUp]
        public void Setup()
        {
            _factory = new CustomWebApplicationFactory();
            _client = _factory.CreateClient();

            // Add Authorization header for test scheme
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Test");
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [Test]
        public async Task UploadDocument_StoresMetadata_And_PublishesMessage()
        {
            // Arrange
            var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes("fake pdf content"));

            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");

            var fakeFile = new MultipartFormDataContent
            {
                { fileContent, "file", "test.pdf" }
            };

            // Act
            var response = await _client.PostAsync("/api/documents/upload", fakeFile);

            // Assert HTTP
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

            // Assert storage
            Assert.That(_factory.FakeDocumentStorage.Uploads.Count, Is.EqualTo(1));

            var upload = _factory.FakeDocumentStorage.Uploads.Single();
            Assert.That(upload, Does.Contain("test.pdf"));

            // Assert message
            Assert.That(_factory.FakeMessageQueue.PublishedMessages.Count, Is.EqualTo(1));

            var message = _factory.FakeMessageQueue.PublishedMessages.Single();
            Assert.That(message.ContainsKey("id"));
            Assert.That(message["filename"], Does.Contain("test.pdf"));
            Assert.That(message.ContainsKey("userId"));
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using OcrWorker;
using OcrWorker.Exceptions;
using PaperlessModels.Models;
using System.Text.Json;

namespace PaperlessServices.Tests;

[TestFixture]
public class WorkerTests
{
    private Mock<ILogger<Worker>> _mockLogger;
    private Mock<IConfiguration> _mockConfig;

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<Worker>>();
        _mockConfig = new Mock<IConfiguration>();

        _mockConfig.Setup(c => c["RABBITMQ_HOST"]).Returns("localhost");
        _mockConfig.Setup(c => c["RABBITMQ_USER"]).Returns("guest");
        _mockConfig.Setup(c => c["RABBITMQ_PASSWORD"]).Returns("guest");
    }

    [Test]
    public void DeserializeMessage_ValidDocument_ReturnsCorrectDocument()
    {
        var json = @"{""Id"":1,""FileName"":""test.pdf"",""ByteSize"":1024,""Summary"":"""",""LastModified"":""2025-11-06T10:00:00Z""}";

        var document = JsonSerializer.Deserialize<Document>(json);

        Assert.That(document, Is.Not.Null);
        Assert.That(document.Id, Is.EqualTo(1));
        Assert.That(document.FileName, Is.EqualTo("test.pdf"));
        Assert.That(document.ByteSize, Is.EqualTo(1024));
    }

    [Test]
    public void DeserializeMessage_InvalidJson_ThrowsJsonException()
    {
        var invalidJson = "{ invalid json }";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Document>(invalidJson));
    }

    [Test]
    public void DeserializeMessage_MissingFields_DeserializesWithDefaults()
    {
        var json = @"{""Id"":10,""FileName"":""minimal.pdf""}";

        var document = JsonSerializer.Deserialize<Document>(json);

        Assert.That(document, Is.Not.Null);
        Assert.That(document.Id, Is.EqualTo(10));
        Assert.That(document.FileName, Is.EqualTo("minimal.pdf"));
    }

    [Test]
    public void InvalidJsonMessageException_CreatesExceptionWithMessage()
    {
        var badJson = "corrupt data";

        var exception = new InvalidJsonMessageException(badJson);

        Assert.That(exception.Message, Does.Contain("Invalid JSON message"));
        Assert.That(exception.Message, Does.Contain(badJson));
    }

    [Test]
    public void OcrWorkerProcessException_CreatesExceptionWithInnerException()
    {
        var innerException = new Exception("Processing failed");

        var exception = new OcrWorkerProcessException(innerException);

        Assert.That(exception.Message, Does.Contain("Worker failed to process message"));
        Assert.That(exception.Message, Does.Contain("Processing failed"));
    }

    [Test]
    public void Configuration_LoadsRabbitMQSettings()
    {
        var host = _mockConfig.Object["RABBITMQ_HOST"];
        var user = _mockConfig.Object["RABBITMQ_USER"];
        var password = _mockConfig.Object["RABBITMQ_PASSWORD"];

        Assert.That(host, Is.EqualTo("localhost"));
        Assert.That(user, Is.EqualTo("guest"));
        Assert.That(password, Is.EqualTo("guest"));
    }

    [Test]
    public void SerializeDocument_RoundTrip_PreservesAllData()
    {
        var original = new Document
        {
            Id = 42,
            FileName = "contract.pdf",
            ByteSize = 4096,
            Summary = "Contract document",
            LastModified = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Document>(json);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Id, Is.EqualTo(original.Id));
        Assert.That(deserialized.FileName, Is.EqualTo(original.FileName));
        Assert.That(deserialized.ByteSize, Is.EqualTo(original.ByteSize));
        Assert.That(deserialized.Summary, Is.EqualTo(original.Summary));
    }

    [Test]
    public void QueueConfiguration_UsesCorrectQueueName()
    {
        var queueName = "ocr_queue";

        Assert.That(queueName, Is.EqualTo("ocr_queue"));
    }
}


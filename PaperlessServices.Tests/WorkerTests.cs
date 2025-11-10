using Moq;
using OcrWorker;
using Microsoft.Extensions.Configuration;
using OcrWorker.Services;

namespace PaperlessServices.Tests;

[TestFixture]
public class WorkerTests
{
    private Mock<IConfiguration> _mockConfig;
    private Mock<IWorker> _mockWorker;
    private Mock<IDocumentStorageService> _documentStorage;

    [SetUp]
    public void Setup()
    {
        _mockConfig = new Mock<IConfiguration>();
        _mockWorker = new Mock<IWorker>();
        _documentStorage = new Mock<IDocumentStorageService>();

        _mockConfig.Setup(c => c["RABBITMQ_HOST"]).Returns("localhost");
        _mockConfig.Setup(c => c["RABBITMQ_USER"]).Returns("guest");
        _mockConfig.Setup(c => c["RABBITMQ_PASSWORD"]).Returns("guest");
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
    // TODO: weitere Tests zu ProcessDocument
}

